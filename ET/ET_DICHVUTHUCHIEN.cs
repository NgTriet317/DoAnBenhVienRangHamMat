using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("DICH_VU_THUC_HIEN")]
    public class ET_DICHVUTHUCHIEN
    {
        private string maDVThucHien = string.Empty;
        private string maLS = string.Empty;
        private string? maCTKH;
        private string maDV = string.Empty;
        private string maNVThucHien = string.Empty;
        private DateTime thoiGianThucHien;
        private decimal soLuong;
        private decimal donGia;
        private decimal thanhTien;
        private string? ketQua;
        private string trangThai = string.Empty;

        public ET_DICHVUTHUCHIEN() { }

        public ET_DICHVUTHUCHIEN(string maDVThucHien, string maLS, string? maCTKH, string maDV, string maNVThucHien, DateTime thoiGianThucHien, decimal soLuong, decimal donGia, decimal thanhTien, string? ketQua, string trangThai)
        {
            this.maDVThucHien = maDVThucHien;
            this.maLS = maLS;
            this.maCTKH = maCTKH;
            this.maDV = maDV;
            this.maNVThucHien = maNVThucHien;
            this.thoiGianThucHien = thoiGianThucHien;
            this.soLuong = soLuong;
            this.donGia = donGia;
            this.thanhTien = thanhTien;
            this.ketQua = ketQua;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaDVThucHien { get => maDVThucHien; set => maDVThucHien = value; }
        public string MaLS { get => maLS; set => maLS = value; }
        public string? MaCTKH { get => maCTKH; set => maCTKH = value; }
        public string MaDV { get => maDV; set => maDV = value; }
        public string MaNVThucHien { get => maNVThucHien; set => maNVThucHien = value; }
        public DateTime ThoiGianThucHien { get => thoiGianThucHien; set => thoiGianThucHien = value; }
        public decimal SoLuong { get => soLuong; set => soLuong = value; }
        public decimal DonGia { get => donGia; set => donGia = value; }
        [Write(false)]
        public decimal ThanhTien { get => thanhTien; set => thanhTien = value; }
        public string? KetQua { get => ketQua; set => ketQua = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
