using System;

namespace ET
{
    // Phiên bản chính sách lương - ánh xạ CHINH_SACH_LUONG.
    public class ET_CHINHSACHLUONG
    {
        public string MaCSL { get; set; } = "";
        public string MaChucVu { get; set; } = "";
        public string TenChinhSach { get; set; } = "";
        public int PhienBan { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime? NgayHetHieuLuc { get; set; }
        public decimal LuongCoBan { get; set; }
        public int SoCaDinhMuc { get; set; } = 26;
        public decimal? DonGiaCa { get; set; }
        public string CoSoHoaHong { get; set; } = "Chi tiết dịch vụ đã xác nhận, hóa đơn đã thu đủ";
        public decimal TyLeHoaHong { get; set; } // Giá trị 0..1, không phải phần trăm.
        public decimal PhuCapCoDinh { get; set; }
        public string TrangThai { get; set; } = "Nháp";
        public string? MaNVDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public decimal TyLeBHXH { get; set; } = 0.08m;
        public decimal TyLeBHYT { get; set; } = 0.015m;
        public decimal TyLeBHTN { get; set; } = 0.01m;
        public decimal HeSoTangCaThuong { get; set; } = 1.50m;
        public decimal HeSoTangCaChuNhat { get; set; } = 2.00m;
        public decimal HeSoTangCaNgayLe { get; set; } = 3.00m;
    }
}
