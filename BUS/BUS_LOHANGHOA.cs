using DAL;
using ET;

namespace BUS
{
    public class BUS_LOHANGHOA
    {
        private readonly DAL_LOHANGHOA dalLoHangHoa;

        public BUS_LOHANGHOA(DAL_GIAODICH? giaoDich = null)
        {
            dalLoHangHoa = new DAL_LOHANGHOA(giaoDich);
        }

        public IEnumerable<ET_LOHANGHOA> layToanBoLoHangHoa()
        {
            return XuLyLoi.ThucHien(() => dalLoHangHoa.layToanBoLoHangHoa());
        }

        public ET_LOHANGHOA? layLoHangHoaTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalLoHangHoa.layLoHangHoaTheoMa(ma));
        }

        public bool themLoHangHoa(ET_LOHANGHOA duLieu)
        {
            return XuLyLoi.ThucHien(() => dalLoHangHoa.themLoHangHoa(duLieu));
        }

        public bool suaLoHangHoa(ET_LOHANGHOA duLieu, ET_LOHANGHOA? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalLoHangHoa.suaLoHangHoa(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaLoHangHoa(string ma, ET_LOHANGHOA? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalLoHangHoa.xoaLoHangHoa(ma, banGoc));
        }
    }
}
