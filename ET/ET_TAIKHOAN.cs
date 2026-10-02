using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_TAIKHOAN
    {
        private string maTK;
        private string tenDangNhap;
        private string matKhauHash;
        private string maVaiTro;
        private string maNV;
        private string maKhoa;
        private string maPhong;        
        private string trangThai;
        public ET_TAIKHOAN() { }

        public ET_TAIKHOAN(string maTK, string tenDangNhap, string matKhauHash, string maVaiTro, string maNV, string maKhoa, string maPhong, string trangThai)
        {
            this.maTK = maTK;
            this.tenDangNhap = tenDangNhap;
            this.matKhauHash = matKhauHash;
            this.maVaiTro = maVaiTro;
            this.maNV = maNV;
            this.maKhoa = maKhoa;
            this.maPhong = maPhong;
            this.trangThai = trangThai;
        }

        public string MaTK { get => maTK; set => maTK = value; }
        public string TenDangNhap { get => tenDangNhap; set => tenDangNhap = value; }
        public string MatKhauHash { get => matKhauHash; set => matKhauHash = value; }
        public string MaVaiTro { get => maVaiTro; set => maVaiTro = value; }
        public string MaNV { get => maNV; set => maNV = value; }
        public string MaKhoa { get => maKhoa; set => maKhoa = value; }
        public string MaPhong { get => maPhong; set => maPhong = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
