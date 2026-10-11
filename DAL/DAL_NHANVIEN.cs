using Dapper;
using ET;

namespace DAL
{
    public class DAL_NHANVIEN : dbconnect
    {
        private readonly DAL_GIAODICH giaoDich;

        public DAL_NHANVIEN(DAL_GIAODICH? giaoDich = null)
        {
            this.giaoDich = giaoDich ?? new DAL_GIAODICH();
        }

        public IEnumerable<ET_NHANVIEN> layToanBoNhanVien()
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.Query<ET_NHANVIEN>("SELECT * FROM dbo.[NHAN_VIEN] ORDER BY [MaNV]", transaction: transaction).ToArray());
        }

        public ET_NHANVIEN? layNhanVienTheoMa(string ma)
        {
            return giaoDich.ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_NHANVIEN>("SELECT * FROM dbo.[NHAN_VIEN] WHERE [MaNV]=@Ma", new { Ma = ma }, transaction));
        }

        public bool themNhanVien(ET_NHANVIEN duLieu)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                duLieu.MaNV = DAL_GIAODICH.TaoMa(connection, transaction, "NHAN_VIEN", "MaNV");
                giaoDich.KiemTra<ET_NHANVIEN>(connection, transaction, "NHAN_VIEN", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[NHAN_VIEN] ([MaNV],[HoTen],[GioiTinh],[NgaySinh],[SDT],[DiaChi],[MaChucVu],[MaCK],[BangCap],[NgayVaoLam],[TrangThai]) VALUES (@MaNV,@HoTen,@GioiTinh,@NgaySinh,@SDT,@DiaChi,@MaChucVu,@MaCK,@BangCap,@NgayVaoLam,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        public bool suaNhanVien(ET_NHANVIEN duLieu, ET_NHANVIEN? banGoc = null, bool chiDoiTrangThai = false)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_NHANVIEN>(
                    "SELECT * FROM dbo.[NHAN_VIEN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaNV]=@Ma", new { Ma = duLieu.MaNV }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "NHAN_VIEN", duLieu, hienTai, banGoc, chiDoiTrangThai);
                connection.Execute("UPDATE dbo.[NHAN_VIEN] SET [HoTen]=@HoTen,[GioiTinh]=@GioiTinh,[NgaySinh]=@NgaySinh,[SDT]=@SDT,[DiaChi]=@DiaChi,[MaChucVu]=@MaChucVu,[MaCK]=@MaCK,[BangCap]=@BangCap,[NgayVaoLam]=@NgayVaoLam,[TrangThai]=@TrangThai WHERE [MaNV]=@MaNV", duLieu, transaction);
                return true;
            });
        }

        public bool xoaNhanVien(string ma, ET_NHANVIEN? banGoc = null)
        {
            return giaoDich.ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_NHANVIEN>(
                    "SELECT * FROM dbo.[NHAN_VIEN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaNV]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                giaoDich.KiemTra(connection, transaction, "NHAN_VIEN", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[NHAN_VIEN] WHERE [MaNV]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
