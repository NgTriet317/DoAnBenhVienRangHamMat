using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("CHI_TIET_KE_HOACH")]
    public class ET_CHITIETKEHOACH
    {
        private string maCTKH = string.Empty;
        private string maKHDT = string.Empty;
        private string maDV = string.Empty;
        private int lanThu;
        private decimal soLuongDuKien;
        private decimal donGiaDuKien;
        private decimal thanhTienDuKien;
        private string trangThai = string.Empty;
        private string? ghiChu;

        public ET_CHITIETKEHOACH() { }

        public ET_CHITIETKEHOACH(string maCTKH, string maKHDT, string maDV, int lanThu, decimal soLuongDuKien, decimal donGiaDuKien, decimal thanhTienDuKien, string trangThai, string? ghiChu)
        {
            this.maCTKH = maCTKH;
            this.maKHDT = maKHDT;
            this.maDV = maDV;
            this.lanThu = lanThu;
            this.soLuongDuKien = soLuongDuKien;
            this.donGiaDuKien = donGiaDuKien;
            this.thanhTienDuKien = thanhTienDuKien;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        [ExplicitKey]
        public string MaCTKH { get => maCTKH; set => maCTKH = value; }
        public string MaKHDT { get => maKHDT; set => maKHDT = value; }
        public string MaDV { get => maDV; set => maDV = value; }
        public int LanThu { get => lanThu; set => lanThu = value; }
        public decimal SoLuongDuKien { get => soLuongDuKien; set => soLuongDuKien = value; }
        public decimal DonGiaDuKien { get => donGiaDuKien; set => donGiaDuKien = value; }
        [Write(false)]
        public decimal ThanhTienDuKien { get => thanhTienDuKien; set => thanhTienDuKien = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
