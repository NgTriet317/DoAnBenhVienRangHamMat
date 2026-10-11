using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("CHI_TIET_HOA_DON")]
    public class ET_CHITIETHOADON
    {
        private string maCTHD = string.Empty;
        private string maHD = string.Empty;
        private string? maDVThucHien;
        private string? maCTPX;
        private string noiDung = string.Empty;
        private decimal? donGia;
        private decimal thanhTien;

        public ET_CHITIETHOADON() { }

        public ET_CHITIETHOADON(string maCTHD, string maHD, string? maDVThucHien, string? maCTPX, string noiDung, decimal? donGia, decimal thanhTien)
        {
            this.maCTHD = maCTHD;
            this.maHD = maHD;
            this.maDVThucHien = maDVThucHien;
            this.maCTPX = maCTPX;
            this.noiDung = noiDung;
            this.donGia = donGia;
            this.thanhTien = thanhTien;
        }

        [ExplicitKey]
        public string MaCTHD { get => maCTHD; set => maCTHD = value; }
        public string MaHD { get => maHD; set => maHD = value; }
        public string? MaDVThucHien { get => maDVThucHien; set => maDVThucHien = value; }
        public string? MaCTPX { get => maCTPX; set => maCTPX = value; }
        public string NoiDung { get => noiDung; set => noiDung = value; }
        public decimal? DonGia { get => donGia; set => donGia = value; }
        [Write(false)]
        public decimal ThanhTien { get => thanhTien; set => thanhTien = value; }
    }
}
