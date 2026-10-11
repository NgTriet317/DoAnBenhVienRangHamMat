using DAL;
using ET;

namespace BUS
{
    public class BUS_TINHTRANGBENH
    {
        private readonly DAL_TINHTRANGBENH dalTinhTrangBenh;

        public BUS_TINHTRANGBENH(DAL_GIAODICH? giaoDich = null)
        {
            dalTinhTrangBenh = new DAL_TINHTRANGBENH(giaoDich);
        }

        public IEnumerable<ET_TINHTRANGBENH> layToanBoTinhTrangBenh()
        {
            return XuLyLoi.ThucHien(() => dalTinhTrangBenh.layToanBoTinhTrangBenh());
        }

        public ET_TINHTRANGBENH? layTinhTrangBenhTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalTinhTrangBenh.layTinhTrangBenhTheoMa(ma));
        }

        public bool themTinhTrangBenh(ET_TINHTRANGBENH duLieu)
        {
            return XuLyLoi.ThucHien(() => dalTinhTrangBenh.themTinhTrangBenh(duLieu));
        }

        public bool suaTinhTrangBenh(ET_TINHTRANGBENH duLieu, ET_TINHTRANGBENH? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalTinhTrangBenh.suaTinhTrangBenh(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaTinhTrangBenh(string ma, ET_TINHTRANGBENH? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalTinhTrangBenh.xoaTinhTrangBenh(ma, banGoc));
        }
    }
}
