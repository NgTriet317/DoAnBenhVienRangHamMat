// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng LICH_SU_KHAM_BENH qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("LICH_SU_KHAM_BENH")]
    public class ET_LICHSUKHAMBENH
    {
        private int khongKeThuoc;
        private string? lyDoKhongKeThuoc;

        private string maLS = string.Empty;
        private string maPhieu = string.Empty;
        private string maBS = string.Empty;
        private string maPhong = string.Empty;
        private DateTime thoiGianKham;
        private string trieuChung = string.Empty;
        private string chanDoan = string.Empty;
        private string keHoachXuLy = string.Empty;
        private string trangThai = string.Empty;
        private DateTime? ngayXacNhan;
        private string? ghiChu;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_LICHSUKHAMBENH() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_LICHSUKHAMBENH(string maLS, string maPhieu, string maBS, string maPhong, DateTime thoiGianKham, string trieuChung, string chanDoan, string keHoachXuLy, string trangThai, DateTime? ngayXacNhan, string? ghiChu)
        {
            this.maLS = maLS;
            this.maPhieu = maPhieu;
            this.maBS = maBS;
            this.maPhong = maPhong;
            this.thoiGianKham = thoiGianKham;
            this.trieuChung = trieuChung;
            this.chanDoan = chanDoan;
            this.keHoachXuLy = keHoachXuLy;
            this.trangThai = trangThai;
            this.ngayXacNhan = ngayXacNhan;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã lần khám trong lịch sử khám bệnh; ánh xạ cột MaLS của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaLS { get => maLS; set => maLS = value; }
        /// <summary>
        /// Mã phiếu tiếp nhận khám bệnh; ánh xạ cột MaPhieu của bảng trong TableAttribute.
        /// </summary>
        public string MaPhieu { get => maPhieu; set => maPhieu = value; }
        /// <summary>
        /// Mã bác sĩ, liên kết danh mục nhân viên; ánh xạ cột MaBS của bảng trong TableAttribute.
        /// </summary>
        public string MaBS { get => maBS; set => maBS = value; }
        /// <summary>
        /// Mã phòng; ánh xạ cột MaPhong của bảng trong TableAttribute.
        /// </summary>
        public string MaPhong { get => maPhong; set => maPhong = value; }
        /// <summary>
        /// Thời điểm thực tế khám; ánh xạ cột ThoiGianKham của bảng trong TableAttribute.
        /// </summary>
        public DateTime ThoiGianKham { get => thoiGianKham; set => thoiGianKham = value; }
        /// <summary>
        /// Triệu chứng được ghi khi khám; ánh xạ cột TrieuChung của bảng trong TableAttribute.
        /// </summary>
        public string TrieuChung { get => trieuChung; set => trieuChung = value; }
        /// <summary>
        /// Chẩn đoán của lần khám; ánh xạ cột ChanDoan của bảng trong TableAttribute.
        /// </summary>
        public string ChanDoan { get => chanDoan; set => chanDoan = value; }
        /// <summary>
        /// Hướng xử lý sau khi khám; ánh xạ cột KeHoachXuLy của bảng trong TableAttribute.
        /// </summary>
        public string KeHoachXuLy { get => keHoachXuLy; set => keHoachXuLy = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
        /// <summary>
        /// Thời điểm xác nhận chứng từ; ánh xạ cột NgayXacNhan của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? NgayXacNhan { get => ngayXacNhan; set => ngayXacNhan = value; }
        /// <summary>
        /// Ghi chú bổ sung; ánh xạ cột GhiChu của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }

        // Cột bổ sung P03; constructor cũ được giữ để tương thích mã gọi hiện có.
        public int KhongKeThuoc { get => khongKeThuoc; set => khongKeThuoc = value; }
        public string? LyDoKhongKeThuoc { get => lyDoKhongKeThuoc; set => lyDoKhongKeThuoc = value; }
        public ET_LICHSUKHAMBENH(string maLS, string maPhieu, string maBS, string maPhong, DateTime thoiGianKham, string trieuChung, string chanDoan, string keHoachXuLy, string trangThai, DateTime? ngayXacNhan, string? ghiChu, int khongKeThuoc, string? lyDoKhongKeThuoc)
        {
            this.maLS = maLS;
            this.maPhieu = maPhieu;
            this.maBS = maBS;
            this.maPhong = maPhong;
            this.thoiGianKham = thoiGianKham;
            this.trieuChung = trieuChung;
            this.chanDoan = chanDoan;
            this.keHoachXuLy = keHoachXuLy;
            this.trangThai = trangThai;
            this.ngayXacNhan = ngayXacNhan;
            this.ghiChu = ghiChu;
            this.khongKeThuoc = khongKeThuoc;
            this.lyDoKhongKeThuoc = lyDoKhongKeThuoc;
        }
    }
}
