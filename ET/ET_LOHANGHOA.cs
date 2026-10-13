// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System;
using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng LO_HANG_HOA qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("LO_HANG_HOA")]
    public class ET_LOHANGHOA
    {
        private string maLo = string.Empty;
        private string maHH = string.Empty;
        private string maNCC = string.Empty;
        private string maKho = string.Empty;
        private string? soLo;
        private DateTime ngayNhap;
        private DateTime? hanSuDung;
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_LOHANGHOA() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_LOHANGHOA(string maLo, string maHH, string maNCC, string maKho, string? soLo, DateTime ngayNhap, DateTime? hanSuDung, string trangThai)
        {
            this.maLo = maLo;
            this.maHH = maHH;
            this.maNCC = maNCC;
            this.maKho = maKho;
            this.soLo = soLo;
            this.ngayNhap = ngayNhap;
            this.hanSuDung = hanSuDung;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã lô hàng hóa; ánh xạ cột MaLo của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaLo { get => maLo; set => maLo = value; }
        /// <summary>
        /// Mã hàng hóa; ánh xạ cột MaHH của bảng trong TableAttribute.
        /// </summary>
        public string MaHH { get => maHH; set => maHH = value; }
        /// <summary>
        /// Mã nhà cung cấp; ánh xạ cột MaNCC của bảng trong TableAttribute.
        /// </summary>
        public string MaNCC { get => maNCC; set => maNCC = value; }
        /// <summary>
        /// Mã kho; ánh xạ cột MaKho của bảng trong TableAttribute.
        /// </summary>
        public string MaKho { get => maKho; set => maKho = value; }
        /// <summary>
        /// Số lô hàng theo nhà cung cấp; ánh xạ cột SoLo của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? SoLo { get => soLo; set => soLo = value; }
        /// <summary>
        /// Ngày nhập lô/phiếu nhập; ánh xạ cột NgayNhap của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayNhap { get => ngayNhap; set => ngayNhap = value; }
        /// <summary>
        /// Hạn sử dụng lô hàng; ánh xạ cột HanSuDung của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? HanSuDung { get => hanSuDung; set => hanSuDung = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
