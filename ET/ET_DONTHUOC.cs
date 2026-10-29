// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng DON_THUOC qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("DON_THUOC")]
    public class ET_DONTHUOC
    {
        private string? maDTThayThe;

        private string maDT = string.Empty;
        private string maLS = string.Empty;
        private string maBS = string.Empty;
        private DateTime ngayKe;
        private string? lyDoThayThe;
        private string trangThai = string.Empty;
        private string? ghiChu;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_DONTHUOC() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_DONTHUOC(string maDT, string maLS, string maBS, DateTime ngayKe, string? lyDoThayThe, string trangThai, string? ghiChu)
        {
            this.maDT = maDT;
            this.maLS = maLS;
            this.maBS = maBS;
            this.ngayKe = ngayKe;
            this.lyDoThayThe = lyDoThayThe;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã đơn thuốc; ánh xạ cột MaDT của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaDT { get => maDT; set => maDT = value; }
        /// <summary>
        /// Mã lần khám trong lịch sử khám bệnh; ánh xạ cột MaLS của bảng trong TableAttribute.
        /// </summary>
        public string MaLS { get => maLS; set => maLS = value; }
        /// <summary>
        /// Mã bác sĩ, liên kết danh mục nhân viên; ánh xạ cột MaBS của bảng trong TableAttribute.
        /// </summary>
        public string MaBS { get => maBS; set => maBS = value; }
        /// <summary>
        /// Ngày/giờ kê đơn thuốc; ánh xạ cột NgayKe của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayKe { get => ngayKe; set => ngayKe = value; }
        /// <summary>
        /// Lý do thay đổi chỉ định đơn thuốc; ánh xạ cột LyDoThayThe của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? LyDoThayThe { get => lyDoThayThe; set => lyDoThayThe = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
        /// <summary>
        /// Ghi chú bổ sung; ánh xạ cột GhiChu của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }

        // Cột bổ sung P03; constructor cũ được giữ để tương thích mã gọi hiện có.
        public string? MaDTThayThe { get => maDTThayThe; set => maDTThayThe = value; }
        public ET_DONTHUOC(string maDT, string maLS, string maBS, DateTime ngayKe, string? lyDoThayThe, string trangThai, string? ghiChu, string? maDTThayThe)
        {
            this.maDT = maDT;
            this.maLS = maLS;
            this.maBS = maBS;
            this.ngayKe = ngayKe;
            this.lyDoThayThe = lyDoThayThe;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
            this.maDTThayThe = maDTThayThe;
        }
    }
}
