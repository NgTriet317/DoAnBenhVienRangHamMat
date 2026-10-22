using System;

namespace ET
{
    // Dòng hàng nhập mới. Mã lô do hệ thống cấp, người dùng chỉ nhập số lô của NCC.
    public class ET_DONGNHAPMOI
    {
        public string MaHH { get; set; } = "";
        public string SoLo { get; set; } = "";
        public DateTime? HanSuDung { get; set; }
        public decimal SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string? GhiChu { get; set; }
    }
}
