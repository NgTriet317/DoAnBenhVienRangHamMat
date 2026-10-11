using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("HO_SO_BENH_AN")]
    public class ET_HOSOBENHAN
    {
        private string maHS = string.Empty;
        private string maBN = string.Empty;
        private DateTime ngayLap;
        private string lyDoKhamBanDau = string.Empty;
        private string? ghiChu;
        private string trangThai = string.Empty;

        public ET_HOSOBENHAN() { }

        public ET_HOSOBENHAN(string maHS, string maBN, DateTime ngayLap, string lyDoKhamBanDau, string? ghiChu, string trangThai)
        {
            this.maHS = maHS;
            this.maBN = maBN;
            this.ngayLap = ngayLap;
            this.lyDoKhamBanDau = lyDoKhamBanDau;
            this.ghiChu = ghiChu;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaHS { get => maHS; set => maHS = value; }
        public string MaBN { get => maBN; set => maBN = value; }
        public DateTime NgayLap { get => ngayLap; set => ngayLap = value; }
        public string LyDoKhamBanDau { get => lyDoKhamBanDau; set => lyDoKhamBanDau = value; }
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
