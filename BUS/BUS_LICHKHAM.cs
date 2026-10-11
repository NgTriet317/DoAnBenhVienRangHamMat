using DAL;
using ET;

namespace BUS
{
    public class BUS_LICHKHAM
    {
        private readonly DAL_LICHKHAM dalLichKham;

        public BUS_LICHKHAM(DAL_GIAODICH? giaoDich = null)
        {
            dalLichKham = new DAL_LICHKHAM(giaoDich);
        }

        public IEnumerable<ET_LICHKHAM> layToanBoLichKham()
        {
            return XuLyLoi.ThucHien(() => dalLichKham.layToanBoLichKham());
        }

        public ET_LICHKHAM? layLichKhamTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalLichKham.layLichKhamTheoMa(ma));
        }

        public bool themLichKham(ET_LICHKHAM duLieu)
        {
            return XuLyLoi.ThucHien(() => dalLichKham.themLichKham(duLieu));
        }

        public bool suaLichKham(ET_LICHKHAM duLieu, ET_LICHKHAM? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalLichKham.suaLichKham(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaLichKham(string ma, ET_LICHKHAM? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalLichKham.xoaLichKham(ma, banGoc));
        }
    }
}
