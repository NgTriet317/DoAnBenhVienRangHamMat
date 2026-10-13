// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng PHIEU_NHAP qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("PHIEU_NHAP")]
    public class ET_PHIEUNHAP
    {
        private string maPN = string.Empty;
        private string maHDN = string.Empty;
        private string maKho = string.Empty;
        private string maNV = string.Empty;
        private DateTime ngayNhap;
        private decimal tongTien;
        private string trangThai = string.Empty;
        private string lyDo = string.Empty;
        private string? ghiChu;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_PHIEUNHAP() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_PHIEUNHAP(string maPN, string maHDN, string maKho, string maNV, DateTime ngayNhap, decimal tongTien, string trangThai, string lyDo, string? ghiChu)
        {
            this.maPN = maPN;
            this.maHDN = maHDN;
            this.maKho = maKho;
            this.maNV = maNV;
            this.ngayNhap = ngayNhap;
            this.tongTien = tongTien;
            this.trangThai = trangThai;
            this.lyDo = lyDo;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã phiếu nhập kho; ánh xạ cột MaPN của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaPN { get => maPN; set => maPN = value; }
        /// <summary>
        /// Mã hóa đơn nhập từ nhà cung cấp; ánh xạ cột MaHDN của bảng trong TableAttribute.
        /// </summary>
        public string MaHDN { get => maHDN; set => maHDN = value; }
        /// <summary>
        /// Mã kho; ánh xạ cột MaKho của bảng trong TableAttribute.
        /// </summary>
        public string MaKho { get => maKho; set => maKho = value; }
        /// <summary>
        /// Mã nhân viên; ánh xạ cột MaNV của bảng trong TableAttribute.
        /// </summary>
        public string MaNV { get => maNV; set => maNV = value; }
        /// <summary>
        /// Ngày nhập lô/phiếu nhập; ánh xạ cột NgayNhap của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayNhap { get => ngayNhap; set => ngayNhap = value; }
        /// <summary>
        /// Tổng tiền của chứng từ; ánh xạ cột TongTien của bảng trong TableAttribute. Chỉ đọc kết quả SQL, Dapper.Contrib không đưa cột này vào INSERT/UPDATE.
        /// </summary>
        [Write(false)]
        public decimal TongTien { get => tongTien; set => tongTien = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
        /// <summary>
        /// Lý do của chứng từ/giao dịch; ánh xạ cột LyDo của bảng trong TableAttribute.
        /// </summary>
        public string LyDo { get => lyDo; set => lyDo = value; }
        /// <summary>
        /// Ghi chú bổ sung; ánh xạ cột GhiChu của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
