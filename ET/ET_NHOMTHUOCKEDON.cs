using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>Hợp đồng dữ liệu bảng NHOM_THUOC_KE_DON; không tự ghi SQL.</summary>
    [Table("NHOM_THUOC_KE_DON")]
    public class ET_NHOMTHUOCKEDON
    {
        /// <summary>
        /// Mã nhóm thuốc được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maNhomThuoc = string.Empty;
        /// <summary>
        /// Tên nhóm được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string tenNhom = string.Empty;
        /// <summary>
        /// Mô tả được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? moTa;
        /// <summary>
        /// Trạng thái được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string trangThai = string.Empty;
        /// <summary>
        /// Khởi tạo nhóm thuốc kê đơn.
        /// </summary>
        public ET_NHOMTHUOCKEDON() { }
        /// <summary>
        /// Khởi tạo nhóm thuốc kê đơn. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
        /// </summary>
        /// <param name="maNhomThuoc">Mã nhóm thuốc xác định đối tượng cần xử lý.</param>
        /// <param name="tenNhom">Giá trị tên nhóm dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="moTa">Giá trị mô tả dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="trangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
        public ET_NHOMTHUOCKEDON(string maNhomThuoc, string tenNhom, string? moTa, string trangThai)
        {
            this.maNhomThuoc = maNhomThuoc;
            this.tenNhom = tenNhom;
            this.moTa = moTa;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Giá trị Mã nhóm thuốc; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        [ExplicitKey]
        public string MaNhomThuoc { get => maNhomThuoc; set => maNhomThuoc = value; }
        /// <summary>
        /// Giá trị Tên nhóm; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TenNhom { get => tenNhom; set => tenNhom = value; }
        /// <summary>
        /// Giá trị Mô tả; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MoTa { get => moTa; set => moTa = value; }
        /// <summary>
        /// Giá trị Trạng thái; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
