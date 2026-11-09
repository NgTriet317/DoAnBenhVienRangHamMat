// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng HANG_HOA qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("HANG_HOA")]
    public class ET_HANGHOA
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaHH, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maHH = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TenHH, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string tenHH = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaLoaiHH, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maLoaiHH = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính DonViTinh, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string donViTinh = string.Empty;
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
        public ET_HANGHOA() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_HANGHOA(string maHH, string tenHH, string maLoaiHH, string donViTinh, string moTa, string trangThai)
        {
            this.maHH = maHH;
            this.tenHH = tenHH;
            this.maLoaiHH = maLoaiHH;
            this.donViTinh = donViTinh;
            this.moTa = moTa;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã hàng hóa; ánh xạ cột MaHH của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaHH { get => maHH; set => maHH = value; }
        /// <summary>
        /// Tên hàng hóa; ánh xạ cột TenHH của bảng trong TableAttribute.
        /// </summary>
        public string TenHH { get => tenHH; set => tenHH = value; }
        /// <summary>
        /// Mã loại hàng hóa; ánh xạ cột MaLoaiHH của bảng trong TableAttribute.
        /// </summary>
        public string MaLoaiHH { get => maLoaiHH; set => maLoaiHH = value; }
        /// <summary>
        /// Đơn vị tính của hàng hóa; ánh xạ cột DonViTinh của bảng trong TableAttribute.
        /// </summary>
        public string DonViTinh { get => donViTinh; set => donViTinh = value; }
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
