using System;

namespace ET
{
    // Dữ liệu đơn xin nghỉ kèm tên nhân viên và chuyên khoa để hiển thị.
    public class ET_DON_XIN_NGHI_HIENTHI : ET_DON_XIN_NGHI
    {
        /// <summary>
        /// Họ tên.
        /// </summary>
        public string HoTen { get; set; } = "";
        /// <summary>
        /// Tên chuyên khoa.
        /// </summary>
        public string TenCK { get; set; } = "";
        /// <summary>
        /// Tên người duyệt.
        /// </summary>
        public string TenNguoiDuyet { get; set; } = "";

        /// <summary>
        /// Giá trị Loại đơn; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string LoaiDon
        {
            get
            {
                if (!string.IsNullOrEmpty(GhiChu))
                {
                    if (GhiChu.Contains("Loại đơn: Nghỉ việc", StringComparison.OrdinalIgnoreCase))
                        return "Nghỉ việc";
                    if (GhiChu.Contains("Đơn khẩn cấp", StringComparison.OrdinalIgnoreCase))
                        return "Nghỉ phép khẩn cấp";
                    if (GhiChu.Contains("Loại đơn: Nghỉ phép", StringComparison.OrdinalIgnoreCase))
                        return "Nghỉ phép";
                }
                return "Chưa xác định";
            }
        }

        /// <summary>
        /// Giá trị Lý do không duyệt; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string LyDoKhongDuyet
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GhiChu)) return "";
                string nhan = "Lý do không duyệt: ";
                int viTri = GhiChu.LastIndexOf(nhan, StringComparison.OrdinalIgnoreCase);
                if (viTri < 0) return "";
                return GhiChu.Substring(viTri + nhan.Length).Trim();
            }
        }
    }
}
