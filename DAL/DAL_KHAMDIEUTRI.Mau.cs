using System.Data;
using Dapper;
using ET;
using Microsoft.Data.SqlClient;
namespace DAL;

public sealed partial class DAL_KHAMDIEUTRI
{
    public BanNhapMauDieuTri LayMauDieuTri(string maMauKH,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {ClinicalLock(db,tx,"Mau:"+ValidCode(maMauKH));TemplateUser(db,tx,user,false);return MauSnapshot(db,tx,maMauKH);});

    public BanNhapMauDieuTri LuuMauDieuTri(YeuCauLuuMauDieuTri request,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        ArgumentNullException.ThrowIfNull(request);ClinicalLock(db,tx,"Mau:"+(request.MaMauKH??"Moi"));var actor=TemplateUser(db,tx,user,false);
        var old=string.IsNullOrEmpty(request.MaMauKH)?null:MauSnapshot(db,tx,ValidCode(request.MaMauKH));
        if(old is not null){if(old.Mau.TrangThai!="Nháp")throw KhamError("MaMauKH","Mẫu đã duyệt giữ nguyên; tạo mã/phiên bản mới.");RequireFingerprint(request.DauVanTay,old.DauVanTay);}
        ActiveCondition(db,tx,request.MaDMTT);Required(request.TenMau,"TenMau","Nhập tên mẫu.");if(request.PhienBan<=0)throw KhamError("PhienBan","Phiên bản phải dương.");
        var header=new ET_MAUKEHOACHDIEUTRI {MaMauKH=old?.Mau.MaMauKH??TaoMa(db,tx,"MAU_KE_HOACH_DIEU_TRI","MaMauKH"),MaDMTT=request.MaDMTT,TenMau=request.TenMau,MucDo=request.MucDo,PhienBan=request.PhienBan,TrangThai="Nháp"};
        KiemTra<ET_MAUKEHOACHDIEUTRI>(db,tx,"MAU_KE_HOACH_DIEU_TRI",header,null,null);
        if(request.CacBuoc is null||request.CacBuoc.Count>200)throw KhamError("CacBuoc","Tối đa 200 bước mẫu.");
        var rows=new List<ET_CHITIETMAUKEHOACH>();
        foreach(var r in request.CacBuoc)
        {
            if(r is null)throw KhamError("CacBuoc","Bước mẫu không hợp lệ.");ActiveService(db,tx,r.MaDV);Required(r.NoiDungBuoc,"CacBuoc","Nhập nội dung từng bước.");
            if(r.LanThu<=0||r.SoLuongDuKien<=0)throw KhamError("CacBuoc","Lần và lượng dự kiến phải dương.");
            var row=new ET_CHITIETMAUKEHOACH {MaCTMau="",MaMauKH=header.MaMauKH,MaDV=r.MaDV,LanThu=r.LanThu,ThuTu=rows.Count+1,SoLuongDuKien=r.SoLuongDuKien,NoiDung=r.NoiDungBuoc};
            KiemTra<ET_CHITIETMAUKEHOACH>(db,tx,"CHI_TIET_MAU_KE_HOACH",row,null,null);rows.Add(row);
        }
        WithConfigurationActor(db,tx,actor,()=>
        {
            if(old is null)db.Execute("INSERT dbo.MAU_KE_HOACH_DIEU_TRI(MaMauKH,MaDMTT,MucDo,TenMau,PhienBan,TrangThai,MaBSDuyet,NgayDuyet) VALUES(@MaMauKH,@MaDMTT,@MucDo,@TenMau,@PhienBan,@TrangThai,NULL,NULL)",header,tx);
            else {db.Execute("UPDATE dbo.MAU_KE_HOACH_DIEU_TRI SET MaDMTT=@MaDMTT,MucDo=@MucDo,TenMau=@TenMau,PhienBan=@PhienBan WHERE MaMauKH=@MaMauKH",header,tx);db.Execute("DELETE dbo.CHI_TIET_MAU_KE_HOACH WHERE MaMauKH=@MaMauKH",header,tx);}
            foreach(var row in rows){row.MaCTMau=TaoMa(db,tx,"CHI_TIET_MAU_KE_HOACH","MaCTMau");db.Execute("INSERT dbo.CHI_TIET_MAU_KE_HOACH(MaCTMau,MaMauKH,MaDV,ThuTu,LanThu,SoLuongDuKien,NoiDung) VALUES(@MaCTMau,@MaMauKH,@MaDV,@ThuTu,@LanThu,@SoLuongDuKien,@NoiDung)",row,tx);}
            return true;
        });
        return MauSnapshot(db,tx,header.MaMauKH);
    });

    public BanNhapMauDieuTri DuyetMauDieuTri(string maMauKH,string dauVanTay,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        ClinicalLock(db,tx,"Mau:"+ValidCode(maMauKH));var actor=TemplateUser(db,tx,user,true);var m=MauSnapshot(db,tx,maMauKH);RequireFingerprint(dauVanTay,m.DauVanTay);
        if(m.Mau.TrangThai!="Nháp")throw KhamError("MaMauKH","Chỉ ký duyệt mẫu nháp.");ActiveCondition(db,tx,m.Mau.MaDMTT);
        if(m.CacBuoc.Count==0)throw KhamError("CacBuoc","Mẫu cần ít nhất một bước trước khi duyệt.");foreach(var s in m.CacBuoc)ActiveService(db,tx,s.MaDV);
        WithConfigurationActor(db,tx,actor,()=>db.Execute("UPDATE dbo.MAU_KE_HOACH_DIEU_TRI SET TrangThai=N'Đã duyệt',MaBSDuyet=@MaNV,NgayDuyet=CONVERT(datetime2(0),SYSDATETIME()) WHERE MaMauKH=@maMauKH",new{maMauKH,actor.MaNV},tx));
        return MauSnapshot(db,tx,maMauKH);
    });

    public void NgungApDungMau(string maMauKH,string dauVanTay,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        ClinicalLock(db,tx,"Mau:"+ValidCode(maMauKH));var actor=TemplateUser(db,tx,user,false);var m=MauSnapshot(db,tx,maMauKH);RequireFingerprint(dauVanTay,m.DauVanTay);
        if(m.Mau.TrangThai!="Đã duyệt")throw KhamError("MaMauKH","Chỉ ngừng áp dụng mẫu đã duyệt.");
        return WithConfigurationActor(db,tx,actor,()=>db.Execute("UPDATE dbo.MAU_KE_HOACH_DIEU_TRI SET TrangThai=N'Ngừng áp dụng' WHERE MaMauKH=@maMauKH",new{maMauKH},tx));
    });
    private static NguCanhNghiepVu TemplateUser(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu user,bool approve)
    {
        var actor=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);
        if(approve ? !actor.CoQuyen(QuyenLamViec.DuyetMauDieuTri) : !actor.LaAdmin && !(actor.CoQuyen(QuyenLamViec.CauHinhHoTro)&&actor.CoQuyen(QuyenLamViec.Kham)))throw DAL_QUYENNGHIEPVU.TuChoi();
        return actor;
    }
    private static ET_CHITIETMAUKEHOACH[] MauSteps(IDbConnection db,IDbTransaction? tx,string id)=>db.Query<ET_CHITIETMAUKEHOACH>("SELECT * FROM dbo.CHI_TIET_MAU_KE_HOACH WITH(HOLDLOCK) WHERE MaMauKH=@id ORDER BY ThuTu,MaCTMau",new{id},tx).ToArray();
    private static BanNhapMauDieuTri MauSnapshot(IDbConnection db,IDbTransaction? tx,string id)
    {var header=db.QuerySingleOrDefault<ET_MAUKEHOACHDIEUTRI>("SELECT * FROM dbo.MAU_KE_HOACH_DIEU_TRI WITH(UPDLOCK,HOLDLOCK) WHERE MaMauKH=@id",new{id},tx)??throw KhamError("MaMauKH","Mẫu không còn tồn tại.");var steps=MauSteps(db,tx,id);return new(header,steps,Fingerprint(new{Mau=header,Steps=steps}));}
    private static T WithConfigurationActor<T>(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu user,Func<T> action)
    {
        var previous=db.ExecuteScalar<string?>("SELECT CONVERT(nvarchar(128),SESSION_CONTEXT(N'RHM.MaTK'))",transaction:tx);var succeeded=false;
        try{db.Execute("EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@actor",new{actor=user.MaTK},tx);var result=action();succeeded=true;return result;}
        finally
        {
            try{db.Execute("EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@actor",new{actor=previous},tx?.Connection is null?null:tx);}
            catch{if(db is SqlConnection c){SqlConnection.ClearPool(c);c.Close();}if(succeeded)throw;}
        }
    }
}
