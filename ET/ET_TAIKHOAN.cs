// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng TAI_KHOAN qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("TAI_KHOAN")]
    public class ET_TAIKHOAN
    {
        private string maTK = string.Empty;
        private string tenDangNhap = string.Empty;
        private string matKhauHash = string.Empty;
        private string maVaiTro = string.Empty;
        private string maNV = string.Empty;
        private string? maKhoa;
        private string? maPhong;
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_TAIKHOAN() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_TAIKHOAN(string maTK, string tenDangNhap, string matKhauHash, string maVaiTro, string maNV, string? maKhoa, string? maPhong, string trangThai)
        {
            this.maTK = maTK;
            this.tenDangNhap = tenDangNhap;
            this.matKhauHash = matKhauHash;
            this.maVaiTro = maVaiTro;
            this.maNV = maNV;
            this.maKhoa = maKhoa;
            this.maPhong = maPhong;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã tài khoản; ánh xạ cột MaTK của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaTK { get => maTK; set => maTK = value; }
        /// <summary>
        /// Tên đăng nhập tài khoản; ánh xạ cột TenDangNhap của bảng trong TableAttribute.
        /// </summary>
        public string TenDangNhap { get => tenDangNhap; set => tenDangNhap = value; }
        /// <summary>
        /// Giá trị băm mật khẩu trong thực thể tài khoản; không phải chuỗi mật khẩu rõ; ánh xạ cột MatKhauHash của bảng trong TableAttribute.
        /// </summary>
        public string MatKhauHash { get => matKhauHash; set => matKhauHash = value; }
        /// <summary>
        /// Mã vai trò; ánh xạ cột MaVaiTro của bảng trong TableAttribute.
        /// </summary>
        public string MaVaiTro { get => maVaiTro; set => maVaiTro = value; }
        /// <summary>
        /// Mã nhân viên; ánh xạ cột MaNV của bảng trong TableAttribute.
        /// </summary>
        public string MaNV { get => maNV; set => maNV = value; }
        /// <summary>
        /// Mã khoa bệnh viện; ánh xạ cột MaKhoa của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaKhoa { get => maKhoa; set => maKhoa = value; }
        /// <summary>
        /// Mã phòng; ánh xạ cột MaPhong của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaPhong { get => maPhong; set => maPhong = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}