using System;
using Dapper.Contrib.Extensions;

namespace ET
{
    [Table("CA_LAM")]
    public class ET_CALAM
    {
        private string maCa = string.Empty;
        private TimeSpan gioBatDau;
        private TimeSpan gioKetThuc;
        private string trangThai = string.Empty;
        private string? ghiChu;

        public ET_CALAM() { }

        public ET_CALAM(string maCa, TimeSpan gioBatDau, TimeSpan gioKetThuc, string trangThai, string? ghiChu)
        {
            this.maCa = maCa;
            this.gioBatDau = gioBatDau;
            this.gioKetThuc = gioKetThuc;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        [ExplicitKey]
        public string MaCa { get => maCa; set => maCa = value; }
        public TimeSpan GioBatDau { get => gioBatDau; set => gioBatDau = value; }
        public TimeSpan GioKetThuc { get => gioKetThuc; set => gioKetThuc = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
