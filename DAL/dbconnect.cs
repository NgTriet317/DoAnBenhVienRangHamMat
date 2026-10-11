using Microsoft.Data.SqlClient;
using System.Data;

namespace DAL
{
    public class dbconnect
    {
        private readonly string _connectionString =
            Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING") ?? @"Data Source=DESKTOP-C9VCE8A;
              Initial Catalog=QLBenhVienRangHamMat;
              Integrated Security=True;
              TrustServerCertificate=True;";

        protected IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
