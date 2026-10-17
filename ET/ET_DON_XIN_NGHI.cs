using System;

namespace ET
{
    public class ET_DON_XIN_NGHI
    {
        public string MaDonNghi { get; set; } = "";
        public string MaNV { get; set; } = "";
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public string LyDo { get; set; } = "";
        public DateTime NgayGui { get; set; }
        public string TrangThaiDuyet { get; set; } = "Chờ duyệt";
        public string? MaNVDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? GhiChu { get; set; }
    }
}
