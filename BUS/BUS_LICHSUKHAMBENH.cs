using DAL;
using ET;

namespace BUS
{
    public class BUS_LICHSUKHAMBENH
    {
        private readonly DAL_LICHSUKHAMBENH dalLichSuKhamBenh;

        public BUS_LICHSUKHAMBENH(DAL_GIAODICH? giaoDich = null)
        {
            dalLichSuKhamBenh = new DAL_LICHSUKHAMBENH(giaoDich);
        }

        public IEnumerable<ET_LICHSUKHAMBENH> layToanBoLichSuKhamBenh()
        {
            return XuLyLoi.ThucHien(() => dalLichSuKhamBenh.layToanBoLichSuKhamBenh());
        }

        public ET_LICHSUKHAMBENH? layLichSuKhamBenhTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalLichSuKhamBenh.layLichSuKhamBenhTheoMa(ma));
        }

        public bool themLichSuKhamBenh(ET_LICHSUKHAMBENH duLieu)
        {
            return XuLyLoi.ThucHien(() => dalLichSuKhamBenh.themLichSuKhamBenh(duLieu));
        }

        public bool suaLichSuKhamBenh(ET_LICHSUKHAMBENH duLieu, ET_LICHSUKHAMBENH? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalLichSuKhamBenh.suaLichSuKhamBenh(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaLichSuKhamBenh(string ma, ET_LICHSUKHAMBENH? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalLichSuKhamBenh.xoaLichSuKhamBenh(ma, banGoc));
        }
    }
}
