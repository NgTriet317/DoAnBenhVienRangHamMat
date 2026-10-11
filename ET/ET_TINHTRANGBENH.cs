using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    [Table("TINH_TRANG_BENH")]
    public class ET_TINHTRANGBENH
    {
        private string maTTB = string.Empty;
        private string maLS = string.Empty;
        private string loaiTinhTrang = string.Empty;
        private string moTaTinhTrang = string.Empty;
        private string mucDo = string.Empty;
        private string? ghiChu;

        public ET_TINHTRANGBENH() { }

        public ET_TINHTRANGBENH(string maTTB, string maLS, string loaiTinhTrang, string moTaTinhTrang, string mucDo, string? ghiChu)
        {
            this.maTTB = maTTB;
            this.maLS = maLS;
            this.loaiTinhTrang = loaiTinhTrang;
            this.moTaTinhTrang = moTaTinhTrang;
            this.mucDo = mucDo;
            this.ghiChu = ghiChu;
        }

        [ExplicitKey]
        public string MaTTB { get => maTTB; set => maTTB = value; }
        public string MaLS { get => maLS; set => maLS = value; }
        public string LoaiTinhTrang { get => loaiTinhTrang; set => loaiTinhTrang = value; }
        public string MoTaTinhTrang { get => moTaTinhTrang; set => moTaTinhTrang = value; }
        public string MucDo { get => mucDo; set => mucDo = value; }
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
