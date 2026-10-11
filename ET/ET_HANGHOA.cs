using System;
using Dapper.Contrib.Extensions;

namespace ET
{
    [Table("HANG_HOA")]
    public class ET_HANGHOA
    {
        private string maHH = string.Empty;
        private string tenHH = string.Empty;
        private string maLoaiHH = string.Empty;
        private string donViTinh = string.Empty;
        private string moTa = string.Empty;
        private string trangThai = string.Empty;

        public ET_HANGHOA() { }

        public ET_HANGHOA(string maHH, string tenHH, string maLoaiHH, string donViTinh, string moTa, string trangThai)
        {
            this.maHH = maHH;
            this.tenHH = tenHH;
            this.maLoaiHH = maLoaiHH;
            this.donViTinh = donViTinh;
            this.moTa = moTa;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaHH { get => maHH; set => maHH = value; }
        public string TenHH { get => tenHH; set => tenHH = value; }
        public string MaLoaiHH { get => maLoaiHH; set => maLoaiHH = value; }
        public string DonViTinh { get => donViTinh; set => donViTinh = value; }
        public string MoTa { get => moTa; set => moTa = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
