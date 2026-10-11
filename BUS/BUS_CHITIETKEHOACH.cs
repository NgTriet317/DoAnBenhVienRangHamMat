using DAL;
using ET;

namespace BUS
{
    public class BUS_CHITIETKEHOACH
    {
        private readonly DAL_CHITIETKEHOACH dalChiTietKeHoach;

        public BUS_CHITIETKEHOACH(DAL_GIAODICH? giaoDich = null)
        {
            dalChiTietKeHoach = new DAL_CHITIETKEHOACH(giaoDich);
        }

        public IEnumerable<ET_CHITIETKEHOACH> layToanBoChiTietKeHoach()
        {
            return XuLyLoi.ThucHien(() => dalChiTietKeHoach.layToanBoChiTietKeHoach());
        }

        public ET_CHITIETKEHOACH? layChiTietKeHoachTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalChiTietKeHoach.layChiTietKeHoachTheoMa(ma));
        }

        public bool themChiTietKeHoach(ET_CHITIETKEHOACH duLieu)
        {
            return XuLyLoi.ThucHien(() => dalChiTietKeHoach.themChiTietKeHoach(duLieu));
        }

        public bool suaChiTietKeHoach(ET_CHITIETKEHOACH duLieu, ET_CHITIETKEHOACH? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalChiTietKeHoach.suaChiTietKeHoach(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaChiTietKeHoach(string ma, ET_CHITIETKEHOACH? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalChiTietKeHoach.xoaChiTietKeHoach(ma, banGoc));
        }
    }
}
