using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("DON_THUOC")]
    public class ET_DONTHUOC
    {
        private string maDT = string.Empty;
        private string maLS = string.Empty;
        private string maBS = string.Empty;
        private DateTime ngayKe;
        private string? lyDoThayThe;
        private string trangThai = string.Empty;
        private string? ghiChu;

        public ET_DONTHUOC() { }

        public ET_DONTHUOC(string maDT, string maLS, string maBS, DateTime ngayKe, string? lyDoThayThe, string trangThai, string? ghiChu)
        {
            this.maDT = maDT;
            this.maLS = maLS;
            this.maBS = maBS;
            this.ngayKe = ngayKe;
            this.lyDoThayThe = lyDoThayThe;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        [ExplicitKey]
        public string MaDT { get => maDT; set => maDT = value; }
        public string MaLS { get => maLS; set => maLS = value; }
        public string MaBS { get => maBS; set => maBS = value; }
        public DateTime NgayKe { get => ngayKe; set => ngayKe = value; }
        public string? LyDoThayThe { get => lyDoThayThe; set => lyDoThayThe = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
