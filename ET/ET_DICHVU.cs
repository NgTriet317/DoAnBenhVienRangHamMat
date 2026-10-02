using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_DICHVU
    {
        private string maDV;
        private string tenDV;
        private string nhomDV;
        private string maCK;
        private decimal donGia;
        private string thoiGianDuKien;
        private string moTa;
        private DateTime ngayApDung;
        private string trangThai;

        public ET_DICHVU() { }

        public ET_DICHVU(string maDV, string tenDV, string nhomDV, string maCK, decimal donGia, string thoiGianDuKien, string moTa, DateTime ngayApDung, string trangThai)
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

        public string MaDV { get => maDV; set => maDV = value; }
        public string TenDV { get => tenDV; set => tenDV = value; }
        public string NhomDV { get => nhomDV; set => nhomDV = value; }
        public string MaCK { get => maCK; set => maCK = value; }
        public decimal DonGia { get => donGia; set => donGia = value; }
        public string ThoiGianDuKien { get => thoiGianDuKien; set => thoiGianDuKien = value; }
        public string MoTa { get => moTa; set => moTa = value; }
        public DateTime NgayApDung { get => ngayApDung; set => ngayApDung = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
