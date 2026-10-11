using System;
using Dapper.Contrib.Extensions;

namespace ET
{
    [Table("PHONG")]
    public class ET_PHONG
    {
        private string maPhong = string.Empty;
        private string tenPhong = string.Empty;
        private string maKhoa = string.Empty;
        private string loaiPhong = string.Empty;
        private string tinhTrang = string.Empty;
        private string trangThai = string.Empty;

        public ET_PHONG() { }

        public ET_PHONG(string maPhong, string tenPhong, string maKhoa, string loaiPhong, string tinhTrang, string trangThai)
        {
            this.maPhong = maPhong;
            this.tenPhong = tenPhong;
            this.maKhoa = maKhoa;
            this.loaiPhong = loaiPhong;
            this.tinhTrang = tinhTrang;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaPhong { get => maPhong; set => maPhong = value; }
        public string TenPhong { get => tenPhong; set => tenPhong = value; }
        public string MaKhoa { get => maKhoa; set => maKhoa = value; }
        public string LoaiPhong { get => loaiPhong; set => loaiPhong = value; }
        public string TinhTrang { get => tinhTrang; set => tinhTrang = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
