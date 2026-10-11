using Dapper;
using ET;

namespace DAL
{
    public class DAL_THUOC : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_THUOC(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_THUOC> layToanBoThuoc()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_THUOC>("SELECT * FROM dbo.[THUOC] ORDER BY [MaThuoc]", transaction: transaction).ToArray());
        }

        public ET_THUOC? layThuocTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_THUOC>("SELECT * FROM dbo.[THUOC] WHERE [MaThuoc]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themThuoc(ET_THUOC duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaThuoc = DAL_GIAODICH.TaoMa(connection, transaction, "THUOC", "MaThuoc");
                giaoDich.KiemTra<ET_THUOC>(connection, transaction, "THUOC", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[THUOC] ([MaThuoc],[MaHH],[HamLuong],[MucTonToiThieu],[DonGiaBan]) VALUES (@MaThuoc,@MaHH,@HamLuong,@MucTonToiThieu,@DonGiaBan)", duLieu, transaction);
                return true;
            });
        }

        public bool suaThuoc(ET_THUOC duLieu, ET_THUOC? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_THUOC>(
                    "SELECT * FROM dbo.[THUOC] WITH(UPDLOCK,HOLDLOCK) WHERE [MaThuoc]=@Ma", new { Ma = duLieu.MaThuoc }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "THUOC", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[THUOC] SET [MaHH]=@MaHH,[HamLuong]=@HamLuong,[MucTonToiThieu]=@MucTonToiThieu,[DonGiaBan]=@DonGiaBan WHERE [MaThuoc]=@MaThuoc", duLieu, transaction);
                return true;
            });
        }

        public bool xoaThuoc(string ma, ET_THUOC? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_THUOC>(
                    "SELECT * FROM dbo.[THUOC] WITH(UPDLOCK,HOLDLOCK) WHERE [MaThuoc]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "THUOC", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[THUOC] WHERE [MaThuoc]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
