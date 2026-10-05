using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_PHIEUXUAT
    {
        private string maPX;
        private string maKho;
        private string maNV;
        private string maKhoa;
        private string maPhong;
        private string maLS;
        private DateTime ngayXuat;
        private string lyDo;
        private string trangThai;
        private string ghiChu;

        public ET_PHIEUXUAT(string maPX, string maKho, string maNV, string maKhoa, string maPhong, string maLS, DateTime ngayXuat, string lyDo, string trangThai, string ghiChu)
        {
            this.MaPX = maPX;
            this.MaKho = maKho;
            this.MaNV = maNV;
            this.MaKhoa = maKhoa;
            this.MaPhong = maPhong;
            this.MaLS = maLS;
            this.NgayXuat = ngayXuat;
            this.LyDo = lyDo;
            this.TrangThai = trangThai;
            this.GhiChu = ghiChu;
        }

        public ET_PHIEUXUAT()
        {
        }

        public string MaPX { get => maPX; set => maPX = value; }
        public string MaKho { get => maKho; set => maKho = value; }
        public string MaNV { get => maNV; set => maNV = value; }
        public string MaKhoa { get => maKhoa; set => maKhoa = value; }
        public string MaPhong { get => maPhong; set => maPhong = value; }
        public string MaLS { get => maLS; set => maLS = value; }
        public DateTime NgayXuat { get => ngayXuat; set => ngayXuat = value; }
        public string LyDo { get => lyDo; set => lyDo = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
