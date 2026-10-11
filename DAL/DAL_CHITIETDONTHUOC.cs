using Dapper;
using ET;

namespace DAL
{
    public class DAL_CHITIETDONTHUOC : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_CHITIETDONTHUOC(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_CHITIETDONTHUOC> layToanBoChiTietDonThuoc()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_CHITIETDONTHUOC>("SELECT * FROM dbo.[CHI_TIET_DON_THUOC] ORDER BY [MaCTDT]", transaction: transaction).ToArray());
        }

        public ET_CHITIETDONTHUOC? layChiTietDonThuocTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_CHITIETDONTHUOC>("SELECT * FROM dbo.[CHI_TIET_DON_THUOC] WHERE [MaCTDT]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themChiTietDonThuoc(ET_CHITIETDONTHUOC duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaCTDT = DAL_GIAODICH.TaoMa(connection, transaction, "CHI_TIET_DON_THUOC", "MaCTDT");
                giaoDich.KiemTra<ET_CHITIETDONTHUOC>(connection, transaction, "CHI_TIET_DON_THUOC", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[CHI_TIET_DON_THUOC] ([MaCTDT],[MaDT],[MaThuoc],[LieuDung],[SoLuong],[SoNgayDung],[HuongDan]) VALUES (@MaCTDT,@MaDT,@MaThuoc,@LieuDung,@SoLuong,@SoNgayDung,@HuongDan)", duLieu, transaction);
                return true;
            });
        }

        public bool suaChiTietDonThuoc(ET_CHITIETDONTHUOC duLieu, ET_CHITIETDONTHUOC? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHITIETDONTHUOC>(
                    "SELECT * FROM dbo.[CHI_TIET_DON_THUOC] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCTDT]=@Ma", new { Ma = duLieu.MaCTDT }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "CHI_TIET_DON_THUOC", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[CHI_TIET_DON_THUOC] SET [MaDT]=@MaDT,[MaThuoc]=@MaThuoc,[LieuDung]=@LieuDung,[SoLuong]=@SoLuong,[SoNgayDung]=@SoNgayDung,[HuongDan]=@HuongDan WHERE [MaCTDT]=@MaCTDT", duLieu, transaction);
                return true;
            });
        }

        public bool xoaChiTietDonThuoc(string ma, ET_CHITIETDONTHUOC? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHITIETDONTHUOC>(
                    "SELECT * FROM dbo.[CHI_TIET_DON_THUOC] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCTDT]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "CHI_TIET_DON_THUOC", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[CHI_TIET_DON_THUOC] WHERE [MaCTDT]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
