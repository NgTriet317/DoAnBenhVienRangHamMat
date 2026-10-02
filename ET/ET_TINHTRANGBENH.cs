using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_TINHTRANGBENH
    {
        private string maTTB;
        private string maLS;
        private string loaiTinhTrang;
        private string moTaTinhTrang;
        private string mucDo;
        private string ghiChu;

        public ET_TINHTRANGBENH() { }

        public ET_TINHTRANGBENH(string maTTB, string maLS, string loaiTinhTrang, string moTaTinhTrang, string mucDo, string ghiChu)
        {
            this.maTTB = maTTB;
            this.maLS = maLS;
            this.loaiTinhTrang = loaiTinhTrang;
            this.moTaTinhTrang = moTaTinhTrang;
            this.mucDo = mucDo;
            this.ghiChu = ghiChu;
        }

        public string MaTTB { get => maTTB; set => maTTB = value; }
        public string MaLS { get => maLS; set => maLS = value; }
        public string LoaiTinhTrang { get => loaiTinhTrang; set => loaiTinhTrang = value; }
        public string MoTaTinhTrang { get => moTaTinhTrang; set => moTaTinhTrang = value; }
        public string MucDo { get => mucDo; set => mucDo = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
