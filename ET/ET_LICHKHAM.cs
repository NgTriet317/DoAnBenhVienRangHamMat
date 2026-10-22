// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng LICH_KHAM qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("LICH_KHAM")]
    public class ET_LICHKHAM
    {
        private string maLich = string.Empty;
        private string maBN = string.Empty;
        private string maBS = string.Empty;
        private string maCa = string.Empty;
        private string maPhong = string.Empty;
        private string? maDV;
        private DateTime ngayGioKham;
        private int thoiLuongDuKien;
        private string uuTien = string.Empty;
        private string lyDoKham = string.Empty;
        private string? lyDoThayDoi;
        private string trangThai = string.Empty;
        private string? ghiChu;
        private DateTime ngayTao;
        private DateTime? ngayCapNhat;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_LICHKHAM() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_LICHKHAM(string maLich, string maBN, string maBS, string maCa, string maPhong, string? maDV, DateTime ngayGioKham, int thoiLuongDuKien, string uuTien, string lyDoKham, string? lyDoThayDoi, string trangThai, string? ghiChu, DateTime ngayTao, DateTime? ngayCapNhat)
        {
            this.maLich = maLich;
            this.maBN = maBN;
            this.maBS = maBS;
            this.maCa = maCa;
            this.maPhong = maPhong;
            this.maDV = maDV;
            this.ngayGioKham = ngayGioKham;
            this.thoiLuongDuKien = thoiLuongDuKien;
            this.uuTien = uuTien;
            this.lyDoKham = lyDoKham;
            this.lyDoThayDoi = lyDoThayDoi;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
            this.ngayTao = ngayTao;
            this.ngayCapNhat = ngayCapNhat;
        }

        /// <summary>
        /// Mã lịch hẹn khám; ánh xạ cột MaLich của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaLich { get => maLich; set => maLich = value; }
        /// <summary>
        /// Mã bệnh nhân; ánh xạ cột MaBN của bảng trong TableAttribute.
        /// </summary>
        public string MaBN { get => maBN; set => maBN = value; }
        /// <summary>
        /// Mã bác sĩ, liên kết danh mục nhân viên; ánh xạ cột MaBS của bảng trong TableAttribute.
        /// </summary>
        public string MaBS { get => maBS; set => maBS = value; }
        /// <summary>
        /// Mã ca làm việc; ánh xạ cột MaCa của bảng trong TableAttribute.
        /// </summary>
        public string MaCa { get => maCa; set => maCa = value; }
        /// <summary>
        /// Mã phòng; ánh xạ cột MaPhong của bảng trong TableAttribute.
        /// </summary>
        public string MaPhong { get => maPhong; set => maPhong = value; }
        /// <summary>
        /// Mã dịch vụ; ánh xạ cột MaDV của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaDV { get => maDV; set => maDV = value; }
        /// <summary>
        /// Ngày/giờ hẹn khám; ánh xạ cột NgayGioKham của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayGioKham { get => ngayGioKham; set => ngayGioKham = value; }
        /// <summary>
        /// Thời lượng dự kiến của lịch hẹn tính bằng phút; ánh xạ cột ThoiLuongDuKien của bảng trong TableAttribute.
        /// </summary>
        public int ThoiLuongDuKien { get => thoiLuongDuKien; set => thoiLuongDuKien = value; }
        /// <summary>
        /// Mức ưu tiên lịch hẹn; ánh xạ cột UuTien của bảng trong TableAttribute.
        /// </summary>
        public string UuTien { get => uuTien; set => uuTien = value; }
        // Khác mức ưu tiên: mức nghiêm trọng của ca bệnh.
        public string MucDoNghiemTrong { get; set; } = "Thấp";
        // Nhân sự dự bị đã chọn lúc tạo lịch; được điều phối thủ công khi bác sĩ chính nghỉ.
        public string? MaBSDuBi { get; set; }

        /// <summary>
        /// Lý do đến khám; ánh xạ cột LyDoKham của bảng trong TableAttribute.
        /// </summary>
        public string LyDoKham { get => lyDoKham; set => lyDoKham = value; }
        /// <summary>
        /// Lý do thay đổi lịch hẹn; ánh xạ cột LyDoThayDoi của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? LyDoThayDoi { get => lyDoThayDoi; set => lyDoThayDoi = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
        /// <summary>
        /// Ghi chú bổ sung; ánh xạ cột GhiChu của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
        /// <summary>
        /// Thời điểm tạo lịch hẹn/tài khoản; ánh xạ cột NgayTao của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayTao { get => ngayTao; set => ngayTao = value; }
        /// <summary>
        /// Thời điểm cập nhật lịch; ánh xạ cột NgayCapNhat của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? NgayCapNhat { get => ngayCapNhat; set => ngayCapNhat = value; }
    }
}
