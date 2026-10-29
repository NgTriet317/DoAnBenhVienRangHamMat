// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng KHO qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("KHO")]
    public class ET_KHO
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaKho, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maKho = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TenKho, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string tenKho = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaNV, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maNV = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính LoaiKho, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string loaiKho = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TrangThai, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_KHO() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_KHO(string maKho, string tenKho, string maNV, string loaiKho, string trangThai)
        {
            this.maKho = maKho;
            this.tenKho = tenKho;
            this.maNV = maNV;
            this.loaiKho = loaiKho;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã kho; ánh xạ cột MaKho của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaKho { get => maKho; set => maKho = value; }
        /// <summary>
        /// Tên kho; ánh xạ cột TenKho của bảng trong TableAttribute.
        /// </summary>
        public string TenKho { get => tenKho; set => tenKho = value; }
        /// <summary>
        /// Mã nhân viên; ánh xạ cột MaNV của bảng trong TableAttribute.
        /// </summary>
        public string MaNV { get => maNV; set => maNV = value; }
        /// <summary>
        /// Loại kho; ánh xạ cột LoaiKho của bảng trong TableAttribute.
        /// </summary>
        public string LoaiKho { get => loaiKho; set => loaiKho = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
