using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("CHI_TIET_PHIEU_XUAT")]
    public class ET_CHITIETPHIEUXUAT
    {
        private string maCTPX = string.Empty;
        private string maPX = string.Empty;
        private string maHH = string.Empty;
        private string? maLo;
        private string? maCTDT;
        private decimal soLuong;
        private string ghiChu = string.Empty;

        public ET_CHITIETPHIEUXUAT() { }

        public ET_CHITIETPHIEUXUAT(string maCTPX, string maPX, string maHH, string? maLo, string? maCTDT, decimal soLuong, string ghiChu)
        {
            this.maCTPX = maCTPX;
            this.maPX = maPX;
            this.maHH = maHH;
            this.maLo = maLo;
            this.maCTDT = maCTDT;
            this.soLuong = soLuong;
            this.ghiChu = ghiChu;
        }

        [ExplicitKey]
        public string MaCTPX { get => maCTPX; set => maCTPX = value; }
        public string MaPX { get => maPX; set => maPX = value; }
        public string MaHH { get => maHH; set => maHH = value; }
        public string? MaLo { get => maLo; set => maLo = value; }
        public string? MaCTDT { get => maCTDT; set => maCTDT = value; }
        public decimal SoLuong { get => soLuong; set => soLuong = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
