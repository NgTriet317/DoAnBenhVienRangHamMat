using DAL;
using ET;

namespace BUS
{
    public class BUS_DONTHUOC
    {
        private readonly DAL_DONTHUOC dalDonThuoc;

        public BUS_DONTHUOC(DAL_GIAODICH? giaoDich = null)
        {
            dalDonThuoc = new DAL_DONTHUOC(giaoDich);
        }

        public IEnumerable<ET_DONTHUOC> layToanBoDonThuoc()
        {
            return XuLyLoi.ThucHien(() => dalDonThuoc.layToanBoDonThuoc());
        }

        public ET_DONTHUOC? layDonThuocTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalDonThuoc.layDonThuocTheoMa(ma));
        }

        public bool themDonThuoc(ET_DONTHUOC duLieu)
        {
            return XuLyLoi.ThucHien(() => dalDonThuoc.themDonThuoc(duLieu));
        }

        public bool suaDonThuoc(ET_DONTHUOC duLieu, ET_DONTHUOC? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalDonThuoc.suaDonThuoc(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaDonThuoc(string ma, ET_DONTHUOC? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalDonThuoc.xoaDonThuoc(ma, banGoc));
        }
    }
}
