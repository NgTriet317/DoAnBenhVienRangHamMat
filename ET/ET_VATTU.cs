// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng VAT_TU qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("VAT_TU")]
    public class ET_VATTU
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaVT, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maVT = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaHH, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maHH = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MucTonToiThieu, dùng bởi getter/setter tương ứng.
        /// </summary>
        private decimal mucTonToiThieu;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_VATTU() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_VATTU(string maVT, string maHH, decimal mucTonToiThieu)
        {
            this.maVT = maVT;
            this.maHH = maHH;
            this.mucTonToiThieu = mucTonToiThieu;
        }

        /// <summary>
        /// Mã vật tư; ánh xạ cột MaVT của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaVT { get => maVT; set => maVT = value; }
        /// <summary>
        /// Mã hàng hóa; ánh xạ cột MaHH của bảng trong TableAttribute.
        /// </summary>
        public string MaHH { get => maHH; set => maHH = value; }
        /// <summary>
        /// Mức tồn kho tối thiểu cấu hình cho thuốc/vật tư; ánh xạ cột MucTonToiThieu của bảng trong TableAttribute.
        /// </summary>
        public decimal MucTonToiThieu { get => mucTonToiThieu; set => mucTonToiThieu = value; }
    }
}