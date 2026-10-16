// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng NHAN_VIEN qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("NHAN_VIEN")]
    public class ET_NHANVIEN
    {
        private string maNV = string.Empty;
        private string hoTen = string.Empty;
        private string? gioiTinh;
        private DateTime? ngaySinh;
        private string? sdt;
        private string? diaChi;
        private string maChucVu = string.Empty;
        private string? maCK;
        private string? bangCap;
        private DateTime ngayVaoLam;
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_NHANVIEN() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_NHANVIEN(string maNV, string hoTen, string? gioiTinh, DateTime? ngaySinh, string? sdt, string? diaChi, string maChucVu, string? maCK, string? bangCap, DateTime ngayVaoLam, string trangThai)
        {
            this.maNV = maNV;
            this.hoTen = hoTen;
            this.gioiTinh = gioiTinh;
            this.ngaySinh = ngaySinh;
            this.sdt = sdt;
            this.diaChi = diaChi;
            this.maChucVu = maChucVu;
            this.maCK = maCK;
            this.bangCap = bangCap;
            this.ngayVaoLam = ngayVaoLam;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã nhân viên; ánh xạ cột MaNV của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaNV { get => maNV; set => maNV = value; }
        /// <summary>
        /// Họ tên nhân viên; ánh xạ cột HoTen của bảng trong TableAttribute.
        /// </summary>
        public string HoTen { get => hoTen; set => hoTen = value; }
        /// <summary>
        /// Giới tính được cung cấp; ánh xạ cột GioiTinh của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? GioiTinh { get => gioiTinh; set => gioiTinh = value; }
        /// <summary>
        /// Ngày sinh được cung cấp; ánh xạ cột NgaySinh của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public DateTime? NgaySinh { get => ngaySinh; set => ngaySinh = value; }
        /// <summary>
        /// Số điện thoại dạng chuỗi để giữ số 0 đầu; ánh xạ cột SDT của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? SDT { get => sdt; set => sdt = value; }
        /// <summary>
        /// Địa chỉ liên hệ; ánh xạ cột DiaChi của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? DiaChi { get => diaChi; set => diaChi = value; }
        /// <summary>
        /// Mã chức vụ; ánh xạ cột MaChucVu của bảng trong TableAttribute.
        /// </summary>
        public string MaChucVu { get => maChucVu; set => maChucVu = value; }
        /// <summary>
        /// Mã chuyên khoa; ánh xạ cột MaCK của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaCK { get => maCK; set => maCK = value; }
        /// <summary>
        /// Bằng cấp chuyên môn; ánh xạ cột BangCap của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? BangCap { get => bangCap; set => bangCap = value; }
        /// <summary>
        /// Ngày nhân viên vào làm; ánh xạ cột NgayVaoLam của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayVaoLam { get => ngayVaoLam; set => ngayVaoLam = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
