using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_VATTU
    {
        private string maVT;
        private string maHH;
        private string mucTonToiThieu;
        private string trangThai;

        public ET_VATTU(string maVT, string maHH, string mucTonToiThieu, string trangThai)
        {
            this.MaVT = maVT;
            this.MaHH = maHH;
            this.MucTonToiThieu = mucTonToiThieu;
            this.TrangThai = trangThai;
        }

        public ET_VATTU()
        {
        }

        public string MaVT { get => maVT; set => maVT = value; }
        public string MaHH { get => maHH; set => maHH = value; }
        public string MucTonToiThieu { get => mucTonToiThieu; set => mucTonToiThieu = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
