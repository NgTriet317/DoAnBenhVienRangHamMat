using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("CHI_TIET_DON_THUOC")]
    public class ET_CHITIETDONTHUOC
    {
        private string maCTDT = string.Empty;
        private string maDT = string.Empty;
        private string maThuoc = string.Empty;
        private string lieuDung = string.Empty;
        private decimal soLuong;
        private int soNgayDung;
        private string huongDan = string.Empty;

        public ET_CHITIETDONTHUOC() { }

        public ET_CHITIETDONTHUOC(string maCTDT, string maDT, string maThuoc, string lieuDung, decimal soLuong, int soNgayDung, string huongDan)
        {
            this.maCTDT = maCTDT;
            this.maDT = maDT;
            this.maThuoc = maThuoc;
            this.lieuDung = lieuDung;
            this.soLuong = soLuong;
            this.soNgayDung = soNgayDung;
            this.huongDan = huongDan;
        }

        [ExplicitKey]
        public string MaCTDT { get => maCTDT; set => maCTDT = value; }
        public string MaDT { get => maDT; set => maDT = value; }
        public string MaThuoc { get => maThuoc; set => maThuoc = value; }
        public string LieuDung { get => lieuDung; set => lieuDung = value; }
        public decimal SoLuong { get => soLuong; set => soLuong = value; }
        public int SoNgayDung { get => soNgayDung; set => soNgayDung = value; }
        public string HuongDan { get => huongDan; set => huongDan = value; }
    }
}
