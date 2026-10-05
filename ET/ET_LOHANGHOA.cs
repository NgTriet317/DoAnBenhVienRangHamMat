using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_LOHANGHOA
    {
        private string maLo;
        private string maHH;
        private string maNCC;
        private string maKho;
        private int soLo;
        private DateTime ngayNhap;
        private DateTime hanSuDung;
        private string trangThai;

        public ET_LOHANGHOA(string maLo, string maHH, string maNCC, string maKho, int soLo, DateTime ngayNhap, DateTime hanSuDung, string trangThai)
        {
            this.MaLo = maLo;
            this.MaHH = maHH;
            this.MaNCC = maNCC;
            this.MaKho = maKho;
            this.SoLo = soLo;
            this.NgayNhap = ngayNhap;
            this.HanSuDung = hanSuDung;
            this.TrangThai = trangThai;
        }

        public ET_LOHANGHOA()
        {
        }

        public string MaLo { get => maLo; set => maLo = value; }
        public string MaHH { get => maHH; set => maHH = value; }
        public string MaNCC { get => maNCC; set => maNCC = value; }
        public string MaKho { get => maKho; set => maKho = value; }
        public int SoLo { get => soLo; set => soLo = value; }
        public DateTime NgayNhap { get => ngayNhap; set => ngayNhap = value; }
        public DateTime HanSuDung { get => hanSuDung; set => hanSuDung = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
