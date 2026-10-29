// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng CHI_TIET_KE_HOACH qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("CHI_TIET_KE_HOACH")]
    public class ET_CHITIETKEHOACH
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NoiDungBuoc, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? noiDungBuoc;

        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaCTKH, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maCTKH = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaKHDT, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maKHDT = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaDV, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maDV = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính LanThu, dùng bởi getter/setter tương ứng.
        /// </summary>
        private int lanThu;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính SoLuongDuKien, dùng bởi getter/setter tương ứng.
        /// </summary>
        private decimal soLuongDuKien;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính DonGiaDuKien, dùng bởi getter/setter tương ứng.
        /// </summary>
        private decimal donGiaDuKien;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính ThanhTienDuKien, dùng bởi getter/setter tương ứng.
        /// </summary>
        private decimal thanhTienDuKien;
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
        public ET_CHITIETKEHOACH() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_CHITIETKEHOACH(string maCTKH, string maKHDT, string maDV, int lanThu, decimal soLuongDuKien, decimal donGiaDuKien, decimal thanhTienDuKien, string trangThai, string? ghiChu)
        {
            this.maCTKH = maCTKH;
            this.maKHDT = maKHDT;
            this.maDV = maDV;
            this.lanThu = lanThu;
            this.soLuongDuKien = soLuongDuKien;
            this.donGiaDuKien = donGiaDuKien;
            this.thanhTienDuKien = thanhTienDuKien;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã bước chi tiết kế hoạch; ánh xạ cột MaCTKH của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaCTKH { get => maCTKH; set => maCTKH = value; }
        /// <summary>
        /// Mã kế hoạch điều trị; ánh xạ cột MaKHDT của bảng trong TableAttribute.
        /// </summary>
        public string MaKHDT { get => maKHDT; set => maKHDT = value; }
        /// <summary>
        /// Mã dịch vụ; ánh xạ cột MaDV của bảng trong TableAttribute.
        /// </summary>
        public string MaDV { get => maDV; set => maDV = value; }
        /// <summary>
        /// Số thứ tự lần/bước điều trị; ánh xạ cột LanThu của bảng trong TableAttribute.
        /// </summary>
        public int LanThu { get => lanThu; set => lanThu = value; }
        /// <summary>
        /// Số lượng dự kiến của bước điều trị; ánh xạ cột SoLuongDuKien của bảng trong TableAttribute.
        /// </summary>
        public decimal SoLuongDuKien { get => soLuongDuKien; set => soLuongDuKien = value; }
        /// <summary>
        /// Đơn giá dự kiến của bước kế hoạch; ánh xạ cột DonGiaDuKien của bảng trong TableAttribute.
        /// </summary>
        public decimal DonGiaDuKien { get => donGiaDuKien; set => donGiaDuKien = value; }
        /// <summary>
        /// Thành tiền dự kiến của bước do SQL tính; ánh xạ cột ThanhTienDuKien của bảng trong TableAttribute. Chỉ đọc kết quả SQL, Dapper.Contrib không đưa cột này vào INSERT/UPDATE.
        /// </summary>
        [Write(false)]
        public decimal ThanhTienDuKien { get => thanhTienDuKien; set => thanhTienDuKien = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
        /// <summary>
        /// Ghi chú bổ sung; ánh xạ cột GhiChu của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }

        // Cột bổ sung P03; constructor cũ được giữ để tương thích mã gọi hiện có.
        public string? NoiDungBuoc { get => noiDungBuoc; set => noiDungBuoc = value; }
        /// <summary>
        /// Khởi tạo chi tiết kế hoạch. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
        /// </summary>
        /// <param name="maCTKH">Mã chi tiết kế hoạch xác định đối tượng cần xử lý.</param>
        /// <param name="maKHDT">Mã kế hoạch điều trị xác định đối tượng cần xử lý.</param>
        /// <param name="maDV">Mã dịch vụ xác định đối tượng cần xử lý.</param>
        /// <param name="lanThu">Giá trị lần thứ dùng khi thực hiện thao tác; kiểu int.</param>
        /// <param name="soLuongDuKien">Giá trị số lượng dự kiến dùng khi thực hiện thao tác; kiểu decimal.</param>
        /// <param name="donGiaDuKien">Giá trị đơn giá dự kiến dùng khi thực hiện thao tác; kiểu decimal.</param>
        /// <param name="thanhTienDuKien">Giá trị thành tiền dự kiến dùng khi thực hiện thao tác; kiểu decimal.</param>
        /// <param name="trangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="ghiChu">Giá trị ghi chú dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="noiDungBuoc">Giá trị nội dung bước dùng khi thực hiện thao tác; kiểu string?.</param>
        public ET_CHITIETKEHOACH(string maCTKH, string maKHDT, string maDV, int lanThu, decimal soLuongDuKien, decimal donGiaDuKien, decimal thanhTienDuKien, string trangThai, string? ghiChu, string? noiDungBuoc)
        {
            this.maCTKH = maCTKH;
            this.maKHDT = maKHDT;
            this.maDV = maDV;
            this.lanThu = lanThu;
            this.soLuongDuKien = soLuongDuKien;
            this.donGiaDuKien = donGiaDuKien;
            this.thanhTienDuKien = thanhTienDuKien;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
            this.noiDungBuoc = noiDungBuoc;
        }
    }
}
