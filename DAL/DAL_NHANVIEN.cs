using System.Data;
using Dapper;
using ET;

namespace DAL;

/// <summary>Đọc/ghi nhân viên trên cùng giao dịch của BUS; không mở kết nối phụ khi nơi gọi đang lưu nhiều bảng.</summary>
public class DAL_NHANVIEN : dbconnect
{
    public DAL_NHANVIEN(IDbTransaction? transaction = null) : base(transaction) { }

    public IEnumerable<ET_NHANVIEN> layToanBoNV() => ThucHien((db,tx) =>
        db.Query<ET_NHANVIEN>("SELECT * FROM dbo.NHAN_VIEN ORDER BY MaNV",transaction:tx).ToArray());

    public ET_NHANVIEN layNhanVienTheoMa(string maNV) => ThucHien((db,tx) =>
        db.QuerySingleOrDefault<ET_NHANVIEN>("SELECT * FROM dbo.NHAN_VIEN WHERE MaNV=@maNV",new{maNV},tx)!);

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
        var current=db.QuerySingleOrDefault<ET_NHANVIEN>("SELECT * FROM dbo.NHAN_VIEN WITH(UPDLOCK,HOLDLOCK) WHERE MaNV=@MaNV",row,tx)
            ??throw new InvalidOperationException("Nhân viên không còn tồn tại. Hãy tải lại.");
        KiemTra(db,tx,"NHAN_VIEN",row,current,null);
        return db.Execute("""
            UPDATE dbo.NHAN_VIEN SET HoTen=@HoTen,GioiTinh=@GioiTinh,NgaySinh=@NgaySinh,SDT=@SDT,DiaChi=@DiaChi,
                MaChucVu=@MaChucVu,MaCK=@MaCK,BangCap=@BangCap,NgayVaoLam=@NgayVaoLam,TrangThai=@TrangThai WHERE MaNV=@MaNV
            """,row,tx)==1;
    });

    public bool khoiPhucNhanVien(string maNV) => ThucHien((db,tx) => db.Execute(
        "UPDATE dbo.NHAN_VIEN SET TrangThai=N'Đang làm việc' WHERE MaNV=@maNV AND TrangThai=N'Đã nghỉ'",new{maNV},tx)==1);

    public bool nghiViecNhanVien(string maNV) => ThucHien((db,tx) => db.Execute(
        "UPDATE dbo.NHAN_VIEN SET TrangThai=N'Đã nghỉ' WHERE MaNV=@maNV",new{maNV},tx)==1);
}