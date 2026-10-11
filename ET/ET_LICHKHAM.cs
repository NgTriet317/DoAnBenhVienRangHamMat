using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("LICH_KHAM")]
    public class ET_LICHKHAM
    {
        private string maLich = string.Empty;
        private string maBN = string.Empty;
        private string maBS = string.Empty;
        private string maCa = string.Empty;
        private string maPhong = string.Empty;
        private string? maDV;
        private DateTime ngayGioKham;
        private int thoiLuongDuKien;
        private string uuTien = string.Empty;
        private string lyDoKham = string.Empty;
        private string? lyDoThayDoi;
        private string trangThai = string.Empty;
        private string? ghiChu;
        private DateTime ngayTao;
        private DateTime? ngayCapNhat;

        public ET_LICHKHAM() { }

        public ET_LICHKHAM(string maLich, string maBN, string maBS, string maCa, string maPhong, string? maDV, DateTime ngayGioKham, int thoiLuongDuKien, string uuTien, string lyDoKham, string? lyDoThayDoi, string trangThai, string? ghiChu, DateTime ngayTao, DateTime? ngayCapNhat)
        {
            this.maLich = maLich;
            this.maBN = maBN;
            this.maBS = maBS;
            this.maCa = maCa;
            this.maPhong = maPhong;
            this.maDV = maDV;
            this.ngayGioKham = ngayGioKham;
            this.thoiLuongDuKien = thoiLuongDuKien;
            this.uuTien = uuTien;
            this.lyDoKham = lyDoKham;
            this.lyDoThayDoi = lyDoThayDoi;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
            this.ngayTao = ngayTao;
            this.ngayCapNhat = ngayCapNhat;
        }

        [ExplicitKey]
        public string MaLich { get => maLich; set => maLich = value; }
        public string MaBN { get => maBN; set => maBN = value; }
        public string MaBS { get => maBS; set => maBS = value; }
        public string MaCa { get => maCa; set => maCa = value; }
        public string MaPhong { get => maPhong; set => maPhong = value; }
        public string? MaDV { get => maDV; set => maDV = value; }
        public DateTime NgayGioKham { get => ngayGioKham; set => ngayGioKham = value; }
        public int ThoiLuongDuKien { get => thoiLuongDuKien; set => thoiLuongDuKien = value; }
        public string UuTien { get => uuTien; set => uuTien = value; }
        public string LyDoKham { get => lyDoKham; set => lyDoKham = value; }
        public string? LyDoThayDoi { get => lyDoThayDoi; set => lyDoThayDoi = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
        public DateTime NgayTao { get => ngayTao; set => ngayTao = value; }
        public DateTime? NgayCapNhat { get => ngayCapNhat; set => ngayCapNhat = value; }
    }
}
