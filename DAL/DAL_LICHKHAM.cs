using Dapper;
using ET;

namespace DAL
{
    public class DAL_LICHKHAM : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_LICHKHAM(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_LICHKHAM> layToanBoLichKham()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_LICHKHAM>("SELECT * FROM dbo.[LICH_KHAM] ORDER BY [MaLich]", transaction: transaction).ToArray());
        }

        public ET_LICHKHAM? layLichKhamTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_LICHKHAM>("SELECT * FROM dbo.[LICH_KHAM] WHERE [MaLich]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themLichKham(ET_LICHKHAM duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaLich = DAL_GIAODICH.TaoMa(connection, transaction, "LICH_KHAM", "MaLich");
                giaoDich.KiemTra<ET_LICHKHAM>(connection, transaction, "LICH_KHAM", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[LICH_KHAM] ([MaLich],[MaBN],[MaBS],[MaCa],[MaPhong],[MaDV],[NgayGioKham],[ThoiLuongDuKien],[UuTien],[LyDoKham],[LyDoThayDoi],[TrangThai],[GhiChu],[NgayTao],[NgayCapNhat]) VALUES (@MaLich,@MaBN,@MaBS,@MaCa,@MaPhong,@MaDV,@NgayGioKham,@ThoiLuongDuKien,@UuTien,@LyDoKham,@LyDoThayDoi,@TrangThai,@GhiChu,@NgayTao,@NgayCapNhat)", duLieu, transaction);
                return true;
            });
        }

        public bool suaLichKham(ET_LICHKHAM duLieu, ET_LICHKHAM? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_LICHKHAM>(
                    "SELECT * FROM dbo.[LICH_KHAM] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLich]=@Ma", new { Ma = duLieu.MaLich }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "LICH_KHAM", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[LICH_KHAM] SET [MaBN]=@MaBN,[MaBS]=@MaBS,[MaCa]=@MaCa,[MaPhong]=@MaPhong,[MaDV]=@MaDV,[NgayGioKham]=@NgayGioKham,[ThoiLuongDuKien]=@ThoiLuongDuKien,[UuTien]=@UuTien,[LyDoKham]=@LyDoKham,[LyDoThayDoi]=@LyDoThayDoi,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu,[NgayTao]=@NgayTao,[NgayCapNhat]=@NgayCapNhat WHERE [MaLich]=@MaLich", duLieu, transaction);
                return true;
            });
        }

        public bool xoaLichKham(string ma, ET_LICHKHAM? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_LICHKHAM>(
                    "SELECT * FROM dbo.[LICH_KHAM] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLich]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "LICH_KHAM", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[LICH_KHAM] WHERE [MaLich]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
