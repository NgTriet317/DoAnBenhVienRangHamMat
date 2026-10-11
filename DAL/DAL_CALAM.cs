using Dapper;
using ET;

namespace DAL
{
    public class DAL_CALAM : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_CALAM(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_CALAM> layToanBoCaLam()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_CALAM>("SELECT * FROM dbo.[CA_LAM] ORDER BY [MaCa]", transaction: transaction).ToArray());
        }

        public ET_CALAM? layCaLamTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_CALAM>("SELECT * FROM dbo.[CA_LAM] WHERE [MaCa]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themCaLam(ET_CALAM duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaCa = DAL_GIAODICH.TaoMa(connection, transaction, "CA_LAM", "MaCa");
                giaoDich.KiemTra<ET_CALAM>(connection, transaction, "CA_LAM", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[CA_LAM] ([MaCa],[GioBatDau],[GioKetThuc],[TrangThai],[GhiChu]) VALUES (@MaCa,@GioBatDau,@GioKetThuc,@TrangThai,@GhiChu)", duLieu, transaction);
                return true;
            });
        }

        public bool suaCaLam(ET_CALAM duLieu, ET_CALAM? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CALAM>(
                    "SELECT * FROM dbo.[CA_LAM] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCa]=@Ma", new { Ma = duLieu.MaCa }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "CA_LAM", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[CA_LAM] SET [GioBatDau]=@GioBatDau,[GioKetThuc]=@GioKetThuc,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu WHERE [MaCa]=@MaCa", duLieu, transaction);
                return true;
            });
        }

        public bool xoaCaLam(string ma, ET_CALAM? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CALAM>(
                    "SELECT * FROM dbo.[CA_LAM] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCa]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "CA_LAM", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[CA_LAM] WHERE [MaCa]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
