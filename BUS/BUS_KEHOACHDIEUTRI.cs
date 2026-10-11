using DAL;
using ET;

namespace BUS
{
    public class BUS_KEHOACHDIEUTRI
    {
        private readonly DAL_KEHOACHDIEUTRI dalKeHoachDieuTri;

        public BUS_KEHOACHDIEUTRI(DAL_GIAODICH? giaoDich = null)
        {
            dalKeHoachDieuTri = new DAL_KEHOACHDIEUTRI(giaoDich);
        }

        public IEnumerable<ET_KEHOACHDIEUTRI> layToanBoKeHoachDieuTri()
        {
            return XuLyLoi.ThucHien(() => dalKeHoachDieuTri.layToanBoKeHoachDieuTri());
        }

        public ET_KEHOACHDIEUTRI? layKeHoachDieuTriTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalKeHoachDieuTri.layKeHoachDieuTriTheoMa(ma));
        }

        public bool themKeHoachDieuTri(ET_KEHOACHDIEUTRI duLieu)
        {
            return XuLyLoi.ThucHien(() => dalKeHoachDieuTri.themKeHoachDieuTri(duLieu));
        }

        public bool suaKeHoachDieuTri(ET_KEHOACHDIEUTRI duLieu, ET_KEHOACHDIEUTRI? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalKeHoachDieuTri.suaKeHoachDieuTri(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaKeHoachDieuTri(string ma, ET_KEHOACHDIEUTRI? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalKeHoachDieuTri.xoaKeHoachDieuTri(ma, banGoc));
        }
    }
}
