using System;

namespace ET
{
    /// <summary>
    /// Dữ liệu thế trao đổi giữa các tầng và ánh xạ với kết quả truy vấn SQL.
    /// </summary>
    public class ET_CA_CAN_THAY_THE
    {
        /// <summary>
        /// Mã lịch làm việc.
        /// </summary>
        public string MaLLV { get; set; } = "";
        /// <summary>
        /// Mã ca làm việc.
        /// </summary>
        public string MaCa { get; set; } = "";
        /// <summary>
        /// Mã phòng.
        /// </summary>
        public string MaPhong { get; set; } = "";
        /// <summary>
        /// Tên phòng.
        /// </summary>
        public string TenPhong { get; set; } = "";
        /// <summary>
        /// Tên ca.
        /// </summary>
        public string TenCa { get; set; } = "";
        /// <summary>
        /// Ngày làm.
        /// </summary>
        public DateTime NgayLam { get; set; }
        /// <summary>
        /// Trạng thái.
        /// </summary>
        public string TrangThai { get; set; } = "";
        /// <summary>
        /// Mã nhân viên thay thế.
        /// </summary>
        public string MaNVThayThe { get; set; } = "";
        /// <summary>
        /// Họ tên thay thế.
        /// </summary>
        public string HoTenThayThe { get; set; } = "";
        /// <summary>
        /// Mã lịch làm việc thay thế.
        /// </summary>
        public string MaLLVThayThe { get; set; } = "";
        /// <summary>
        /// Giá trị Tình trạng; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TinhTrang
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MaLLVThayThe)) return "Chưa phân người thay";
                return "Đã chọn: " + HoTenThayThe;
            }
        }
    }

    /// <summary>
    /// Dữ liệu thế trao đổi giữa các tầng và ánh xạ với kết quả truy vấn SQL.
    /// </summary>
    public class ET_NGUOI_THAY_THE
    {
        /// <summary>
        /// Mã nhân viên thực hiện hoặc được gán cho bản ghi.
        /// </summary>
        public string MaNV { get; set; } = "";
        /// <summary>
        /// Họ tên.
        /// </summary>
        public string HoTen { get; set; } = "";
        /// <summary>
        /// Giá trị Thông tin; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string ThongTin
        {
            get { return HoTen; }
        }
    }
}
