using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_HANGHOA
    {
        private string maHH;
        private string tenHH;
        private string maLoaiHH;
        private string donViTinh;
        private string moTa;
        private string trangThai;

        public ET_HANGHOA(string maHH, string tenHH, string maLoaiHH, string donViTinh, string moTa, string trangThai)
        {
            this.MaHH = maHH;
            this.TenHH = tenHH;
            this.MaLoaiHH = maLoaiHH;
            this.DonViTinh = donViTinh;
            this.MoTa = moTa;
            this.TrangThai = trangThai;
        }

        public ET_HANGHOA()
        {
        }

        public string MaHH { get => maHH; set => maHH = value; }
        public string TenHH { get => tenHH; set => tenHH = value; }
        public string MaLoaiHH { get => maLoaiHH; set => maLoaiHH = value; }
        public string DonViTinh { get => donViTinh; set => donViTinh = value; }
        public string MoTa { get => moTa; set => moTa = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
