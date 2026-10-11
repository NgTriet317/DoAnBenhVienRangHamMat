using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("PHIEU_KHAM_BENH")]
    public class ET_PHIEUKHAMBENH
    {
        private string maPhieu = string.Empty;
        private string maHS = string.Empty;
        private string? maLich;
        private string maPhong = string.Empty;
        private string? maBS;
        private string maNVTiepNhan = string.Empty;
        private string? maDV;
        private int soThuTu;
        private DateTime thoiGianTiepNhan;
        private string lyDoKham = string.Empty;
        private string trangThai = string.Empty;
        private string? ghiChu;

        public ET_PHIEUKHAMBENH() { }

        public ET_PHIEUKHAMBENH(string maPhieu, string maHS, string? maLich, string maPhong, string? maBS, string maNVTiepNhan, string? maDV, int soThuTu, DateTime thoiGianTiepNhan, string lyDoKham, string trangThai, string? ghiChu)
        {
            this.maPhieu = maPhieu;
            this.maHS = maHS;
            this.maLich = maLich;
            this.maPhong = maPhong;
            this.maBS = maBS;
            this.maNVTiepNhan = maNVTiepNhan;
            this.maDV = maDV;
            this.soThuTu = soThuTu;
            this.thoiGianTiepNhan = thoiGianTiepNhan;
            this.lyDoKham = lyDoKham;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        [ExplicitKey]
        public string MaPhieu { get => maPhieu; set => maPhieu = value; }
        public string MaHS { get => maHS; set => maHS = value; }
        public string? MaLich { get => maLich; set => maLich = value; }
        public string MaPhong { get => maPhong; set => maPhong = value; }
        public string? MaBS { get => maBS; set => maBS = value; }
        public string MaNVTiepNhan { get => maNVTiepNhan; set => maNVTiepNhan = value; }
        public string? MaDV { get => maDV; set => maDV = value; }
        public int SoThuTu { get => soThuTu; set => soThuTu = value; }
        public DateTime ThoiGianTiepNhan { get => thoiGianTiepNhan; set => thoiGianTiepNhan = value; }
        public string LyDoKham { get => lyDoKham; set => lyDoKham = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
