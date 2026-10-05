using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_PHONG
    {
        private string maPhong;
        private string tenPhong;
        private string maKhoa;
        private string loaiPhong;
        private string trangThai;
        private string ghiChu;

        public ET_PHONG() { }

        public ET_PHONG(string maPhong, string tenPhong, string maKhoa, string loaiPhong, string trangThai, string ghiChu)
        {
            this.maPhong = maPhong;
            this.tenPhong = tenPhong;
            this.maKhoa = maKhoa;
            this.loaiPhong = loaiPhong;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        public string MaPhong { get => maPhong; set => maPhong = value; }
        public string TenPhong { get => tenPhong; set => tenPhong = value; }
        public string MaKhoa { get => maKhoa; set => maKhoa = value; }
        public string LoaiPhong { get => loaiPhong; set => loaiPhong = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
