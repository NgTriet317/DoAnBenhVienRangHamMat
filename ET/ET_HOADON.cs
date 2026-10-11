using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("HOA_DON")]
    public class ET_HOADON
    {
        private string maHD = string.Empty;
        private string maLS = string.Empty;
        private string maNV = string.Empty;
        private DateTime ngayLap;
        private DateTime? ngayXacNhan;
        private decimal tongTien;
        private decimal giamGia;
        private decimal soTienPhaiThu;
        private string trangThai = string.Empty;

        public ET_HOADON() { }

        public ET_HOADON(string maHD, string maLS, string maNV, DateTime ngayLap, DateTime? ngayXacNhan, decimal tongTien, decimal giamGia, decimal soTienPhaiThu, string trangThai)
        {
            this.maHD = maHD;
            this.maLS = maLS;
            this.maNV = maNV;
            this.ngayLap = ngayLap;
            this.ngayXacNhan = ngayXacNhan;
            this.tongTien = tongTien;
            this.giamGia = giamGia;
            this.soTienPhaiThu = soTienPhaiThu;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaHD { get => maHD; set => maHD = value; }
        public string MaLS { get => maLS; set => maLS = value; }
        public string MaNV { get => maNV; set => maNV = value; }
        public DateTime NgayLap { get => ngayLap; set => ngayLap = value; }
        public DateTime? NgayXacNhan { get => ngayXacNhan; set => ngayXacNhan = value; }
        [Write(false)]
        public decimal TongTien { get => tongTien; set => tongTien = value; }
        public decimal GiamGia { get => giamGia; set => giamGia = value; }
        [Write(false)]
        public decimal SoTienPhaiThu { get => soTienPhaiThu; set => soTienPhaiThu = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
