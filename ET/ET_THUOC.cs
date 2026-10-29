// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng THUOC qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("THUOC")]
    public class ET_THUOC
    {
        private string maThuoc = string.Empty;
        private string maHH = string.Empty;
        private string hamLuong = string.Empty;
        private decimal mucTonToiThieu;
        private decimal donGiaBan;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_THUOC() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_THUOC(string maThuoc, string maHH, string hamLuong, decimal mucTonToiThieu, decimal donGiaBan)
        {
            this.maThuoc = maThuoc;
            this.maHH = maHH;
            this.hamLuong = hamLuong;
            this.mucTonToiThieu = mucTonToiThieu;
            this.donGiaBan = donGiaBan;
        }

        /// <summary>
        /// Mã thuốc; ánh xạ cột MaThuoc của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaThuoc { get => maThuoc; set => maThuoc = value; }
        /// <summary>
        /// Mã hàng hóa; ánh xạ cột MaHH của bảng trong TableAttribute.
        /// </summary>
        public string MaHH { get => maHH; set => maHH = value; }
        public string? MaNhomThuoc { get; set; }
        /// <summary>
        /// Hàm lượng thuốc; ánh xạ cột HamLuong của bảng trong TableAttribute.
        /// </summary>
        public string HamLuong { get => hamLuong; set => hamLuong = value; }
        /// <summary>
        /// Mức tồn kho tối thiểu cấu hình cho thuốc/vật tư; ánh xạ cột MucTonToiThieu của bảng trong TableAttribute.
        /// </summary>
        public decimal MucTonToiThieu { get => mucTonToiThieu; set => mucTonToiThieu = value; }
        /// <summary>
        /// Giá bán thuốc hiện hành; ánh xạ cột DonGiaBan của bảng trong TableAttribute.
        /// </summary>
        public decimal DonGiaBan { get => donGiaBan; set => donGiaBan = value; }
    }
}
