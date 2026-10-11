using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("PHIEU_XUAT")]
    public class ET_PHIEUXUAT
    {
        private string maPX = string.Empty;
        private string maKho = string.Empty;
        private string maNV = string.Empty;
        private string? maKhoa;
        private string? maPhong;
        private string? maLS;
        private DateTime ngayXuat;
        private string lyDo = string.Empty;
        private string trangThai = string.Empty;
        private string? ghiChu;

        public ET_PHIEUXUAT() { }

        public ET_PHIEUXUAT(string maPX, string maKho, string maNV, string? maKhoa, string? maPhong, string? maLS, DateTime ngayXuat, string lyDo, string trangThai, string? ghiChu)
        {
            this.maPX = maPX;
            this.maKho = maKho;
            this.maNV = maNV;
            this.maKhoa = maKhoa;
            this.maPhong = maPhong;
            this.maLS = maLS;
            this.ngayXuat = ngayXuat;
            this.lyDo = lyDo;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        [ExplicitKey]
        public string MaPX { get => maPX; set => maPX = value; }
        public string MaKho { get => maKho; set => maKho = value; }
        public string MaNV { get => maNV; set => maNV = value; }
        public string? MaKhoa { get => maKhoa; set => maKhoa = value; }
        public string? MaPhong { get => maPhong; set => maPhong = value; }
        public string? MaLS { get => maLS; set => maLS = value; }
        public DateTime NgayXuat { get => ngayXuat; set => ngayXuat = value; }
        public string LyDo { get => lyDo; set => lyDo = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
