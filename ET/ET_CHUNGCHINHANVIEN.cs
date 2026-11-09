// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng CHUNG_CHI_NHAN_VIEN qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("CHUNG_CHI_NHAN_VIEN")]
    public class ET_CHUNGCHINHANVIEN
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaCC, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maCC = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaNV, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maNV = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính SoChungChi, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string soChungChi = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TenChungChi, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string tenChungChi = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgayCap, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime ngayCap;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NoiCap, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string noiCap = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính HanChungChi, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime? hanChungChi;
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
        public ET_CHUNGCHINHANVIEN() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_CHUNGCHINHANVIEN(string maCC, string maNV, string soChungChi, string tenChungChi, DateTime ngayCap, string noiCap, DateTime? hanChungChi, string trangThai, string? ghiChu)
        {
            this.maCC = maCC;
            this.maNV = maNV;
            this.soChungChi = soChungChi;
            this.tenChungChi = tenChungChi;
            this.ngayCap = ngayCap;
            this.noiCap = noiCap;
            this.hanChungChi = hanChungChi;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã chứng chỉ nhân viên; ánh xạ cột MaCC của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaCC { get => maCC; set => maCC = value; }
        /// <summary>
        /// Mã nhân viên; ánh xạ cột MaNV của bảng trong TableAttribute.
        /// </summary>
        public string MaNV { get => maNV; set => maNV = value; }
        /// <summary>
        /// Số chứng chỉ do đơn vị cấp; ánh xạ cột SoChungChi của bảng trong TableAttribute.
        /// </summary>
        public string SoChungChi { get => soChungChi; set => soChungChi = value; }
        /// <summary>
        /// Tên chứng chỉ; ánh xạ cột TenChungChi của bảng trong TableAttribute.
        /// </summary>
        public string TenChungChi { get => tenChungChi; set => tenChungChi = value; }
        /// <summary>
        /// Ngày cấp chứng chỉ; ánh xạ cột NgayCap của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayCap { get => ngayCap; set => ngayCap = value; }
        /// <summary>
        /// Nơi cấp chứng chỉ; ánh xạ cột NoiCap của bảng trong TableAttribute.
        /// </summary>
        public string NoiCap { get => noiCap; set => noiCap = value; }
        /// <summary>
        /// Ngày hết hạn chứng chỉ; ánh xạ cột HanChungChi của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? HanChungChi { get => hanChungChi; set => hanChungChi = value; }
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