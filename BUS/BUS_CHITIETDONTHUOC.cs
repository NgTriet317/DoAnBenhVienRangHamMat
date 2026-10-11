using DAL;
using ET;

namespace BUS
{
    public class BUS_CHITIETDONTHUOC
    {
        private readonly DAL_CHITIETDONTHUOC dalChiTietDonThuoc;

        public BUS_CHITIETDONTHUOC(DAL_GIAODICH? giaoDich = null)
        {
            dalChiTietDonThuoc = new DAL_CHITIETDONTHUOC(giaoDich);
        }

        public IEnumerable<ET_CHITIETDONTHUOC> layToanBoChiTietDonThuoc()
        {
            return XuLyLoi.ThucHien(() => dalChiTietDonThuoc.layToanBoChiTietDonThuoc());
        }

        public ET_CHITIETDONTHUOC? layChiTietDonThuocTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalChiTietDonThuoc.layChiTietDonThuocTheoMa(ma));
        }

        public bool themChiTietDonThuoc(ET_CHITIETDONTHUOC duLieu)
        {
            return XuLyLoi.ThucHien(() => dalChiTietDonThuoc.themChiTietDonThuoc(duLieu));
        }

        public bool suaChiTietDonThuoc(ET_CHITIETDONTHUOC duLieu, ET_CHITIETDONTHUOC? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalChiTietDonThuoc.suaChiTietDonThuoc(duLieu, banGoc, chiDoiTrangThai));
        }

        public bool xoaChiTietDonThuoc(string ma, ET_CHITIETDONTHUOC? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalChiTietDonThuoc.xoaChiTietDonThuoc(ma, banGoc));
        }
    }
}
