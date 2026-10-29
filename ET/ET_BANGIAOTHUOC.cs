using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>Hợp đồng dữ liệu bảng BAN_GIAO_THUOC; không tự ghi SQL.</summary>
    [Table("BAN_GIAO_THUOC")]
    public class ET_BANGIAOTHUOC
    {
        /// <summary>
        /// Mã đơn thuốc được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maDT = string.Empty;
        /// <summary>
        /// Mã hóa đơn được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maHD = string.Empty;
        /// <summary>
        /// Mã phiếu xuất được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maPX = string.Empty;
        /// <summary>
        /// Trạng thái được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string trangThai = string.Empty;
        /// <summary>
        /// Mã nhân viên xác nhận bàn giao thuốc được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? maNVGiao;
        /// <summary>
        /// Thời điểm xác nhận bàn giao thuốc được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DateTime? thoiGianGiao;
        /// <summary>
        /// Nội dung ghi chú bổ sung của bản ghi được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? ghiChu;
        /// <summary>
        /// Khởi tạo bàn giao thuốc.
        /// </summary>
        public ET_BANGIAOTHUOC() { }
        /// <summary>
        /// Khởi tạo bàn giao thuốc. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
        /// </summary>
        /// <param name="maDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
        /// <param name="maHD">Mã hóa đơn xác định đối tượng cần xử lý.</param>
        /// <param name="maPX">Mã phiếu xuất xác định đối tượng cần xử lý.</param>
        /// <param name="trangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="maNVGiao">Mã nhân viên giao xác định đối tượng cần xử lý.</param>
        /// <param name="thoiGianGiao">Giá trị thời gian giao dùng khi thực hiện thao tác; kiểu DateTime?.</param>
        /// <param name="ghiChu">Giá trị ghi chú dùng khi thực hiện thao tác; kiểu string?.</param>
        public ET_BANGIAOTHUOC(string maDT, string maHD, string maPX, string trangThai, string? maNVGiao, DateTime? thoiGianGiao, string? ghiChu)
        {
            this.maDT = maDT;
            this.maHD = maHD;
            this.maPX = maPX;
            this.trangThai = trangThai;
            this.maNVGiao = maNVGiao;
            this.thoiGianGiao = thoiGianGiao;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Giá trị Mã đơn thuốc; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        [ExplicitKey]
        public string MaDT { get => maDT; set => maDT = value; }
        /// <summary>
        /// Giá trị Mã hóa đơn; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string MaHD { get => maHD; set => maHD = value; }
        /// <summary>
        /// Giá trị Mã phiếu xuất; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string MaPX { get => maPX; set => maPX = value; }
        /// <summary>
        /// Giá trị Trạng thái; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
        /// <summary>
        /// Giá trị Mã nhân viên xác nhận bàn giao thuốc; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaNVGiao { get => maNVGiao; set => maNVGiao = value; }
        /// <summary>
        /// Giá trị Thời điểm xác nhận bàn giao thuốc; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? ThoiGianGiao { get => thoiGianGiao; set => thoiGianGiao = value; }
        /// <summary>
        /// Giá trị Nội dung ghi chú bổ sung của bản ghi; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
