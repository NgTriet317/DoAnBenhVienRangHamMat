using Dapper;
using ET;
namespace DAL;
public sealed partial class DAL_KHAMDIEUTRI
{
    public NguCanhHoSo XacMinhNguCanh(NguCanhHoSo context,NguCanhNghiepVu actor)=>ThucHien((db,tx)=>
    {
        var user=DAL_QUYENNGHIEPVU.XacThuc(db,tx,actor);
        if(context is null||user.Quyen==QuyenLamViec.Khong&&!user.LaAdmin)throw DAL_QUYENNGHIEPVU.TuChoi();
        foreach(var id in new[]{context.MaBN,context.MaHS,context.MaPhieu,context.MaLS,context.MaDT,context.MaHD})
            if(id is not null&&(string.IsNullOrWhiteSpace(id)||id.Length>50))throw DAL_QUYENNGHIEPVU.TuChoi();
        string? Merge(string? supplied,string? actual)
        {if(actual is null||supplied is not null&&supplied!=actual)throw DAL_QUYENNGHIEPVU.TuChoi();return actual;}
        var c=context;
        var doctor=user.CoQuyen(QuyenLamViec.Kham);var reception=user.CoQuyen(QuyenLamViec.TiepNhan);
        var pharmacy=user.CoQuyen(QuyenLamViec.GiaoThuoc);var finance=doctor||user.CoQuyen(QuyenLamViec.ThuTien)||pharmacy||user.LaAdmin;
        ET_DONTHUOC? rx=null;
        if(c.MaHD is not null)
        {
            if(!finance)throw DAL_QUYENNGHIEPVU.TuChoi();
            c=c with{MaLS=Merge(c.MaLS,db.QuerySingleOrDefault<string>("SELECT MaLS FROM dbo.HOA_DON WHERE MaHD=@MaHD",c,tx))};
        }
        if(c.MaDT is not null)
        {
            rx=db.QuerySingleOrDefault<ET_DONTHUOC>("SELECT * FROM dbo.DON_THUOC WHERE MaDT=@MaDT",c,tx)??throw DAL_QUYENNGHIEPVU.TuChoi();
            c=c with{MaLS=Merge(c.MaLS,rx.MaLS)};
        }
        ET_LICHSUKHAMBENH? visit=null;
        if(c.MaLS is not null)
        {
            visit=db.QuerySingleOrDefault<ET_LICHSUKHAMBENH>("SELECT * FROM dbo.LICH_SU_KHAM_BENH WHERE MaLS=@MaLS",c,tx)??throw DAL_QUYENNGHIEPVU.TuChoi();
            c=c with{MaPhieu=Merge(c.MaPhieu,visit.MaPhieu)};
        }
        ET_PHIEUKHAMBENH? receipt=null;
        if(c.MaPhieu is not null)
        {
            receipt=db.QuerySingleOrDefault<ET_PHIEUKHAMBENH>("SELECT * FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@MaPhieu",c,tx)??throw DAL_QUYENNGHIEPVU.TuChoi();
            c=c with{MaHS=Merge(c.MaHS,receipt.MaHS)};
            if(visit is null)
            {visit=db.QuerySingleOrDefault<ET_LICHSUKHAMBENH>("SELECT * FROM dbo.LICH_SU_KHAM_BENH WHERE MaPhieu=@MaPhieu",c,tx);if(visit is not null)c=c with{MaLS=visit.MaLS};}
        }
        if(c.MaHS is not null)c=c with{MaBN=Merge(c.MaBN,db.QuerySingleOrDefault<string>("SELECT MaBN FROM dbo.HO_SO_BENH_AN WHERE MaHS=@MaHS",c,tx))};
        if(c.MaBN is not null)
        {
            var allowed=db.ExecuteScalar<int>("""
                SELECT COUNT(*) FROM dbo.BENH_NHAN b WHERE b.MaBN=@MaBN AND (@reception=1 OR @LaAdmin=1
                 OR (@doctor=1 AND EXISTS(SELECT 1 FROM dbo.HO_SO_BENH_AN h JOIN dbo.PHIEU_KHAM_BENH p ON p.MaHS=h.MaHS WHERE h.MaBN=b.MaBN AND p.MaBS=@MaNV AND p.TrangThai<>N'Hủy'))
                 OR (@finance=1 AND @doctor=0 AND EXISTS(SELECT 1 FROM dbo.HO_SO_BENH_AN h JOIN dbo.PHIEU_KHAM_BENH p ON p.MaHS=h.MaHS JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=p.MaPhieu WHERE h.MaBN=b.MaBN AND l.TrangThai=N'Đã xác nhận')))
                """,new{c.MaBN,user.MaNV,user.LaAdmin,reception,doctor,finance},tx);
            if(allowed!=1)throw DAL_QUYENNGHIEPVU.TuChoi();
        }
        if(!user.LaAdmin&&visit is not null&&!(doctor&&visit.MaBS==user.MaNV&&receipt?.MaBS==user.MaNV)&&visit.TrangThai!="Đã xác nhận")
        {
            var assigned=user.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao)&&db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DICH_VU_THUC_HIEN WHERE MaLS=@MaLS AND MaNVThucHien=@MaNV",new{c.MaLS,user.MaNV},tx)>0;
            if(!assigned)throw DAL_QUYENNGHIEPVU.TuChoi();
        }
        if(!user.LaAdmin&&rx is not null&&!(doctor&&rx.MaBS==user.MaNV&&visit?.MaBS==user.MaNV&&receipt?.MaBS==user.MaNV)&&!(visit?.TrangThai=="Đã xác nhận"&&rx.TrangThai is "Đã xác nhận" or "Ngừng áp dụng"))throw DAL_QUYENNGHIEPVU.TuChoi();
        DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db,tx,user,"NGU_CANH_HO_SO",c.MaHD??c.MaDT??c.MaLS??c.MaPhieu??c.MaHS??c.MaBN??user.MaNV);
        return c;
    });
}
