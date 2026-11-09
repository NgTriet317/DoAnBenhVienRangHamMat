using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>Hợp đồng dữ liệu bảng MAU_KE_HOACH_DIEU_TRI; không tự ghi SQL.</summary>
    [Table("MAU_KE_HOACH_DIEU_TRI")]
    public class ET_MAUKEHOACHDIEUTRI
    {
        /// <summary>
        /// Mã mẫu kế hoạch điều trị được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maMauKH = string.Empty;
        /// <summary>
        /// Mã tình trạng trong danh mục tham khảo được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maDMTT = string.Empty;
        /// <summary>
        /// Mức độ được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? mucDo;
        /// <summary>
        /// Tên mẫu được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string tenMau = string.Empty;
        /// <summary>
        /// Phiên bản được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private int phienBan;
        /// <summary>
        /// Trạng thái được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string trangThai = string.Empty;
        /// <summary>
        /// Mã bác sĩ duyệt được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? maBSDuyet;
        /// <summary>
        /// Ngày duyệt được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DateTime? ngayDuyet;
        /// <summary>
        /// Khởi tạo mẫu kế hoạch điều trị.
        /// </summary>
        public ET_MAUKEHOACHDIEUTRI() { }
        /// <summary>
        /// Khởi tạo mẫu kế hoạch điều trị. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
        /// </summary>
        /// <param name="maMauKH">Mã mẫu kế hoạch xác định đối tượng cần xử lý.</param>
        /// <param name="maDMTT">Mã danh mục tình trạng xác định đối tượng cần xử lý.</param>
        /// <param name="mucDo">Giá trị mức độ dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="tenMau">Giá trị tên mẫu dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="phienBan">Giá trị phiên bản dùng khi thực hiện thao tác; kiểu int.</param>
        /// <param name="trangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="maBSDuyet">Mã bác sĩ duyệt xác định đối tượng cần xử lý.</param>
        /// <param name="ngayDuyet">Giá trị ngày duyệt dùng khi thực hiện thao tác; kiểu DateTime?.</param>
        public ET_MAUKEHOACHDIEUTRI(string maMauKH, string maDMTT, string? mucDo, string tenMau, int phienBan, string trangThai, string? maBSDuyet, DateTime? ngayDuyet)
        {
            this.maMauKH = maMauKH;
            this.maDMTT = maDMTT;
            this.mucDo = mucDo;
            this.tenMau = tenMau;
            this.phienBan = phienBan;
            this.trangThai = trangThai;
            this.maBSDuyet = maBSDuyet;
            this.ngayDuyet = ngayDuyet;
        }

        /// <summary>
        /// Giá trị Mã mẫu kế hoạch điều trị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        [ExplicitKey]
        public string MaMauKH { get => maMauKH; set => maMauKH = value; }
        /// <summary>
        /// Giá trị Mã tình trạng trong danh mục tham khảo; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string MaDMTT { get => maDMTT; set => maDMTT = value; }
        /// <summary>
        /// Giá trị Mức độ; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MucDo { get => mucDo; set => mucDo = value; }
        /// <summary>
        /// Giá trị Tên mẫu; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TenMau { get => tenMau; set => tenMau = value; }
        /// <summary>
        /// Giá trị Phiên bản; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public int PhienBan { get => phienBan; set => phienBan = value; }
        /// <summary>
        /// Giá trị Trạng thái; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
        /// <summary>
        /// Giá trị Mã bác sĩ duyệt; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaBSDuyet { get => maBSDuyet; set => maBSDuyet = value; }
        /// <summary>
        /// Giá trị Ngày duyệt; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? NgayDuyet { get => ngayDuyet; set => ngayDuyet = value; }
    }
}
