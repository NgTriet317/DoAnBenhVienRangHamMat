using DAL;
using ET;

namespace BUS
{
    public class BUS_CHUYENKHOA
    {
        public IEnumerable<ET_CHUYENKHOA> layToanBoChuyenKhoa()
        {
            DAL_CHUYENKHOA dalChuyenKhoa = new DAL_CHUYENKHOA();
            return dalChuyenKhoa.layToanBoCK();
        }
        public ET_CHUYENKHOA layChuyenKhoaTheoMa(string maCK)
        {
            DAL_CHUYENKHOA dalChuyenKhoa = new DAL_CHUYENKHOA();
            return dalChuyenKhoa.layChuyenKhoaTheoMa(maCK);
        }
        public bool themChuyenKhoa(ET_CHUYENKHOA chuyenKhoa)
        {
            DAL_CHUYENKHOA dalChuyenKhoa = new DAL_CHUYENKHOA();
            return dalChuyenKhoa.themChuyenKhoa(chuyenKhoa);
        }
        public bool capNhatChuyenKhoa(ET_CHUYENKHOA chuyenKhoa)
        {
            DAL_CHUYENKHOA dalChuyenKhoa = new DAL_CHUYENKHOA();
            return dalChuyenKhoa.capNhatChuyenKhoa(chuyenKhoa);
        }
        public bool xoaChuyenKhoa(string maCK)
        {
            DAL_CHUYENKHOA dalChuyenKhoa = new DAL_CHUYENKHOA();
            return dalChuyenKhoa.xoaChuyenKhoa(maCK);
        }
    }
}
