using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_CHUNGCHINHANVIEN
    {
        private string maCC;
        private string maNV;
        private int soChungChi;
        private string tenChungChi;
        private DateTime ngayCap;
        private string noiCap;
        private DateTime hanChungChi;
        private string trangThai;
        private string ghiChu;

        public ET_CHUNGCHINHANVIEN() { }

        public ET_CHUNGCHINHANVIEN(string maCC, string maNV, int soChungChi, string tenChungChi, DateTime ngayCap, string noiCap, DateTime hanChungChi, string trangThai, string ghiChu)
        {
            this.maCC = maCC;
            this.maNV = maNV;
            this.soChungChi = soChungChi;
            this.tenChungChi = tenChungChi;
            this.ngayCap = ngayCap;
            this.noiCap = noiCap;
            this.hanChungChi = hanChungChi;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        public string MaCC { get => maCC; set => maCC = value; }
        public string MaNV { get => maNV; set => maNV = value; }
        public int SoChungChi { get => soChungChi; set => soChungChi = value; }
        public string TenChungChi { get => tenChungChi; set => tenChungChi = value; }
        public DateTime NgayCap { get => ngayCap; set => ngayCap = value; }
        public string NoiCap { get => noiCap; set => noiCap = value; }
        public DateTime HanChungChi { get => hanChungChi; set => hanChungChi = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
