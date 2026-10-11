using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("THUOC")]
    public class ET_THUOC
    {
        private string maThuoc = string.Empty;
        private string maHH = string.Empty;
        private string hamLuong = string.Empty;
        private decimal mucTonToiThieu;
        private decimal donGiaBan;

        public ET_THUOC() { }

        public ET_THUOC(string maThuoc, string maHH, string hamLuong, decimal mucTonToiThieu, decimal donGiaBan)
        {
            this.maThuoc = maThuoc;
            this.maHH = maHH;
            this.hamLuong = hamLuong;
            this.mucTonToiThieu = mucTonToiThieu;
            this.donGiaBan = donGiaBan;
        }

        [ExplicitKey]
        public string MaThuoc { get => maThuoc; set => maThuoc = value; }
        public string MaHH { get => maHH; set => maHH = value; }
        public string HamLuong { get => hamLuong; set => hamLuong = value; }
        public decimal MucTonToiThieu { get => mucTonToiThieu; set => mucTonToiThieu = value; }
        public decimal DonGiaBan { get => donGiaBan; set => donGiaBan = value; }
    }
}
