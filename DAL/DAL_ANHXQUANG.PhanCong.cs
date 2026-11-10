using System.Text.Json;
using Dapper;
using ET;

namespace DAL;

public sealed partial class DAL_ANHXQUANG
{
    public IReadOnlyList<NhanVienChupAnh> LayKyThuatVien(string maLS,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var(c,a)=Context(db,tx,maLS,user,true);
        if(!Editable(c)||!a.LaAdmin&&!Owner(c,a))throw DAL_QUYENNGHIEPVU.TuChoi();
        return db.Query<NhanVienChupAnh>("""
            SELECT n.MaNV,n.HoTen FROM dbo.NHAN_VIEN n WITH(HOLDLOCK)
            WHERE n.MaChucVu=N'CV003' AND n.TrangThai=N'Đang làm việc' AND EXISTS(
             SELECT 1 FROM dbo.TAI_KHOAN t WITH(HOLDLOCK) WHERE t.MaNV=n.MaNV AND t.TrangThai=N'Đang hoạt động') ORDER BY n.HoTen,n.MaNV
            """,transaction:tx).ToArray();
    });
    public string PhanCong(string maLS,string source,string maNVKyThuat,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var(c,a)=Context(db,tx,maLS,user,true);
        if(!Editable(c)||!a.LaAdmin&&!Owner(c,a))throw DAL_QUYENNGHIEPVU.TuChoi();
        if(!Services(db,tx,c,a).Any(s=>s.MaDVThucHien==source))throw DAL_QUYENNGHIEPVU.TuChoi();
        if(!new DAL_ANHXQUANG(tx).LayKyThuatVien(maLS,a).Any(n=>n.MaNV==maNVKyThuat))throw DAL_QUYENNGHIEPVU.TuChoi();
        var old=db.QuerySingleOrDefault<ET_PHANCONGANHXQUANG>("SELECT * FROM dbo.PHAN_CONG_ANH_XQUANG WITH(UPDLOCK,HOLDLOCK) WHERE MaDVThucHien=@source AND TrangThai=N'Đang áp dụng'",new{source},tx);
        if(old?.MaNVKyThuat==maNVKyThuat)return old.MaPC;
        if(old is not null)db.Execute("UPDATE dbo.PHAN_CONG_ANH_XQUANG SET TrangThai=N'Đã hủy',NgayHuy=SYSDATETIME() WHERE MaPC=@MaPC",old,tx);
        var id=Guid.NewGuid().ToString("N");
        db.Execute("INSERT dbo.PHAN_CONG_ANH_XQUANG(MaPC,MaDVThucHien,MaNVKyThuat,MaNVPhanCong) VALUES(@id,@source,@maNVKyThuat,@MaNV)",new{id,source,maNVKyThuat,a.MaNV},tx);
        db.Execute("""
            INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,ThoiGianUtc,MaPhien,MaSuKien,PhanHe,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuSau)
            VALUES(@nk,@MaTK,SYSDATETIME(),SYSUTCDATETIME(),TRY_CONVERT(uniqueidentifier,SESSION_CONTEXT(N'RHM.MaPhien')),NEWID(),N'Ảnh X-quang',N'XR_ASSIGN',N'PHAN_CONG_ANH_XQUANG',@id,N'Thành công',@detail)
            """,new{nk="NK"+Guid.NewGuid().ToString("N"),a.MaTK,id,detail=JsonSerializer.Serialize(new{MaPC=id,MaLS=maLS,MaDVThucHien=source,MaNVKyThuat=maNVKyThuat})},tx);
        return id;
    });
}
