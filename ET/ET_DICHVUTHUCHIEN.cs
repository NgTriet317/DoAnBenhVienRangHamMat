using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_DICHVUTHUCHIEN
    {
        private string maDVThucHien;
        private string maLS;
        private string maCTKH;
        private string maDV;
        private string maNVThucHien;
        private DateTime thoiGianThucHien;
        private int soLuong;
        private decimal donGia;
        private string ketQua;
        private string trangThai;

        public ET_DICHVUTHUCHIEN() { }

        public ET_DICHVUTHUCHIEN(string maDVThucHien, string maLS, string maCTKH, string maDV, string maNVThucHien, DateTime thoiGianThucHien, int soLuong, decimal donGia, string ketQua, string trangThai)
        {
            this.maDVThucHien = maDVThucHien;
            this.maLS = maLS;
            this.maCTKH = maCTKH;
            this.maDV = maDV;
            this.maNVThucHien = maNVThucHien;
            this.thoiGianThucHien = thoiGianThucHien;
            this.soLuong = soLuong;
            this.donGia = donGia;
            this.ketQua = ketQua;
            this.trangThai = trangThai;
        }

        public string MaDVThucHien { get => maDVThucHien; set => maDVThucHien = value; }
        public string MaLS { get => maLS; set => maLS = value; }
        public string MaCTKH { get => maCTKH; set => maCTKH = value; }
        public string MaDV { get => maDV; set => maDV = value; }
        public string MaNVThucHien { get => maNVThucHien; set => maNVThucHien = value; }
        public DateTime ThoiGianThucHien { get => thoiGianThucHien; set => thoiGianThucHien = value; }
        public int SoLuong { get => soLuong; set => soLuong = value; }
        public decimal DonGia { get => donGia; set => donGia = value; }
        public string KetQua { get => ketQua; set => ketQua = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
