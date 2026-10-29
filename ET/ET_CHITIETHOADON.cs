// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng CHI_TIET_HOA_DON qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("CHI_TIET_HOA_DON")]
    public class ET_CHITIETHOADON
    {
        private string maCTHD = string.Empty;
        private string maHD = string.Empty;
        private string? maDVThucHien;
        private string? maCTPX;
        private string noiDung = string.Empty;
        private decimal? donGia;
        private decimal thanhTien;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_CHITIETHOADON() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_CHITIETHOADON(string maCTHD, string maHD, string? maDVThucHien, string? maCTPX, string noiDung, decimal? donGia, decimal thanhTien)
        {
            this.maCTHD = maCTHD;
            this.maHD = maHD;
            this.maDVThucHien = maDVThucHien;
            this.maCTPX = maCTPX;
            this.noiDung = noiDung;
            this.donGia = donGia;
            this.thanhTien = thanhTien;
        }

        /// <summary>
        /// Mã dòng chi tiết hóa đơn; ánh xạ cột MaCTHD của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaCTHD { get => maCTHD; set => maCTHD = value; }
        /// <summary>
        /// Mã hóa đơn bệnh viện; ánh xạ cột MaHD của bảng trong TableAttribute.
        /// </summary>
        public string MaHD { get => maHD; set => maHD = value; }
        /// <summary>
        /// Mã bản ghi dịch vụ thực hiện; ánh xạ cột MaDVThucHien của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaDVThucHien { get => maDVThucHien; set => maDVThucHien = value; }
        /// <summary>
        /// Mã dòng chi tiết phiếu xuất; ánh xạ cột MaCTPX của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaCTPX { get => maCTPX; set => maCTPX = value; }
        /// <summary>Dòng đơn đã ký, chưa xuất kho; được thay bằng các dòng xuất khi thu.</summary>
        public string? MaCTDT { get; set; }
        /// <summary>
        /// Nội dung chứng từ hoặc khoản thu; ánh xạ cột NoiDung của bảng trong TableAttribute.
        /// </summary>
        public string NoiDung { get => noiDung; set => noiDung = value; }
        /// <summary>
        /// Đơn giá; ánh xạ cột DonGia của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public decimal? DonGia { get => donGia; set => donGia = value; }
        /// <summary>
        /// Thành tiền dòng chi tiết do SQL trả về; ánh xạ cột ThanhTien của bảng trong TableAttribute. Chỉ đọc kết quả SQL, Dapper.Contrib không đưa cột này vào INSERT/UPDATE.
        /// </summary>
        [Write(false)]
        public decimal ThanhTien { get => thanhTien; set => thanhTien = value; }
    }
}
