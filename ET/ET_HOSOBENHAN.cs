using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_HOSOBENHAN
    {
        private string maHS;
        private string maBN;
        private DateTime ngayLap;
        private string lyDoKhamBanDau;
        private string ghiChu;
        private string trangThai;

        public ET_HOSOBENHAN()
        {

        }
        public ET_HOSOBENHAN(string maHS, string maBN, DateTime ngayLap, string lyDoKhamBanDau, string ghiChu, string trangThai)
        {
            this.maHS = maHS;
            this.maBN = maBN;
            this.ngayLap = ngayLap;
            this.lyDoKhamBanDau = lyDoKhamBanDau;
            this.ghiChu = ghiChu;
            this.trangThai = trangThai;
        }

        public string MaHS { get => maHS; set => maHS = value; }
        public string MaBN { get => maBN; set => maBN = value; }
        public DateTime NgayLap { get => ngayLap; set => ngayLap = value; }
        public string LyDoKhamBanDau { get => lyDoKhamBanDau; set => lyDoKhamBanDau = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
