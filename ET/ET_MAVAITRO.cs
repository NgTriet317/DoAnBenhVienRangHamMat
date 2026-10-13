// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng VAI_TRO qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("VAI_TRO")]
    public class ET_MAVAITRO
    {
        private string maVaiTro = string.Empty;
        private string tenVaiTro = string.Empty;
        private string moTa = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_MAVAITRO() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_MAVAITRO(string maVaiTro, string tenVaiTro, string moTa)
        {
            this.maVaiTro = maVaiTro;
            this.tenVaiTro = tenVaiTro;
            this.moTa = moTa;
        }

        /// <summary>
        /// Mã vai trò; ánh xạ cột MaVaiTro của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaVaiTro { get => maVaiTro; set => maVaiTro = value; }
        /// <summary>
        /// Tên vai trò; ánh xạ cột TenVaiTro của bảng trong TableAttribute.
        /// </summary>
        public string TenVaiTro { get => tenVaiTro; set => tenVaiTro = value; }
        /// <summary>
        /// Mô tả thông tin danh mục; ánh xạ cột MoTa của bảng trong TableAttribute.
        /// </summary>
        public string MoTa { get => moTa; set => moTa = value; }
    }
}