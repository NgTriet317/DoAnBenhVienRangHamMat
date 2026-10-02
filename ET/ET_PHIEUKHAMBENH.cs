using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    internal class ET_PHIEUKHAMBENH
    {
        private string maPhieu;
        private string maBN;
        private string maHS;
        private string maLich;
        private string maKhoa;
        private string maPhong;
        private string maBS;
        private string maKhoNVTiepNhan;
        private string maDV;
        private int stt;
        private string lyDoKham;
        private string trangThai;
        private string ghiChu;

        public ET_PHIEUKHAMBENH() { }
        public ET_PHIEUKHAMBENH(string maPhieu, string maBN, string maHS, string maLich, string maKhoa, string maPhong, string maBS, string maKhoNVTiepNhan, string maDV, int stt, string lyDoKham, string trangThai, string ghiChu)
        {
            this.maPhieu = maPhieu;
            this.maBN = maBN;
            this.maHS = maHS;
            this.maLich = maLich;
            this.maKhoa = maKhoa;
            this.maPhong = maPhong;
            this.maBS = maBS;
            this.maKhoNVTiepNhan = maKhoNVTiepNhan;
            this.maDV = maDV;
            this.stt = stt;
            this.lyDoKham = lyDoKham;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        public string MaPhieu { get => maPhieu; set => maPhieu = value; }
        public string MaBN { get => maBN; set => maBN = value; }
        public string MaHS { get => maHS; set => maHS = value; }
        public string MaLich { get => maLich; set => maLich = value; }
        public string MaKhoa { get => maKhoa; set => maKhoa = value; }
        public string MaPhong { get => maPhong; set => maPhong = value; }
        public string MaBS { get => maBS; set => maBS = value; }
        public string MaKhoNVTiepNhan { get => maKhoNVTiepNhan; set => maKhoNVTiepNhan = value; }
        public string MaDV { get => maDV; set => maDV = value; }
        public int Stt { get => stt; set => stt = value; }
        public string LyDoKham { get => lyDoKham; set => lyDoKham = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
