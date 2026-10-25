// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng KE_HOACH_DIEU_TRI qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("KE_HOACH_DIEU_TRI")]
    public class ET_KEHOACHDIEUTRI
    {
        private string? maTTBNguon;
        private string? maMauKH;
        private int? phienBanMau;

        private string maKHDT = string.Empty;
        private string maLS = string.Empty;
        private string maBS = string.Empty;
        private DateTime ngayLap;
        private DateTime? ngayBatDau;
        private DateTime? ngayDuKienKetThuc;
        private decimal tongTienDuKien;
        private string trangThai = string.Empty;
        private string? ghiChu;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_KEHOACHDIEUTRI() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_KEHOACHDIEUTRI(string maKHDT, string maLS, string maBS, DateTime ngayLap, DateTime? ngayBatDau, DateTime? ngayDuKienKetThuc, decimal tongTienDuKien, string trangThai, string? ghiChu)
        {
            this.maKHDT = maKHDT;
            this.maLS = maLS;
            this.maBS = maBS;
            this.ngayLap = ngayLap;
            this.ngayBatDau = ngayBatDau;
            this.ngayDuKienKetThuc = ngayDuKienKetThuc;
            this.tongTienDuKien = tongTienDuKien;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã kế hoạch điều trị; ánh xạ cột MaKHDT của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaKHDT { get => maKHDT; set => maKHDT = value; }
        /// <summary>
        /// Mã lần khám trong lịch sử khám bệnh; ánh xạ cột MaLS của bảng trong TableAttribute.
        /// </summary>
        public string MaLS { get => maLS; set => maLS = value; }
        /// <summary>
        /// Mã bác sĩ, liên kết danh mục nhân viên; ánh xạ cột MaBS của bảng trong TableAttribute.
        /// </summary>
        public string MaBS { get => maBS; set => maBS = value; }
        /// <summary>
        /// Thời điểm lập bản ghi; ánh xạ cột NgayLap của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayLap { get => ngayLap; set => ngayLap = value; }
        /// <summary>
        /// Ngày bắt đầu kế hoạch; ánh xạ cột NgayBatDau của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? NgayBatDau { get => ngayBatDau; set => ngayBatDau = value; }
        /// <summary>
        /// Ngày dự kiến kết thúc kế hoạch; ánh xạ cột NgayDuKienKetThuc của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? NgayDuKienKetThuc { get => ngayDuKienKetThuc; set => ngayDuKienKetThuc = value; }
        /// <summary>
        /// Tổng tiền dự kiến của kế hoạch do SQL cập nhật; ánh xạ cột TongTienDuKien của bảng trong TableAttribute. Chỉ đọc kết quả SQL, Dapper.Contrib không đưa cột này vào INSERT/UPDATE.
        /// </summary>
        [Write(false)]
        public decimal TongTienDuKien { get => tongTienDuKien; set => tongTienDuKien = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
        /// <summary>
        /// Ghi chú bổ sung; ánh xạ cột GhiChu của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }

        // Cột bổ sung P03; constructor cũ được giữ để tương thích mã gọi hiện có.
        public string? MaTTBNguon { get => maTTBNguon; set => maTTBNguon = value; }
        public string? MaMauKH { get => maMauKH; set => maMauKH = value; }
        public int? PhienBanMau { get => phienBanMau; set => phienBanMau = value; }
        public ET_KEHOACHDIEUTRI(string maKHDT, string maLS, string maBS, DateTime ngayLap, DateTime? ngayBatDau, DateTime? ngayDuKienKetThuc, decimal tongTienDuKien, string trangThai, string? ghiChu, string? maTTBNguon, string? maMauKH, int? phienBanMau)
        {
            this.maKHDT = maKHDT;
            this.maLS = maLS;
            this.maBS = maBS;
            this.ngayLap = ngayLap;
            this.ngayBatDau = ngayBatDau;
            this.ngayDuKienKetThuc = ngayDuKienKetThuc;
            this.tongTienDuKien = tongTienDuKien;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
            this.maTTBNguon = maTTBNguon;
            this.maMauKH = maMauKH;
            this.phienBanMau = phienBanMau;
        }
    }
}
