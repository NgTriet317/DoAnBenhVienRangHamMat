using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace ET
{
    [Table("CHUC_VU")]
    public class ET_CHUCVU  
    {
        private string maChucVu;
        private string tenChucVu;
        private string moTa;

        public ET_CHUCVU() { }

        public ET_CHUCVU(string maChucVu, string tenChucVu, string moTa)
        {
            this.maChucVu = maChucVu;
            this.tenChucVu = tenChucVu;
            this.moTa = moTa;
        }

        [ExplicitKey]
        public string MaChucVu { get => maChucVu; set => maChucVu = value; }
        public string TenChucVu { get => tenChucVu; set => tenChucVu = value; }
        public string MoTa { get => moTa; set => moTa = value; }
    }
}
