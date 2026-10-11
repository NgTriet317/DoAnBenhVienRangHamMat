using Dapper;
using ET;

namespace DAL
{
    public class DAL_HANGHOA : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_HANGHOA(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_HANGHOA> layToanBoHangHoa()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_HANGHOA>("SELECT * FROM dbo.[HANG_HOA] ORDER BY [MaHH]", transaction: transaction).ToArray());
        }

        public ET_HANGHOA? layHangHoaTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_HANGHOA>("SELECT * FROM dbo.[HANG_HOA] WHERE [MaHH]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themHangHoa(ET_HANGHOA duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaHH = DAL_GIAODICH.TaoMa(connection, transaction, "HANG_HOA", "MaHH");
                giaoDich.KiemTra<ET_HANGHOA>(connection, transaction, "HANG_HOA", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[HANG_HOA] ([MaHH],[TenHH],[MaLoaiHH],[DonViTinh],[MoTa],[TrangThai]) VALUES (@MaHH,@TenHH,@MaLoaiHH,@DonViTinh,@MoTa,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        public bool suaHangHoa(ET_HANGHOA duLieu, ET_HANGHOA? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_HANGHOA>(
                    "SELECT * FROM dbo.[HANG_HOA] WITH(UPDLOCK,HOLDLOCK) WHERE [MaHH]=@Ma", new { Ma = duLieu.MaHH }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "HANG_HOA", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[HANG_HOA] SET [TenHH]=@TenHH,[MaLoaiHH]=@MaLoaiHH,[DonViTinh]=@DonViTinh,[MoTa]=@MoTa,[TrangThai]=@TrangThai WHERE [MaHH]=@MaHH", duLieu, transaction);
                return true;
            });
        }

        public bool xoaHangHoa(string ma, ET_HANGHOA? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_HANGHOA>(
                    "SELECT * FROM dbo.[HANG_HOA] WITH(UPDLOCK,HOLDLOCK) WHERE [MaHH]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "HANG_HOA", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[HANG_HOA] WHERE [MaHH]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
