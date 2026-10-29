// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng HO_SO_BENH_AN qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("HO_SO_BENH_AN")]
    public class ET_HOSOBENHAN
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaHS, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maHS = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaBN, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maBN = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgayLap, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime ngayLap;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính LyDoKhamBanDau, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string lyDoKhamBanDau = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính GhiChu, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? ghiChu;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TrangThai, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_HOSOBENHAN() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_HOSOBENHAN(string maHS, string maBN, DateTime ngayLap, string lyDoKhamBanDau, string? ghiChu, string trangThai)
        {
            this.maHS = maHS;
            this.maBN = maBN;
            this.ngayLap = ngayLap;
            this.lyDoKhamBanDau = lyDoKhamBanDau;
            this.ghiChu = ghiChu;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã hồ sơ bệnh án; ánh xạ cột MaHS của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaHS { get => maHS; set => maHS = value; }
        /// <summary>
        /// Mã bệnh nhân; ánh xạ cột MaBN của bảng trong TableAttribute.
        /// </summary>
        public string MaBN { get => maBN; set => maBN = value; }
        /// <summary>
        /// Thời điểm lập bản ghi; ánh xạ cột NgayLap của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayLap { get => ngayLap; set => ngayLap = value; }
        /// <summary>
        /// Lý do khám ban đầu trên bệnh án; ánh xạ cột LyDoKhamBanDau của bảng trong TableAttribute.
        /// </summary>
        public string LyDoKhamBanDau { get => lyDoKhamBanDau; set => lyDoKhamBanDau = value; }
        /// <summary>
        /// Ghi chú bổ sung; ánh xạ cột GhiChu của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
