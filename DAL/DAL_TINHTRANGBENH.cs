// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho tình trạng bệnh trong bảng TINH_TRANG_BENH. Lưu mô tả và mức độ thuộc lần khám; chỉ sửa nội dung khi lần khám cha còn nháp. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_TINHTRANGBENH : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý tình trạng bệnh, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_TINHTRANGBENH(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ tình trạng bệnh từ dbo.TINH_TRANG_BENH, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_TINHTRANGBENH> layToanBoTinhTrangBenh()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_TINHTRANGBENH>("SELECT * FROM dbo.[TINH_TRANG_BENH] ORDER BY [MaTTB]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm tình trạng bệnh theo khóa trong dbo.TINH_TRANG_BENH; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_TINHTRANGBENH? layTinhTrangBenhTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_TINHTRANGBENH>("SELECT * FROM dbo.[TINH_TRANG_BENH] WHERE [MaTTB]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm tình trạng bệnh: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.TINH_TRANG_BENH trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themTinhTrangBenh(ET_TINHTRANGBENH duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaTTB = TaoMa(connection, transaction, "TINH_TRANG_BENH", "MaTTB");
                KiemTraNghiepVu(transaction, duLieu, null);
                KiemTra<ET_TINHTRANGBENH>(connection, transaction, "TINH_TRANG_BENH", duLieu, null, null);
                connection.Execute("""
                    INSERT INTO dbo.[TINH_TRANG_BENH] ([MaTTB],[MaLS],[LoaiTinhTrang],
                        [MoTaTinhTrang],[MucDo],[GhiChu])
                    VALUES (@MaTTB,
                        @MaLS,@LoaiTinhTrang,@MoTaTinhTrang,
                        @MucDo,@GhiChu)
                    """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa tình trạng bệnh: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaTinhTrangBenh(ET_TINHTRANGBENH duLieu, ET_TINHTRANGBENH? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_TINHTRANGBENH>("SELECT * FROM dbo.[TINH_TRANG_BENH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaTTB]=@Ma", new { Ma = duLieu.MaTTB }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(transaction, duLieu, hienTai);
                KiemTra(connection, transaction, "TINH_TRANG_BENH", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("""
                    UPDATE dbo.[TINH_TRANG_BENH]
                    SET [MaLS]=@MaLS,[LoaiTinhTrang]=@LoaiTinhTrang,[MoTaTinhTrang]=@MoTaTinhTrang,
                        [MucDo]=@MucDo,[GhiChu]=@GhiChu
                    WHERE [MaTTB]=@MaTTB
                    """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa tình trạng bệnh: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.TINH_TRANG_BENH; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaTinhTrangBenh(string ma, ET_TINHTRANGBENH? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_TINHTRANGBENH>("SELECT * FROM dbo.[TINH_TRANG_BENH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaTTB]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(transaction, hienTai, hienTai);
                KiemTra(connection, transaction, "TINH_TRANG_BENH", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[TINH_TRANG_BENH] WHERE [MaTTB]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Yêu cầu lần khám cha tồn tại và có trạng thái Nháp; cấm chuyển tình trạng đã lưu sang lần khám khác.
        /// </summary>
        /// <param name = "giaoDich">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
        /// <param name = "duLieu">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <param name = "banHienTai">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        private void KiemTraNghiepVu(IDbTransaction? giaoDich, ET_TINHTRANGBENH duLieu, ET_TINHTRANGBENH? banHienTai)
        {
            var parent = new DAL_LICHSUKHAMBENH(giaoDich).layLichSuKhamBenhTheoMa(duLieu.MaLS);
            if (parent is null || parent.TrangThai != "Nháp")
            {
                throw new InvalidOperationException("Chỉ sửa nội dung trong bản nháp có liên kết hợp lệ.");
            }

            if (banHienTai is not null && duLieu.MaLS != banHienTai.MaLS)
            {
                throw new InvalidOperationException("Không chuyển bản ghi đã lưu sang hồ sơ khác.");
            }
        }
    }
}
