using Dapper;
using ET;

namespace DAL
{
    public class DAL_TINHTRANGBENH : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_TINHTRANGBENH(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_TINHTRANGBENH> layToanBoTinhTrangBenh()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_TINHTRANGBENH>("SELECT * FROM dbo.[TINH_TRANG_BENH] ORDER BY [MaTTB]", transaction: transaction).ToArray());
        }

        public ET_TINHTRANGBENH? layTinhTrangBenhTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_TINHTRANGBENH>("SELECT * FROM dbo.[TINH_TRANG_BENH] WHERE [MaTTB]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themTinhTrangBenh(ET_TINHTRANGBENH duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaTTB = DAL_GIAODICH.TaoMa(connection, transaction, "TINH_TRANG_BENH", "MaTTB");
                giaoDich.KiemTra<ET_TINHTRANGBENH>(connection, transaction, "TINH_TRANG_BENH", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[TINH_TRANG_BENH] ([MaTTB],[MaLS],[LoaiTinhTrang],[MoTaTinhTrang],[MucDo],[GhiChu]) VALUES (@MaTTB,@MaLS,@LoaiTinhTrang,@MoTaTinhTrang,@MucDo,@GhiChu)", duLieu, transaction);
                return true;
            });
        }

        public bool suaTinhTrangBenh(ET_TINHTRANGBENH duLieu, ET_TINHTRANGBENH? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_TINHTRANGBENH>(
                    "SELECT * FROM dbo.[TINH_TRANG_BENH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaTTB]=@Ma", new { Ma = duLieu.MaTTB }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "TINH_TRANG_BENH", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[TINH_TRANG_BENH] SET [MaLS]=@MaLS,[LoaiTinhTrang]=@LoaiTinhTrang,[MoTaTinhTrang]=@MoTaTinhTrang,[MucDo]=@MucDo,[GhiChu]=@GhiChu WHERE [MaTTB]=@MaTTB", duLieu, transaction);
                return true;
            });
        }

        public bool xoaTinhTrangBenh(string ma, ET_TINHTRANGBENH? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_TINHTRANGBENH>(
                    "SELECT * FROM dbo.[TINH_TRANG_BENH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaTTB]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "TINH_TRANG_BENH", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[TINH_TRANG_BENH] WHERE [MaTTB]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
