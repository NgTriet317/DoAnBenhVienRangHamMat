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
        private string maCTDT = string.Empty;
        private string maDT = string.Empty;
        private string maThuoc = string.Empty;
        private string lieuDung = string.Empty;
        private decimal soLuong;
        private int soNgayDung;
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
    }
}
