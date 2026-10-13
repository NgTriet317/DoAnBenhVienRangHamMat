// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho hồ sơ bệnh án trong bảng HO_SO_BENH_AN. Liên kết bệnh nhân với ngày lập và lý do khám ban đầu; không chuyển hồ sơ đã lưu sang bệnh nhân khác. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_HOSOBENHAN : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý hồ sơ bệnh án, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_HOSOBENHAN(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ hồ sơ bệnh án từ dbo.HO_SO_BENH_AN, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_HOSOBENHAN> layToanBoHoSoBenhAn()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_HOSOBENHAN>("SELECT * FROM dbo.[HO_SO_BENH_AN] ORDER BY [MaHS]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm hồ sơ bệnh án theo khóa trong dbo.HO_SO_BENH_AN; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_HOSOBENHAN? layHoSoBenhAnTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_HOSOBENHAN>("SELECT * FROM dbo.[HO_SO_BENH_AN] WHERE [MaHS]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm hồ sơ bệnh án: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.HO_SO_BENH_AN trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themHoSoBenhAn(ET_HOSOBENHAN duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaHS = TaoMa(connection, transaction, "HO_SO_BENH_AN", "MaHS");
                KiemTraNghiepVu(duLieu, null);
                KiemTra<ET_HOSOBENHAN>(connection, transaction, "HO_SO_BENH_AN", duLieu, null, null);
                connection.Execute("""
                    INSERT INTO dbo.[HO_SO_BENH_AN] ([MaHS],[MaBN],[NgayLap],
                        [LyDoKhamBanDau],[GhiChu],[TrangThai])
                    VALUES (@MaHS,
                        @MaBN,@NgayLap,@LyDoKhamBanDau,
                        @GhiChu,@TrangThai)
                    """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa hồ sơ bệnh án: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaHoSoBenhAn(ET_HOSOBENHAN duLieu, ET_HOSOBENHAN? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_HOSOBENHAN>("SELECT * FROM dbo.[HO_SO_BENH_AN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaHS]=@Ma", new { Ma = duLieu.MaHS }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(duLieu, hienTai);
                KiemTra(connection, transaction, "HO_SO_BENH_AN", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("UPDATE dbo.[HO_SO_BENH_AN] SET [MaBN]=@MaBN,[NgayLap]=@NgayLap,[LyDoKhamBanDau]=@LyDoKhamBanDau,[GhiChu]=@GhiChu,[TrangThai]=@TrangThai WHERE [MaHS]=@MaHS", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa hồ sơ bệnh án: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.HO_SO_BENH_AN; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaHoSoBenhAn(string ma, ET_HOSOBENHAN? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_HOSOBENHAN>("SELECT * FROM dbo.[HO_SO_BENH_AN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaHS]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(hienTai, hienTai);
                KiemTra(connection, transaction, "HO_SO_BENH_AN", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[HO_SO_BENH_AN] WHERE [MaHS]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// So mã bệnh nhân mới với bản hiện tại; nếu đã lưu thì cấm chuyển hồ sơ sang bệnh nhân khác. Không kiểm tra trạng thái tại hàm này.
        /// </summary>
        /// <param name = "duLieu">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <param name = "banHienTai">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        private void KiemTraNghiepVu(ET_HOSOBENHAN duLieu, ET_HOSOBENHAN? banHienTai)
        {
            if (banHienTai is not null && duLieu.MaBN != banHienTai.MaBN)
            {
                throw new InvalidOperationException("Không chuyển bản ghi đã lưu sang hồ sơ khác.");
            }
        }
    }
}
