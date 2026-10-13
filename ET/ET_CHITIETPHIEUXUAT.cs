// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng CHI_TIET_PHIEU_XUAT qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("CHI_TIET_PHIEU_XUAT")]
    public class ET_CHITIETPHIEUXUAT
    {
        private string maCTPX = string.Empty;
        private string maPX = string.Empty;
        private string maHH = string.Empty;
        private string? maLo;
        private string? maCTDT;
        private decimal soLuong;
        private string ghiChu = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_CHITIETPHIEUXUAT() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_CHITIETPHIEUXUAT(string maCTPX, string maPX, string maHH, string? maLo, string? maCTDT, decimal soLuong, string ghiChu)
        {
            this.maCTPX = maCTPX;
            this.maPX = maPX;
            this.maHH = maHH;
            this.maLo = maLo;
            this.maCTDT = maCTDT;
            this.soLuong = soLuong;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã dòng chi tiết phiếu xuất; ánh xạ cột MaCTPX của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaCTPX { get => maCTPX; set => maCTPX = value; }
        /// <summary>
        /// Mã phiếu xuất kho; ánh xạ cột MaPX của bảng trong TableAttribute.
        /// </summary>
        public string MaPX { get => maPX; set => maPX = value; }
        /// <summary>
        /// Mã hàng hóa; ánh xạ cột MaHH của bảng trong TableAttribute.
        /// </summary>
        public string MaHH { get => maHH; set => maHH = value; }
        /// <summary>
        /// Mã lô hàng hóa; ánh xạ cột MaLo của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaLo { get => maLo; set => maLo = value; }
        /// <summary>
        /// Mã dòng chi tiết đơn thuốc; ánh xạ cột MaCTDT của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaCTDT { get => maCTDT; set => maCTDT = value; }
        /// <summary>
        /// Số lượng thuốc/hàng hóa/dịch vụ; ánh xạ cột SoLuong của bảng trong TableAttribute.
        /// </summary>
        public decimal SoLuong { get => soLuong; set => soLuong = value; }
        /// <summary>
        /// Ghi chú bổ sung; ánh xạ cột GhiChu của bảng trong TableAttribute.
        /// </summary>
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
