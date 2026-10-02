using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_CHITIETDONTHUOC
    {
        private string maCTDT;
        private string maDT;
        private string maThuoc;
        private string lieuDung;
        private string soLuong;
        private string donViTinh;
        private int soNgayDung;
        private string huongDan;

        public ET_CHITIETDONTHUOC() { }

        public ET_CHITIETDONTHUOC(string maCTDT, string maDT, string maThuoc, string lieuDung, string soLuong, string donViTinh, int soNgayDung, string huongDan)
        {
            this.maCTDT = maCTDT;
            this.maDT = maDT;
            this.maThuoc = maThuoc;
            this.lieuDung = lieuDung;
            this.soLuong = soLuong;
            this.donViTinh = donViTinh;
            this.soNgayDung = soNgayDung;
            this.huongDan = huongDan;
        }

        public string MaCTDT { get => maCTDT; set => maCTDT = value; }
        public string MaDT { get => maDT; set => maDT = value; }
        public string MaThuoc { get => maThuoc; set => maThuoc = value; }
        public string LieuDung { get => lieuDung; set => lieuDung = value; }
        public string SoLuong { get => soLuong; set => soLuong = value; }
        public string DonViTinh { get => donViTinh; set => donViTinh = value; }
        public int SoNgayDung { get => soNgayDung; set => soNgayDung = value; }
        public string HuongDan { get => huongDan; set => huongDan = value; }
    }
}
