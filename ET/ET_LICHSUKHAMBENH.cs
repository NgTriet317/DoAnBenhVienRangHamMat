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
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính KhongKeThuoc, dùng bởi getter/setter tương ứng.
        /// </summary>
        private int khongKeThuoc;
        /// <summary>
        /// Lý do không kê thuốc được ghi nhận cho lần khám được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? lyDoKhongKeThuoc;

        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaLS, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maLS = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaPhieu, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maPhieu = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaBS, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maBS = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaPhong, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maPhong = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính ThoiGianKham, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime thoiGianKham;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TrieuChung, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string trieuChung = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính ChanDoan, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string chanDoan = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính KeHoachXuLy, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string keHoachXuLy = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TrangThai, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string trangThai = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgayXacNhan, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime? ngayXacNhan;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính GhiChu, dùng bởi getter/setter tương ứng.
        /// </summary>
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
        /// <summary>
        /// Giá trị Lý do không kê thuốc được ghi nhận cho lần khám; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? LyDoKhongKeThuoc { get => lyDoKhongKeThuoc; set => lyDoKhongKeThuoc = value; }
        /// <summary>
        /// Khởi tạo lịch sử khám bệnh. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
        /// </summary>
        /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
        /// <param name="maPhieu">Mã phiếu xác định đối tượng cần xử lý.</param>
        /// <param name="maBS">Mã bác sĩ xác định đối tượng cần xử lý.</param>
        /// <param name="maPhong">Mã phòng xác định đối tượng cần xử lý.</param>
        /// <param name="thoiGianKham">Giá trị thời gian khám dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="trieuChung">Giá trị triệu chứng dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="chanDoan">Giá trị chẩn đoán dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="keHoachXuLy">Giá trị kế hoạch xử lý dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="trangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="ngayXacNhan">Giá trị ngày xác nhận dùng khi thực hiện thao tác; kiểu DateTime?.</param>
        /// <param name="ghiChu">Giá trị ghi chú dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="khongKeThuoc">Giá trị không kê thuốc dùng khi thực hiện thao tác; kiểu int.</param>
        /// <param name="lyDoKhongKeThuoc">Giá trị lý do không kê thuốc dùng khi thực hiện thao tác; kiểu string?.</param>
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
