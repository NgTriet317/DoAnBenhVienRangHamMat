using DAL;
using ET;

namespace BUS
{
    public class BUS_PHONG
    {
        private readonly DAL_PHONG dalPhong;

        public BUS_PHONG(DAL_GIAODICH? giaoDich = null)
        {
            dalPhong = new DAL_PHONG(giaoDich);
        }

        public IEnumerable<ET_PHONG> layToanBoPhong()
        {
            return XuLyLoi.ThucHien(() => dalPhong.layToanBoPhong());
        }

        public ET_PHONG? layPhongTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalPhong.layPhongTheoMa(ma));
        }

        public bool themPhong(ET_PHONG duLieu)
        {
            return XuLyLoi.ThucHien(() => dalPhong.themPhong(duLieu));
        }

        public bool suaPhong(ET_PHONG duLieu, ET_PHONG? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalPhong.suaPhong(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaPhong(string ma, ET_PHONG? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalPhong.xoaPhong(ma, banGoc));
        }
    }
}
