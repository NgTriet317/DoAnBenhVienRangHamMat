// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng DICH_VU qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("DICH_VU")]
    public class ET_DICHVU
    {
        private string maDV = string.Empty;
        private string tenDV = string.Empty;
        private string nhomDV = string.Empty;
        private string maCK = string.Empty;
        private decimal donGia;
        private int thoiGianDuKien;
        private string? moTa;
        private DateTime ngayApDung;
        private string trangThai = string.Empty;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_DICHVU() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_DICHVU(string maDV, string tenDV, string nhomDV, string maCK, decimal donGia, int thoiGianDuKien, string? moTa, DateTime ngayApDung, string trangThai)
        {
            this.maDV = maDV;
            this.tenDV = tenDV;
            this.nhomDV = nhomDV;
            this.maCK = maCK;
            this.donGia = donGia;
            this.thoiGianDuKien = thoiGianDuKien;
            this.moTa = moTa;
            this.ngayApDung = ngayApDung;
            this.trangThai = trangThai;
        }

        /// <summary>
        /// Mã dịch vụ; ánh xạ cột MaDV của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaDV { get => maDV; set => maDV = value; }
        /// <summary>
        /// Tên dịch vụ; ánh xạ cột TenDV của bảng trong TableAttribute.
        /// </summary>
        public string TenDV { get => tenDV; set => tenDV = value; }
        /// <summary>
        /// Nhóm dịch vụ để phân loại/tìm kiếm; ánh xạ cột NhomDV của bảng trong TableAttribute.
        /// </summary>
        public string NhomDV { get => nhomDV; set => nhomDV = value; }
        /// <summary>
        /// Mã chuyên khoa; ánh xạ cột MaCK của bảng trong TableAttribute.
        /// </summary>
        public string MaCK { get => maCK; set => maCK = value; }
        /// <summary>
        /// Đơn giá; ánh xạ cột DonGia của bảng trong TableAttribute.
        /// </summary>
        public decimal DonGia { get => donGia; set => donGia = value; }
        /// <summary>
        /// Thời gian thực hiện dịch vụ dự kiến; ánh xạ cột ThoiGianDuKien của bảng trong TableAttribute.
        /// </summary>
        public int ThoiGianDuKien { get => thoiGianDuKien; set => thoiGianDuKien = value; }
        /// <summary>
        /// Mô tả thông tin danh mục; ánh xạ cột MoTa của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? MoTa { get => moTa; set => moTa = value; }
        /// <summary>
        /// Ngày bắt đầu áp dụng danh mục/giá; ánh xạ cột NgayApDung của bảng trong TableAttribute.
        /// </summary>
        public DateTime NgayApDung { get => ngayApDung; set => ngayApDung = value; }
        /// <summary>
        /// Trạng thái nghiệp vụ của bản ghi; ánh xạ cột TrangThai của bảng trong TableAttribute.
        /// </summary>
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
