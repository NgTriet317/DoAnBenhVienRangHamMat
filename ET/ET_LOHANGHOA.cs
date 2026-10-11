using System;
using Dapper.Contrib.Extensions;

namespace ET
{
    [Table("LO_HANG_HOA")]
    public class ET_LOHANGHOA
    {
        private string maLo = string.Empty;
        private string maHH = string.Empty;
        private string maNCC = string.Empty;
        private string maKho = string.Empty;
        private string? soLo;
        private DateTime ngayNhap;
        private DateTime? hanSuDung;
        private string trangThai = string.Empty;

        public ET_LOHANGHOA() { }

        public ET_LOHANGHOA(string maLo, string maHH, string maNCC, string maKho, string? soLo, DateTime ngayNhap, DateTime? hanSuDung, string trangThai)
        {
            this.maLo = maLo;
            this.maHH = maHH;
            this.maNCC = maNCC;
            this.maKho = maKho;
            this.soLo = soLo;
            this.ngayNhap = ngayNhap;
            this.hanSuDung = hanSuDung;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaLo { get => maLo; set => maLo = value; }
        public string MaHH { get => maHH; set => maHH = value; }
        public string MaNCC { get => maNCC; set => maNCC = value; }
        public string MaKho { get => maKho; set => maKho = value; }
        public string? SoLo { get => soLo; set => soLo = value; }
        public DateTime NgayNhap { get => ngayNhap; set => ngayNhap = value; }
        public DateTime? HanSuDung { get => hanSuDung; set => hanSuDung = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
