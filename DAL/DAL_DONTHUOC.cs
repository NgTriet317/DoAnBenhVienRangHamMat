using Dapper;
using ET;

namespace DAL
{
    public class DAL_DONTHUOC : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_DONTHUOC(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_DONTHUOC> layToanBoDonThuoc()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_DONTHUOC>("SELECT * FROM dbo.[DON_THUOC] ORDER BY [MaDT]", transaction: transaction).ToArray());
        }

        public ET_DONTHUOC? layDonThuocTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_DONTHUOC>("SELECT * FROM dbo.[DON_THUOC] WHERE [MaDT]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themDonThuoc(ET_DONTHUOC duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaDT = DAL_GIAODICH.TaoMa(connection, transaction, "DON_THUOC", "MaDT");
                giaoDich.KiemTra<ET_DONTHUOC>(connection, transaction, "DON_THUOC", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[DON_THUOC] ([MaDT],[MaLS],[MaBS],[NgayKe],[LyDoThayThe],[TrangThai],[GhiChu]) VALUES (@MaDT,@MaLS,@MaBS,@NgayKe,@LyDoThayThe,@TrangThai,@GhiChu)", duLieu, transaction);
                return true;
            });
        }

        public bool suaDonThuoc(ET_DONTHUOC duLieu, ET_DONTHUOC? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_DONTHUOC>(
                    "SELECT * FROM dbo.[DON_THUOC] WITH(UPDLOCK,HOLDLOCK) WHERE [MaDT]=@Ma", new { Ma = duLieu.MaDT }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "DON_THUOC", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[DON_THUOC] SET [MaLS]=@MaLS,[MaBS]=@MaBS,[NgayKe]=@NgayKe,[LyDoThayThe]=@LyDoThayThe,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu WHERE [MaDT]=@MaDT", duLieu, transaction);
                return true;
            });
        }

        public bool xoaDonThuoc(string ma, ET_DONTHUOC? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_DONTHUOC>(
                    "SELECT * FROM dbo.[DON_THUOC] WITH(UPDLOCK,HOLDLOCK) WHERE [MaDT]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "DON_THUOC", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[DON_THUOC] WHERE [MaDT]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
