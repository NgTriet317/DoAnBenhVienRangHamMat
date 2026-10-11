using DAL;
using ET;

namespace BUS
{
    public class BUS_THUOC
    {
        private readonly DAL_THUOC dalThuoc;

        public BUS_THUOC(DAL_GIAODICH? giaoDich = null)
        {
            dalThuoc = new DAL_THUOC(giaoDich);
        }

        public IEnumerable<ET_THUOC> layToanBoThuoc()
        {
            return XuLyLoi.ThucHien(() => dalThuoc.layToanBoThuoc());
        }

        public ET_THUOC? layThuocTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalThuoc.layThuocTheoMa(ma));
        }

        public bool themThuoc(ET_THUOC duLieu)
        {
            return XuLyLoi.ThucHien(() => dalThuoc.themThuoc(duLieu));
        }

        public bool suaThuoc(ET_THUOC duLieu, ET_THUOC? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalThuoc.suaThuoc(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaThuoc(string ma, ET_THUOC? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalThuoc.xoaThuoc(ma, banGoc));
        }
    }
}
