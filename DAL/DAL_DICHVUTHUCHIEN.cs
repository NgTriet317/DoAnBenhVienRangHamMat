using Dapper;
using ET;

namespace DAL
{
    public class DAL_DICHVUTHUCHIEN : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_DICHVUTHUCHIEN(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_DICHVUTHUCHIEN> layToanBoDichVuThucHien()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_DICHVUTHUCHIEN>("SELECT * FROM dbo.[DICH_VU_THUC_HIEN] ORDER BY [MaDVThucHien]", transaction: transaction).ToArray());
        }

        public ET_DICHVUTHUCHIEN? layDichVuThucHienTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_DICHVUTHUCHIEN>("SELECT * FROM dbo.[DICH_VU_THUC_HIEN] WHERE [MaDVThucHien]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themDichVuThucHien(ET_DICHVUTHUCHIEN duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaDVThucHien = DAL_GIAODICH.TaoMa(connection, transaction, "DICH_VU_THUC_HIEN", "MaDVThucHien");
                giaoDich.KiemTra<ET_DICHVUTHUCHIEN>(connection, transaction, "DICH_VU_THUC_HIEN", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[DICH_VU_THUC_HIEN] ([MaDVThucHien],[MaLS],[MaCTKH],[MaDV],[MaNVThucHien],[ThoiGianThucHien],[SoLuong],[DonGia],[KetQua],[TrangThai]) VALUES (@MaDVThucHien,@MaLS,@MaCTKH,@MaDV,@MaNVThucHien,@ThoiGianThucHien,@SoLuong,@DonGia,@KetQua,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        public bool suaDichVuThucHien(ET_DICHVUTHUCHIEN duLieu, ET_DICHVUTHUCHIEN? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_DICHVUTHUCHIEN>(
                    "SELECT * FROM dbo.[DICH_VU_THUC_HIEN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaDVThucHien]=@Ma", new { Ma = duLieu.MaDVThucHien }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "DICH_VU_THUC_HIEN", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[DICH_VU_THUC_HIEN] SET [MaLS]=@MaLS,[MaCTKH]=@MaCTKH,[MaDV]=@MaDV,[MaNVThucHien]=@MaNVThucHien,[ThoiGianThucHien]=@ThoiGianThucHien,[SoLuong]=@SoLuong,[DonGia]=@DonGia,[KetQua]=@KetQua,[TrangThai]=@TrangThai WHERE [MaDVThucHien]=@MaDVThucHien", duLieu, transaction);
                return true;
            });
        }

        public bool xoaDichVuThucHien(string ma, ET_DICHVUTHUCHIEN? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_DICHVUTHUCHIEN>(
                    "SELECT * FROM dbo.[DICH_VU_THUC_HIEN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaDVThucHien]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "DICH_VU_THUC_HIEN", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[DICH_VU_THUC_HIEN] WHERE [MaDVThucHien]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
