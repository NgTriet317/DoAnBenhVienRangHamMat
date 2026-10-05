using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class ET_NHACUNGCAP
    {
        private string maNCC;
        private string tenNCC;
        private int sdt;
        private string email;
        private string diaChi;
        private string nguoiLienHe;
        private string trangThai;

        public ET_NHACUNGCAP(string maNCC, string tenNCC, int sdt, string email, string diaChi, string nguoiLienHe, string trangThai)
        {
            this.MaNCC = maNCC;
            this.TenNCC = tenNCC;
            this.Sdt = sdt;
            this.Email = email;
            this.DiaChi = diaChi;
            this.NguoiLienHe = nguoiLienHe;
            this.TrangThai = trangThai;
        }

        public ET_NHACUNGCAP()
        {
        }
        public string MaNCC { get => maNCC; set => maNCC = value; }
        public string TenNCC { get => tenNCC; set => tenNCC = value; }
        public int Sdt { get => sdt; set => sdt = value; }
        public string Email { get => email; set => email = value; }
        public string DiaChi { get => diaChi; set => diaChi = value; }
        public string NguoiLienHe { get => nguoiLienHe; set => nguoiLienHe = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
