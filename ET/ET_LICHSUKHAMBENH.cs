using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("LICH_SU_KHAM_BENH")]
    public class ET_LICHSUKHAMBENH
    {
        private string maLS = string.Empty;
        private string maPhieu = string.Empty;
        private string maBS = string.Empty;
        private string maPhong = string.Empty;
        private DateTime thoiGianKham;
        private string trieuChung = string.Empty;
        private string chanDoan = string.Empty;
        private string keHoachXuLy = string.Empty;
        private string trangThai = string.Empty;
        private DateTime? ngayXacNhan;
        private string? ghiChu;

        public ET_LICHSUKHAMBENH() { }

        public ET_LICHSUKHAMBENH(string maLS, string maPhieu, string maBS, string maPhong, DateTime thoiGianKham, string trieuChung, string chanDoan, string keHoachXuLy, string trangThai, DateTime? ngayXacNhan, string? ghiChu)
        {
            this.maLS = maLS;
            this.maPhieu = maPhieu;
            this.maBS = maBS;
            this.maPhong = maPhong;
            this.thoiGianKham = thoiGianKham;
            this.trieuChung = trieuChung;
            this.chanDoan = chanDoan;
            this.keHoachXuLy = keHoachXuLy;
            this.trangThai = trangThai;
            this.ngayXacNhan = ngayXacNhan;
            this.ghiChu = ghiChu;
        }

        [ExplicitKey]
        public string MaLS { get => maLS; set => maLS = value; }
        public string MaPhieu { get => maPhieu; set => maPhieu = value; }
        public string MaBS { get => maBS; set => maBS = value; }
        public string MaPhong { get => maPhong; set => maPhong = value; }
        public DateTime ThoiGianKham { get => thoiGianKham; set => thoiGianKham = value; }
        public string TrieuChung { get => trieuChung; set => trieuChung = value; }
        public string ChanDoan { get => chanDoan; set => chanDoan = value; }
        public string KeHoachXuLy { get => keHoachXuLy; set => keHoachXuLy = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public DateTime? NgayXacNhan { get => ngayXacNhan; set => ngayXacNhan = value; }
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
