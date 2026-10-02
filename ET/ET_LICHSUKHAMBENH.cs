using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    {
        private string maLS;
        private string maHS;
        private string maLich;
        private string maBS;
        private string maKhoa;
        private string maPhong;
        private DateTime thoiGianKham;
        private string trieuChung;
        private string chanDoan;
        private string keHoachXuLy;
        private string trangThai;
        private DateTime ngayXacNhan;
        private string ghiChu;

        public ET_LICHSUKHAMBENH() { }
        public ET_LICHSUKHAMBENH(string maLS, string maHS, string maLich, string maBS, string maKhoa, string maPhong, DateTime thoiGianKham, string trieuChung, string chanDoan, string keHoachXuLy, string trangThai, DateTime ngayXacNhan, string ghiChu)
        {
            this.maLS = maLS;
            this.maHS = maHS;
            this.maLich = maLich;
            this.maBS = maBS;
            this.maKhoa = maKhoa;
            this.maPhong = maPhong;
            this.thoiGianKham = thoiGianKham;
            this.trieuChung = trieuChung;
            this.chanDoan = chanDoan;
            this.keHoachXuLy = keHoachXuLy;
            this.trangThai = trangThai;
            this.ngayXacNhan = ngayXacNhan;
            this.ghiChu = ghiChu;
        }

        public string MaLS { get => maLS; set => maLS = value; }
        public string MaHS { get => maHS; set => maHS = value; }
        public string MaLich { get => maLich; set => maLich = value; }
        public string MaBS { get => maBS; set => maBS = value; }
        public string MaKhoa { get => maKhoa; set => maKhoa = value; }
        public string MaPhong { get => maPhong; set => maPhong = value; }
        public DateTime ThoiGianKham { get => thoiGianKham; set => thoiGianKham = value; }
        public string TrieuChung { get => trieuChung; set => trieuChung = value; }
        public string ChanDoan { get => chanDoan; set => chanDoan = value; }
        public string KeHoachXuLy { get => keHoachXuLy; set => keHoachXuLy = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public DateTime NgayXacNhan { get => ngayXacNhan; set => ngayXacNhan = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
