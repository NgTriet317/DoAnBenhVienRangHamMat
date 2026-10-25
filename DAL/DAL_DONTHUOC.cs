// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho đơn thuốc trong bảng DON_THUOC. Lưu đầu đơn theo lần khám và bác sĩ; giới hạn sửa nội dung theo trạng thái trước khi cấp thuốc. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_DONTHUOC : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý đơn thuốc, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_DONTHUOC(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ đơn thuốc từ dbo.DON_THUOC, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_DONTHUOC> layToanBoDonThuoc()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_DONTHUOC>("SELECT * FROM dbo.[DON_THUOC] ORDER BY [MaDT]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm đơn thuốc theo khóa trong dbo.DON_THUOC; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_DONTHUOC? layDonThuocTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_DONTHUOC>("SELECT * FROM dbo.[DON_THUOC] WHERE [MaDT]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm đơn thuốc: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.DON_THUOC trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themDonThuoc(ET_DONTHUOC duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaDT = TaoMa(connection, transaction, "DON_THUOC", "MaDT");
                KiemTraNghiepVu(duLieu, null);
                KiemTra<ET_DONTHUOC>(connection, transaction, "DON_THUOC", duLieu, null, null);
                connection.Execute("""
                    INSERT INTO dbo.[DON_THUOC] ([MaDT],[MaLS],[MaBS],
                        [NgayKe],[LyDoThayThe],[TrangThai],
                        [GhiChu],[MaDTThayThe])
                    VALUES (@MaDT,@MaLS,@MaBS,
                        @NgayKe,@LyDoThayThe,@TrangThai,
                        @GhiChu,@MaDTThayThe)
                    """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa đơn thuốc: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaDonThuoc(ET_DONTHUOC duLieu, ET_DONTHUOC? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_DONTHUOC>("SELECT * FROM dbo.[DON_THUOC] WITH(UPDLOCK,HOLDLOCK) WHERE [MaDT]=@Ma", new { Ma = duLieu.MaDT }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(duLieu, hienTai, chiDoiTrangThai);
                KiemTra(connection, transaction, "DON_THUOC", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("UPDATE dbo.[DON_THUOC] SET [MaLS]=@MaLS,[MaBS]=@MaBS,[NgayKe]=@NgayKe,[LyDoThayThe]=@LyDoThayThe,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu,[MaDTThayThe]=@MaDTThayThe WHERE [MaDT]=@MaDT", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa đơn thuốc: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.DON_THUOC; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaDonThuoc(string ma, ET_DONTHUOC? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_DONTHUOC>("SELECT * FROM dbo.[DON_THUOC] WITH(UPDLOCK,HOLDLOCK) WHERE [MaDT]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(hienTai, hienTai);
                KiemTra(connection, transaction, "DON_THUOC", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[DON_THUOC] WHERE [MaDT]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Phân biệt các trạng thái nháp với bản đã xác nhận/hủy; chặn sửa nội dung ngoài nháp, chặn quay về Nháp và giữ nguyên lần khám của đơn đã lưu.
        /// </summary>
        /// <param name = "duLieu">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <param name = "banHienTai">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        /// <param name = "chiDoiTrangThai">true khi nơi gọi chỉ đổi trạng thái; cờ ảnh hưởng việc chặn sửa nội dung ngoài trạng thái nháp, các giới hạn khác vẫn áp dụng.</param>
        private void KiemTraNghiepVu(ET_DONTHUOC duLieu, ET_DONTHUOC? banHienTai, bool chiDoiTrangThai = false)
        {
            if (banHienTai is not null)
            {
                var trangThaiCu = banHienTai.TrangThai;
                var laBanNhap = trangThaiCu is null or "" or "Nháp" or "Chưa xác nhận" or "Chờ xác nhận" or "Đang lập" or "Dự kiến" or "Chờ thực hiện";
                if (!laBanNhap && (!chiDoiTrangThai))
                {
                    throw new InvalidOperationException("Bản ghi đã xác nhận hoặc đã hủy; hãy lập bản mới.");
                }

                if (!laBanNhap && duLieu.TrangThai == "Nháp")
                {
                    throw new InvalidOperationException("Không chuyển chứng từ đã xác nhận về nháp.");
                }
            }

            if (banHienTai is not null && duLieu.MaLS != banHienTai.MaLS)
            {
                throw new InvalidOperationException("Không chuyển bản ghi đã lưu sang hồ sơ khác.");
            }
        }
    }
}
