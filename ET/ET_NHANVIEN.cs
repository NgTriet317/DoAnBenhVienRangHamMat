using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    internal class ET_NHANVIEN
    {
        private string maNV;
        private string hoTen;
        private string gioiTinh;
        private DateTime ngaySinh;
        private int sdt;
        private string diaChi;
        private string maChucVu;
        private string maCK;
        private string bangCap;
        private string ngayVaoLam;
        private string trangThai;

        public ET_NHANVIEN() { }

        public ET_NHANVIEN(string maNV, string hoTen, string gioiTinh, DateTime ngaySinh, int sdt, string diaChi, string maChucVu, string maCK, string bangCap, string ngayVaoLam, string trangThai)
        {
            this.maNV = maNV;
            this.hoTen = hoTen;
            this.gioiTinh = gioiTinh;
            this.ngaySinh = ngaySinh;
            this.sdt = sdt;
            this.diaChi = diaChi;
            this.maChucVu = maChucVu;
            this.maCK = maCK;
            this.bangCap = bangCap;
            this.ngayVaoLam = ngayVaoLam;
            this.trangThai = trangThai;
        }

        public string MaNV { get => maNV; set => maNV = value; }
        public string HoTen { get => hoTen; set => hoTen = value; }
        public string GioiTinh { get => gioiTinh; set => gioiTinh = value; }
        public DateTime NgaySinh { get => ngaySinh; set => ngaySinh = value; }
        public int Sdt { get => sdt; set => sdt = value; }
        public string DiaChi { get => diaChi; set => diaChi = value; }
        public string MaChucVu { get => maChucVu; set => maChucVu = value; }
        public string MaCK { get => maCK; set => maCK = value; }
        public string BangCap { get => bangCap; set => bangCap = value; }
        public string NgayVaoLam { get => ngayVaoLam; set => ngayVaoLam = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
