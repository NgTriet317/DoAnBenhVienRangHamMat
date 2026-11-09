using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>Hợp đồng dữ liệu bảng CHI_TIET_MAU_KE_HOACH; không tự ghi SQL.</summary>
    [Table("CHI_TIET_MAU_KE_HOACH")]
    public class ET_CHITIETMAUKEHOACH
    {
        /// <summary>
        /// Mã chi tiết mẫu được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maCTMau = string.Empty;
        /// <summary>
        /// Mã mẫu kế hoạch điều trị được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maMauKH = string.Empty;
        /// <summary>
        /// Mã dịch vụ được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maDV = string.Empty;
        /// <summary>
        /// Thứ tự sắp xếp mục trong danh sách được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private int thuTu;
        /// <summary>
        /// Số lần hoặc số bước tương ứng trong kế hoạch được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private int lanThu;
        /// <summary>
        /// Số lượng dự kiến của bước kế hoạch được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private decimal soLuongDuKien;
        /// <summary>
        /// Nội dung được lưu hoặc hiển thị cho mục này được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string noiDung = string.Empty;
        /// <summary>
        /// Khởi tạo chi tiết mẫu kế hoạch.
        /// </summary>
        public ET_CHITIETMAUKEHOACH() { }
        /// <summary>
        /// Khởi tạo chi tiết mẫu kế hoạch. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
        /// </summary>
        /// <param name="maCTMau">Mã chi tiết mẫu xác định đối tượng cần xử lý.</param>
        /// <param name="maMauKH">Mã mẫu kế hoạch xác định đối tượng cần xử lý.</param>
        /// <param name="maDV">Mã dịch vụ xác định đối tượng cần xử lý.</param>
        /// <param name="thuTu">Giá trị thu từ dùng khi thực hiện thao tác; kiểu int.</param>
        /// <param name="lanThu">Giá trị lần thứ dùng khi thực hiện thao tác; kiểu int.</param>
        /// <param name="soLuongDuKien">Giá trị số lượng dự kiến dùng khi thực hiện thao tác; kiểu decimal.</param>
        /// <param name="noiDung">Giá trị nội dung dùng khi thực hiện thao tác; kiểu string.</param>
        public ET_CHITIETMAUKEHOACH(string maCTMau, string maMauKH, string maDV, int thuTu, int lanThu, decimal soLuongDuKien, string noiDung)
        {
            this.maCTMau = maCTMau;
            this.maMauKH = maMauKH;
            this.maDV = maDV;
            this.thuTu = thuTu;
            this.lanThu = lanThu;
            this.soLuongDuKien = soLuongDuKien;
            this.noiDung = noiDung;
        }

        /// <summary>
        /// Giá trị Mã chi tiết mẫu; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        [ExplicitKey]
        public string MaCTMau { get => maCTMau; set => maCTMau = value; }
        /// <summary>
        /// Giá trị Mã mẫu kế hoạch điều trị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string MaMauKH { get => maMauKH; set => maMauKH = value; }
        /// <summary>
        /// Giá trị Mã dịch vụ; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string MaDV { get => maDV; set => maDV = value; }
        /// <summary>
        /// Giá trị Thứ tự sắp xếp mục trong danh sách; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public int ThuTu { get => thuTu; set => thuTu = value; }
        /// <summary>
        /// Giá trị Số lần hoặc số bước tương ứng trong kế hoạch; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public int LanThu { get => lanThu; set => lanThu = value; }
        /// <summary>
        /// Giá trị Số lượng dự kiến của bước kế hoạch; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public decimal SoLuongDuKien { get => soLuongDuKien; set => soLuongDuKien = value; }
        /// <summary>
        /// Giá trị Nội dung được lưu hoặc hiển thị cho mục này; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string NoiDung { get => noiDung; set => noiDung = value; }
    }
}
