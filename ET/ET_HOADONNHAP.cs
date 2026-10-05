using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_HOADONNHAP
    {
        private string maHDN;
        private string soHoaDonNCC;
        private string maNCC;
        private string maKho;
        private string maNV;
        private DateTime ngayHoaDon;
        private DateTime ngayNhap;
        private decimal tongTien;
        private string trangThai;
        private string ghiChu;

        public ET_HOADONNHAP(string maHDN, string soHoaDonNCC, string maNCC, string maKho, string maNV, DateTime ngayHoaDon, DateTime ngayNhap, decimal tongTien, string trangThai, string ghiChu)
        {
            this.MaHDN = maHDN;
            this.SoHoaDonNCC = soHoaDonNCC;
            this.MaNCC = maNCC;
            this.MaKho = maKho;
            this.MaNV = maNV;
            this.NgayHoaDon = ngayHoaDon;
            this.NgayNhap = ngayNhap;
            this.TongTien = tongTien;
            this.TrangThai = trangThai;
            this.GhiChu = ghiChu;
        }

        public ET_HOADONNHAP()
        {
        }

        public string MaHDN { get => maHDN; set => maHDN = value; }
        public string SoHoaDonNCC { get => soHoaDonNCC; set => soHoaDonNCC = value; }
        public string MaNCC { get => maNCC; set => maNCC = value; }
        public string MaKho { get => maKho; set => maKho = value; }
        public string MaNV { get => maNV; set => maNV = value; }
        public DateTime NgayHoaDon { get => ngayHoaDon; set => ngayHoaDon = value; }
        public DateTime NgayNhap { get => ngayNhap; set => ngayNhap = value; }
        public decimal TongTien { get => tongTien; set => tongTien = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
