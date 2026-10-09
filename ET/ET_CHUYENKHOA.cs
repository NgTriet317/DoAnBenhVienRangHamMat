using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;

namespace ET
{
    [Table("CHUYEN_KHOA")]
    public class ET_CHUYENKHOA
    {
        private string maCK;
        private string maKhoa;
        private string tenCK;
        private string moTa;
        private string trangThai;
        public ET_CHUYENKHOA() { }

        public ET_CHUYENKHOA(string maCK, string maKhoa, string tenCK, string moTa, string trangThai)
        {
            this.maCK = maCK;
            this.maKhoa = maKhoa;
            this.tenCK = tenCK;
            this.moTa = moTa;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaCK { get => maCK; set => maCK = value; }
        public string MaKhoa { get => maKhoa; set => maKhoa = value; }
        public string TenCK { get => tenCK; set => tenCK = value; }
        public string MoTa { get => moTa; set => moTa = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
