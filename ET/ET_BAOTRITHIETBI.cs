using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_BAOTRITHIETBI
    {
        private string maBT;
        private string maTB;
        private string congTyBaoTri;
        private DateTime ngayBatDau;
        private DateTime ngayKetThuc;
        private string loaiBaoTri;
        private string noiDung;
        private decimal chiPhi;
        private DateTime ngayBaoTriTiepTheo;
        private string trangThai;
        private string ghiChu;

        public ET_BAOTRITHIETBI(string maBT, string maTB, string congTyBaoTri, DateTime ngayBatDau, DateTime ngayKetThuc, string loaiBaoTri, string noiDung, decimal chiPhi, DateTime ngayBaoTriTiepTheo, string trangThai, string ghiChu)
        {
            this.MaBT = maBT;
            this.MaTB = maTB;
            this.CongTyBaoTri = congTyBaoTri;
            this.NgayBatDau = ngayBatDau;
            this.NgayKetThuc = ngayKetThuc;
            this.LoaiBaoTri = loaiBaoTri;
            this.NoiDung = noiDung;
            this.ChiPhi = chiPhi;
            this.NgayBaoTriTiepTheo = ngayBaoTriTiepTheo;
            this.TrangThai = trangThai;
            this.GhiChu = ghiChu;
        }

        public ET_BAOTRITHIETBI()
        {
        }

        public string MaBT { get => maBT; set => maBT = value; }
        public string MaTB { get => maTB; set => maTB = value; }
        public string CongTyBaoTri { get => congTyBaoTri; set => congTyBaoTri = value; }
        public DateTime NgayBatDau { get => ngayBatDau; set => ngayBatDau = value; }
        public DateTime NgayKetThuc { get => ngayKetThuc; set => ngayKetThuc = value; }
        public string LoaiBaoTri { get => loaiBaoTri; set => loaiBaoTri = value; }
        public string NoiDung { get => noiDung; set => noiDung = value; }
        public decimal ChiPhi { get => chiPhi; set => chiPhi = value; }
        public DateTime NgayBaoTriTiepTheo { get => ngayBaoTriTiepTheo; set => ngayBaoTriTiepTheo = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
