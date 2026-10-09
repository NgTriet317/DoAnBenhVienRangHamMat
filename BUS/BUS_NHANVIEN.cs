using DAL;
using ET;

namespace BUS
{
    public class BUS_NHANVIEN
    {
        public IEnumerable<ET_NHANVIEN> layToanBoNhanVien()
        {
            DAL_NHANVIEN dalNhanVien = new DAL_NHANVIEN();
            return dalNhanVien.layToanBoNV();
        }

        public ET_NHANVIEN layNhanVienTheoMa(string maNV)
        {
            DAL_NHANVIEN dalNhanVien = new DAL_NHANVIEN();
            return dalNhanVien.layNhanVienTheoMa(maNV);
        }
        public bool themNhanVien(ET_NHANVIEN nhanVien)
        {
            DAL_NHANVIEN dalNhanVien = new DAL_NHANVIEN();
            return dalNhanVien.themNhanVien(nhanVien);
        }
    }
}
