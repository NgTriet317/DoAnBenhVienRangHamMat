// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng THANH_TOAN qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("THANH_TOAN")]
    public class ET_THANHTOAN
    {
        private decimal? soTienKhachDua;
        private decimal? tienThua;
        private string? maThamChieu;

        private string maTT = string.Empty;
        private string maHD = string.Empty;
        private string maNV = string.Empty;
        private DateTime ngayThanhToan;
        private decimal soTien;
        private string loaiGiaoDich = string.Empty;
        private string phuongThuc = string.Empty;
        private string? lyDo;
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_THANHTOAN() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_THANHTOAN(string maTT, string maHD, string maNV, DateTime ngayThanhToan, decimal soTien, string loaiGiaoDich, string phuongThuc, string? lyDo, string trangThai)
        {
            this.maTT = maTT;
            this.maHD = maHD;
            this.maNV = maNV;
            this.ngayThanhToan = ngayThanhToan;
            this.soTien = soTien;
            this.loaiGiaoDich = loaiGiaoDich;
            this.phuongThuc = phuongThuc;
            this.lyDo = lyDo;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã bản ghi thanh toán; ánh xạ cột MaTT của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaTT { get => maTT; set => maTT = value; }
        /// <summary>
        /// Mã hóa đơn bệnh viện; ánh xạ cột MaHD của bảng trong TableAttribute.
        /// </summary>
        public string MaHD { get => maHD; set => maHD = value; }
        /// <summary>
        /// Mã nhân viên; ánh xạ cột MaNV của bảng trong TableAttribute.
        /// </summary>
        public string MaNV { get => maNV; set => maNV = value; }
        /// <summary>
        /// Thời điểm giao dịch thanh toán; ánh xạ cột NgayThanhToan của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayThanhToan { get => ngayThanhToan; set => ngayThanhToan = value; }
        /// <summary>
        /// Số tiền của giao dịch; ánh xạ cột SoTien của bảng trong TableAttribute.
        /// </summary>
        public decimal SoTien { get => soTien; set => soTien = value; }
        /// <summary>
        /// Loại giao dịch Thu hoặc Hoàn; ánh xạ cột LoaiGiaoDich của bảng trong TableAttribute.
        /// </summary>
        public string LoaiGiaoDich { get => loaiGiaoDich; set => loaiGiaoDich = value; }
        /// <summary>
        /// Phương thức giao dịch thanh toán; ánh xạ cột PhuongThuc của bảng trong TableAttribute.
        /// </summary>
        public string PhuongThuc { get => phuongThuc; set => phuongThuc = value; }
        /// <summary>
        /// Lý do của chứng từ/giao dịch; ánh xạ cột LyDo của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? LyDo { get => lyDo; set => lyDo = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }

        // Cột bổ sung P03; constructor cũ được giữ để tương thích mã gọi hiện có.
        public decimal? SoTienKhachDua { get => soTienKhachDua; set => soTienKhachDua = value; }
        public decimal? TienThua { get => tienThua; set => tienThua = value; }
        public string? MaThamChieu { get => maThamChieu; set => maThamChieu = value; }
        public ET_THANHTOAN(string maTT, string maHD, string maNV, DateTime ngayThanhToan, decimal soTien, string loaiGiaoDich, string phuongThuc, string? lyDo, string trangThai, decimal? soTienKhachDua, decimal? tienThua, string? maThamChieu)
        {
            this.maTT = maTT;
            this.maHD = maHD;
            this.maNV = maNV;
            this.ngayThanhToan = ngayThanhToan;
            this.soTien = soTien;
            this.loaiGiaoDich = loaiGiaoDich;
            this.phuongThuc = phuongThuc;
            this.lyDo = lyDo;
            this.trangThai = trangThai;
            this.soTienKhachDua = soTienKhachDua;
            this.tienThua = tienThua;
            this.maThamChieu = maThamChieu;
        }
    }
}
