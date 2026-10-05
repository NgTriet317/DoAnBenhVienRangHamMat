using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_THIETBI
    {
        private string maTB;
        private string maHH;
        private string tenTB;
        private string serial;
        private string maNCC;
        private string maPhong;
        private string tinhTrang;
        private string hanBaoHanh;
        private string trangThai;
        private string ghiChu;

        public ET_THIETBI() { }

        public ET_THIETBI(string maTB, string maHH, string tenTB, string serial, string maNCC, string maPhong, string tinhTrang, string hanBaoHanh, string trangThai, string ghiChu)
        {
            this.maTB = maTB;
            this.maHH = maHH;
            this.tenTB = tenTB;
            this.serial = serial;
            this.maNCC = maNCC;
            this.maPhong = maPhong;
            this.tinhTrang = tinhTrang;
            this.hanBaoHanh = hanBaoHanh;
            this.trangThai = trangThai;
            this.ghiChu = ghiChu;
        }

        public string MaTB { get => maTB; set => maTB = value; }
        public string MaHH { get => maHH; set => maHH = value; }
        public string TenTB { get => tenTB; set => tenTB = value; }
        public string Serial { get => serial; set => serial = value; }
        public string MaNCC { get => maNCC; set => maNCC = value; }
        public string MaPhong { get => maPhong; set => maPhong = value; }
        public string TinhTrang { get => tinhTrang; set => tinhTrang = value; }
        public string HanBaoHanh { get => hanBaoHanh; set => hanBaoHanh = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
