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
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaTTBNguon, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? maTTBNguon;
        /// <summary>
        /// Mã mẫu kế hoạch điều trị được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? maMauKH;
        /// <summary>
        /// Phiên bản mẫu được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private int? phienBanMau;

        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaKHDT, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maKHDT = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaLS, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maLS = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaBS, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maBS = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgayLap, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime ngayLap;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgayBatDau, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime? ngayBatDau;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgayDuKienKetThuc, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime? ngayDuKienKetThuc;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TongTienDuKien, dùng bởi getter/setter tương ứng.
        /// </summary>
        private decimal tongTienDuKien;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TrangThai, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string trangThai = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính GhiChu, dùng bởi getter/setter tương ứng.
        /// </summary>
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
        /// <summary>
        /// Giá trị Mã mẫu kế hoạch điều trị; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaMauKH { get => maMauKH; set => maMauKH = value; }
        /// <summary>
        /// Giá trị Phiên bản mẫu; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public int? PhienBanMau { get => phienBanMau; set => phienBanMau = value; }
        /// <summary>
        /// Khởi tạo kế hoạch điều trị. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
        /// </summary>
        /// <param name="maKHDT">Mã kế hoạch điều trị xác định đối tượng cần xử lý.</param>
        /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
        /// <param name="maBS">Mã bác sĩ xác định đối tượng cần xử lý.</param>
        /// <param name="ngayLap">Giá trị ngày lập dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="ngayBatDau">Giá trị ngày bắt đầu dùng khi thực hiện thao tác; kiểu DateTime?.</param>
        /// <param name="ngayDuKienKetThuc">Giá trị ngày dự kiến kết thúc dùng khi thực hiện thao tác; kiểu DateTime?.</param>
        /// <param name="tongTienDuKien">Giá trị tổng tiền dự kiến dùng khi thực hiện thao tác; kiểu decimal.</param>
        /// <param name="trangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="ghiChu">Giá trị ghi chú dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="maTTBNguon">Mã tình trạng bệnh nguồn xác định đối tượng cần xử lý.</param>
        /// <param name="maMauKH">Mã mẫu kế hoạch xác định đối tượng cần xử lý.</param>
        /// <param name="phienBanMau">Giá trị phiên bản mẫu dùng khi thực hiện thao tác; kiểu int?.</param>
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
