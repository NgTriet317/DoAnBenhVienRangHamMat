// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng DICH_VU_THUC_HIEN qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("DICH_VU_THUC_HIEN")]
    public class ET_DICHVUTHUCHIEN
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaDVThucHien, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maDVThucHien = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaLS, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maLS = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaCTKH, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? maCTKH;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaDV, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maDV = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaNVThucHien, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maNVThucHien = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính ThoiGianThucHien, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime thoiGianThucHien;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính SoLuong, dùng bởi getter/setter tương ứng.
        /// </summary>
        private decimal soLuong;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính DonGia, dùng bởi getter/setter tương ứng.
        /// </summary>
        private decimal donGia;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính ThanhTien, dùng bởi getter/setter tương ứng.
        /// </summary>
        private decimal thanhTien;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính KetQua, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? ketQua;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TrangThai, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_DICHVUTHUCHIEN() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_DICHVUTHUCHIEN(string maDVThucHien, string maLS, string? maCTKH, string maDV, string maNVThucHien, DateTime thoiGianThucHien, decimal soLuong, decimal donGia, decimal thanhTien, string? ketQua, string trangThai)
        {
            this.maDVThucHien = maDVThucHien;
            this.maLS = maLS;
            this.maCTKH = maCTKH;
            this.maDV = maDV;
            this.maNVThucHien = maNVThucHien;
            this.thoiGianThucHien = thoiGianThucHien;
            this.soLuong = soLuong;
            this.donGia = donGia;
            this.thanhTien = thanhTien;
            this.ketQua = ketQua;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã bản ghi dịch vụ thực hiện; ánh xạ cột MaDVThucHien của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaDVThucHien { get => maDVThucHien; set => maDVThucHien = value; }
        /// <summary>
        /// Mã lần khám trong lịch sử khám bệnh; ánh xạ cột MaLS của bảng trong TableAttribute.
        /// </summary>
        public string MaLS { get => maLS; set => maLS = value; }
        /// <summary>
        /// Mã bước chi tiết kế hoạch; ánh xạ cột MaCTKH của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaCTKH { get => maCTKH; set => maCTKH = value; }
        /// <summary>
        /// Mã dịch vụ; ánh xạ cột MaDV của bảng trong TableAttribute.
        /// </summary>
        public string MaDV { get => maDV; set => maDV = value; }
        /// <summary>
        /// Mã nhân viên thực hiện dịch vụ; ánh xạ cột MaNVThucHien của bảng trong TableAttribute.
        /// </summary>
        public string MaNVThucHien { get => maNVThucHien; set => maNVThucHien = value; }
        /// <summary>
        /// Thời điểm thực hiện dịch vụ; ánh xạ cột ThoiGianThucHien của bảng trong TableAttribute.
        /// </summary>
        public DateTime ThoiGianThucHien { get => thoiGianThucHien; set => thoiGianThucHien = value; }
        /// <summary>
        /// Số lượng thuốc/hàng hóa/dịch vụ; ánh xạ cột SoLuong của bảng trong TableAttribute.
        /// </summary>
        public decimal SoLuong { get => soLuong; set => soLuong = value; }
        /// <summary>
        /// Đơn giá; ánh xạ cột DonGia của bảng trong TableAttribute.
        /// </summary>
        public decimal DonGia { get => donGia; set => donGia = value; }
        /// <summary>
        /// Thành tiền dòng chi tiết do SQL trả về; ánh xạ cột ThanhTien của bảng trong TableAttribute. Chỉ đọc kết quả SQL, Dapper.Contrib không đưa cột này vào INSERT/UPDATE.
        /// </summary>
        [Write(false)]
        public decimal ThanhTien { get => thanhTien; set => thanhTien = value; }
        /// <summary>
        /// Kết quả thực hiện dịch vụ; ánh xạ cột KetQua của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? KetQua { get => ketQua; set => ketQua = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
