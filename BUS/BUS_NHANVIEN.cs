using DAL;
using ET;
using System.Collections.Generic;
using System.Data;

namespace BUS
{
    public class BUS_NHANVIEN
    {
        private readonly DAL_NHANVIEN data;
        public BUS_NHANVIEN(IDbTransaction? transaction = null) => data = new(transaction);
        public IEnumerable<ET_NHANVIEN> layToanBoNhanVien()
        {
            DAL_NHANVIEN dalNhanVien = data;
            return dalNhanVien.layToanBoNV();
        }

        public ET_NHANVIEN layNhanVienTheoMa(string maNV)
        {
            DAL_NHANVIEN dalNhanVien = data;
            return dalNhanVien.layNhanVienTheoMa(maNV);
        }

        public bool themNhanVien(ET_NHANVIEN nhanVien)
        {
            DAL_NHANVIEN dalNhanVien = data;
            return dalNhanVien.themNhanVien(nhanVien);
        }

        public bool capNhatNhanVien(ET_NHANVIEN nhanVien)
        {
            DAL_NHANVIEN dalNhanVien = data;
            return dalNhanVien.capNhatNhanVien(nhanVien);
        }

        public bool khoiPhucNhanVien(string maNV)
        {
            DAL_NHANVIEN dalNhanVien = data;
            return dalNhanVien.khoiPhucNhanVien(maNV);
        }

        public bool nghiViecNhanVien(string maNV)
        {
            DAL_NHANVIEN dalNhanVien = data;
            return dalNhanVien.nghiViecNhanVien(maNV);
        }
    }
}
