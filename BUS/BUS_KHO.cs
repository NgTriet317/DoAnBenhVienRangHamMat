using DAL;
using ET;

namespace BUS
{
    public class BUS_KHO
    {
        private readonly DAL_KHO dalKho;

        public BUS_KHO(DAL_GIAODICH? giaoDich = null)
        {
            dalKho = new DAL_KHO(giaoDich);
        }

        public IEnumerable<ET_KHO> layToanBoKho()
        {
            return XuLyLoi.ThucHien(() => dalKho.layToanBoKho());
        }

        public ET_KHO? layKhoTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalKho.layKhoTheoMa(ma));
        }

        public bool themKho(ET_KHO duLieu)
        {
            return XuLyLoi.ThucHien(() => dalKho.themKho(duLieu));
        }

        public bool suaKho(ET_KHO duLieu, ET_KHO? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalKho.suaKho(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaKho(string ma, ET_KHO? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalKho.xoaKho(ma, banGoc));
        }
    }
}
