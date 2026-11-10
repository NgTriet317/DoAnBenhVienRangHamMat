using System;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ET;

namespace DAL
{
    public class DAL_CHINHSACHLUONG : dbconnect
    {
        public DataTable layDanhSachChinhSach()
        {
            using var db = (SqlConnection)CreateConnection();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"SELECT cs.MaCSL,cs.MaChucVu,cv.TenChucVu,cs.TenChinhSach,
                cs.PhienBan,cs.NgayHieuLuc,cs.NgayHetHieuLuc,cs.LuongCoBan,
                cs.SoCaDinhMuc,cs.DonGiaCa,cs.TyLeHoaHong,cs.PhuCapCoDinh,
                cs.TrangThai,cs.CoSoHoaHong,cs.MaNVDuyet,cs.NgayDuyet
                FROM dbo.CHINH_SACH_LUONG cs
                JOIN dbo.CHUC_VU cv ON cv.MaChucVu=cs.MaChucVu
                ORDER BY cs.NgayHieuLuc DESC,cv.TenChucVu,cs.PhienBan DESC";
            db.Open();
            using var doc = cmd.ExecuteReader();
            var bang = new DataTable();
            bang.Load(doc);
            return bang;
        }

        public DataTable layDanhSachChucVu()
        {
            using var db = (SqlConnection)CreateConnection();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT MaChucVu,TenChucVu FROM dbo.CHUC_VU ORDER BY TenChucVu";
            db.Open();
            using var doc = cmd.ExecuteReader();
            var bang = new DataTable(); bang.Load(doc);
            return bang;
        }

        public string luuChinhSach(string maNguoiSua, ET_CHINHSACHLUONG chinhSach)
        {
            using var db = (SqlConnection)CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                int khoa = db.ExecuteScalar<int>(@"DECLARE @k int;
                    EXEC @k=sys.sp_getapplock @Resource=N'RHM:LuuChinhSachLuong',
                    @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
                    SELECT @k",transaction:tx);
                if (khoa<0) throw new InvalidOperationException("Có người khác đang sửa chính sách.");
                int admin = db.ExecuteScalar<int>(@"SELECT COUNT(1)
                    FROM dbo.TAI_KHOAN tk JOIN dbo.VAI_TRO vt ON vt.MaVaiTro=tk.MaVaiTro
                    WHERE tk.MaNV=@MaNV AND tk.TrangThai=N'Đang hoạt động'
                      AND vt.TenVaiTro IN (N'Admin',N'Quản trị',N'Quản trị viên')",
                    new { MaNV=maNguoiSua },tx);
                if (admin==0) throw new InvalidOperationException("Bạn không có quyền sửa chính sách lương.");

                if (string.IsNullOrWhiteSpace(chinhSach.MaCSL))
                {
                    chinhSach.MaCSL = "CSL" + Guid.NewGuid().ToString("N");
                    chinhSach.PhienBan = db.ExecuteScalar<int>(@"SELECT ISNULL(MAX(PhienBan),0)+1
                        FROM dbo.CHINH_SACH_LUONG WITH(UPDLOCK,HOLDLOCK)
                        WHERE MaChucVu=@MaChucVu",new {chinhSach.MaChucVu},tx);
                    db.Execute(@"INSERT dbo.CHINH_SACH_LUONG
                        (MaCSL,MaChucVu,TenChinhSach,PhienBan,NgayHieuLuc,NgayHetHieuLuc,
                         LuongCoBan,SoCaDinhMuc,DonGiaCa,CoSoHoaHong,TyLeHoaHong,
                         PhuCapCoDinh,TrangThai,MaNVDuyet,NgayDuyet,
                         TyLeBHXH,TyLeBHYT,TyLeBHTN,HeSoTangCaThuong,HeSoTangCaChuNhat,HeSoTangCaNgayLe)
                        VALUES(@MaCSL,@MaChucVu,@TenChinhSach,@PhienBan,@NgayHieuLuc,NULL,
                         @LuongCoBan,@SoCaDinhMuc,@DonGiaCa,@CoSoHoaHong,@TyLeHoaHong,
                         @PhuCapCoDinh,N'Nháp',NULL,NULL,
                         @TyLeBHXH,@TyLeBHYT,@TyLeBHTN,@HeSoTangCaThuong,@HeSoTangCaChuNhat,@HeSoTangCaNgayLe)",chinhSach,tx);
                }
                else
                {
                    int suDung = db.ExecuteScalar<int>(@"SELECT COUNT(1) FROM dbo.BANG_LUONG
                        WHERE MaCSL=@MaCSL",new {chinhSach.MaCSL},tx);
                    if(suDung>0) throw new InvalidOperationException("Chính sách đã có bảng lương; hãy tạo phiên bản mới.");
                    int n = db.Execute(@"UPDATE dbo.CHINH_SACH_LUONG SET
                        TenChinhSach=@TenChinhSach,NgayHieuLuc=@NgayHieuLuc,
                        LuongCoBan=@LuongCoBan,SoCaDinhMuc=@SoCaDinhMuc,
                        DonGiaCa=@DonGiaCa,TyLeHoaHong=@TyLeHoaHong,PhuCapCoDinh=@PhuCapCoDinh
                        WHERE MaCSL=@MaCSL AND MaChucVu=@MaChucVu AND TrangThai=N'Nháp'",
                        chinhSach,tx);
                    if(n!=1) throw new InvalidOperationException("Chỉ được sửa chính sách nháp chưa sử dụng.");
                }
                tx.Commit();
                return chinhSach.MaCSL;
            }
            catch {tx.Rollback();throw;}
        }

        public void duyetChinhSach(string maCSL, string maNguoiDuyet)
        {
            using var db = (SqlConnection)CreateConnection();
            using var cmd = db.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.RHM_DuyetChinhSachLuong";
            cmd.Parameters.Add(new SqlParameter("@MaCSL",SqlDbType.NVarChar,50){Value=maCSL});
            cmd.Parameters.Add(new SqlParameter("@MaNVDuyet",SqlDbType.NVarChar,50){Value=maNguoiDuyet});
            db.Open();cmd.ExecuteNonQuery();
        }
    }
}
