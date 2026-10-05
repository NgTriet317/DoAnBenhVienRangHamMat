using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_LOAIHANGHOA
    {
        private string maLoaiHH;
        private string tenLoaiHH;
        private string moTa;
        private string trangThai;

        public ET_LOAIHANGHOA(string maLoaiHH, string tenLoaiHH, string moTa, string trangThai)
        {
            this.MaLoaiHH = maLoaiHH;
            this.TenLoaiHH = tenLoaiHH;
            this.MoTa = moTa;
            this.TrangThai = trangThai;
        }

        public ET_LOAIHANGHOA()
        {
        }

        public string MaLoaiHH { get => maLoaiHH; set => maLoaiHH = value; }
        public string TenLoaiHH { get => tenLoaiHH; set => tenLoaiHH = value; }
        public string MoTa { get => moTa; set => moTa = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
