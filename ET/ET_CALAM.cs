using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_CALAM
    {
        private string maCa;
        private DateTime gioBatDau;
        private DateTime gioKetThuc;
        private string loaiCa;
        private string trangThai;
        private string ghiChu;

        public ET_CALAM(string maCa, DateTime gioBatDau, DateTime gioKetThuc, string loaiCa, string trangThai, string ghiChu)
        {
            this.MaCa = maCa;
            this.GioBatDau = gioBatDau;
            this.GioKetThuc = gioKetThuc;
            this.LoaiCa = loaiCa;
            this.TrangThai = trangThai;
            this.GhiChu = ghiChu;
        }

        public ET_CALAM()
        {
        }

        public string MaCa { get => maCa; set => maCa = value; }
        public DateTime GioBatDau { get => gioBatDau; set => gioBatDau = value; }
        public DateTime GioKetThuc { get => gioKetThuc; set => gioKetThuc = value; }
        public string LoaiCa { get => loaiCa; set => loaiCa = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
