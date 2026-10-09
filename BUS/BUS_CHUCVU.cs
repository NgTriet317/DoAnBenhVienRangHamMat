using DAL;
using ET;

namespace BUS
{
    public class BUS_CHUCVU
    {
        public IEnumerable<ET_CHUCVU> layToanBoChucVu()
        {
            DAL_CHUCVU dalChucVu = new DAL_CHUCVU();
            return dalChucVu.layToanBoCV();            
        }

        public ET_CHUCVU layChucVuTheoMa(string maCV)
        {
            DAL_CHUCVU dalChucVu = new DAL_CHUCVU();
            return dalChucVu.layChucVuTheoMa(maCV);
        }
        public bool themChucVu(ET_CHUCVU chucVu)
        {
            DAL_CHUCVU dalChucVu = new DAL_CHUCVU();
            return dalChucVu.themChucVu(chucVu);
        }
        public bool capNhatChucVu(ET_CHUCVU chucVu)
        {
            DAL_CHUCVU dalChucVu = new DAL_CHUCVU();
            return dalChucVu.capNhatChucVu(chucVu);
        }
        public bool xoaChucVu(string maCV)
        {
            DAL_CHUCVU dalChucVu = new DAL_CHUCVU();
            return dalChucVu.xoaChucVu(maCV);
        }
    }
}
