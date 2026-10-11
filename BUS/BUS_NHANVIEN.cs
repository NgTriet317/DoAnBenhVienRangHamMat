using DAL;
using ET;

namespace BUS
{
    public class BUS_NHANVIEN
    {
        private readonly DAL_NHANVIEN dalNhanVien;

        public BUS_NHANVIEN(DAL_GIAODICH? giaoDich = null)
        {
            dalNhanVien = new DAL_NHANVIEN(giaoDich);
        }

        public IEnumerable<ET_NHANVIEN> layToanBoNhanVien()
        {
            return XuLyLoi.ThucHien(() => dalNhanVien.layToanBoNhanVien());
        }

        public ET_NHANVIEN? layNhanVienTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalNhanVien.layNhanVienTheoMa(ma));
        }

        public bool themNhanVien(ET_NHANVIEN duLieu)
        {
            return XuLyLoi.ThucHien(() => dalNhanVien.themNhanVien(duLieu));
        }

        public bool suaNhanVien(ET_NHANVIEN duLieu, ET_NHANVIEN? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalNhanVien.suaNhanVien(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaNhanVien(string ma, ET_NHANVIEN? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalNhanVien.xoaNhanVien(ma, banGoc));
        }
    }
}
