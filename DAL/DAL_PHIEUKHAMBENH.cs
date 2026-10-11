using Dapper;
using ET;

namespace DAL
{
    public class DAL_PHIEUKHAMBENH : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_PHIEUKHAMBENH(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_PHIEUKHAMBENH> layToanBoPhieuKhamBenh()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_PHIEUKHAMBENH>("SELECT * FROM dbo.[PHIEU_KHAM_BENH] ORDER BY [MaPhieu]", transaction: transaction).ToArray());
        }

        public ET_PHIEUKHAMBENH? layPhieuKhamBenhTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_PHIEUKHAMBENH>("SELECT * FROM dbo.[PHIEU_KHAM_BENH] WHERE [MaPhieu]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themPhieuKhamBenh(ET_PHIEUKHAMBENH duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaPhieu = DAL_GIAODICH.TaoMa(connection, transaction, "PHIEU_KHAM_BENH", "MaPhieu");
                giaoDich.KiemTra<ET_PHIEUKHAMBENH>(connection, transaction, "PHIEU_KHAM_BENH", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[PHIEU_KHAM_BENH] ([MaPhieu],[MaHS],[MaLich],[MaPhong],[MaBS],[MaNVTiepNhan],[MaDV],[SoThuTu],[ThoiGianTiepNhan],[LyDoKham],[TrangThai],[GhiChu]) VALUES (@MaPhieu,@MaHS,@MaLich,@MaPhong,@MaBS,@MaNVTiepNhan,@MaDV,@SoThuTu,@ThoiGianTiepNhan,@LyDoKham,@TrangThai,@GhiChu)", duLieu, transaction);
                return true;
            });
        }

        public bool suaPhieuKhamBenh(ET_PHIEUKHAMBENH duLieu, ET_PHIEUKHAMBENH? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_PHIEUKHAMBENH>(
                    "SELECT * FROM dbo.[PHIEU_KHAM_BENH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaPhieu]=@Ma", new { Ma = duLieu.MaPhieu }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "PHIEU_KHAM_BENH", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[PHIEU_KHAM_BENH] SET [MaHS]=@MaHS,[MaLich]=@MaLich,[MaPhong]=@MaPhong,[MaBS]=@MaBS,[MaNVTiepNhan]=@MaNVTiepNhan,[MaDV]=@MaDV,[SoThuTu]=@SoThuTu,[ThoiGianTiepNhan]=@ThoiGianTiepNhan,[LyDoKham]=@LyDoKham,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu WHERE [MaPhieu]=@MaPhieu", duLieu, transaction);
                return true;
            });
        }

        public bool xoaPhieuKhamBenh(string ma, ET_PHIEUKHAMBENH? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_PHIEUKHAMBENH>(
                    "SELECT * FROM dbo.[PHIEU_KHAM_BENH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaPhieu]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "PHIEU_KHAM_BENH", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[PHIEU_KHAM_BENH] WHERE [MaPhieu]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
