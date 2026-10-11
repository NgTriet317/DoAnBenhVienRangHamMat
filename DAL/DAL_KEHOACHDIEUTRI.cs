using Dapper;
using ET;

namespace DAL
{
    public class DAL_KEHOACHDIEUTRI : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_KEHOACHDIEUTRI(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_KEHOACHDIEUTRI> layToanBoKeHoachDieuTri()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_KEHOACHDIEUTRI>("SELECT * FROM dbo.[KE_HOACH_DIEU_TRI] ORDER BY [MaKHDT]", transaction: transaction).ToArray());
        }

        public ET_KEHOACHDIEUTRI? layKeHoachDieuTriTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_KEHOACHDIEUTRI>("SELECT * FROM dbo.[KE_HOACH_DIEU_TRI] WHERE [MaKHDT]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themKeHoachDieuTri(ET_KEHOACHDIEUTRI duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaKHDT = DAL_GIAODICH.TaoMa(connection, transaction, "KE_HOACH_DIEU_TRI", "MaKHDT");
                giaoDich.KiemTra<ET_KEHOACHDIEUTRI>(connection, transaction, "KE_HOACH_DIEU_TRI", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[KE_HOACH_DIEU_TRI] ([MaKHDT],[MaLS],[MaBS],[NgayLap],[NgayBatDau],[NgayDuKienKetThuc],[TrangThai],[GhiChu]) VALUES (@MaKHDT,@MaLS,@MaBS,@NgayLap,@NgayBatDau,@NgayDuKienKetThuc,@TrangThai,@GhiChu)", duLieu, transaction);
                return true;
            });
        }

        public bool suaKeHoachDieuTri(ET_KEHOACHDIEUTRI duLieu, ET_KEHOACHDIEUTRI? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_KEHOACHDIEUTRI>(
                    "SELECT * FROM dbo.[KE_HOACH_DIEU_TRI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaKHDT]=@Ma", new { Ma = duLieu.MaKHDT }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "KE_HOACH_DIEU_TRI", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[KE_HOACH_DIEU_TRI] SET [MaLS]=@MaLS,[MaBS]=@MaBS,[NgayLap]=@NgayLap,[NgayBatDau]=@NgayBatDau,[NgayDuKienKetThuc]=@NgayDuKienKetThuc,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu WHERE [MaKHDT]=@MaKHDT", duLieu, transaction);
                return true;
            });
        }

        public bool xoaKeHoachDieuTri(string ma, ET_KEHOACHDIEUTRI? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_KEHOACHDIEUTRI>(
                    "SELECT * FROM dbo.[KE_HOACH_DIEU_TRI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaKHDT]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "KE_HOACH_DIEU_TRI", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[KE_HOACH_DIEU_TRI] WHERE [MaKHDT]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
