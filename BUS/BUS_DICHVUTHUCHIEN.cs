using DAL;
using ET;

namespace BUS
{
    public class BUS_DICHVUTHUCHIEN
    {
        private readonly DAL_DICHVUTHUCHIEN dalDichVuThucHien;

        public BUS_DICHVUTHUCHIEN(DAL_GIAODICH? giaoDich = null)
        {
            dalDichVuThucHien = new DAL_DICHVUTHUCHIEN(giaoDich);
        }

        public IEnumerable<ET_DICHVUTHUCHIEN> layToanBoDichVuThucHien()
        {
            return XuLyLoi.ThucHien(() => dalDichVuThucHien.layToanBoDichVuThucHien());
        }

        public ET_DICHVUTHUCHIEN? layDichVuThucHienTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalDichVuThucHien.layDichVuThucHienTheoMa(ma));
        }

        public bool themDichVuThucHien(ET_DICHVUTHUCHIEN duLieu)
        {
            return XuLyLoi.ThucHien(() => dalDichVuThucHien.themDichVuThucHien(duLieu));
        }

        public bool suaDichVuThucHien(ET_DICHVUTHUCHIEN duLieu, ET_DICHVUTHUCHIEN? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalDichVuThucHien.suaDichVuThucHien(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaDichVuThucHien(string ma, ET_DICHVUTHUCHIEN? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalDichVuThucHien.xoaDichVuThucHien(ma, banGoc));
        }
    }
}
