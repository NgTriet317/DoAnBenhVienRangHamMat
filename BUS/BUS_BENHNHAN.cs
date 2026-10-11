using DAL;
using ET;

namespace BUS
{
    public class BUS_BENHNHAN
    {
        private readonly DAL_BENHNHAN dalBenhNhan;

        public BUS_BENHNHAN(DAL_GIAODICH? giaoDich = null)
        {
            dalBenhNhan = new DAL_BENHNHAN(giaoDich);
        }

        public IEnumerable<ET_BENHNHAN> layToanBoBenhNhan()
        {
            return XuLyLoi.ThucHien(() => dalBenhNhan.layToanBoBenhNhan());
        }

        public ET_BENHNHAN? layBenhNhanTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalBenhNhan.layBenhNhanTheoMa(ma));
        }

        public bool themBenhNhan(ET_BENHNHAN duLieu)
        {
            return XuLyLoi.ThucHien(() => dalBenhNhan.themBenhNhan(duLieu));
        }

        public bool suaBenhNhan(ET_BENHNHAN duLieu, ET_BENHNHAN? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalBenhNhan.suaBenhNhan(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaBenhNhan(string ma, ET_BENHNHAN? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalBenhNhan.xoaBenhNhan(ma, banGoc));
        }
    }
}
