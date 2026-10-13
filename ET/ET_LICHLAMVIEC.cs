// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng LICH_LAM_VIEC qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("LICH_LAM_VIEC")]
    public class ET_LICHLAMVIEC
    {
        private string maLLV = string.Empty;
        private string maNV = string.Empty;
        private string maCa = string.Empty;
        private DateTime ngayLam;
        private string? maPhong;
        private string trangThai = string.Empty;
        private string trangThaiHienDien = string.Empty;
        private DateTime? gioVao;
        private DateTime? gioRa;
        private string? ghiChu;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_LICHLAMVIEC() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_LICHLAMVIEC(string maLLV, string maNV, string maCa, DateTime ngayLam, string? maPhong, string trangThai, string trangThaiHienDien, DateTime? gioVao, DateTime? gioRa, string? ghiChu)
        {
            this.maLLV = maLLV;
            this.maNV = maNV;
            this.maCa = maCa;
            this.ngayLam = ngayLam;
            this.maPhong = maPhong;
            this.trangThai = trangThai;
            this.trangThaiHienDien = trangThaiHienDien;
            this.gioVao = gioVao;
            this.gioRa = gioRa;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã lịch làm việc nhân viên; ánh xạ cột MaLLV của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaLLV { get => maLLV; set => maLLV = value; }
        /// <summary>
        /// Mã nhân viên; ánh xạ cột MaNV của bảng trong TableAttribute.
        /// </summary>
        public string MaNV { get => maNV; set => maNV = value; }
        /// <summary>
        /// Mã ca làm việc; ánh xạ cột MaCa của bảng trong TableAttribute.
        /// </summary>
        public string MaCa { get => maCa; set => maCa = value; }
        /// <summary>
        /// Ngày làm việc theo lịch; ánh xạ cột NgayLam của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayLam { get => ngayLam; set => ngayLam = value; }
        /// <summary>
        /// Mã phòng; ánh xạ cột MaPhong của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaPhong { get => maPhong; set => maPhong = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
        /// <summary>
        /// Trạng thái hiện diện của nhân viên theo lịch; ánh xạ cột TrangThaiHienDien của bảng trong TableAttribute.
        /// </summary>
        public string TrangThaiHienDien { get => trangThaiHienDien; set => trangThaiHienDien = value; }
        /// <summary>
        /// Giờ nhân viên vào ca; ánh xạ cột GioVao của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? GioVao { get => gioVao; set => gioVao = value; }
        /// <summary>
        /// Giờ nhân viên ra ca; ánh xạ cột GioRa của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? GioRa { get => gioRa; set => gioRa = value; }
        /// <summary>
        /// Ghi chú bổ sung; ánh xạ cột GhiChu của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}