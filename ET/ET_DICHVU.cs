using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("DICH_VU")]
    public class ET_DICHVU
    {
        private string maDV = string.Empty;
        private string tenDV = string.Empty;
        private string nhomDV = string.Empty;
        private string maCK = string.Empty;
        private decimal donGia;
        private int thoiGianDuKien;
        private string? moTa;
        private DateTime ngayApDung;
        private string trangThai = string.Empty;

        public ET_DICHVU() { }

        public ET_DICHVU(string maDV, string tenDV, string nhomDV, string maCK, decimal donGia, int thoiGianDuKien, string? moTa, DateTime ngayApDung, string trangThai)
        {
            this.maDV = maDV;
            this.tenDV = tenDV;
            this.nhomDV = nhomDV;
            this.maCK = maCK;
            this.donGia = donGia;
            this.thoiGianDuKien = thoiGianDuKien;
            this.moTa = moTa;
            this.ngayApDung = ngayApDung;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaDV { get => maDV; set => maDV = value; }
        public string TenDV { get => tenDV; set => tenDV = value; }
        public string NhomDV { get => nhomDV; set => nhomDV = value; }
        public string MaCK { get => maCK; set => maCK = value; }
        public decimal DonGia { get => donGia; set => donGia = value; }
        public int ThoiGianDuKien { get => thoiGianDuKien; set => thoiGianDuKien = value; }
        public string? MoTa { get => moTa; set => moTa = value; }
        public DateTime NgayApDung { get => ngayApDung; set => ngayApDung = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
