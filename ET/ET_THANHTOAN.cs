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
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính SoTienKhachDua, dùng bởi getter/setter tương ứng.
        /// </summary>
        private decimal? soTienKhachDua;
        /// <summary>
        /// Tiền hoàn lại được tính từ số tiền khách đưa và số phải thu được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private decimal? tienThua;
        /// <summary>
        /// Mã tham chiếu được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? maThamChieu;

        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaTT, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maTT = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaHD, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maHD = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaNV, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maNV = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgayThanhToan, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime ngayThanhToan;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính SoTien, dùng bởi getter/setter tương ứng.
        /// </summary>
        private decimal soTien;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính LoaiGiaoDich, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string loaiGiaoDich = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính PhuongThuc, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string phuongThuc = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính LyDo, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? lyDo;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TrangThai, dùng bởi getter/setter tương ứng.
        /// </summary>
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
        /// <summary>
        /// Giá trị Tiền hoàn lại được tính từ số tiền khách đưa và số phải thu; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public decimal? TienThua { get => tienThua; set => tienThua = value; }
        /// <summary>
        /// Giá trị Mã tham chiếu; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaThamChieu { get => maThamChieu; set => maThamChieu = value; }
        /// <summary>
        /// Khởi tạo thanh toán. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
        /// </summary>
        /// <param name="maTT">Mã thanh toán xác định đối tượng cần xử lý.</param>
        /// <param name="maHD">Mã hóa đơn xác định đối tượng cần xử lý.</param>
        /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
        /// <param name="ngayThanhToan">Giá trị ngày thanh toán dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="soTien">Giá trị số tiền dùng khi thực hiện thao tác; kiểu decimal.</param>
        /// <param name="loaiGiaoDich">Giá trị loại giao dịch dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="phuongThuc">Giá trị phương thức dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="lyDo">Giá trị lý do dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="trangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="soTienKhachDua">Giá trị số tiền khách đưa dùng khi thực hiện thao tác; kiểu decimal?.</param>
        /// <param name="tienThua">Giá trị tiền thừa dùng khi thực hiện thao tác; kiểu decimal?.</param>
        /// <param name="maThamChieu">Mã tham chiếu xác định đối tượng cần xử lý.</param>
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
