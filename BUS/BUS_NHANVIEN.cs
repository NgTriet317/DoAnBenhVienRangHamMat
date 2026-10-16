using DAL;
using ET;
using System.Collections.Generic;

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

        public bool capNhatNhanVien(ET_NHANVIEN nhanVien)
        {
            DAL_NHANVIEN dalNhanVien = new DAL_NHANVIEN();
            return dalNhanVien.capNhatNhanVien(nhanVien);
        }

        public bool khoiPhucNhanVien(string maNV)
        {
            DAL_NHANVIEN dalNhanVien = new DAL_NHANVIEN();
            return dalNhanVien.khoiPhucNhanVien(maNV);
        }

        public bool nghiViecNhanVien(string maNV)
        {
            DAL_NHANVIEN dalNhanVien = new DAL_NHANVIEN();
            return dalNhanVien.nghiViecNhanVien(maNV);
        }
    }
}
