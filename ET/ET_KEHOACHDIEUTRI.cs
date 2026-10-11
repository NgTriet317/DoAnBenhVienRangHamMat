using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("KE_HOACH_DIEU_TRI")]
    public class ET_KEHOACHDIEUTRI
    {
        private string maKHDT = string.Empty;
        private string maLS = string.Empty;
        private string maBS = string.Empty;
        private DateTime ngayLap;
        private DateTime? ngayBatDau;
        private DateTime? ngayDuKienKetThuc;
        private decimal tongTienDuKien;
        private string trangThai = string.Empty;
        private string? ghiChu;

        public ET_KEHOACHDIEUTRI() { }

        public ET_KEHOACHDIEUTRI(string maKHDT, string maLS, string maBS, DateTime ngayLap, DateTime? ngayBatDau, DateTime? ngayDuKienKetThuc, decimal tongTienDuKien, string trangThai, string? ghiChu)
        {
            this.maKHDT = maKHDT;
            this.maLS = maLS;
            this.maBS = maBS;
            this.ngayLap = ngayLap;
            this.ngayBatDau = ngayBatDau;
            this.ngayDuKienKetThuc = ngayDuKienKetThuc;
            this.tongTienDuKien = tongTienDuKien;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        [ExplicitKey]
        public string MaKHDT { get => maKHDT; set => maKHDT = value; }
        public string MaLS { get => maLS; set => maLS = value; }
        public string MaBS { get => maBS; set => maBS = value; }
        public DateTime NgayLap { get => ngayLap; set => ngayLap = value; }
        public DateTime? NgayBatDau { get => ngayBatDau; set => ngayBatDau = value; }
        public DateTime? NgayDuKienKetThuc { get => ngayDuKienKetThuc; set => ngayDuKienKetThuc = value; }
        [Write(false)]
        public decimal TongTienDuKien { get => tongTienDuKien; set => tongTienDuKien = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
