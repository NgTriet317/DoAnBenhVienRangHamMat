using Dapper;
using ET;

namespace DAL
{
    public class DAL_HOSOBENHAN : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_HOSOBENHAN(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_HOSOBENHAN> layToanBoHoSoBenhAn()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_HOSOBENHAN>("SELECT * FROM dbo.[HO_SO_BENH_AN] ORDER BY [MaHS]", transaction: transaction).ToArray());
        }

        public ET_HOSOBENHAN? layHoSoBenhAnTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_HOSOBENHAN>("SELECT * FROM dbo.[HO_SO_BENH_AN] WHERE [MaHS]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themHoSoBenhAn(ET_HOSOBENHAN duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaHS = DAL_GIAODICH.TaoMa(connection, transaction, "HO_SO_BENH_AN", "MaHS");
                giaoDich.KiemTra<ET_HOSOBENHAN>(connection, transaction, "HO_SO_BENH_AN", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[HO_SO_BENH_AN] ([MaHS],[MaBN],[NgayLap],[LyDoKhamBanDau],[GhiChu],[TrangThai]) VALUES (@MaHS,@MaBN,@NgayLap,@LyDoKhamBanDau,@GhiChu,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        public bool suaHoSoBenhAn(ET_HOSOBENHAN duLieu, ET_HOSOBENHAN? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_HOSOBENHAN>(
                    "SELECT * FROM dbo.[HO_SO_BENH_AN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaHS]=@Ma", new { Ma = duLieu.MaHS }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "HO_SO_BENH_AN", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[HO_SO_BENH_AN] SET [MaBN]=@MaBN,[NgayLap]=@NgayLap,[LyDoKhamBanDau]=@LyDoKhamBanDau,[GhiChu]=@GhiChu,[TrangThai]=@TrangThai WHERE [MaHS]=@MaHS", duLieu, transaction);
                return true;
            });
        }

        public bool xoaHoSoBenhAn(string ma, ET_HOSOBENHAN? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_HOSOBENHAN>(
                    "SELECT * FROM dbo.[HO_SO_BENH_AN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaHS]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "HO_SO_BENH_AN", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[HO_SO_BENH_AN] WHERE [MaHS]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
