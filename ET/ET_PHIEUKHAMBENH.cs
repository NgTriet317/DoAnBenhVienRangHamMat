// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng PHIEU_KHAM_BENH qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("PHIEU_KHAM_BENH")]
    public class ET_PHIEUKHAMBENH
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaPhieu, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maPhieu = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaHS, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maHS = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaLich, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? maLich;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaPhong, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maPhong = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaBS, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? maBS;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaNVTiepNhan, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maNVTiepNhan = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaDV, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? maDV;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính SoThuTu, dùng bởi getter/setter tương ứng.
        /// </summary>
        private int soThuTu;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính ThoiGianTiepNhan, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime thoiGianTiepNhan;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính LyDoKham, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string lyDoKham = string.Empty;
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
        public ET_PHIEUKHAMBENH() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_PHIEUKHAMBENH(string maPhieu, string maHS, string? maLich, string maPhong, string? maBS, string maNVTiepNhan, string? maDV, int soThuTu, DateTime thoiGianTiepNhan, string lyDoKham, string trangThai, string? ghiChu)
        {
            this.maPhieu = maPhieu;
            this.maHS = maHS;
            this.maLich = maLich;
            this.maPhong = maPhong;
            this.maBS = maBS;
            this.maNVTiepNhan = maNVTiepNhan;
            this.maDV = maDV;
            this.soThuTu = soThuTu;
            this.thoiGianTiepNhan = thoiGianTiepNhan;
            this.lyDoKham = lyDoKham;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã phiếu tiếp nhận khám bệnh; ánh xạ cột MaPhieu của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaPhieu { get => maPhieu; set => maPhieu = value; }
        /// <summary>
        /// Mã hồ sơ bệnh án; ánh xạ cột MaHS của bảng trong TableAttribute.
        /// </summary>
        public string MaHS { get => maHS; set => maHS = value; }
        /// <summary>
        /// Mã lịch hẹn khám; ánh xạ cột MaLich của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaLich { get => maLich; set => maLich = value; }
        /// <summary>
        /// Mã phòng; ánh xạ cột MaPhong của bảng trong TableAttribute.
        /// </summary>
        public string MaPhong { get => maPhong; set => maPhong = value; }
        /// <summary>
        /// Mã bác sĩ, liên kết danh mục nhân viên; ánh xạ cột MaBS của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaBS { get => maBS; set => maBS = value; }
        /// <summary>
        /// Mã nhân viên tiếp nhận bệnh nhân; ánh xạ cột MaNVTiepNhan của bảng trong TableAttribute.
        /// </summary>
        public string MaNVTiepNhan { get => maNVTiepNhan; set => maNVTiepNhan = value; }
        /// <summary>
        /// Mã dịch vụ; ánh xạ cột MaDV của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaDV { get => maDV; set => maDV = value; }
        /// <summary>
        /// Số thứ tự tiếp nhận khám; ánh xạ cột SoThuTu của bảng trong TableAttribute.
        /// </summary>
        public int SoThuTu { get => soThuTu; set => soThuTu = value; }
        /// <summary>
        /// Thời điểm tiếp nhận tại bệnh viện; ánh xạ cột ThoiGianTiepNhan của bảng trong TableAttribute.
        /// </summary>
        public DateTime ThoiGianTiepNhan { get => thoiGianTiepNhan; set => thoiGianTiepNhan = value; }
        /// <summary>
        /// Lý do đến khám; ánh xạ cột LyDoKham của bảng trong TableAttribute.
        /// </summary>
        public string LyDoKham { get => lyDoKham; set => lyDoKham = value; }
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
