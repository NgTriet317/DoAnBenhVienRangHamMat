// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng LOAI_HANG_HOA qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("LOAI_HANG_HOA")]
    public class ET_LOAIHANGHOA
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaLoaiHH, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maLoaiHH = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TenLoaiHH, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string tenLoaiHH = string.Empty;
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
        public ET_LOAIHANGHOA() { }
        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_LOAIHANGHOA(string maLoaiHH, string tenLoaiHH, string moTa, string trangThai)
        {
            this.maLoaiHH = maLoaiHH;
            this.tenLoaiHH = tenLoaiHH;
            this.moTa = moTa;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã loại hàng hóa; ánh xạ cột MaLoaiHH của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaLoaiHH { get => maLoaiHH; set => maLoaiHH = value; }
        /// <summary>
        /// Tên loại hàng hóa; ánh xạ cột TenLoaiHH của bảng trong TableAttribute.
        /// </summary>
        public string TenLoaiHH { get => tenLoaiHH; set => tenLoaiHH = value; }
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