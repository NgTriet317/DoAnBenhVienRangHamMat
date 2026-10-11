using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("THANH_TOAN")]
    public class ET_THANHTOAN
    {
        private string maTT = string.Empty;
        private string maHD = string.Empty;
        private string maNV = string.Empty;
        private DateTime ngayThanhToan;
        private decimal soTien;
        private string loaiGiaoDich = string.Empty;
        private string phuongThuc = string.Empty;
        private string? lyDo;
        private string trangThai = string.Empty;

        public ET_THANHTOAN() { }

        public ET_THANHTOAN(string maTT, string maHD, string maNV, DateTime ngayThanhToan, decimal soTien, string loaiGiaoDich, string phuongThuc, string? lyDo, string trangThai)
        {
            this.maTT = maTT;
            this.maHD = maHD;
            this.maNV = maNV;
            this.ngayThanhToan = ngayThanhToan;
            this.soTien = soTien;
            this.loaiGiaoDich = loaiGiaoDich;
            this.phuongThuc = phuongThuc;
            this.lyDo = lyDo;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaTT { get => maTT; set => maTT = value; }
        public string MaHD { get => maHD; set => maHD = value; }
        public string MaNV { get => maNV; set => maNV = value; }
        public DateTime NgayThanhToan { get => ngayThanhToan; set => ngayThanhToan = value; }
        public decimal SoTien { get => soTien; set => soTien = value; }
        public string LoaiGiaoDich { get => loaiGiaoDich; set => loaiGiaoDich = value; }
        public string PhuongThuc { get => phuongThuc; set => phuongThuc = value; }
        public string? LyDo { get => lyDo; set => lyDo = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
