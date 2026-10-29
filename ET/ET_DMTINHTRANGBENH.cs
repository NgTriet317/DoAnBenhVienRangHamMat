using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>Hợp đồng dữ liệu bảng DM_TINH_TRANG_BENH; không tự ghi SQL.</summary>
    [Table("DM_TINH_TRANG_BENH")]
    public class ET_DMTINHTRANGBENH
    {
        /// <summary>
        /// Mã tình trạng trong danh mục tham khảo được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maDMTT = string.Empty;
        /// <summary>
        /// Tên tình trạng được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string tenTinhTrang = string.Empty;
        /// <summary>
        /// Mô tả được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? moTa;
        /// <summary>
        /// Trạng thái được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string trangThai = string.Empty;
        /// <summary>
        /// Khởi tạo danh mục tình trạng bệnh.
        /// </summary>
        public ET_DMTINHTRANGBENH() { }
        /// <summary>
        /// Khởi tạo danh mục tình trạng bệnh. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
        /// </summary>
        /// <param name="maDMTT">Mã danh mục tình trạng xác định đối tượng cần xử lý.</param>
        /// <param name="tenTinhTrang">Giá trị tên tình trạng dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="moTa">Giá trị mô tả dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="trangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
        public ET_DMTINHTRANGBENH(string maDMTT, string tenTinhTrang, string? moTa, string trangThai)
        {
            this.maDMTT = maDMTT;
            this.tenTinhTrang = tenTinhTrang;
            this.moTa = moTa;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Giá trị Mã tình trạng trong danh mục tham khảo; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        [ExplicitKey]
        public string MaDMTT { get => maDMTT; set => maDMTT = value; }
        /// <summary>
        /// Giá trị Tên tình trạng; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TenTinhTrang { get => tenTinhTrang; set => tenTinhTrang = value; }
        /// <summary>
        /// Giá trị Mô tả; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MoTa { get => moTa; set => moTa = value; }
        /// <summary>
        /// Giá trị Trạng thái; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
        /// <summary>
        /// Giá trị Nhóm; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        [Write(false)]
        public string Nhom
        {
            get
            {
                if(MoTa?.StartsWith("Nhóm: ",StringComparison.Ordinal)==true)
                {var end=MoTa.IndexOf('.',6);if(end>6)return MoTa[6..end].Trim();}
                return MaDMTT.StartsWith("REF28_",StringComparison.Ordinal)?"Răng và mô quanh răng":"Khác";
            }
        }
    }
}
