using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_CHITIETKEHOACH
    {
        private string maCTKH;
        private string maKHDT;
        private string maDV;
        private int lanThu;
        private int soLuongDuKien;
        private decimal donGiaDuKien;
        private decimal thanhTienDuKien;
        private string trangThai;
        private string ghiChu;

        public ET_CHITIETKEHOACH() { }

        public ET_CHITIETKEHOACH(string maCTKH, string maKHDT, string maDV, int lanThu, int soLuongDuKien, decimal donGiaDuKien, decimal thanhTienDuKien, string trangThai, string ghiChu)
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

        public string MaCTKH { get => maCTKH; set => maCTKH = value; }
        public string MaKHDT { get => maKHDT; set => maKHDT = value; }
        public string MaDV { get => maDV; set => maDV = value; }
        public int LanThu { get => lanThu; set => lanThu = value; }
        public int SoLuongDuKien { get => soLuongDuKien; set => soLuongDuKien = value; }
        public decimal DonGiaDuKien { get => donGiaDuKien; set => donGiaDuKien = value; }
        public decimal ThanhTienDuKien { get => thanhTienDuKien; set => thanhTienDuKien = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
