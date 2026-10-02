using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_MAVAITRO
    {
        private string maVaiTro;
        private string tenVaiTro;
        private string moTa;

        public ET_MAVAITRO() { }

        public ET_MAVAITRO(string maVaiTro, string tenVaiTro, string moTa)
        {
            this.maVaiTro = maVaiTro;
            this.tenVaiTro = tenVaiTro;
            this.moTa = moTa;
        }

        public string MaVaiTro { get => maVaiTro; set => maVaiTro = value; }
        public string TenVaiTro { get => tenVaiTro; set => tenVaiTro = value; }
        public string MoTa { get => moTa; set => moTa = value; }
    }
}
