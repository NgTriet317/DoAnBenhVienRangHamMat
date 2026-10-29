// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng KHOA qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("KHOA")]
    public class ET_KHOA
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaKhoa, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maKhoa = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TenKhoa, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string tenKhoa = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính DienThoai, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string dienThoai = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MoTa, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string moTa = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TrangThai, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_KHOA() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_KHOA(string maKhoa, string tenKhoa, string dienThoai, string moTa, string trangThai)
        {
            this.maKhoa = maKhoa;
            this.tenKhoa = tenKhoa;
            this.dienThoai = dienThoai;
            this.moTa = moTa;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã khoa bệnh viện; ánh xạ cột MaKhoa của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaKhoa { get => maKhoa; set => maKhoa = value; }
        /// <summary>
        /// Tên khoa bệnh viện; ánh xạ cột TenKhoa của bảng trong TableAttribute.
        /// </summary>
        public string TenKhoa { get => tenKhoa; set => tenKhoa = value; }
        /// <summary>
        /// Số điện thoại liên hệ; ánh xạ cột DienThoai của bảng trong TableAttribute.
        /// </summary>
        public string DienThoai { get => dienThoai; set => dienThoai = value; }
        /// <summary>
        /// Mô tả thông tin danh mục; ánh xạ cột MoTa của bảng trong TableAttribute.
        /// </summary>
        public string MoTa { get => moTa; set => moTa = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}