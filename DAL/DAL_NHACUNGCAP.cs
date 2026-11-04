// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho nhà cung cấp trong bảng NHA_CUNG_CAP. Lưu thông tin liên hệ và trạng thái đối tác để hóa đơn nhập, lô hàng và thiết bị tham chiếu. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_NHACUNGCAP : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý nhà cung cấp, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_NHACUNGCAP(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ nhà cung cấp từ dbo.NHA_CUNG_CAP, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_NHACUNGCAP> layToanBoNhaCungCap()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_NHACUNGCAP>("SELECT * FROM dbo.[NHA_CUNG_CAP] ORDER BY [MaNCC]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm nhà cung cấp theo khóa trong dbo.NHA_CUNG_CAP; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_NHACUNGCAP? layNhaCungCapTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_NHACUNGCAP>("SELECT * FROM dbo.[NHA_CUNG_CAP] WHERE [MaNCC]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm nhà cung cấp: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.NHA_CUNG_CAP trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themNhaCungCap(ET_NHACUNGCAP duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaNCC = TaoMa(connection, transaction, "NHA_CUNG_CAP", "MaNCC");
                KiemTra<ET_NHACUNGCAP>(connection, transaction, "NHA_CUNG_CAP", duLieu, null, null);
                KiemTraNghiepVu(duLieu);
                connection.Execute("""
                INSERT INTO dbo.[NHA_CUNG_CAP] ([MaNCC],[TenNCC],[SDT],
                    [Email],[DiaChi],[NguoiLienHe],
                    [TrangThai])
                VALUES (@MaNCC,@TenNCC,@SDT,
                    @Email,@DiaChi,@NguoiLienHe,
                    @TrangThai)
                """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa nhà cung cấp: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaNhaCungCap(ET_NHACUNGCAP duLieu, ET_NHACUNGCAP? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_NHACUNGCAP>("SELECT * FROM dbo.[NHA_CUNG_CAP] WITH(UPDLOCK,HOLDLOCK) WHERE [MaNCC]=@Ma", new { Ma = duLieu.MaNCC }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "NHA_CUNG_CAP", duLieu, hienTai, banGoc);
                KiemTraNghiepVu(duLieu);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("""
                UPDATE dbo.[NHA_CUNG_CAP]
                SET [TenNCC]=@TenNCC,[SDT]=@SDT,[Email]=@Email,
                    [DiaChi]=@DiaChi,[NguoiLienHe]=@NguoiLienHe,[TrangThai]=@TrangThai
                WHERE [MaNCC]=@MaNCC
                """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa nhà cung cấp: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.NHA_CUNG_CAP; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaNhaCungCap(string ma, ET_NHACUNGCAP? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_NHACUNGCAP>("SELECT * FROM dbo.[NHA_CUNG_CAP] WITH(UPDLOCK,HOLDLOCK) WHERE [MaNCC]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "NHA_CUNG_CAP", hienTai, hienTai, banGoc);
                // Kiểm tra đủ cả ba bảng liên kết, kể cả dữ liệu chưa được giao diện tải lại.
                bool daSuDung = connection.ExecuteScalar<bool>("""
                    SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.THIET_BI WHERE MaNCC=@Ma)
                        OR EXISTS (SELECT 1 FROM dbo.LO_HANG_HOA WHERE MaNCC=@Ma)
                        OR EXISTS (SELECT 1 FROM dbo.HOA_DON_NHAP WHERE MaNCC=@Ma)
                        THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
                    """, new { Ma = ma }, transaction);
                if (daSuDung)
                    throw new InvalidOperationException("Nhà cung cấp đã có thiết bị, lô hàng hoặc hóa đơn nhập. Hãy chuyển sang Ngừng hoạt động để giữ lịch sử.");
                connection.Execute("DELETE FROM dbo.[NHA_CUNG_CAP] WHERE [MaNCC]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>Áp dụng cùng quy tắc cho thêm/sửa từ mọi nơi gọi BUS, sau khi chuẩn hóa theo schema.</summary>
        private static void KiemTraNghiepVu(ET_NHACUNGCAP duLieu)
        {
            if (string.IsNullOrWhiteSpace(duLieu.TenNCC) || string.IsNullOrWhiteSpace(duLieu.NguoiLienHe))
                throw new InvalidOperationException("Vui lòng nhập tên nhà cung cấp và người liên hệ.");
            if (!Regex.IsMatch(duLieu.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new InvalidOperationException("Email không đúng định dạng, ví dụ ten@example.com.");
            if (duLieu.SDT is not null)
            {
                int soChuSo = duLieu.SDT.Count(char.IsAsciiDigit);
                if (!Regex.IsMatch(duLieu.SDT, @"^\+?[0-9 ()\-.]+$") || soChuSo is < 8 or > 15)
                    throw new InvalidOperationException("Số điện thoại cần 8–15 chữ số; chỉ dùng số, dấu + ở đầu, khoảng trắng, dấu ngoặc, chấm hoặc gạch nối.");
            }
            if (duLieu.TrangThai is not "Đang hoạt động" and not "Ngừng hoạt động")
                throw new InvalidOperationException("Vui lòng chọn Đang hoạt động hoặc Ngừng hoạt động.");
        }
    }
}
