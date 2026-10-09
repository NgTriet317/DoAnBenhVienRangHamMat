using Dapper;
using Dapper.Contrib.Extensions;
using ET;
namespace DAL
{
    public class DAL_CHUCVU : dbconnect
    {
        public IEnumerable<ET_CHUCVU> layToanBoCV()
        {
            using var connection = CreateConnection();

            return connection.GetAll<ET_CHUCVU>();
        }

        public ET_CHUCVU layChucVuTheoMa(string maCV)
        {
            using var connection = CreateConnection();
            return connection.Query<ET_CHUCVU>("SELECT * FROM CHUC_VU WHERE MaCV = @MaCV", new { MaCV = maCV }).FirstOrDefault();
        }

        public bool themChucVu(ET_CHUCVU chucVu)
        {
            try
            {
                using var connection = CreateConnection();
                connection.Insert(chucVu);
                return true;
            }
            catch
            {                
                return false;
            }
        }

        public bool capNhatChucVu(ET_CHUCVU chucVu)
        {
            using var connection = CreateConnection();
            return connection.Update(chucVu);
        }

        public bool xoaChucVu(string maCV)
        {
            using var connection = CreateConnection();
            var chucVu = connection.Get<ET_CHUCVU>(maCV);
            if (chucVu != null)
            {
                return connection.Delete(chucVu);
            }
            return false;
        }
    }
}