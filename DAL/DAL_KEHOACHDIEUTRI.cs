// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho kế hoạch điều trị trong bảng KE_HOACH_DIEU_TRI. Lưu kế hoạch thuộc lần khám cùng bác sĩ và mốc điều trị; quản lý nội dung nháp và chuyển trạng thái. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_KEHOACHDIEUTRI : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý kế hoạch điều trị, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_KEHOACHDIEUTRI(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ kế hoạch điều trị từ dbo.KE_HOACH_DIEU_TRI, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_KEHOACHDIEUTRI> layToanBoKeHoachDieuTri()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_KEHOACHDIEUTRI>("SELECT * FROM dbo.[KE_HOACH_DIEU_TRI] ORDER BY [MaKHDT]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm kế hoạch điều trị theo khóa trong dbo.KE_HOACH_DIEU_TRI; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_KEHOACHDIEUTRI? layKeHoachDieuTriTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_KEHOACHDIEUTRI>("SELECT * FROM dbo.[KE_HOACH_DIEU_TRI] WHERE [MaKHDT]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm kế hoạch điều trị: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.KE_HOACH_DIEU_TRI trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themKeHoachDieuTri(ET_KEHOACHDIEUTRI duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaKHDT = TaoMa(connection, transaction, "KE_HOACH_DIEU_TRI", "MaKHDT");
                KiemTraNghiepVu(duLieu, null);
                KiemTra<ET_KEHOACHDIEUTRI>(connection, transaction, "KE_HOACH_DIEU_TRI", duLieu, null, null);
                connection.Execute("""
                    INSERT INTO dbo.[KE_HOACH_DIEU_TRI] ([MaKHDT],[MaLS],[MaBS],
                        [NgayLap],[NgayBatDau],[NgayDuKienKetThuc],
                        [TrangThai],[GhiChu])
                    VALUES (@MaKHDT,@MaLS,
                        @MaBS,@NgayLap,@NgayBatDau,
                        @NgayDuKienKetThuc,@TrangThai,@GhiChu)
                    """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa kế hoạch điều trị: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaKeHoachDieuTri(ET_KEHOACHDIEUTRI duLieu, ET_KEHOACHDIEUTRI? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_KEHOACHDIEUTRI>("SELECT * FROM dbo.[KE_HOACH_DIEU_TRI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaKHDT]=@Ma", new { Ma = duLieu.MaKHDT }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(duLieu, hienTai, chiDoiTrangThai);
                KiemTra(connection, transaction, "KE_HOACH_DIEU_TRI", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("""
                    UPDATE dbo.[KE_HOACH_DIEU_TRI]
                    SET [MaLS]=@MaLS,[MaBS]=@MaBS,[NgayLap]=@NgayLap,
                        [NgayBatDau]=@NgayBatDau,[NgayDuKienKetThuc]=@NgayDuKienKetThuc,[TrangThai]=@TrangThai,
                        [GhiChu]=@GhiChu
                    WHERE [MaKHDT]=@MaKHDT
                    """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa kế hoạch điều trị: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.KE_HOACH_DIEU_TRI; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaKeHoachDieuTri(string ma, ET_KEHOACHDIEUTRI? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_KEHOACHDIEUTRI>("SELECT * FROM dbo.[KE_HOACH_DIEU_TRI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaKHDT]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(hienTai, hienTai);
                KiemTra(connection, transaction, "KE_HOACH_DIEU_TRI", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[KE_HOACH_DIEU_TRI] WHERE [MaKHDT]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Phân biệt các trạng thái nháp với bản đã xác nhận/hủy; chặn sửa nội dung ngoài nháp, chặn quay về Nháp và giữ nguyên lần khám của kế hoạch đã lưu.
        /// </summary>
        /// <param name = "duLieu">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <param name = "banHienTai">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        /// <param name = "chiDoiTrangThai">true khi nơi gọi chỉ đổi trạng thái; cờ ảnh hưởng việc chặn sửa nội dung ngoài trạng thái nháp, các giới hạn khác vẫn áp dụng.</param>
        private void KiemTraNghiepVu(ET_KEHOACHDIEUTRI duLieu, ET_KEHOACHDIEUTRI? banHienTai, bool chiDoiTrangThai = false)
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
