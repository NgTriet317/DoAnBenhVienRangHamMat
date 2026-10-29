using System.Data;
using Dapper;
using ET;
namespace DAL;

public sealed partial class DAL_KHAMDIEUTRI
{
    public BanNhapKeHoach HuyKeHoach(string maKHDT,string dauVanTay,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var(actor,p)=OwnedPlan(db,tx,maKHDT,user,true);RequireFingerprint(dauVanTay,p.DauVanTay);
        if(p.KeHoach.TrangThai!="Nháp")throw KhamError("KeHoach","Chỉ hủy kế hoạch nháp chưa có thực hiện.");
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DICH_VU_THUC_HIEN d WITH(UPDLOCK,HOLDLOCK) JOIN dbo.CHI_TIET_KE_HOACH c WITH(HOLDLOCK) ON c.MaCTKH=d.MaCTKH WHERE c.MaKHDT=@maKHDT",new{maKHDT},tx)>0)throw KhamError("KeHoach","Kế hoạch đã có thực hiện; xử lý dịch vụ trước.");
        db.Execute("UPDATE dbo.KE_HOACH_DIEU_TRI SET TrangThai=N'Hủy' WHERE MaKHDT=@maKHDT",new{maKHDT},tx);var saved=PlanSnapshot(db,tx,maKHDT);
        ClinicalAudit(db,tx,actor,"Hủy kế hoạch nháp","KE_HOACH_DIEU_TRI",maKHDT,p.DauVanTay,saved.DauVanTay);return saved;
    });
    public BanNhapKeHoach LayKeHoach(string maKHDT,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {var (_,plan)=OwnedPlan(db,tx,maKHDT,user,false);return plan;});

    public BanNhapKeHoach LuuKeHoach(YeuCauLuuKeHoach request,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        ArgumentNullException.ThrowIfNull(request);var(actor,visit)=OwnedVisit(db,tx,request.MaLS,user,true);
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DON_THUOC WITH(HOLDLOCK) WHERE MaLS=@MaLS AND TrangThai IN(N'Nháp',N'Đã xác nhận')",visit,tx)>0)
            throw KhamError("KeHoach","Lần khám đã bắt đầu kê thuốc. Không thay kế hoạch sau khi kê đơn; cần đối chiếu đơn trước.");
        var original=string.IsNullOrEmpty(request.MaKHDT)?null:PlanSnapshot(db,tx,ValidCode(request.MaKHDT));
        if(original is not null && (original.KeHoach.MaLS!=visit.MaLS || !actor.LaAdmin && original.KeHoach.MaBS!=actor.MaNV))throw DAL_QUYENNGHIEPVU.TuChoi();
        if(original is not null){if(original.KeHoach.TrangThai!="Nháp")throw KhamError("KeHoach","Kế hoạch đã duyệt chỉ còn cập nhật tiến độ.");RequireFingerprint(request.DauVanTay,original.DauVanTay);}
        ET_KEHOACHDIEUTRI header=original?.KeHoach??new() {MaKHDT=TaoMa(db,tx,"KE_HOACH_DIEU_TRI","MaKHDT"),MaLS=visit.MaLS,MaBS=visit.MaBS,NgayLap=ClinicalNow(db,tx),TrangThai="Nháp",MaTTBNguon=string.IsNullOrWhiteSpace(request.MaTTBNguon)?null:request.MaTTBNguon};
        if(original is not null && request.MaTTBNguon is not null && request.MaTTBNguon!=header.MaTTBNguon)throw DAL_QUYENNGHIEPVU.TuChoi();
        if(header.MaTTBNguon is not null && db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.TINH_TRANG_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaTTB=@id AND MaLS=@ls",new{id=header.MaTTBNguon,ls=visit.MaLS},tx)!=1)throw DAL_QUYENNGHIEPVU.TuChoi();
        header.NgayBatDau=request.NgayBatDau?.Date;header.NgayDuKienKetThuc=request.NgayDuKienKetThuc?.Date;header.GhiChu=request.GhiChu;
        if(header.NgayBatDau>header.NgayDuKienKetThuc)throw KhamError("NgayDuKienKetThuc","Ngày kết thúc không trước ngày bắt đầu.");
        KiemTra<ET_KEHOACHDIEUTRI>(db,tx,"KE_HOACH_DIEU_TRI",header,null,null);
        var rows=PreparePlanSteps(db,tx,header,request.CacBuoc,original?.CacBuoc??Array.Empty<ET_CHITIETKEHOACH>());
        if(original is null)InsertPlan(db,tx,header);
        else db.Execute("UPDATE dbo.KE_HOACH_DIEU_TRI SET NgayBatDau=@NgayBatDau,NgayDuKienKetThuc=@NgayDuKienKetThuc,GhiChu=@GhiChu WHERE MaKHDT=@MaKHDT",header,tx);
        WritePlanSteps(db,tx,header,rows,original?.CacBuoc??Array.Empty<ET_CHITIETKEHOACH>());
        var saved=PlanSnapshot(db,tx,header.MaKHDT);ClinicalAudit(db,tx,actor,"Lưu nháp kế hoạch","KE_HOACH_DIEU_TRI",header.MaKHDT,original?.DauVanTay,saved.DauVanTay);return saved;
    });

    public BanNhapKeHoach XacNhanKeHoach(string maKHDT,string dauVanTay,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var(actor,p)=OwnedPlan(db,tx,maKHDT,user,true);
        if(!actor.LaAdmin && p.KeHoach.MaBS!=actor.MaNV)throw DAL_QUYENNGHIEPVU.TuChoi();RequireFingerprint(dauVanTay,p.DauVanTay);
        if(p.KeHoach.TrangThai!="Nháp")throw KhamError("KeHoach","Chỉ duyệt kế hoạch nháp.");
        if(p.CacBuoc.Count==0)throw KhamError("CacBuoc","Kế hoạch cần ít nhất một bước.");
        foreach(var s in p.CacBuoc){ActiveService(db,tx,s.MaDV);Required(s.NoiDungBuoc,"CacBuoc","Bổ sung nội dung từng bước.");}
        db.Execute("UPDATE dbo.KE_HOACH_DIEU_TRI SET TrangThai=N'Đã xác nhận' WHERE MaKHDT=@maKHDT",new{maKHDT},tx);
        var saved=PlanSnapshot(db,tx,maKHDT);ClinicalAudit(db,tx,actor,"Duyệt kế hoạch điều trị","KE_HOACH_DIEU_TRI",maKHDT,p.DauVanTay,saved.DauVanTay);return saved;
    });

    private ET_CHITIETKEHOACH[] PreparePlanSteps(IDbConnection db,IDbTransaction? tx,ET_KEHOACHDIEUTRI header,IReadOnlyList<BuocKeHoachNhap> input,IReadOnlyList<ET_CHITIETKEHOACH> old)
    {
        if(input is null || input.Count>200)throw KhamError("CacBuoc","Tối đa 200 bước trong một kế hoạch.");
        var keys=new HashSet<string>(StringComparer.Ordinal);var rows=new List<ET_CHITIETKEHOACH>();
        foreach(var r in input)
        {
            if(r is null)throw KhamError("CacBuoc","Dòng kế hoạch không hợp lệ.");
            var current=string.IsNullOrEmpty(r.MaCTKH)?null:old.SingleOrDefault(s=>s.MaCTKH==r.MaCTKH);
            if(!string.IsNullOrEmpty(r.MaCTKH)&&(current is null||!keys.Add(r.MaCTKH)))throw DAL_QUYENNGHIEPVU.TuChoi();
            var active=ActiveService(db,tx,r.MaDV);Required(r.NoiDungBuoc,"CacBuoc","Nhập nội dung từng bước.");
            if((current is null||current.MaDV!=r.MaDV||current.DonGiaDuKien!=r.DonGiaDuKien)&&r.DonGiaDuKien!=active.DonGia)
                throw KhamError("CacBuoc","Giá dịch vụ đã thay đổi. Cập nhật giá từ danh mục trước khi lưu kế hoạch.");
            if(r.LanThu<=0||r.SoLuongDuKien<=0||r.DonGiaDuKien<0)throw KhamError("CacBuoc","Lần và lượng phải dương, giá không âm.");
            var row=new ET_CHITIETKEHOACH {MaCTKH=current?.MaCTKH??"",MaKHDT=header.MaKHDT,MaDV=r.MaDV,LanThu=r.LanThu,SoLuongDuKien=r.SoLuongDuKien,DonGiaDuKien=r.DonGiaDuKien,NoiDungBuoc=r.NoiDungBuoc,GhiChu=r.GhiChu,TrangThai=current?.TrangThai??"Dự kiến"};
            KiemTra(db,tx,"CHI_TIET_KE_HOACH",row,current,null);rows.Add(row);
        }
        foreach(var existing in old)
            if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DICH_VU_THUC_HIEN WITH(UPDLOCK,HOLDLOCK) WHERE MaCTKH=@MaCTKH",existing,tx)>0)throw KhamError("CacBuoc","Bước đã có thực hiện; không sửa qua kế hoạch nháp.");
        return rows.ToArray();
    }
    private static void WritePlanSteps(IDbConnection db,IDbTransaction? tx,ET_KEHOACHDIEUTRI header,IReadOnlyList<ET_CHITIETKEHOACH> rows,IReadOnlyList<ET_CHITIETKEHOACH> old)
    {
        foreach(var removed in old.Where(o=>!rows.Any(r=>r.MaCTKH==o.MaCTKH)))db.Execute("DELETE dbo.CHI_TIET_KE_HOACH WHERE MaCTKH=@MaCTKH",removed,tx);
        foreach(var row in rows)
            if(string.IsNullOrEmpty(row.MaCTKH))
            {
                row.MaCTKH=TaoMa(db,tx,"CHI_TIET_KE_HOACH","MaCTKH");
                db.Execute("INSERT dbo.CHI_TIET_KE_HOACH(MaCTKH,MaKHDT,MaDV,LanThu,SoLuongDuKien,DonGiaDuKien,TrangThai,GhiChu,NoiDungBuoc) VALUES(@MaCTKH,@MaKHDT,@MaDV,@LanThu,@SoLuongDuKien,@DonGiaDuKien,@TrangThai,@GhiChu,@NoiDungBuoc)",row,tx);
            }
            else db.Execute("UPDATE dbo.CHI_TIET_KE_HOACH SET MaDV=@MaDV,LanThu=@LanThu,SoLuongDuKien=@SoLuongDuKien,DonGiaDuKien=@DonGiaDuKien,GhiChu=@GhiChu,NoiDungBuoc=@NoiDungBuoc WHERE MaCTKH=@MaCTKH",row,tx);
    }
    private static void InsertPlan(IDbConnection db,IDbTransaction? tx,ET_KEHOACHDIEUTRI p)=>db.Execute("INSERT dbo.KE_HOACH_DIEU_TRI(MaKHDT,MaLS,MaBS,NgayLap,NgayBatDau,NgayDuKienKetThuc,TongTienDuKien,TrangThai,GhiChu,MaTTBNguon,MaMauKH,PhienBanMau) VALUES(@MaKHDT,@MaLS,@MaBS,@NgayLap,@NgayBatDau,@NgayDuKienKetThuc,0,@TrangThai,@GhiChu,@MaTTBNguon,@MaMauKH,@PhienBanMau)",p,tx);
    private static ET_KEHOACHDIEUTRI PlanHeader(IDbConnection db,IDbTransaction? tx,string id)=>db.QuerySingleOrDefault<ET_KEHOACHDIEUTRI>("SELECT * FROM dbo.KE_HOACH_DIEU_TRI WITH(UPDLOCK,HOLDLOCK) WHERE MaKHDT=@id",new{id},tx)??throw DAL_QUYENNGHIEPVU.TuChoi();
    private static (NguCanhNghiepVu,BanNhapKeHoach) OwnedPlan(IDbConnection db,IDbTransaction? tx,string id,NguCanhNghiepVu user,bool draft)
    {
        // Chỉ tìm đường tới khóa cha; giá trị đọc sơ bộ không cấp quyền và phải kiểm tra lại sau khóa.
        var parent=db.QuerySingleOrDefault<string>("SELECT MaLS FROM dbo.KE_HOACH_DIEU_TRI WITH(READUNCOMMITTED) WHERE MaKHDT=@id",new{id=ValidCode(id)},tx)??throw DAL_QUYENNGHIEPVU.TuChoi();
        var(actor,_)=OwnedVisit(db,tx,parent,user,draft);var plan=PlanSnapshot(db,tx,id);
        if(plan.KeHoach.MaLS!=parent || !actor.LaAdmin && plan.KeHoach.MaBS!=actor.MaNV)throw DAL_QUYENNGHIEPVU.TuChoi();return(actor,plan);
    }
    private static BanNhapKeHoach PlanSnapshot(IDbConnection db,IDbTransaction? tx,string id)=>PlanSnapshot(db,tx,PlanHeader(db,tx,id));
    private static BanNhapKeHoach PlanSnapshot(IDbConnection db,IDbTransaction? tx,ET_KEHOACHDIEUTRI p)
    {var rows=db.Query<ET_CHITIETKEHOACH>("SELECT * FROM dbo.CHI_TIET_KE_HOACH WITH(UPDLOCK,HOLDLOCK) WHERE MaKHDT=@MaKHDT ORDER BY LanThu,MaCTKH",p,tx).ToArray();return new(p,rows,Fingerprint(new{Plan=p,Steps=rows}));}
    private static ET_DICHVU ActiveService(IDbConnection db,IDbTransaction? tx,string id)=>db.QuerySingleOrDefault<ET_DICHVU>("SELECT * FROM dbo.DICH_VU WITH(HOLDLOCK) WHERE MaDV=@id AND TrangThai=N'Đang hoạt động'",new{id=ValidCode(id)},tx)??throw KhamError("MaDV","Dịch vụ không còn hoạt động.");
}
