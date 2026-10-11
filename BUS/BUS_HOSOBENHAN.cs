using DAL;
using ET;

namespace BUS
{
    public class BUS_HOSOBENHAN
    {
        private readonly DAL_HOSOBENHAN dalHoSoBenhAn;

        public BUS_HOSOBENHAN(DAL_GIAODICH? giaoDich = null)
        {
            dalHoSoBenhAn = new DAL_HOSOBENHAN(giaoDich);
        }

        public IEnumerable<ET_HOSOBENHAN> layToanBoHoSoBenhAn()
        {
            return XuLyLoi.ThucHien(() => dalHoSoBenhAn.layToanBoHoSoBenhAn());
        }

        public ET_HOSOBENHAN? layHoSoBenhAnTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalHoSoBenhAn.layHoSoBenhAnTheoMa(ma));
        }

        public bool themHoSoBenhAn(ET_HOSOBENHAN duLieu)
        {
            return XuLyLoi.ThucHien(() => dalHoSoBenhAn.themHoSoBenhAn(duLieu));
        }

        public bool suaHoSoBenhAn(ET_HOSOBENHAN duLieu, ET_HOSOBENHAN? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalHoSoBenhAn.suaHoSoBenhAn(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaHoSoBenhAn(string ma, ET_HOSOBENHAN? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalHoSoBenhAn.xoaHoSoBenhAn(ma, banGoc));
        }
    }
}
