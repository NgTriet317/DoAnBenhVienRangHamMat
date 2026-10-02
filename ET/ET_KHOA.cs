using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_KHOA
    {
        private string maKhoa;
        private string tenKhoa;
        private int sdt;
        private string moTa;
        private string trangThai;

        public ET_KHOA() { }

        public ET_KHOA(string maKhoa, string tenKhoa, int sdt, string moTa, string trangThai)
        {
            this.maKhoa = maKhoa;
            this.tenKhoa = tenKhoa;
            this.sdt = sdt;
            this.moTa = moTa;
            this.trangThai = trangThai;
        }

        public string MaKhoa { get => maKhoa; set => maKhoa = value; }
        public string TenKhoa { get => tenKhoa; set => tenKhoa = value; }
        public int Sdt { get => sdt; set => sdt = value; }
        public string MoTa { get => moTa; set => moTa = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
