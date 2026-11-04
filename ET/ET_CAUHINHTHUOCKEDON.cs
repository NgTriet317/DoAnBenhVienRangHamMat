using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>Hợp đồng dữ liệu bảng CAU_HINH_THUOC_KE_DON; không tự ghi SQL.</summary>
    [Table("CAU_HINH_THUOC_KE_DON")]
    public class ET_CAUHINHTHUOCKEDON
    {
        /// <summary>
        /// Mã thuốc được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maThuoc = string.Empty;
        /// <summary>
        /// Mã nhóm thuốc được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? maNhomThuoc;
        /// <summary>
        /// Đổi tượng áp dụng được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? doiTuongApDung;
        /// <summary>
        /// Đường dùng thuốc được lưu cùng dòng kê đơn được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? duongDung;
        /// <summary>
        /// Liều dùng gợi Y được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? lieuDungGoiY;
        /// <summary>
        /// Hướng dẫn gợi Y được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? huongDanGoiY;
        /// <summary>
        /// Phiên bản được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private int phienBan;
        /// <summary>
        /// Trạng thái được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string trangThai = string.Empty;
        /// <summary>
        /// Mã người duyệt được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? maNguoiDuyet;
        /// <summary>
        /// Ngày duyệt được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DateTime? ngayDuyet;
        /// <summary>
        /// Khởi tạo cấu hình thuốc kê đơn.
        /// </summary>
        public ET_CAUHINHTHUOCKEDON() { }
        /// <summary>
        /// Khởi tạo cấu hình thuốc kê đơn. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
        /// </summary>
        /// <param name="maThuoc">Mã thuốc xác định đối tượng cần xử lý.</param>
        /// <param name="maNhomThuoc">Mã nhóm thuốc xác định đối tượng cần xử lý.</param>
        /// <param name="doiTuongApDung">Giá trị đổi tượng áp dụng dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="duongDung">Giá trị đường dùng dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="lieuDungGoiY">Giá trị liều dùng gợi Y dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="huongDanGoiY">Giá trị hướng dẫn gợi Y dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="phienBan">Giá trị phiên bản dùng khi thực hiện thao tác; kiểu int.</param>
        /// <param name="trangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="maNguoiDuyet">Mã người duyệt xác định đối tượng cần xử lý.</param>
        /// <param name="ngayDuyet">Giá trị ngày duyệt dùng khi thực hiện thao tác; kiểu DateTime?.</param>
        public ET_CAUHINHTHUOCKEDON(string maThuoc, string? maNhomThuoc, string? doiTuongApDung, string? duongDung, string? lieuDungGoiY, string? huongDanGoiY, int phienBan, string trangThai, string? maNguoiDuyet, DateTime? ngayDuyet)
        {
            this.maThuoc = maThuoc;
            this.maNhomThuoc = maNhomThuoc;
            this.doiTuongApDung = doiTuongApDung;
            this.duongDung = duongDung;
            this.lieuDungGoiY = lieuDungGoiY;
            this.huongDanGoiY = huongDanGoiY;
            this.phienBan = phienBan;
            this.trangThai = trangThai;
            this.maNguoiDuyet = maNguoiDuyet;
            this.ngayDuyet = ngayDuyet;
        }

        /// <summary>
        /// Giá trị Mã thuốc; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        [ExplicitKey]
        public string MaThuoc { get => maThuoc; set => maThuoc = value; }
        /// <summary>
        /// Giá trị Mã nhóm thuốc; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaNhomThuoc { get => maNhomThuoc; set => maNhomThuoc = value; }
        /// <summary>
        /// Giá trị Đổi tượng áp dụng; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? DoiTuongApDung { get => doiTuongApDung; set => doiTuongApDung = value; }
        /// <summary>
        /// Giá trị Đường dùng thuốc được lưu cùng dòng kê đơn; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? DuongDung { get => duongDung; set => duongDung = value; }
        /// <summary>
        /// Giá trị Liều dùng gợi Y; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? LieuDungGoiY { get => lieuDungGoiY; set => lieuDungGoiY = value; }
        /// <summary>
        /// Giá trị Hướng dẫn gợi Y; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? HuongDanGoiY { get => huongDanGoiY; set => huongDanGoiY = value; }
        /// <summary>
        /// Giá trị Phiên bản; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public int PhienBan { get => phienBan; set => phienBan = value; }
        /// <summary>
        /// Giá trị Trạng thái; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
        /// <summary>
        /// Giá trị Mã người duyệt; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaNguoiDuyet { get => maNguoiDuyet; set => maNguoiDuyet = value; }
        /// <summary>
        /// Giá trị Ngày duyệt; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? NgayDuyet { get => ngayDuyet; set => ngayDuyet = value; }
    }
}
