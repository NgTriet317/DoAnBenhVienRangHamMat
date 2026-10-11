using System;
using Dapper.Contrib.Extensions;

namespace ET
{
    [Table("KHO")]
    public class ET_KHO
    {
        private string maKho = string.Empty;
        private string tenKho = string.Empty;
        private string maNV = string.Empty;
        private string loaiKho = string.Empty;
        private string trangThai = string.Empty;

        public ET_KHO() { }

        public ET_KHO(string maKho, string tenKho, string maNV, string loaiKho, string trangThai)
        {
            this.maKho = maKho;
            this.tenKho = tenKho;
            this.maNV = maNV;
            this.loaiKho = loaiKho;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaKho { get => maKho; set => maKho = value; }
        public string TenKho { get => tenKho; set => tenKho = value; }
        public string MaNV { get => maNV; set => maNV = value; }
        public string LoaiKho { get => loaiKho; set => loaiKho = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
