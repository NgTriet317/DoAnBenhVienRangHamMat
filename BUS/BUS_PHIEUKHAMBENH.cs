using DAL;
using ET;

namespace BUS
{
    public class BUS_PHIEUKHAMBENH
    {
        private readonly DAL_PHIEUKHAMBENH dalPhieuKhamBenh;

        public BUS_PHIEUKHAMBENH(DAL_GIAODICH? giaoDich = null)
        {
            dalPhieuKhamBenh = new DAL_PHIEUKHAMBENH(giaoDich);
        }

        public IEnumerable<ET_PHIEUKHAMBENH> layToanBoPhieuKhamBenh()
        {
            return XuLyLoi.ThucHien(() => dalPhieuKhamBenh.layToanBoPhieuKhamBenh());
        }

        public ET_PHIEUKHAMBENH? layPhieuKhamBenhTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalPhieuKhamBenh.layPhieuKhamBenhTheoMa(ma));
        }

        public bool themPhieuKhamBenh(ET_PHIEUKHAMBENH duLieu)
        {
            return XuLyLoi.ThucHien(() => dalPhieuKhamBenh.themPhieuKhamBenh(duLieu));
        }

        public bool suaPhieuKhamBenh(ET_PHIEUKHAMBENH duLieu, ET_PHIEUKHAMBENH? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalPhieuKhamBenh.suaPhieuKhamBenh(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaPhieuKhamBenh(string ma, ET_PHIEUKHAMBENH? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalPhieuKhamBenh.xoaPhieuKhamBenh(ma, banGoc));
        }
    }
}
