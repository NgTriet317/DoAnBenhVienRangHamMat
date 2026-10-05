using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_CHITIETPHIEUNHAP
    {
        private string maCTPN;
        private string maPN;
        private string maHH;
        private string maLo;
        private int soLuong;
        private decimal donGia;
        private decimal thanhTien;
        private string ghiChu;

        public ET_CHITIETPHIEUNHAP(string maCTPN, string maPN, string maHH, string maLo, int soLuong, decimal donGia, decimal thanhTien, string ghiChu)
        {
            this.MaCTPN = maCTPN;
            this.MaPN = maPN;
            this.MaHH = maHH;
            this.MaLo = maLo;
            this.SoLuong = soLuong;
            this.DonGia = donGia;
            this.ThanhTien = thanhTien;
            this.GhiChu = ghiChu;
        }

        public ET_CHITIETPHIEUNHAP()
        {
        }

        public string MaCTPN { get => maCTPN; set => maCTPN = value; }
        public string MaPN { get => maPN; set => maPN = value; }
        public string MaHH { get => maHH; set => maHH = value; }
        public string MaLo { get => maLo; set => maLo = value; }
        public int SoLuong { get => soLuong; set => soLuong = value; }
        public decimal DonGia { get => donGia; set => donGia = value; }
        public decimal ThanhTien { get => thanhTien; set => thanhTien = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
