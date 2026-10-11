using DAL;
using ET;

namespace BUS
{
    public class BUS_DICHVU
    {
        private readonly DAL_DICHVU dalDichVu;

        public BUS_DICHVU(DAL_GIAODICH? giaoDich = null)
        {
            dalDichVu = new DAL_DICHVU(giaoDich);
        }

        public IEnumerable<ET_DICHVU> layToanBoDichVu()
        {
            return XuLyLoi.ThucHien(() => dalDichVu.layToanBoDichVu());
        }

        public ET_DICHVU? layDichVuTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalDichVu.layDichVuTheoMa(ma));
        }

        public bool themDichVu(ET_DICHVU duLieu)
        {
            return XuLyLoi.ThucHien(() => dalDichVu.themDichVu(duLieu));
        }

        public bool suaDichVu(ET_DICHVU duLieu, ET_DICHVU? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalDichVu.suaDichVu(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaDichVu(string ma, ET_DICHVU? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalDichVu.xoaDichVu(ma, banGoc));
        }
    }
}
