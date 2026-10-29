// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng BENH_NHAN qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("BENH_NHAN")]
    public class ET_BENHNHAN
    {
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaBN, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string maBN = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính HoTenBN, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string hoTenBN = string.Empty;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính GioiTinh, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? gioiTinh;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NgaySinh, dùng bởi getter/setter tương ứng.
        /// </summary>
        private DateTime? ngaySinh;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính SDT, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? sdt;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính DiaChi, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? diaChi;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính MaBHYT, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? maBHYT;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính NhomMau, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? nhomMau;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TienSuBenh, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? tienSuBenh;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính DiUng, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string? diUng;
        /// <summary>
        /// Giá trị lưu nội bộ của thuộc tính TrangThai, dùng bởi getter/setter tương ứng.
        /// </summary>
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_BENHNHAN() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_BENHNHAN(string maBN, string hoTenBN, string? gioiTinh, DateTime? ngaySinh, string? sdt, string? diaChi, string? maBHYT, string? nhomMau, string? tienSuBenh, string? diUng, string trangThai)
        {
            this.maBN = maBN;
            this.hoTenBN = hoTenBN;
            this.gioiTinh = gioiTinh;
            this.ngaySinh = ngaySinh;
            this.sdt = sdt;
            this.diaChi = diaChi;
            this.maBHYT = maBHYT;
            this.nhomMau = nhomMau;
            this.tienSuBenh = tienSuBenh;
            this.diUng = diUng;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã bệnh nhân; ánh xạ cột MaBN của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaBN { get => maBN; set => maBN = value; }
        /// <summary>
        /// Họ tên bệnh nhân; ánh xạ cột HoTenBN của bảng trong TableAttribute.
        /// </summary>
        public string HoTenBN { get => hoTenBN; set => hoTenBN = value; }
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
        /// Mã bảo hiểm y tế của bệnh nhân; ánh xạ cột MaBHYT của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MaBHYT { get => maBHYT; set => maBHYT = value; }
        /// <summary>
        /// Nhóm máu bệnh nhân; ánh xạ cột NhomMau của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? NhomMau { get => nhomMau; set => nhomMau = value; }
        /// <summary>
        /// Tiền sử bệnh của bệnh nhân; ánh xạ cột TienSuBenh của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? TienSuBenh { get => tienSuBenh; set => tienSuBenh = value; }
        /// <summary>
        /// Thông tin dị ứng của bệnh nhân; ánh xạ cột DiUng của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? DiUng { get => diUng; set => diUng = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
