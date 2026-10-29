// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng CHUYEN_KHOA qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("CHUYEN_KHOA")]
    public class ET_CHUYENKHOA
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaCK, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maCK = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaKhoa, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maKhoa = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TenCK, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string tenCK = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MoTa, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string moTa = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TrangThai, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_CHUYENKHOA() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_CHUYENKHOA(string maCK, string maKhoa, string tenCK, string moTa, string trangThai)
        {
            this.maCK = maCK;
            this.maKhoa = maKhoa;
            this.tenCK = tenCK;
            this.moTa = moTa;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã chuyên khoa; ánh xạ cột MaCK của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaCK { get => maCK; set => maCK = value; }
        /// <summary>
        /// Mã khoa bệnh viện; ánh xạ cột MaKhoa của bảng trong TableAttribute.
        /// </summary>
        public string MaKhoa { get => maKhoa; set => maKhoa = value; }
        /// <summary>
        /// Tên chuyên khoa; ánh xạ cột TenCK của bảng trong TableAttribute.
        /// </summary>
        public string TenCK { get => tenCK; set => tenCK = value; }
        /// <summary>
        /// Mô tả thông tin danh mục; ánh xạ cột MoTa của bảng trong TableAttribute.
        /// </summary>
        public string MoTa { get => moTa; set => moTa = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}