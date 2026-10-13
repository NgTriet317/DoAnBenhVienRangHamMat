// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho nhân viên trong bảng NHAN_VIEN. Lưu nhân thân, chức vụ, chuyên khoa và trạng thái làm việc để các giao dịch xác định người thực hiện. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_NHANVIEN : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý nhân viên, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_NHANVIEN(IDbTransaction? transaction = null) : base(transaction) { }

        /// <summary>
        /// Đọc toàn bộ nhân viên từ dbo.NHAN_VIEN, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_NHANVIEN> layToanBoNhanVien()
        {
            return ThucHien((connection, transaction) =>
                connection.Query<ET_NHANVIEN>("SELECT * FROM dbo.[NHAN_VIEN] ORDER BY [MaNV]", transaction: transaction).ToArray());
        }

        /// <summary>
        /// Tìm nhân viên theo khóa trong dbo.NHAN_VIEN; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_NHANVIEN? layNhanVienTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_NHANVIEN>("SELECT * FROM dbo.[NHAN_VIEN] WHERE [MaNV]=@Ma", new { Ma = ma }, transaction));
        }

        /// <summary>
        /// Thêm nhân viên: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.NHAN_VIEN trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themNhanVien(ET_NHANVIEN duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaNV = TaoMa(connection, transaction, "NHAN_VIEN", "MaNV");
                KiemTra<ET_NHANVIEN>(connection, transaction, "NHAN_VIEN", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[NHAN_VIEN] ([MaNV],[HoTen],[GioiTinh],[NgaySinh],[SDT],[DiaChi],[MaChucVu],[MaCK],[BangCap],[NgayVaoLam],[TrangThai]) VALUES (@MaNV,@HoTen,@GioiTinh,@NgaySinh,@SDT,@DiaChi,@MaChucVu,@MaCK,@BangCap,@NgayVaoLam,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa nhân viên: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name="chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaNhanVien(ET_NHANVIEN duLieu, ET_NHANVIEN? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_NHANVIEN>(
                    "SELECT * FROM dbo.[NHAN_VIEN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaNV]=@Ma", new { Ma = duLieu.MaNV }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTra(connection, transaction, "NHAN_VIEN", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                    foreach (var pair in GiaTri(duLieu))
                        if (pair.Key is not "TrangThai" and not "NgayXacNhan" and not "NgayCapNhat" and not "LyDoThayDoi"
                            && !Equals(pair.Value, GiaTri(hienTai).GetValueOrDefault(pair.Key)))
                            throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
                connection.Execute("UPDATE dbo.[NHAN_VIEN] SET [HoTen]=@HoTen,[GioiTinh]=@GioiTinh,[NgaySinh]=@NgaySinh,[SDT]=@SDT,[DiaChi]=@DiaChi,[MaChucVu]=@MaChucVu,[MaCK]=@MaCK,[BangCap]=@BangCap,[NgayVaoLam]=@NgayVaoLam,[TrangThai]=@TrangThai WHERE [MaNV]=@MaNV", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa nhân viên: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.NHAN_VIEN; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaNhanVien(string ma, ET_NHANVIEN? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_NHANVIEN>(
                    "SELECT * FROM dbo.[NHAN_VIEN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaNV]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTra(connection, transaction, "NHAN_VIEN", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[NHAN_VIEN] WHERE [MaNV]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
