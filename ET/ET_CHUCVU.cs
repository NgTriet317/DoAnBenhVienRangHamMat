// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng CHUC_VU qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("CHUC_VU")]
    public class ET_CHUCVU
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaChucVu, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maChucVu = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TenChucVu, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string tenChucVu = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MoTa, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string moTa = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_CHUCVU() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_CHUCVU(string maChucVu, string tenChucVu, string moTa)
        {
            this.maChucVu = maChucVu;
            this.tenChucVu = tenChucVu;
            this.moTa = moTa;
        }

        /// <summary>
        /// Mã chức vụ; ánh xạ cột MaChucVu của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaChucVu { get => maChucVu; set => maChucVu = value; }
        /// <summary>
        /// Tên chức vụ; ánh xạ cột TenChucVu của bảng trong TableAttribute.
        /// </summary>
        public string TenChucVu { get => tenChucVu; set => tenChucVu = value; }
        /// <summary>
        /// Mô tả thông tin danh mục; ánh xạ cột MoTa của bảng trong TableAttribute.
        /// </summary>
        public string MoTa { get => moTa; set => moTa = value; }
    }
}