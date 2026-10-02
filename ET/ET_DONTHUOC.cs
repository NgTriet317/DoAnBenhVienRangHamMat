using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_DONTHUOC
    {
        private string maDT;
        private string maHS;
        private string maBS;
        private DateTime ngayKe;
        private string lyDoThayThe;
        private string trangThai;
        private string ghiChu;

        public ET_DONTHUOC() { }

        public ET_DONTHUOC(string maDT, string maHS, string maBS, DateTime ngayKe, string lyDoThayThe, string trangThai, string ghiChu)
        {
            this.maDT = maDT;
            this.maHS = maHS;
            this.maBS = maBS;
            this.ngayKe = ngayKe;
            this.lyDoThayThe = lyDoThayThe;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        public string MaDT { get => maDT; set => maDT = value; }
        public string MaHS { get => maHS; set => maHS = value; }
        public string MaBS { get => maBS; set => maBS = value; }
        public DateTime NgayKe { get => ngayKe; set => ngayKe = value; }
        public string LyDoThayThe { get => lyDoThayThe; set => lyDoThayThe = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
