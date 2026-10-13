// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng HOA_DON_NHAP qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("HOA_DON_NHAP")]
    public class ET_HOADONNHAP
    {
        private string maHDN = string.Empty;
        private string soHoaDonNCC = string.Empty;
        private string maNCC = string.Empty;
        private string maNV = string.Empty;
        private DateTime ngayHoaDon;
        private decimal tongTien;
        private string trangThai = string.Empty;
        private string? ghiChu;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_HOADONNHAP() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_HOADONNHAP(string maHDN, string soHoaDonNCC, string maNCC, string maNV, DateTime ngayHoaDon, decimal tongTien, string trangThai, string? ghiChu)
        {
            this.maHDN = maHDN;
            this.soHoaDonNCC = soHoaDonNCC;
            this.maNCC = maNCC;
            this.maNV = maNV;
            this.ngayHoaDon = ngayHoaDon;
            this.tongTien = tongTien;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã hóa đơn nhập từ nhà cung cấp; ánh xạ cột MaHDN của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaHDN { get => maHDN; set => maHDN = value; }
        /// <summary>
        /// Số hóa đơn nhà cung cấp; ánh xạ cột SoHoaDonNCC của bảng trong TableAttribute.
        /// </summary>
        public string SoHoaDonNCC { get => soHoaDonNCC; set => soHoaDonNCC = value; }
        /// <summary>
        /// Mã nhà cung cấp; ánh xạ cột MaNCC của bảng trong TableAttribute.
        /// </summary>
        public string MaNCC { get => maNCC; set => maNCC = value; }
        /// <summary>
        /// Mã nhân viên; ánh xạ cột MaNV của bảng trong TableAttribute.
        /// </summary>
        public string MaNV { get => maNV; set => maNV = value; }
        /// <summary>
        /// Ngày hóa đơn nhà cung cấp; ánh xạ cột NgayHoaDon của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayHoaDon { get => ngayHoaDon; set => ngayHoaDon = value; }
        /// <summary>
        /// Tổng tiền của chứng từ; ánh xạ cột TongTien của bảng trong TableAttribute.
        /// </summary>
        public decimal TongTien { get => tongTien; set => tongTien = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
        /// <summary>
        /// Ghi chú bổ sung; ánh xạ cột GhiChu của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}