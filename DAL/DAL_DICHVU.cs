using Dapper;
using ET;

namespace DAL
{
    public class DAL_DICHVU : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_DICHVU(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_DICHVU> layToanBoDichVu()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_DICHVU>("SELECT * FROM dbo.[DICH_VU] ORDER BY [MaDV]", transaction: transaction).ToArray());
        }

        public ET_DICHVU? layDichVuTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_DICHVU>("SELECT * FROM dbo.[DICH_VU] WHERE [MaDV]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themDichVu(ET_DICHVU duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaDV = DAL_GIAODICH.TaoMa(connection, transaction, "DICH_VU", "MaDV");
                giaoDich.KiemTra<ET_DICHVU>(connection, transaction, "DICH_VU", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[DICH_VU] ([MaDV],[TenDV],[NhomDV],[MaCK],[DonGia],[ThoiGianDuKien],[MoTa],[NgayApDung],[TrangThai]) VALUES (@MaDV,@TenDV,@NhomDV,@MaCK,@DonGia,@ThoiGianDuKien,@MoTa,@NgayApDung,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        public bool suaDichVu(ET_DICHVU duLieu, ET_DICHVU? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_DICHVU>(
                    "SELECT * FROM dbo.[DICH_VU] WITH(UPDLOCK,HOLDLOCK) WHERE [MaDV]=@Ma", new { Ma = duLieu.MaDV }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "DICH_VU", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[DICH_VU] SET [TenDV]=@TenDV,[NhomDV]=@NhomDV,[MaCK]=@MaCK,[DonGia]=@DonGia,[ThoiGianDuKien]=@ThoiGianDuKien,[MoTa]=@MoTa,[NgayApDung]=@NgayApDung,[TrangThai]=@TrangThai WHERE [MaDV]=@MaDV", duLieu, transaction);
                return true;
            });
        }

        public bool xoaDichVu(string ma, ET_DICHVU? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_DICHVU>(
                    "SELECT * FROM dbo.[DICH_VU] WITH(UPDLOCK,HOLDLOCK) WHERE [MaDV]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "DICH_VU", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[DICH_VU] WHERE [MaDV]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
