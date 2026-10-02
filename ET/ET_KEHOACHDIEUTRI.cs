using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_KEHOACHDIEUTRI
    {
        private string maKHDT;
        private string maLS;
        private string maBS;
        private DateTime ngayLap;
        private DateTime ngayBatDau;
        private DateTime ngayDuKienKetThuc;
        private string trangThai;
        private string ghiChu;

        public ET_KEHOACHDIEUTRI() { }

        public ET_KEHOACHDIEUTRI(string maKHDT, string maLS, string maBS, DateTime ngayLap, DateTime ngayBatDau, DateTime ngayDuKienKetThuc, string trangThai, string ghiChu)
        {
            this.maKHDT = maKHDT;
            this.maLS = maLS;
            this.maBS = maBS;
            this.ngayLap = ngayLap;
            this.ngayBatDau = ngayBatDau;
            this.ngayDuKienKetThuc = ngayDuKienKetThuc;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        public string MaKHDT { get => maKHDT; set => maKHDT = value; }
        public string MaLS { get => maLS; set => maLS = value; }
        public string MaBS { get => maBS; set => maBS = value; }
        public DateTime NgayLap { get => ngayLap; set => ngayLap = value; }
        public DateTime NgayBatDau { get => ngayBatDau; set => ngayBatDau = value; }
        public DateTime NgayDuKienKetThuc { get => ngayDuKienKetThuc; set => ngayDuKienKetThuc = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
