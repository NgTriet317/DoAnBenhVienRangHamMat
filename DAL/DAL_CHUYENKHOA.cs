using Dapper;
using Dapper.Contrib.Extensions;
using ET;
namespace DAL
{
    public class DAL_CHUYENKHOA : dbconnect
    {
        public IEnumerable<ET_CHUYENKHOA> layToanBoCK()
        {
            using var connection = CreateConnection();

            return connection.GetAll<ET_CHUYENKHOA>();
        }

        public ET_CHUYENKHOA layChuyenKhoaTheoMa(string maCK)
        {
            using var connection = CreateConnection();
            return connection.Query<ET_CHUYENKHOA>("SELECT * FROM CHUYEN_KHOA WHERE MaCK = @MaCK", new { MaCK = maCK }).FirstOrDefault();
        }

        public bool themChuyenKhoa(ET_CHUYENKHOA chuyenKhoa)
        {
            try
            {
                using var connection = CreateConnection();
                connection.Insert(chuyenKhoa);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool capNhatChuyenKhoa(ET_CHUYENKHOA chuyenKhoa)
        {
            try
            {
                using var connection = CreateConnection();
                connection.Update(chuyenKhoa);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool xoaChuyenKhoa(string maCK)
        {
            try
            {
                using var connection = CreateConnection();
                var chuyenKhoa = connection.Get<ET_CHUYENKHOA>(maCK);

                connection.Delete(chuyenKhoa);
                return true;

            }
            catch
            {
                return false;
            }            
        }
    }
}