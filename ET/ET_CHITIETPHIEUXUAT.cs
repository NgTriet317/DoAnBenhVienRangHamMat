using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_CHITIETPHIEUXUAT
    {
        private string maCTPX;
        private string maPX;
        private string maHH;
        private string maLo;
        private string soLuong;
        private string ghiChu;

        public ET_CHITIETPHIEUXUAT(string maCTPX, string maPX, string maHH, string maLo, string soLuong, string ghiChu)
        {
            this.MaCTPX = maCTPX;
            this.MaPX = maPX;
            this.MaHH = maHH;
            this.MaLo = maLo;
            this.SoLuong = soLuong;
            this.GhiChu = ghiChu;
        }

        public ET_CHITIETPHIEUXUAT()
        {
        }

        public string MaCTPX { get => maCTPX; set => maCTPX = value; }
        public string MaPX { get => maPX; set => maPX = value; }
        public string MaHH { get => maHH; set => maHH = value; }
        public string MaLo { get => maLo; set => maLo = value; }
        public string SoLuong { get => soLuong; set => soLuong = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
