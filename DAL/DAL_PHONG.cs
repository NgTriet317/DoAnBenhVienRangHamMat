using Dapper;
using ET;

namespace DAL
{
    public class DAL_PHONG : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_PHONG(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_PHONG> layToanBoPhong()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_PHONG>("SELECT * FROM dbo.[PHONG] ORDER BY [MaPhong]", transaction: transaction).ToArray());
        }

        public ET_PHONG? layPhongTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_PHONG>("SELECT * FROM dbo.[PHONG] WHERE [MaPhong]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themPhong(ET_PHONG duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaPhong = DAL_GIAODICH.TaoMa(connection, transaction, "PHONG", "MaPhong");
                giaoDich.KiemTra<ET_PHONG>(connection, transaction, "PHONG", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[PHONG] ([MaPhong],[TenPhong],[MaKhoa],[LoaiPhong],[TinhTrang],[TrangThai]) VALUES (@MaPhong,@TenPhong,@MaKhoa,@LoaiPhong,@TinhTrang,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        public bool suaPhong(ET_PHONG duLieu, ET_PHONG? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_PHONG>(
                    "SELECT * FROM dbo.[PHONG] WITH(UPDLOCK,HOLDLOCK) WHERE [MaPhong]=@Ma", new { Ma = duLieu.MaPhong }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "PHONG", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[PHONG] SET [TenPhong]=@TenPhong,[MaKhoa]=@MaKhoa,[LoaiPhong]=@LoaiPhong,[TinhTrang]=@TinhTrang,[TrangThai]=@TrangThai WHERE [MaPhong]=@MaPhong", duLieu, transaction);
                return true;
            });
        }

        public bool xoaPhong(string ma, ET_PHONG? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_PHONG>(
                    "SELECT * FROM dbo.[PHONG] WITH(UPDLOCK,HOLDLOCK) WHERE [MaPhong]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "PHONG", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[PHONG] WHERE [MaPhong]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
