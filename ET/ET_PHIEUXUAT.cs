// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng PHIEU_XUAT qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("PHIEU_XUAT")]
    public class ET_PHIEUXUAT
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaPX, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maPX = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaKho, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maKho = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaNV, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maNV = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaKhoa, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? maKhoa;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaPhong, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? maPhong;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaLS, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? maLS;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgayXuat, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime ngayXuat;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính LyDo, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string lyDo = string.Empty;
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
        public ET_PHIEUXUAT() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_PHIEUXUAT(string maPX, string maKho, string maNV, string? maKhoa, string? maPhong, string? maLS, DateTime ngayXuat, string lyDo, string trangThai, string? ghiChu)
        {
            this.maPX = maPX;
            this.maKho = maKho;
            this.maNV = maNV;
            this.maKhoa = maKhoa;
            this.maPhong = maPhong;
            this.maLS = maLS;
            this.ngayXuat = ngayXuat;
            this.lyDo = lyDo;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã phiếu xuất kho; ánh xạ cột MaPX của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaPX { get => maPX; set => maPX = value; }
        /// <summary>
        /// Mã kho; ánh xạ cột MaKho của bảng trong TableAttribute.
        /// </summary>
        public string MaKho { get => maKho; set => maKho = value; }
        /// <summary>
        /// Mã nhân viên; ánh xạ cột MaNV của bảng trong TableAttribute.
        /// </summary>
        public string MaNV { get => maNV; set => maNV = value; }
        /// <summary>
        /// Mã khoa bệnh viện; ánh xạ cột MaKhoa của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaKhoa { get => maKhoa; set => maKhoa = value; }
        /// <summary>
        /// Mã phòng; ánh xạ cột MaPhong của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaPhong { get => maPhong; set => maPhong = value; }
        /// <summary>
        /// Mã lần khám trong lịch sử khám bệnh; ánh xạ cột MaLS của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaLS { get => maLS; set => maLS = value; }
        /// <summary>
        /// Ngày/giờ xuất kho; ánh xạ cột NgayXuat của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayXuat { get => ngayXuat; set => ngayXuat = value; }
        /// <summary>
        /// Lý do của chứng từ/giao dịch; ánh xạ cột LyDo của bảng trong TableAttribute.
        /// </summary>
        public string LyDo { get => lyDo; set => lyDo = value; }
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
