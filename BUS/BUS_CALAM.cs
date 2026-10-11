using DAL;
using ET;

namespace BUS
{
    public class BUS_CALAM
    {
        private readonly DAL_CALAM dalCaLam;

        public BUS_CALAM(DAL_GIAODICH? giaoDich = null)
        {
            dalCaLam = new DAL_CALAM(giaoDich);
        }

        public IEnumerable<ET_CALAM> layToanBoCaLam()
        {
            return XuLyLoi.ThucHien(() => dalCaLam.layToanBoCaLam());
        }

        public ET_CALAM? layCaLamTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalCaLam.layCaLamTheoMa(ma));
        }

        public bool themCaLam(ET_CALAM duLieu)
        {
            return XuLyLoi.ThucHien(() => dalCaLam.themCaLam(duLieu));
        }

        public bool suaCaLam(ET_CALAM duLieu, ET_CALAM? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalCaLam.suaCaLam(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaCaLam(string ma, ET_CALAM? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalCaLam.xoaCaLam(ma, banGoc));
        }
    }
}
