// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng CHI_TIET_DON_THUOC qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("CHI_TIET_DON_THUOC")]
    public class ET_CHITIETDONTHUOC
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính DuongDung, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? duongDung;
        /// <summary>
        /// Tên thuốc chốt được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? tenThuocChot;
        /// <summary>
        /// Hàm lượng chốt được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? hamLuongChot;
        /// <summary>
        /// Đơn vị tính đã chốt được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string? donViTinhChot;
        /// <summary>
        /// Phiên bản hướng dẫn được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private int? phienBanHuongDan;

        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaCTDT, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maCTDT = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaDT, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maDT = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaThuoc, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maThuoc = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính LieuDung, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string lieuDung = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính SoLuong, dùng bởi getter/setter tương ứng.
        /// </summary>
        private decimal soLuong;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính SoNgayDung, dùng bởi getter/setter tương ứng.
        /// </summary>
        private int soNgayDung;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính HuongDan, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string huongDan = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_CHITIETDONTHUOC() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_CHITIETDONTHUOC(string maCTDT, string maDT, string maThuoc, string lieuDung, decimal soLuong, int soNgayDung, string huongDan)
        {
            this.maCTDT = maCTDT;
            this.maDT = maDT;
            this.maThuoc = maThuoc;
            this.lieuDung = lieuDung;
            this.soLuong = soLuong;
            this.soNgayDung = soNgayDung;
            this.huongDan = huongDan;
        }

        /// <summary>
        /// Mã dòng chi tiết đơn thuốc; ánh xạ cột MaCTDT của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaCTDT { get => maCTDT; set => maCTDT = value; }
        /// <summary>
        /// Mã đơn thuốc; ánh xạ cột MaDT của bảng trong TableAttribute.
        /// </summary>
        public string MaDT { get => maDT; set => maDT = value; }
        /// <summary>
        /// Mã thuốc; ánh xạ cột MaThuoc của bảng trong TableAttribute.
        /// </summary>
        public string MaThuoc { get => maThuoc; set => maThuoc = value; }
        /// <summary>
        /// Liều dùng thuốc; ánh xạ cột LieuDung của bảng trong TableAttribute.
        /// </summary>
        public string LieuDung { get => lieuDung; set => lieuDung = value; }
        /// <summary>
        /// Số lượng thuốc/hàng hóa/dịch vụ; ánh xạ cột SoLuong của bảng trong TableAttribute.
        /// </summary>
        public decimal SoLuong { get => soLuong; set => soLuong = value; }
        /// <summary>
        /// Số ngày sử dụng thuốc; ánh xạ cột SoNgayDung của bảng trong TableAttribute.
        /// </summary>
        public int SoNgayDung { get => soNgayDung; set => soNgayDung = value; }
        /// <summary>
        /// Hướng dẫn sử dụng thuốc; ánh xạ cột HuongDan của bảng trong TableAttribute.
        /// </summary>
        public string HuongDan { get => huongDan; set => huongDan = value; }

        // Cột bổ sung P03; constructor cũ được giữ để tương thích mã gọi hiện có.
        public string? DuongDung { get => duongDung; set => duongDung = value; }
        /// <summary>
        /// Giá trị Tên thuốc chốt; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? TenThuocChot { get => tenThuocChot; set => tenThuocChot = value; }
        /// <summary>
        /// Giá trị Hàm lượng chốt; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? HamLuongChot { get => hamLuongChot; set => hamLuongChot = value; }
        /// <summary>
        /// Giá trị Đơn vị tính đã chốt; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? DonViTinhChot { get => donViTinhChot; set => donViTinhChot = value; }
        /// <summary>
        /// Giá trị Phiên bản hướng dẫn; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public int? PhienBanHuongDan { get => phienBanHuongDan; set => phienBanHuongDan = value; }
        /// <summary>
        /// Khởi tạo chi tiết đơn thuốc. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
        /// </summary>
        /// <param name="maCTDT">Mã chi tiết đơn thuốc xác định đối tượng cần xử lý.</param>
        /// <param name="maDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
        /// <param name="maThuoc">Mã thuốc xác định đối tượng cần xử lý.</param>
        /// <param name="lieuDung">Giá trị liều dùng dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="soLuong">Giá trị số lượng dùng khi thực hiện thao tác; kiểu decimal.</param>
        /// <param name="soNgayDung">Giá trị số ngày dụng dùng khi thực hiện thao tác; kiểu int.</param>
        /// <param name="huongDan">Giá trị hướng dẫn dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="duongDung">Giá trị đường dùng dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="tenThuocChot">Giá trị tên thuốc chốt dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="hamLuongChot">Giá trị hàm lượng chốt dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="donViTinhChot">Giá trị đơn vị tính đã chốt dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="phienBanHuongDan">Giá trị phiên bản hướng dẫn dùng khi thực hiện thao tác; kiểu int?.</param>
        public ET_CHITIETDONTHUOC(string maCTDT, string maDT, string maThuoc, string lieuDung, decimal soLuong, int soNgayDung, string huongDan, string? duongDung, string? tenThuocChot, string? hamLuongChot, string? donViTinhChot, int? phienBanHuongDan)
        {
            this.maCTDT = maCTDT;
            this.maDT = maDT;
            this.maThuoc = maThuoc;
            this.lieuDung = lieuDung;
            this.soLuong = soLuong;
            this.soNgayDung = soNgayDung;
            this.huongDan = huongDan;
            this.duongDung = duongDung;
            this.tenThuocChot = tenThuocChot;
            this.hamLuongChot = hamLuongChot;
            this.donViTinhChot = donViTinhChot;
            this.phienBanHuongDan = phienBanHuongDan;
        }
    }
}
