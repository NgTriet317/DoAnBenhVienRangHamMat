using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DAL
{
    // Đọc chứng từ hoa hồng, không thay đổi logic tính/chốt lương V9 đã có.
    public class DAL_BANGKEHOAHONG : dbconnect
    {
        public DataTable layChiTietHoaHong(string maBL)
        {
            using var db=CreateConnection();
            using var cmd=db.CreateCommand();
            cmd.CommandText=@"SELECT hh.MaCTHD,ct.MaHD,hh.VaiTroHoaHong,
               hh.GiaTriTinh,hh.TyLe,CAST(hh.TyLe*100 AS decimal(9,2)) AS PhanTram,
               hh.TienHoaHong
               FROM dbo.CHI_TIET_HOA_HONG_LUONG hh
               JOIN dbo.CHI_TIET_HOA_DON ct ON ct.MaCTHD=hh.MaCTHD
               WHERE hh.MaBL=@MaBL
               ORDER BY ct.MaHD,hh.VaiTroHoaHong";
            cmd.Parameters.Add(new SqlParameter("@MaBL",SqlDbType.NVarChar,50){Value=maBL});
            db.Open();
            using var doc=cmd.ExecuteReader();
            var bang=new DataTable();bang.Load(doc);return bang;
        }
    }
}
