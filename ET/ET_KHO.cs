using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_KHO
    {
        private string maKho;
        private string tenKho;
        private string maNV;
        private string loaiKho;
        private string trangThai;

        public ET_KHO(string maKho, string tenKho, string maNV, string loaiKho, string trangThai)
        {
            this.MaKho = maKho;
            this.TenKho = tenKho;
            this.MaNV = maNV;
            this.LoaiKho = loaiKho;
            this.TrangThai = trangThai;
        }

        public string MaKho { get => maKho; set => maKho = value; }
        public string TenKho { get => tenKho; set => tenKho = value; }
        public string MaNV { get => maNV; set => maNV = value; }
        public string LoaiKho { get => loaiKho; set => loaiKho = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
