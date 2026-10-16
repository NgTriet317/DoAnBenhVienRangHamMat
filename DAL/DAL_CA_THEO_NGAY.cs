using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using ET;

namespace DAL
{
    public class DAL_CA_THEO_NGAY : dbconnect
    {
        public List<ET_CA_THEO_NGAY> layCaTheoTuan(DateTime dauTuan)
        {
            return ThucHien((connection, transaction) => connection.Query<ET_CA_THEO_NGAY>(
                @"SELECT * FROM dbo.CA_THEO_NGAY WHERE NgayLam >= @TuNgay AND NgayLam < @DenNgay",
                new { TuNgay = dauTuan.Date, DenNgay = dauTuan.Date.AddDays(7) }, transaction).ToList());
        }

        public bool themCaTheoNgay(ET_CA_THEO_NGAY ca)
        {
            return ThucHien((connection, transaction) => connection.Execute(
                @"INSERT INTO dbo.CA_THEO_NGAY (NgayLam, MaCa)
                  SELECT @NgayLam, @MaCa
                  WHERE NOT EXISTS (SELECT 1 FROM dbo.CA_THEO_NGAY WITH (UPDLOCK, HOLDLOCK)
                                    WHERE NgayLam = @NgayLam AND MaCa = @MaCa)",
                ca, transaction) > 0);
        }

        public bool xoaCaTheoNgay(DateTime ngay, string maCa)
        {
            return ThucHien((connection, transaction) => connection.Execute(
                @"DELETE FROM dbo.CA_THEO_NGAY
                  WHERE NgayLam = @NgayLam AND MaCa = @MaCa
                    AND NOT EXISTS (SELECT 1 FROM dbo.LICH_LAM_VIEC
                                    WHERE NgayLam = @NgayLam AND MaCa = @MaCa
                                    AND TrangThai IN (N'Chờ xác nhận', N'Đã xác nhận'))",
                new { NgayLam = ngay.Date, MaCa = maCa }, transaction) > 0);
        }
    }
}
