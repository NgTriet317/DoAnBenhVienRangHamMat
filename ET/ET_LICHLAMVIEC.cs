using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_LICHLAMVIEC
    {
        private string maLLV;
        private string maNV;
        private string maCa;
        private string maPhong;
        private DateTime ngayLam;
        private string trangThai;
        private string trangThaiHienDien;
        private DateTime gioVao;
        private DateTime gioRa;
        private string ghiChu;

        public ET_LICHLAMVIEC(string maLLV, string maNV, string maCa, string maPhong, DateTime ngayLam, string trangThai, string trangThaiHienDien, DateTime gioVao, DateTime gioRa, string ghiChu)
        {
            this.MaLLV = maLLV;
            this.MaNV = maNV;
            this.MaCa = maCa;
            this.MaPhong = maPhong;
            this.NgayLam = ngayLam;
            this.TrangThai = trangThai;
            this.TrangThaiHienDien = trangThaiHienDien;
            this.GioVao = gioVao;
            this.GioRa = gioRa;
            this.GhiChu = ghiChu;
        }

        public ET_LICHLAMVIEC()
        {
        }

        public string MaLLV { get => maLLV; set => maLLV = value; }
        public string MaNV { get => maNV; set => maNV = value; }
        public string MaCa { get => maCa; set => maCa = value; }
        public string MaPhong { get => maPhong; set => maPhong = value; }
        public DateTime NgayLam { get => ngayLam; set => ngayLam = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string TrangThaiHienDien { get => trangThaiHienDien; set => trangThaiHienDien = value; }
        public DateTime GioVao { get => gioVao; set => gioVao = value; }
        public DateTime GioRa { get => gioRa; set => gioRa = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
