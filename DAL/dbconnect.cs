using Microsoft.Data.SqlClient;
using System.Data;

namespace DAL
{
    public class dbconnect
    {
        private readonly string _connectionString =
            @"Data Source=MSI;
              Initial Catalog=QLBenhVienRangHamMat;
              Integrated Security=True;
              TrustServerCertificate=True;";

        protected IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}