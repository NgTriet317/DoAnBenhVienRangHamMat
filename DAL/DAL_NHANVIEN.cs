using System.Data;
using Dapper;
using ET;

namespace DAL;

/// <summary>Đọc/ghi nhân viên trên cùng giao dịch của BUS; không mở kết nối phụ khi nơi gọi đang lưu nhiều bảng.</summary>
public class DAL_NHANVIEN : dbconnect
{
    public DAL_NHANVIEN(IDbTransaction? transaction = null) : base(transaction) { }

    // Trạng thái nghỉ được áp dụng theo ngày có hiệu lực, kể cả khi ứng dụng tắt qua ngày đó.
    public void dongBoTrangThaiNghi() => ThucHien((db,tx) =>
    {
        DongBoTrangThaiNghi(db,tx);
        return true;
    });

    private static void DongBoTrangThaiNghi(IDbConnection db, IDbTransaction? tx)
    {
        db.Execute(@"
            DECLARE @HomNay date = CONVERT(date,SYSDATETIME());

            -- Nghỉ việc có hiệu lực vĩnh viễn từ TuNgay; không đổi khi mới duyệt đơn tương lai.
            UPDATE nv SET TrangThai=N'Đã nghỉ'
            FROM dbo.NHAN_VIEN nv
            WHERE nv.TrangThai<>N'Đã nghỉ' AND EXISTS (
                SELECT 1 FROM dbo.DON_XIN_NGHI d
                WHERE d.MaNV=nv.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
                  AND d.TuNgay<=@HomNay
                  AND ISNULL(d.GhiChu,N'') LIKE N'%Loại đơn: Nghỉ việc%');

            -- Hết hạn nghỉ phép mới trả lại Đang làm việc, không phục hồi người Đã nghỉ.
            UPDATE nv SET TrangThai=N'Đang làm việc'
            FROM dbo.NHAN_VIEN nv
            WHERE nv.TrangThai=N'Tạm nghỉ' AND EXISTS (
                SELECT 1 FROM dbo.DON_XIN_NGHI d
                WHERE d.MaNV=nv.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
                  AND d.DenNgay<@HomNay
                  AND ISNULL(d.GhiChu,N'') NOT LIKE N'%Loại đơn: Nghỉ việc%')
              AND NOT EXISTS (
                SELECT 1 FROM dbo.DON_XIN_NGHI d
                WHERE d.MaNV=nv.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
                  AND d.TuNgay<=@HomNay AND d.DenNgay>=@HomNay
                  AND ISNULL(d.GhiChu,N'') NOT LIKE N'%Loại đơn: Nghỉ việc%');

            -- Chỉ tạm nghỉ trong đúng khoảng TuNgay..DenNgay của đơn nghỉ phép đã duyệt.
            UPDATE nv SET TrangThai=N'Tạm nghỉ'
            FROM dbo.NHAN_VIEN nv
            WHERE nv.TrangThai=N'Đang làm việc' AND EXISTS (
                SELECT 1 FROM dbo.DON_XIN_NGHI d
                WHERE d.MaNV=nv.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
                  AND d.TuNgay<=@HomNay AND d.DenNgay>=@HomNay
                  AND ISNULL(d.GhiChu,N'') NOT LIKE N'%Loại đơn: Nghỉ việc%');
        ", transaction:tx);
    }

    public IEnumerable<ET_NHANVIEN> layToanBoNV() => ThucHien((db,tx) =>
    {
        DongBoTrangThaiNghi(db,tx);
        return db.Query<ET_NHANVIEN>("SELECT * FROM dbo.NHAN_VIEN ORDER BY MaNV",transaction:tx).ToArray();
    });

    public ET_NHANVIEN layNhanVienTheoMa(string maNV) => ThucHien((db,tx) =>
    {
        DongBoTrangThaiNghi(db,tx);
        return db.QuerySingleOrDefault<ET_NHANVIEN>("SELECT * FROM dbo.NHAN_VIEN WHERE MaNV=@maNV",new{maNV},tx)!;
    });

    public bool themNhanVien(ET_NHANVIEN row) => ThucHien((db,tx) =>
    {
        if(string.IsNullOrWhiteSpace(row.MaNV))row.MaNV=TaoMa(db,tx,"NHAN_VIEN","MaNV");
        KiemTra<ET_NHANVIEN>(db,tx,"NHAN_VIEN",row,null,null);
        return db.Execute("""
            INSERT dbo.NHAN_VIEN(MaNV,HoTen,GioiTinh,NgaySinh,SDT,DiaChi,MaChucVu,MaCK,BangCap,NgayVaoLam,TrangThai)
            VALUES(@MaNV,@HoTen,@GioiTinh,@NgaySinh,@SDT,@DiaChi,@MaChucVu,@MaCK,@BangCap,@NgayVaoLam,@TrangThai)
            """,row,tx)==1;
    });

    public bool capNhatNhanVien(ET_NHANVIEN row) => ThucHien((db,tx) =>
    {
        DongBoTrangThaiNghi(db,tx);
        var current=db.QuerySingleOrDefault<ET_NHANVIEN>("SELECT * FROM dbo.NHAN_VIEN WITH(UPDLOCK,HOLDLOCK) WHERE MaNV=@MaNV",row,tx)
            ??throw new InvalidOperationException("Nhân viên không còn tồn tại. Hãy tải lại.");
        // Form chi tiết chỉ chỉnh hồ sơ; không được ghi đè trạng thái vừa đồng bộ theo đơn nghỉ.
        row.TrangThai=current.TrangThai;
        KiemTra(db,tx,"NHAN_VIEN",row,current,null);
        return db.Execute("""
            UPDATE dbo.NHAN_VIEN SET HoTen=@HoTen,GioiTinh=@GioiTinh,NgaySinh=@NgaySinh,SDT=@SDT,DiaChi=@DiaChi,
                MaChucVu=@MaChucVu,MaCK=@MaCK,BangCap=@BangCap,NgayVaoLam=@NgayVaoLam WHERE MaNV=@MaNV
            """,row,tx)==1;
    });

    public bool khoiPhucNhanVien(string maNV) => ThucHien((db,tx) =>
    {
        // Cập nhật cùng một giao dịch: tránh đồng bộ lại Đã nghỉ do đơn nghỉ việc cũ.
        int soDong = db.Execute(@"
            UPDATE dbo.NHAN_VIEN SET TrangThai=N'Đang làm việc'
            WHERE MaNV=@maNV AND TrangThai=N'Đã nghỉ'",new{maNV},tx);
        if (soDong!=1) return false;

        // Không xóa đơn và không sửa người/ngày duyệt: lưu lịch sử việc đã duyệt,
        // nhưng đóng hiệu lực để các truy vấn duyệt nghỉ không khóa nhân sự mãi mãi.
        db.Execute(@"
            UPDATE dbo.DON_XIN_NGHI
            SET TrangThaiDuyet=N'Đã kết thúc hiệu lực',
                GhiChu=CONCAT(ISNULL(GhiChu,N''),N' | Nhân viên được khôi phục ngày ',
                    CONVERT(nvarchar(10),SYSDATETIME(),23))
            WHERE MaNV=@maNV AND TrangThaiDuyet=N'Đã duyệt'
                AND TuNgay<=CONVERT(date,SYSDATETIME())
                AND ISNULL(GhiChu,N'') LIKE N'%Loại đơn: Nghỉ việc%'",
            new{maNV},tx);

        // Các ca tương lai từng bị đổi sang Nghỉ việc phải được duyệt lại, không
        // tự động xác nhận hay thay đổi lịch quá khứ. Chỉ tác động ca thuộc đơn vừa hết hiệu lực.
        db.Execute(@"
            UPDATE llv
            SET TrangThai=N'Chờ xác nhận',
                GhiChu=CONCAT(ISNULL(llv.GhiChu,N''),N' | Khôi phục nhân sự: cần duyệt lại ca')
            FROM dbo.LICH_LAM_VIEC llv
            WHERE llv.MaNV=@maNV AND llv.NgayLam>=CONVERT(date,SYSDATETIME())
                AND llv.TrangThai=N'Nghỉ việc'
                AND EXISTS (
                    SELECT 1 FROM dbo.DON_XIN_NGHI d
                    WHERE d.MaNV=llv.MaNV
                      AND d.TrangThaiDuyet=N'Đã kết thúc hiệu lực'
                      AND d.TuNgay<=CONVERT(date,SYSDATETIME())
                      AND ISNULL(d.GhiChu,N'') LIKE N'%Loại đơn: Nghỉ việc%'
                      AND (
                          llv.GhiChu LIKE N'%Nghỉ theo đơn '+d.MaDonNghi+N'%'
                          OR llv.GhiChu LIKE N'%Ca nghỉ từ lịch khám theo đơn '+d.MaDonNghi+N'%'
                      )
                )",new{maNV},tx);
        return true;
    });

    public bool nghiViecNhanVien(string maNV) => ThucHien((db,tx) => db.Execute(
        "UPDATE dbo.NHAN_VIEN SET TrangThai=N'Đã nghỉ' WHERE MaNV=@maNV",new{maNV},tx)==1);
}