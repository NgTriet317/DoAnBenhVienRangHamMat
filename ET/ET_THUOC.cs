using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_THUOC
    {
        private string maThuoc;
        private string maHH;
        private string hamLuong;
        private string mucTonToiThieu;
        private string moTa;
        private string trangThai;

        public ET_THUOC(string maThuoc, string maHH, string hamLuong, string mucTonToiThieu, string moTa, string trangThai)
        {
            this.MaThuoc = maThuoc;
            this.MaHH = maHH;
            this.HamLuong = hamLuong;
            this.MucTonToiThieu = mucTonToiThieu;
            this.MoTa = moTa;
            this.TrangThai = trangThai;
        }

        public ET_THUOC()
        {
        }

        public string MaThuoc { get => maThuoc; set => maThuoc = value; }
        public string MaHH { get => maHH; set => maHH = value; }
        public string HamLuong { get => hamLuong; set => hamLuong = value; }
        public string MucTonToiThieu { get => mucTonToiThieu; set => mucTonToiThieu = value; }
        public string MoTa { get => moTa; set => moTa = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
