using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_PHIEUNHAP
    {
        private string maPN;
        private string maHDN;
        private string maKho;
        private string maNV;
        private DateTime ngayNhap;
        private decimal tongTien;
        private string trangThai;
        private string lyDo;
        private string ghiChu;

        public ET_PHIEUNHAP(string maPN, string maHDN, string maKho, string maNV, DateTime ngayNhap, decimal tongTien, string trangThai, string lyDo, string ghiChu)
        {
            this.MaPN = maPN;
            this.MaHDN = maHDN;
            this.MaKho = maKho;
            this.MaNV = maNV;
            this.NgayNhap = ngayNhap;
            this.TongTien = tongTien;
            this.TrangThai = trangThai;
            this.LyDo = lyDo;
            this.GhiChu = ghiChu;
        }

        public ET_PHIEUNHAP()
        {
        }

        public string MaPN { get => maPN; set => maPN = value; }
        public string MaHDN { get => maHDN; set => maHDN = value; }
        public string MaKho { get => maKho; set => maKho = value; }
        public string MaNV { get => maNV; set => maNV = value; }
        public DateTime NgayNhap { get => ngayNhap; set => ngayNhap = value; }
        public decimal TongTien { get => tongTien; set => tongTien = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string LyDo { get => lyDo; set => lyDo = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
