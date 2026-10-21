namespace ET
{
    // Dòng người dùng chọn trong biểu mẫu xuất vật tư nội bộ.
    public class ET_DONGXUATMOI
    {
        public string MaHH { get; set; } = "";
        public string MaLo { get; set; } = "";
        public decimal SoLuong { get; set; }
        public string GhiChu { get; set; } = "";
    }

    public class ET_TONLOXUAT
    {
        public string MaLo { get; set; } = "";
        public string MaHH { get; set; } = "";
        public string SoLo { get; set; } = "";
        public string TenHH { get; set; } = "";
        public string DonViTinh { get; set; } = "";
        public decimal SoLuongTon { get; set; }
        public DateTime? HanSuDung { get; set; }
    }
}
