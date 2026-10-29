// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng NHA_CUNG_CAP qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("NHA_CUNG_CAP")]
    public class ET_NHACUNGCAP
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaNCC, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maNCC = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TenNCC, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string tenNCC = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính SDT, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? sdt;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính Email, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string email = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính DiaChi, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? diaChi;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NguoiLienHe, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string nguoiLienHe = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TrangThai, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_NHACUNGCAP() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_NHACUNGCAP(string maNCC, string tenNCC, string? sdt, string email, string? diaChi, string nguoiLienHe, string trangThai)
        {
            this.maNCC = maNCC;
            this.tenNCC = tenNCC;
            this.sdt = sdt;
            this.email = email;
            this.diaChi = diaChi;
            this.nguoiLienHe = nguoiLienHe;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã nhà cung cấp; ánh xạ cột MaNCC của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaNCC { get => maNCC; set => maNCC = value; }
        /// <summary>
        /// Tên nhà cung cấp; ánh xạ cột TenNCC của bảng trong TableAttribute.
        /// </summary>
        public string TenNCC { get => tenNCC; set => tenNCC = value; }
        /// <summary>
        /// Số điện thoại dạng chuỗi để giữ số 0 đầu; ánh xạ cột SDT của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? SDT { get => sdt; set => sdt = value; }
        /// <summary>
        /// Địa chỉ email liên hệ; ánh xạ cột Email của bảng trong TableAttribute.
        /// </summary>
        public string Email { get => email; set => email = value; }
        /// <summary>
        /// Địa chỉ liên hệ; ánh xạ cột DiaChi của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? DiaChi { get => diaChi; set => diaChi = value; }
        /// <summary>
        /// Tên người liên hệ nhà cung cấp; ánh xạ cột NguoiLienHe của bảng trong TableAttribute.
        /// </summary>
        public string NguoiLienHe { get => nguoiLienHe; set => nguoiLienHe = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}