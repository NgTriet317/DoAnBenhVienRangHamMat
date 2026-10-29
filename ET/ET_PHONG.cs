// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng PHONG qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("PHONG")]
    public class ET_PHONG
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaPhong, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maPhong = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TenPhong, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string tenPhong = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaKhoa, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maKhoa = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính LoaiPhong, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string loaiPhong = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TinhTrang, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string tinhTrang = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TrangThai, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_PHONG() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_PHONG(string maPhong, string tenPhong, string maKhoa, string loaiPhong, string tinhTrang, string trangThai)
        {
            this.maPhong = maPhong;
            this.tenPhong = tenPhong;
            this.maKhoa = maKhoa;
            this.loaiPhong = loaiPhong;
            this.tinhTrang = tinhTrang;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã phòng; ánh xạ cột MaPhong của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaPhong { get => maPhong; set => maPhong = value; }
        /// <summary>
        /// Tên phòng; ánh xạ cột TenPhong của bảng trong TableAttribute.
        /// </summary>
        public string TenPhong { get => tenPhong; set => tenPhong = value; }
        /// <summary>
        /// Mã khoa bệnh viện; ánh xạ cột MaKhoa của bảng trong TableAttribute.
        /// </summary>
        public string MaKhoa { get => maKhoa; set => maKhoa = value; }
        /// <summary>
        /// Loại phòng; ánh xạ cột LoaiPhong của bảng trong TableAttribute.
        /// </summary>
        public string LoaiPhong { get => loaiPhong; set => loaiPhong = value; }
        /// <summary>
        /// Tình trạng sử dụng phòng/thiết bị; ánh xạ cột TinhTrang của bảng trong TableAttribute.
        /// </summary>
        public string TinhTrang { get => tinhTrang; set => tinhTrang = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
