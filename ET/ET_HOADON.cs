// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng HOA_DON qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("HOA_DON")]
    public class ET_HOADON
    {
        private int phienBanQuyTrinh = 1;
        private string? maYeuCau;
        private string? dauVanTayYeuCau;

        private string maHD = string.Empty;
        private string maLS = string.Empty;
        private string maNV = string.Empty;
        private DateTime ngayLap;
        private DateTime? ngayXacNhan;
        private decimal tongTien;
        private decimal giamGia;
        private decimal soTienPhaiThu;
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_HOADON() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_HOADON(string maHD, string maLS, string maNV, DateTime ngayLap, DateTime? ngayXacNhan, decimal tongTien, decimal giamGia, decimal soTienPhaiThu, string trangThai)
        {
            this.maHD = maHD;
            this.maLS = maLS;
            this.maNV = maNV;
            this.ngayLap = ngayLap;
            this.ngayXacNhan = ngayXacNhan;
            this.tongTien = tongTien;
            this.giamGia = giamGia;
            this.soTienPhaiThu = soTienPhaiThu;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã hóa đơn bệnh viện; ánh xạ cột MaHD của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaHD { get => maHD; set => maHD = value; }
        /// <summary>
        /// Mã lần khám trong lịch sử khám bệnh; ánh xạ cột MaLS của bảng trong TableAttribute.
        /// </summary>
        public string MaLS { get => maLS; set => maLS = value; }
        /// <summary>
        /// Mã nhân viên; ánh xạ cột MaNV của bảng trong TableAttribute.
        /// </summary>
        public string MaNV { get => maNV; set => maNV = value; }
        /// <summary>
        /// Thời điểm lập bản ghi; ánh xạ cột NgayLap của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayLap { get => ngayLap; set => ngayLap = value; }
        /// <summary>
        /// Thời điểm xác nhận chứng từ; ánh xạ cột NgayXacNhan của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? NgayXacNhan { get => ngayXacNhan; set => ngayXacNhan = value; }
        /// <summary>
        /// Tổng tiền của chứng từ; ánh xạ cột TongTien của bảng trong TableAttribute. Chỉ đọc kết quả SQL, Dapper.Contrib không đưa cột này vào INSERT/UPDATE.
        /// </summary>
        [Write(false)]
        public decimal TongTien { get => tongTien; set => tongTien = value; }
        /// <summary>
        /// Số tiền giảm giá của hóa đơn; ánh xạ cột GiamGia của bảng trong TableAttribute.
        /// </summary>
        public decimal GiamGia { get => giamGia; set => giamGia = value; }
        /// <summary>
        /// Số tiền còn thuộc trách nhiệm thu của hóa đơn sau giảm giá; ánh xạ cột SoTienPhaiThu của bảng trong TableAttribute. Chỉ đọc kết quả SQL, Dapper.Contrib không đưa cột này vào INSERT/UPDATE.
        /// </summary>
        [Write(false)]
        public decimal SoTienPhaiThu { get => soTienPhaiThu; set => soTienPhaiThu = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }

        // Cột bổ sung P03; constructor cũ được giữ để tương thích mã gọi hiện có.
        public int PhienBanQuyTrinh { get => phienBanQuyTrinh; set => phienBanQuyTrinh = value; }
        public string? MaYeuCau { get => maYeuCau; set => maYeuCau = value; }
        public string? DauVanTayYeuCau { get => dauVanTayYeuCau; set => dauVanTayYeuCau = value; }
        public ET_HOADON(string maHD, string maLS, string maNV, DateTime ngayLap, DateTime? ngayXacNhan, decimal tongTien, decimal giamGia, decimal soTienPhaiThu, string trangThai, int phienBanQuyTrinh, string? maYeuCau, string? dauVanTayYeuCau)
        {
            this.maHD = maHD;
            this.maLS = maLS;
            this.maNV = maNV;
            this.ngayLap = ngayLap;
            this.ngayXacNhan = ngayXacNhan;
            this.tongTien = tongTien;
            this.giamGia = giamGia;
            this.soTienPhaiThu = soTienPhaiThu;
            this.trangThai = trangThai;
            this.phienBanQuyTrinh = phienBanQuyTrinh;
            this.maYeuCau = maYeuCau;
            this.dauVanTayYeuCau = dauVanTayYeuCau;
        }
    }
}
