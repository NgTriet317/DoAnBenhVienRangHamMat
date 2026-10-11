using Dapper;
using ET;

namespace DAL
{
    public class DAL_CHITIETKEHOACH : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_CHITIETKEHOACH(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_CHITIETKEHOACH> layToanBoChiTietKeHoach()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_CHITIETKEHOACH>("SELECT * FROM dbo.[CHI_TIET_KE_HOACH] ORDER BY [MaCTKH]", transaction: transaction).ToArray());
        }

        public ET_CHITIETKEHOACH? layChiTietKeHoachTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_CHITIETKEHOACH>("SELECT * FROM dbo.[CHI_TIET_KE_HOACH] WHERE [MaCTKH]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themChiTietKeHoach(ET_CHITIETKEHOACH duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaCTKH = DAL_GIAODICH.TaoMa(connection, transaction, "CHI_TIET_KE_HOACH", "MaCTKH");
                giaoDich.KiemTra<ET_CHITIETKEHOACH>(connection, transaction, "CHI_TIET_KE_HOACH", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[CHI_TIET_KE_HOACH] ([MaCTKH],[MaKHDT],[MaDV],[LanThu],[SoLuongDuKien],[DonGiaDuKien],[TrangThai],[GhiChu]) VALUES (@MaCTKH,@MaKHDT,@MaDV,@LanThu,@SoLuongDuKien,@DonGiaDuKien,@TrangThai,@GhiChu)", duLieu, transaction);
                return true;
            });
        }

        public bool suaChiTietKeHoach(ET_CHITIETKEHOACH duLieu, ET_CHITIETKEHOACH? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHITIETKEHOACH>(
                    "SELECT * FROM dbo.[CHI_TIET_KE_HOACH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCTKH]=@Ma", new { Ma = duLieu.MaCTKH }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "CHI_TIET_KE_HOACH", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[CHI_TIET_KE_HOACH] SET [MaKHDT]=@MaKHDT,[MaDV]=@MaDV,[LanThu]=@LanThu,[SoLuongDuKien]=@SoLuongDuKien,[DonGiaDuKien]=@DonGiaDuKien,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu WHERE [MaCTKH]=@MaCTKH", duLieu, transaction);
                return true;
            });
        }

        public bool xoaChiTietKeHoach(string ma, ET_CHITIETKEHOACH? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHITIETKEHOACH>(
                    "SELECT * FROM dbo.[CHI_TIET_KE_HOACH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCTKH]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "CHI_TIET_KE_HOACH", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[CHI_TIET_KE_HOACH] WHERE [MaCTKH]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
