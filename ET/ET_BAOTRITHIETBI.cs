// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng BAO_TRI_THIET_BI qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("BAO_TRI_THIET_BI")]
    public class ET_BAOTRITHIETBI
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaBT, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maBT = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaTB, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maTB = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaNV, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maNV = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgayBatDau, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime ngayBatDau;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgayKetThuc, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime? ngayKetThuc;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NoiDung, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string noiDung = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính ChiPhi, dùng bởi getter/setter tương ứng.
        /// </summary>
        private decimal chiPhi;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgayBaoTriTiepTheo, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime? ngayBaoTriTiepTheo;
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
        public ET_BAOTRITHIETBI() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_BAOTRITHIETBI(string maBT, string maTB, string maNV, DateTime ngayBatDau, DateTime? ngayKetThuc, string noiDung, decimal chiPhi, DateTime? ngayBaoTriTiepTheo, string trangThai, string? ghiChu)
        {
            this.maBT = maBT;
            this.maTB = maTB;
            this.maNV = maNV;
            this.ngayBatDau = ngayBatDau;
            this.ngayKetThuc = ngayKetThuc;
            this.noiDung = noiDung;
            this.chiPhi = chiPhi;
            this.ngayBaoTriTiepTheo = ngayBaoTriTiepTheo;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã bản bảo trì thiết bị; ánh xạ cột MaBT của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaBT { get => maBT; set => maBT = value; }
        /// <summary>
        /// Mã thiết bị; ánh xạ cột MaTB của bảng trong TableAttribute.
        /// </summary>
        public string MaTB { get => maTB; set => maTB = value; }
        /// <summary>
        /// Mã nhân viên; ánh xạ cột MaNV của bảng trong TableAttribute.
        /// </summary>
        public string MaNV { get => maNV; set => maNV = value; }
        /// <summary>
        /// Ngày bắt đầu kế hoạch; ánh xạ cột NgayBatDau của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayBatDau { get => ngayBatDau; set => ngayBatDau = value; }
        /// <summary>
        /// Ngày kết thúc kế hoạch; ánh xạ cột NgayKetThuc của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? NgayKetThuc { get => ngayKetThuc; set => ngayKetThuc = value; }
        /// <summary>Giờ SQL xác nhận phiếu; hệ thống ghi, không nhập hoặc sửa tay.</summary>
        public DateTime? NgayXacNhan { get; set; }
        /// <summary>Giờ bắt đầu thực hiện thực tế; khác ngày dự kiến NgayBatDau.</summary>
        public DateTime? NgayThucHien { get; set; }
        /// <summary>
        /// Nội dung chứng từ hoặc khoản thu; ánh xạ cột NoiDung của bảng trong TableAttribute.
        /// </summary>
        public string NoiDung { get => noiDung; set => noiDung = value; }
        /// <summary>
        /// Chi phí bảo trì; ánh xạ cột ChiPhi của bảng trong TableAttribute.
        /// </summary>
        public decimal ChiPhi { get => chiPhi; set => chiPhi = value; }
        /// <summary>
        /// Ngày dự kiến bảo trì thiết bị lần sau; ánh xạ cột NgayBaoTriTiepTheo của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? NgayBaoTriTiepTheo { get => ngayBaoTriTiepTheo; set => ngayBaoTriTiepTheo = value; }
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
