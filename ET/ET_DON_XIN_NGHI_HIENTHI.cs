using System;

namespace ET
{
    // Dữ liệu đơn xin nghỉ kèm tên nhân viên và chuyên khoa để hiển thị.
    public class ET_DON_XIN_NGHI_HIENTHI : ET_DON_XIN_NGHI
    {
        public string HoTen { get; set; } = "";
        public string TenCK { get; set; } = "";
        public string TenNguoiDuyet { get; set; } = "";

        public string LoaiDon
        {
            get
            {
                if (!string.IsNullOrEmpty(GhiChu))
                {
                    if (GhiChu.Contains("Loại đơn: Nghỉ việc", StringComparison.OrdinalIgnoreCase))
                        return "Nghỉ việc";
                    if (GhiChu.Contains("Loại đơn: Nghỉ phép", StringComparison.OrdinalIgnoreCase))
                        return "Nghỉ phép";
                }
                return "Chưa xác định";
            }
        }

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
