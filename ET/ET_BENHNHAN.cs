using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("BENH_NHAN")]
    public class ET_BENHNHAN
    {
        private string maBN = string.Empty;
        private string hoTenBN = string.Empty;
        private string? gioiTinh;
        private DateTime? ngaySinh;
        private string? sdt;
        private string? diaChi;
        private string? maBHYT;
        private string? nhomMau;
        private string? tienSuBenh;
        private string? diUng;
        private string trangThai = string.Empty;

        public ET_BENHNHAN() { }

        public ET_BENHNHAN(string maBN, string hoTenBN, string? gioiTinh, DateTime? ngaySinh, string? sdt, string? diaChi, string? maBHYT, string? nhomMau, string? tienSuBenh, string? diUng, string trangThai)
        {
            this.maBN = maBN;
            this.hoTenBN = hoTenBN;
            this.gioiTinh = gioiTinh;
            this.ngaySinh = ngaySinh;
            this.sdt = sdt;
            this.diaChi = diaChi;
            this.maBHYT = maBHYT;
            this.nhomMau = nhomMau;
            this.tienSuBenh = tienSuBenh;
            this.diUng = diUng;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaBN { get => maBN; set => maBN = value; }
        public string HoTenBN { get => hoTenBN; set => hoTenBN = value; }
        public string? GioiTinh { get => gioiTinh; set => gioiTinh = value; }
        public DateTime? NgaySinh { get => ngaySinh; set => ngaySinh = value; }
        public string? SDT { get => sdt; set => sdt = value; }
        public string? DiaChi { get => diaChi; set => diaChi = value; }
        public string? MaBHYT { get => maBHYT; set => maBHYT = value; }
        public string? NhomMau { get => nhomMau; set => nhomMau = value; }
        public string? TienSuBenh { get => tienSuBenh; set => tienSuBenh = value; }
        public string? DiUng { get => diUng; set => diUng = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
