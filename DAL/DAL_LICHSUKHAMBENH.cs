using Dapper;
using ET;

namespace DAL
{
    public class DAL_LICHSUKHAMBENH : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_LICHSUKHAMBENH(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_LICHSUKHAMBENH> layToanBoLichSuKhamBenh()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_LICHSUKHAMBENH>("SELECT * FROM dbo.[LICH_SU_KHAM_BENH] ORDER BY [MaLS]", transaction: transaction).ToArray());
        }

        public ET_LICHSUKHAMBENH? layLichSuKhamBenhTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_LICHSUKHAMBENH>("SELECT * FROM dbo.[LICH_SU_KHAM_BENH] WHERE [MaLS]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themLichSuKhamBenh(ET_LICHSUKHAMBENH duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaLS = DAL_GIAODICH.TaoMa(connection, transaction, "LICH_SU_KHAM_BENH", "MaLS");
                giaoDich.KiemTra<ET_LICHSUKHAMBENH>(connection, transaction, "LICH_SU_KHAM_BENH", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[LICH_SU_KHAM_BENH] ([MaLS],[MaPhieu],[MaBS],[MaPhong],[ThoiGianKham],[TrieuChung],[ChanDoan],[KeHoachXuLy],[TrangThai],[NgayXacNhan],[GhiChu]) VALUES (@MaLS,@MaPhieu,@MaBS,@MaPhong,@ThoiGianKham,@TrieuChung,@ChanDoan,@KeHoachXuLy,@TrangThai,@NgayXacNhan,@GhiChu)", duLieu, transaction);
                return true;
            });
        }

        public bool suaLichSuKhamBenh(ET_LICHSUKHAMBENH duLieu, ET_LICHSUKHAMBENH? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_LICHSUKHAMBENH>(
                    "SELECT * FROM dbo.[LICH_SU_KHAM_BENH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLS]=@Ma", new { Ma = duLieu.MaLS }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "LICH_SU_KHAM_BENH", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[LICH_SU_KHAM_BENH] SET [MaPhieu]=@MaPhieu,[MaBS]=@MaBS,[MaPhong]=@MaPhong,[ThoiGianKham]=@ThoiGianKham,[TrieuChung]=@TrieuChung,[ChanDoan]=@ChanDoan,[KeHoachXuLy]=@KeHoachXuLy,[TrangThai]=@TrangThai,[NgayXacNhan]=@NgayXacNhan,[GhiChu]=@GhiChu WHERE [MaLS]=@MaLS", duLieu, transaction);
                return true;
            });
        }

        public bool xoaLichSuKhamBenh(string ma, ET_LICHSUKHAMBENH? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_LICHSUKHAMBENH>(
                    "SELECT * FROM dbo.[LICH_SU_KHAM_BENH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLS]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "LICH_SU_KHAM_BENH", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[LICH_SU_KHAM_BENH] WHERE [MaLS]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
