using Dapper;
using ET;

namespace DAL
{
    public class DAL_KHO : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_KHO(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_KHO> layToanBoKho()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_KHO>("SELECT * FROM dbo.[KHO] ORDER BY [MaKho]", transaction: transaction).ToArray());
        }

        public ET_KHO? layKhoTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_KHO>("SELECT * FROM dbo.[KHO] WHERE [MaKho]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themKho(ET_KHO duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaKho = DAL_GIAODICH.TaoMa(connection, transaction, "KHO", "MaKho");
                giaoDich.KiemTra<ET_KHO>(connection, transaction, "KHO", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[KHO] ([MaKho],[TenKho],[MaNV],[LoaiKho],[TrangThai]) VALUES (@MaKho,@TenKho,@MaNV,@LoaiKho,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        public bool suaKho(ET_KHO duLieu, ET_KHO? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_KHO>(
                    "SELECT * FROM dbo.[KHO] WITH(UPDLOCK,HOLDLOCK) WHERE [MaKho]=@Ma", new { Ma = duLieu.MaKho }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "KHO", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[KHO] SET [TenKho]=@TenKho,[MaNV]=@MaNV,[LoaiKho]=@LoaiKho,[TrangThai]=@TrangThai WHERE [MaKho]=@MaKho", duLieu, transaction);
                return true;
            });
        }

        public bool xoaKho(string ma, ET_KHO? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_KHO>(
                    "SELECT * FROM dbo.[KHO] WITH(UPDLOCK,HOLDLOCK) WHERE [MaKho]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "KHO", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[KHO] WHERE [MaKho]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
