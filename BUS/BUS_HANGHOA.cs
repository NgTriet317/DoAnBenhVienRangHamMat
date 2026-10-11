using DAL;
using ET;

namespace BUS
{
    public class BUS_HANGHOA
    {
        private readonly DAL_HANGHOA dalHangHoa;

        public BUS_HANGHOA(DAL_GIAODICH? giaoDich = null)
        {
            dalHangHoa = new DAL_HANGHOA(giaoDich);
        }

        public IEnumerable<ET_HANGHOA> layToanBoHangHoa()
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.layToanBoHangHoa());
        }

        public ET_HANGHOA? layHangHoaTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.layHangHoaTheoMa(ma));
        }

        public bool themHangHoa(ET_HANGHOA duLieu)
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.themHangHoa(duLieu));
        }

        public bool suaHangHoa(ET_HANGHOA duLieu, ET_HANGHOA? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.suaHangHoa(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaHangHoa(string ma, ET_HANGHOA? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.xoaHangHoa(ma, banGoc));
        }
    }
}
