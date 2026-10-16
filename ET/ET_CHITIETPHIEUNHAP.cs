// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng CHI_TIET_PHIEU_NHAP qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("CHI_TIET_PHIEU_NHAP")]
    public class ET_CHITIETPHIEUNHAP
    {
        private string maCTPN = string.Empty;
        private string maPN = string.Empty;
        private string maHH = string.Empty;
        private string? maLo;
        private decimal soLuong;
        private decimal donGia;
        private decimal thanhTien;
        private string? ghiChu;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_CHITIETPHIEUNHAP() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_CHITIETPHIEUNHAP(string maCTPN, string maPN, string maHH, string? maLo, decimal soLuong, decimal donGia, decimal thanhTien, string? ghiChu)
        {
            this.maCTPN = maCTPN;
            this.maPN = maPN;
            this.maHH = maHH;
            this.maLo = maLo;
            this.soLuong = soLuong;
            this.donGia = donGia;
            this.thanhTien = thanhTien;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã dòng chi tiết phiếu nhập; ánh xạ cột MaCTPN của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaCTPN { get => maCTPN; set => maCTPN = value; }
        /// <summary>
        /// Mã phiếu nhập kho; ánh xạ cột MaPN của bảng trong TableAttribute.
        /// </summary>
        public string MaPN { get => maPN; set => maPN = value; }
        /// <summary>
        /// Mã hàng hóa; ánh xạ cột MaHH của bảng trong TableAttribute.
        /// </summary>
        public string MaHH { get => maHH; set => maHH = value; }
        /// <summary>
        /// Mã lô hàng hóa; ánh xạ cột MaLo của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaLo { get => maLo; set => maLo = value; }
        /// <summary>
        /// Số lượng thuốc/hàng hóa/dịch vụ; ánh xạ cột SoLuong của bảng trong TableAttribute.
        /// </summary>
        public decimal SoLuong { get => soLuong; set => soLuong = value; }
        /// <summary>
        /// Đơn giá; ánh xạ cột DonGia của bảng trong TableAttribute.
        /// </summary>
        public decimal DonGia { get => donGia; set => donGia = value; }
        /// <summary>
        /// Thành tiền dòng chi tiết do SQL trả về; ánh xạ cột ThanhTien của bảng trong TableAttribute. Chỉ đọc kết quả SQL, Dapper.Contrib không đưa cột này vào INSERT/UPDATE.
        /// </summary>
        [Write(false)]
        public decimal ThanhTien { get => thanhTien; set => thanhTien = value; }
        /// <summary>
        /// Ghi chú bổ sung; ánh xạ cột GhiChu của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}