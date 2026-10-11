using Dapper;
using ET;

namespace DAL
{
    public class DAL_BENHNHAN : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_BENHNHAN(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_BENHNHAN> layToanBoBenhNhan()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_BENHNHAN>("SELECT * FROM dbo.[BENH_NHAN] ORDER BY [MaBN]", transaction: transaction).ToArray());
        }

        public ET_BENHNHAN? layBenhNhanTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_BENHNHAN>("SELECT * FROM dbo.[BENH_NHAN] WHERE [MaBN]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themBenhNhan(ET_BENHNHAN duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaBN = DAL_GIAODICH.TaoMa(connection, transaction, "BENH_NHAN", "MaBN");
                giaoDich.KiemTra<ET_BENHNHAN>(connection, transaction, "BENH_NHAN", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[BENH_NHAN] ([MaBN],[HoTenBN],[GioiTinh],[NgaySinh],[SDT],[DiaChi],[MaBHYT],[NhomMau],[TienSuBenh],[DiUng],[TrangThai]) VALUES (@MaBN,@HoTenBN,@GioiTinh,@NgaySinh,@SDT,@DiaChi,@MaBHYT,@NhomMau,@TienSuBenh,@DiUng,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        public bool suaBenhNhan(ET_BENHNHAN duLieu, ET_BENHNHAN? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_BENHNHAN>(
                    "SELECT * FROM dbo.[BENH_NHAN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaBN]=@Ma", new { Ma = duLieu.MaBN }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "BENH_NHAN", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[BENH_NHAN] SET [HoTenBN]=@HoTenBN,[GioiTinh]=@GioiTinh,[NgaySinh]=@NgaySinh,[SDT]=@SDT,[DiaChi]=@DiaChi,[MaBHYT]=@MaBHYT,[NhomMau]=@NhomMau,[TienSuBenh]=@TienSuBenh,[DiUng]=@DiUng,[TrangThai]=@TrangThai WHERE [MaBN]=@MaBN", duLieu, transaction);
                return true;
            });
        }

        public bool xoaBenhNhan(string ma, ET_BENHNHAN? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_BENHNHAN>(
                    "SELECT * FROM dbo.[BENH_NHAN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaBN]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "BENH_NHAN", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[BENH_NHAN] WHERE [MaBN]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
