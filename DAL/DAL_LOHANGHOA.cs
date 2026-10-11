using Dapper;
using ET;

namespace DAL
{
    public class DAL_LOHANGHOA : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_LOHANGHOA(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_LOHANGHOA> layToanBoLoHangHoa()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_LOHANGHOA>("SELECT * FROM dbo.[LO_HANG_HOA] ORDER BY [MaLo]", transaction: transaction).ToArray());
        }

        public ET_LOHANGHOA? layLoHangHoaTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_LOHANGHOA>("SELECT * FROM dbo.[LO_HANG_HOA] WHERE [MaLo]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themLoHangHoa(ET_LOHANGHOA duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaLo = DAL_GIAODICH.TaoMa(connection, transaction, "LO_HANG_HOA", "MaLo");
                giaoDich.KiemTra<ET_LOHANGHOA>(connection, transaction, "LO_HANG_HOA", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[LO_HANG_HOA] ([MaLo],[MaHH],[MaNCC],[MaKho],[SoLo],[NgayNhap],[HanSuDung],[TrangThai]) VALUES (@MaLo,@MaHH,@MaNCC,@MaKho,@SoLo,@NgayNhap,@HanSuDung,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        public bool suaLoHangHoa(ET_LOHANGHOA duLieu, ET_LOHANGHOA? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_LOHANGHOA>(
                    "SELECT * FROM dbo.[LO_HANG_HOA] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLo]=@Ma", new { Ma = duLieu.MaLo }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "LO_HANG_HOA", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[LO_HANG_HOA] SET [MaHH]=@MaHH,[MaNCC]=@MaNCC,[MaKho]=@MaKho,[SoLo]=@SoLo,[NgayNhap]=@NgayNhap,[HanSuDung]=@HanSuDung,[TrangThai]=@TrangThai WHERE [MaLo]=@MaLo", duLieu, transaction);
                return true;
            });
        }

        public bool xoaLoHangHoa(string ma, ET_LOHANGHOA? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_LOHANGHOA>(
                    "SELECT * FROM dbo.[LO_HANG_HOA] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLo]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "LO_HANG_HOA", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[LO_HANG_HOA] WHERE [MaLo]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
