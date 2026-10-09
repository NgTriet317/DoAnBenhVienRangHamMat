using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    [Table("NHAN_VIEN")]
    public class ET_NHANVIEN
    {
        
        private string maNV;
        private string hoTen;
        private string gioiTinh;
        private DateTime ngaySinh;
        private string sdt;
        private string diaChi;
        private string maChucVu;
        private string maCK;
        private string bangCap;
        private DateTime ngayVaoLam;
        private string trangThai;

        public ET_NHANVIEN() { }

        public ET_NHANVIEN(string maNV, string hoTen, string gioiTinh, DateTime ngaySinh, string sdt, string diaChi, string maChucVu, string maCK, string bangCap, DateTime ngayVaoLam, string trangThai)
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

        [ExplicitKey]
        public string MaNV { get => maNV; set => maNV = value; }
        public string HoTen { get => hoTen; set => hoTen = value; }
        public string GioiTinh { get => gioiTinh; set => gioiTinh = value; }
        public DateTime NgaySinh { get => ngaySinh; set => ngaySinh = value; }
        public string SDT { get => sdt; set => sdt = value; }
        public string DiaChi { get => diaChi; set => diaChi = value; }
        public string MaChucVu { get => maChucVu; set => maChucVu = value; }
        public string MaCK { get => maCK; set => maCK = value; }
        public string BangCap { get => bangCap; set => bangCap = value; }
        public DateTime NgayVaoLam { get => ngayVaoLam; set => ngayVaoLam = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
