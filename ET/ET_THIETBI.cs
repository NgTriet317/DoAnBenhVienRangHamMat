// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng THIET_BI qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("THIET_BI")]
    public class ET_THIETBI
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaTB, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maTB = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaHH, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maHH = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính SoSerial, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string soSerial = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgayMua, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime? ngayMua;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaNCC, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? maNCC;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaPhong, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? maPhong;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TinhTrang, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string tinhTrang = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính HanBaoHanh, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime? hanBaoHanh;
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
        public ET_THIETBI() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_THIETBI(string maTB, string maHH, string soSerial, DateTime? ngayMua, string? maNCC, string? maPhong, string tinhTrang, DateTime? hanBaoHanh, string trangThai, string? ghiChu)
        {
            this.maTB = maTB;
            this.maHH = maHH;
            this.soSerial = soSerial;
            this.ngayMua = ngayMua;
            this.maNCC = maNCC;
            this.maPhong = maPhong;
            this.tinhTrang = tinhTrang;
            this.hanBaoHanh = hanBaoHanh;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã thiết bị; ánh xạ cột MaTB của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaTB { get => maTB; set => maTB = value; }
        /// <summary>
        /// Mã hàng hóa; ánh xạ cột MaHH của bảng trong TableAttribute.
        /// </summary>
        public string MaHH { get => maHH; set => maHH = value; }
        /// <summary>
        /// Số serial thiết bị; ánh xạ cột SoSerial của bảng trong TableAttribute.
        /// </summary>
        public string SoSerial { get => soSerial; set => soSerial = value; }
        /// <summary>
        /// Ngày mua thiết bị; ánh xạ cột NgayMua của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? NgayMua { get => ngayMua; set => ngayMua = value; }
        /// <summary>
        /// Mã nhà cung cấp; ánh xạ cột MaNCC của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaNCC { get => maNCC; set => maNCC = value; }
        /// <summary>
        /// Mã phòng; ánh xạ cột MaPhong của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaPhong { get => maPhong; set => maPhong = value; }
        /// <summary>
        /// Tình trạng sử dụng phòng/thiết bị; ánh xạ cột TinhTrang của bảng trong TableAttribute.
        /// </summary>
        public string TinhTrang { get => tinhTrang; set => tinhTrang = value; }
        /// <summary>
        /// Hạn bảo hành thiết bị; ánh xạ cột HanBaoHanh của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? HanBaoHanh { get => hanBaoHanh; set => hanBaoHanh = value; }
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