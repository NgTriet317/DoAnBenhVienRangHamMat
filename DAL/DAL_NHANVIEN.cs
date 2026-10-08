using Dapper;
using Dapper.Contrib.Extensions;
using ET;

namespace DAL
{
    public class DAL_NHANVIEN : dbconnect
    {
        public IEnumerable<ET_NHANVIEN> layToanBoNV()
        {
            using var connection = CreateConnection();

            return connection.GetAll<ET_NHANVIEN>();
        }

        public IEnumerable<ET_NHANVIEN> layNhanVienTheoMa(string maNV)
        {
            using var connection = CreateConnection();
            return connection.Query<ET_NHANVIEN>("SELECT * FROM NHAN_VIEN WHERE MaNV = @MaNV", new { MaNV = maNV });
        }
    }
}