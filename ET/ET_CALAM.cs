// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng CA_LAM qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("CA_LAM")]
    public class ET_CALAM
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaCa, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maCa = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính GioBatDau, dùng bởi getter/setter tương ứng.
        /// </summary>
        private TimeSpan gioBatDau;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính GioKetThuc, dùng bởi getter/setter tương ứng.
        /// </summary>
        private TimeSpan gioKetThuc;
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
        public ET_CALAM() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_CALAM(string maCa, TimeSpan gioBatDau, TimeSpan gioKetThuc, string trangThai, string? ghiChu)
        {
            this.maCa = maCa;
            this.gioBatDau = gioBatDau;
            this.gioKetThuc = gioKetThuc;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã ca làm việc; ánh xạ cột MaCa của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaCa { get => maCa; set => maCa = value; }
        /// <summary>
        /// Giờ bắt đầu ca; ánh xạ cột GioBatDau của bảng trong TableAttribute.
        /// </summary>
        public TimeSpan GioBatDau { get => gioBatDau; set => gioBatDau = value; }
        /// <summary>
        /// Giờ kết thúc ca; ánh xạ cột GioKetThuc của bảng trong TableAttribute.
        /// </summary>
        public TimeSpan GioKetThuc { get => gioKetThuc; set => gioKetThuc = value; }
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
