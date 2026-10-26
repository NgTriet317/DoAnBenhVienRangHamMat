using ET;
using Dapper;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
namespace DAL;
public partial class DAL_DONTHUOC
{
    public IReadOnlyList<ThuocKeDon> TimThuoc(BoLocThuoc filter,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        ArgumentNullException.ThrowIfNull(filter);ConfigUser(db,tx,user,false);
        if(new[]{filter.TuKhoa,filter.HamLuong,filter.DangBaoChe}.Any(s=>s?.Length>200))throw RxError("TuKhoa","Từ khóa tối đa 200 ký tự.");
        if(filter.MaNhomThuoc is not null)DAL_QUYENNGHIEPVU.KiemTraMa(filter.MaNhomThuoc);
        var drugs=db.Query<Drug>(DrugSql+"""
            WHERE h.TrangThai=N'Đang hoạt động'
              AND(@term IS NULL OR t.MaThuoc LIKE @term OR h.TenHH LIKE @term)
              AND(@strength IS NULL OR t.HamLuong LIKE @strength) AND(@form IS NULL OR h.DonViTinh=@form)
              AND(@groupId IS NULL OR EXISTS(SELECT 1 FROM dbo.CAU_HINH_THUOC_KE_DON c JOIN dbo.NHOM_THUOC_KE_DON g ON g.MaNhomThuoc=c.MaNhomThuoc WHERE c.MaThuoc=t.MaThuoc AND c.MaNhomThuoc=@groupId AND g.TrangThai=N'Đang áp dụng'))
            ORDER BY h.TenHH,t.MaThuoc OFFSET 0 ROWS FETCH NEXT 100 ROWS ONLY
            """,new{term=string.IsNullOrWhiteSpace(filter.TuKhoa)?null:"%"+filter.TuKhoa.Trim()+"%",strength=string.IsNullOrWhiteSpace(filter.HamLuong)?null:"%"+filter.HamLuong.Trim()+"%",form=string.IsNullOrWhiteSpace(filter.DangBaoChe)?null:filter.DangBaoChe.Trim(),groupId=filter.MaNhomThuoc},tx).ToArray();
        return (IReadOnlyList<ThuocKeDon>)drugs.Select(d=>new ThuocKeDon(d.MaThuoc,d.MaHH,d.TenThuoc,d.HamLuong,d.DonViTinh,d.DonGiaBan,
            db.ExecuteScalar<decimal>("SELECT COALESCE(SUM(SoLuongTon),0) FROM dbo.VW_TON_KHO WHERE MaHH=@MaHH",d,tx),ReadGuidance(db,tx,d.MaThuoc) is {TrangThai:"Đã duyệt"} c?c:null)).ToArray();
    });
    public IReadOnlyList<LuotKeDon> LayLuotKeDon(NguCanhNghiepVu user,string? patient=null)=>ThucHien((db,tx)=>
    {var a=RxDoctor(db,tx,user);if(patient is not null)DAL_QUYENNGHIEPVU.KiemTraMa(patient);return (IReadOnlyList<LuotKeDon>)db.Query<LuotKeDon>("SELECT TOP(100) l.MaLS,h.MaBN,b.HoTenBN,l.MaBS,l.TrangThai,l.ThoiGianKham FROM dbo.LICH_SU_KHAM_BENH l JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=p.MaHS JOIN dbo.BENH_NHAN b ON b.MaBN=h.MaBN WHERE l.MaBS=@MaNV AND p.MaBS=@MaNV AND(@patient IS NULL OR h.MaBN=@patient) ORDER BY l.ThoiGianKham DESC,l.MaLS",new{a.MaNV,patient},tx).ToArray();});
    public KhongGianKeDon LayKhongGianKeDon(string maLS,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {var(a,v)=RxVisit(db,tx,maLS,user);return new KhongGianKeDon(new(v.MaLS,v.MaBN,v.HoTenBN,v.MaBS,v.TrangThai,v.ThoiGianKham),v.ChanDoan,VisitDraft(v)&&v.KhongKeThuoc==0&&!Processed(db,tx,v.MaLS),db.Query<ET_DONTHUOC>("SELECT * FROM dbo.DON_THUOC WITH(HOLDLOCK) WHERE MaLS=@MaLS ORDER BY NgayKe DESC,MaDT",v,tx).ToArray(),v.NgaySinh,v.DiUng,v.TienSuBenh);});
    public BanNhapDon LayBanNhapDon(string id,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var parent=Route(db,tx,id);RxLock(db,tx,"RHM:Kham:LanKham:"+parent);
        var allowed=new DAL_KHAMDIEUTRI(tx).LayDonThuoc(id,user);if(allowed.DonThuoc.MaLS!=parent)throw DAL_QUYENNGHIEPVU.TuChoi();
        var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);var v=ReadRxVisit(db,tx,parent);var s=RxSnapshot(db,tx,id,v,a);
        if(!a.LaAdmin&&!a.CoQuyen(QuyenLamViec.Kham)&&!a.CoQuyen(QuyenLamViec.GiaoThuoc)&&a.CoQuyen(QuyenLamViec.ThuTien))
        {s.Don.GhiChu=s.Don.LyDoThayThe=null;foreach(var d in s.CacDong){d.LieuDung=d.HuongDan="";d.DuongDung=null;}}
        return s;
    });
    public BanNhapDon LuuDon(YeuCauLuuDon request,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        ArgumentNullException.ThrowIfNull(request);RxLock(db,tx,"RHM:DonThuoc:Write");var(a,v)=RxVisit(db,tx,request.MaLS,user);
        BanNhapDon? old=null;if(request.MaDT is not null){old=RxSnapshot(db,tx,request.MaDT,v,a);if(old.Don.MaLS!=v.MaLS||old.Don.MaBS!=a.MaNV)throw DAL_QUYENNGHIEPVU.TuChoi();if(old.Don.TrangThai!="Nháp")throw RxError("MaDT","Chỉ sửa đơn nháp.");RxHash(request.DauVanTay,old.DauVanTay);if(request.MaDTThayThe!=old.Don.MaDTThayThe)throw RxError("MaDTThayThe","Giữ nguyên nguồn thay; hủy nháp và lập lại nếu chọn sai nguồn.");}
        CanPrescribe(db,tx,v,request.MaDTThayThe,request.LyDoThayThe,request.MaDT);
        RequireAdditionalReason(db,tx,v.MaLS,request.MaDT,request.MaDTThayThe,request.GhiChu);
        var header=new ET_DONTHUOC {MaDT=old?.Don.MaDT??"",MaLS=v.MaLS,MaBS=a.MaNV,NgayKe=old?.Don.NgayKe??db.ExecuteScalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())",transaction:tx),TrangThai="Nháp",MaDTThayThe=request.MaDTThayThe,LyDoThayThe=request.MaDTThayThe is null?null:request.LyDoThayThe,GhiChu=request.GhiChu};
        KiemTra(db,tx,"DON_THUOC",header,old?.Don,null);var rows=PrepareRxRows(db,tx,header,request.CacDong,old?.CacDong??Array.Empty<ET_CHITIETDONTHUOC>(),false);
        if(old is null){header.MaDT=TaoMa(db,tx,"DON_THUOC","MaDT");db.Execute("INSERT dbo.DON_THUOC(MaDT,MaLS,MaBS,NgayKe,TrangThai,LyDoThayThe,GhiChu,MaDTThayThe) VALUES(@MaDT,@MaLS,@MaBS,@NgayKe,@TrangThai,@LyDoThayThe,@GhiChu,@MaDTThayThe)",header,tx);}
        else db.Execute("UPDATE dbo.DON_THUOC SET LyDoThayThe=@LyDoThayThe,GhiChu=@GhiChu WHERE MaDT=@MaDT",header,tx);
        // Mọi dòng đã được kiểm tra trước khi thay thế tập chi tiết, cả trong transaction ngoài.
        db.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@MaDT",header,tx);
        foreach(var row in rows.OrderBy(r=>string.IsNullOrEmpty(r.MaCTDT)?1:0)){row.MaDT=header.MaDT;if(string.IsNullOrEmpty(row.MaCTDT))row.MaCTDT=TaoMa(db,tx,"CHI_TIET_DON_THUOC","MaCTDT");db.Execute("INSERT dbo.CHI_TIET_DON_THUOC(MaCTDT,MaDT,MaThuoc,LieuDung,SoLuong,SoNgayDung,HuongDan,DuongDung,TenThuocChot,HamLuongChot,DonViTinhChot,PhienBanHuongDan) VALUES(@MaCTDT,@MaDT,@MaThuoc,@LieuDung,@SoLuong,@SoNgayDung,@HuongDan,@DuongDung,@TenThuocChot,@HamLuongChot,@DonViTinhChot,@PhienBanHuongDan)",row,tx);}
        var saved=RxSnapshot(db,tx,header.MaDT,v,a);RxAudit(db,tx,a,"Lưu đơn nháp",header.MaDT,old?.DauVanTay,saved.DauVanTay);return saved;
    });
    public BanNhapDon XacNhanDon(string id,string token,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        RxLock(db,tx,"RHM:DonThuoc:Write");var(a,v,s)=OwnRx(db,tx,id,user);
        if(s.Don.TrangThai=="Đã xác nhận")return s;if(s.Don.TrangThai!="Nháp")throw RxError("MaDT","Chỉ ký đơn nháp.");RxHash(token,s.DauVanTay);
        CanPrescribe(db,tx,v,s.Don.MaDTThayThe,s.Don.LyDoThayThe,id);
        RequireAdditionalReason(db,tx,v.MaLS,id,s.Don.MaDTThayThe,s.Don.GhiChu);
        var input=s.CacDong.Select(d=>new DongThuocKeDon{MaCTDT=d.MaCTDT,MaThuoc=d.MaThuoc,LieuDung=d.LieuDung,DuongDung=d.DuongDung??"",HuongDan=d.HuongDan,SoNgayDung=d.SoNgayDung,SoLuong=d.SoLuong,PhienBanHuongDan=d.PhienBanHuongDan,DaXacNhanPhuHop=d.PhienBanHuongDan is not null}).ToArray();
        var rows=PrepareRxRows(db,tx,s.Don,input,s.CacDong,true);
        foreach(var row in rows)db.Execute("UPDATE dbo.CHI_TIET_DON_THUOC SET TenThuocChot=@TenThuocChot,HamLuongChot=@HamLuongChot,DonViTinhChot=@DonViTinhChot WHERE MaCTDT=@MaCTDT",row,tx);
        db.Execute("UPDATE dbo.DON_THUOC SET TrangThai=N'Đã xác nhận' WHERE MaDT=@id",new{id},tx);
        if(s.Don.MaDTThayThe is string old){db.Execute("UPDATE dbo.DON_THUOC SET TrangThai=N'Ngừng áp dụng' WHERE MaDT=@old AND TrangThai=N'Đã xác nhận'",new{old},tx);RxAudit(db,tx,a,"Ngừng đơn do thay thế",old,null,id);}
        var result=RxSnapshot(db,tx,id,v,a);RxAudit(db,tx,a,"Ký đơn thuốc",id,s.DauVanTay,result.DauVanTay);return result;
    });
    public BanNhapDon HuyDonNhap(string id,string token,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {RxLock(db,tx,"RHM:DonThuoc:Write");var(a,v,s)=OwnRx(db,tx,id,user);if(s.Don.TrangThai!="Nháp")throw RxError("MaDT","Chỉ hủy đơn nháp.");RxHash(token,s.DauVanTay);if(Processed(db,tx,v.MaLS))throw RxError("MaDT","Lượt có chứng từ xử lý; đối chiếu trước khi đổi đơn.");db.Execute("UPDATE dbo.DON_THUOC SET TrangThai=N'Hủy' WHERE MaDT=@id",new{id},tx);RxAudit(db,tx,a,"Hủy đơn nháp",id,s.DauVanTay,null);return RxSnapshot(db,tx,id,v,a);});
    public BanInDonThuoc LayBanInDon(string id,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);if(!a.LaAdmin&&!a.CoQuyen(QuyenLamViec.Kham)&&!a.CoQuyen(QuyenLamViec.GiaoThuoc))throw DAL_QUYENNGHIEPVU.TuChoi();var s=new DAL_DONTHUOC(tx).LayBanNhapDon(id,user);if(s.Don.TrangThai!="Đã xác nhận"||s.CacDong.Count==0||s.CacDong.Any(d=>string.IsNullOrWhiteSpace(d.TenThuocChot)||string.IsNullOrWhiteSpace(d.HamLuongChot)||string.IsNullOrWhiteSpace(d.DonViTinhChot)))throw RxError("MaDT","Chỉ in bản đã ký có đầy đủ thông tin chốt. Đơn cũ xem qua lịch sử.");var v=ReadRxVisit(db,tx,s.Don.MaLS);return new BanInDonThuoc(s,v.HoTenBN,db.QuerySingle<string>("SELECT HoTen FROM dbo.NHAN_VIEN WHERE MaNV=@MaBS",s.Don,tx),v.ChanDoan,v.NgaySinh);});
    public bool KiemTraDonChoThu(string maLS,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {DAL_QUYENNGHIEPVU.KiemTraMa(maLS);RxLock(db,tx,"RHM:Kham:LanKham:"+maLS);var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);var v=ReadRxVisit(db,tx,maLS);if(!a.LaAdmin&&!a.CoQuyen(QuyenLamViec.ThuTien)&&!(a.CoQuyen(QuyenLamViec.Kham)&&v.MaBS==a.MaNV&&v.BacSiPhieu==a.MaNV))throw DAL_QUYENNGHIEPVU.TuChoi();if(v.TrangThai!="Đã xác nhận"||db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DON_THUOC WITH(HOLDLOCK) WHERE MaLS=@maLS AND TrangThai=N'Nháp'",new{maLS},tx)>0||db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DICH_VU_THUC_HIEN WITH(HOLDLOCK) WHERE MaLS=@maLS AND TrangThai=N'Nháp'",new{maLS},tx)>0)throw RxError("MaLS","Chưa hoàn tất hoặc còn đơn/dịch vụ nháp cần xử lý trước tính phí.");DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db,tx,a,"LICH_SU_KHAM_BENH",maLS);return true;});

    private List<ET_CHITIETDONTHUOC> PrepareRxRows(IDbConnection db,IDbTransaction? tx,ET_DONTHUOC header,IReadOnlyList<DongThuocKeDon> input,IReadOnlyList<ET_CHITIETDONTHUOC> previous,bool signing)
    {
        if(input is null||input.Count is <1 or >100)throw RxError("CacDong","Đơn cần 1–100 dòng thuốc đầy đủ chỉ định.");var old=previous.ToDictionary(d=>d.MaCTDT,StringComparer.OrdinalIgnoreCase);var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var drugs=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var result=new List<ET_CHITIETDONTHUOC>();
        foreach(var d in input)
        {
            if(d is null)throw RxError("CacDong","Dòng thuốc không hợp lệ.");var drug=ActiveDrug(db,tx,d.MaThuoc);if(!drugs.Add(drug.MaThuoc))throw RxError("MaThuoc","Thuốc đã có trong đơn; chỉnh dòng đang có.");ET_CHITIETDONTHUOC? before=null;
            if(d.MaCTDT is not null&&(!old.TryGetValue(d.MaCTDT,out before)||!ids.Add(d.MaCTDT)))throw DAL_QUYENNGHIEPVU.TuChoi();
            var row=new ET_CHITIETDONTHUOC{MaCTDT=before?.MaCTDT??"",MaDT=header.MaDT,MaThuoc=drug.MaThuoc,LieuDung=d.LieuDung,DuongDung=d.DuongDung,HuongDan=d.HuongDan,SoNgayDung=d.SoNgayDung,SoLuong=d.SoLuong,PhienBanHuongDan=d.PhienBanHuongDan};
            if(signing&&before is not null&&(before.TenThuocChot!=drug.TenThuoc||before.HamLuongChot!=drug.HamLuong||before.DonViTinhChot!=drug.DonViTinh))throw new XungDotDuLieuKhamException("Tên, hàm lượng hoặc đơn vị thuốc đã đổi sau lần lưu. Lưu lại nháp để đối chiếu thông tin mới trước khi ký.");
            row.TenThuocChot=drug.TenThuoc;row.HamLuongChot=drug.HamLuong;row.DonViTinhChot=drug.DonViTinh;
            KiemTra(db,tx,"CHI_TIET_DON_THUOC",row,before,null);
            if(string.IsNullOrWhiteSpace(row.LieuDung)||string.IsNullOrWhiteSpace(row.DuongDung)||string.IsNullOrWhiteSpace(row.HuongDan)||row.SoNgayDung<=0||row.SoLuong<=0)throw RxError("CacDong","Mỗi dòng cần liều, đường dùng, hướng dẫn, ngày nguyên dương và lượng dương.");
            if(row.PhienBanHuongDan is int version)
            {var c=ReadGuidance(db,tx,drug.MaThuoc);if(!d.DaXacNhanPhuHop||c?.TrangThai!="Đã duyệt"||c.PhienBan!=version)throw RxError("PhienBanHuongDan","Chỉ áp hướng dẫn đã duyệt đúng phiên bản sau khi bác sĩ xác nhận phù hợp đối tượng.");}
            result.Add(row);
        }
        return result;
    }
    private static void CanPrescribe(IDbConnection db,IDbTransaction? tx,RxVisitData v,string? replace,string? reason,string? current)
    {
        if(v.KhongKeThuoc==1)throw RxError("MaLS","Lượt đã chọn không kê thuốc; không tự tạo chỉ định mới.");
        if(Processed(db,tx,v.MaLS))throw RxError("MaDT","Lượt đã có hóa đơn hoặc nguồn xuất thuốc; không sửa/thay đơn.");
        if(!VisitDraft(v)&&!(v.TrangThai=="Đã xác nhận"&&v.TrangThaiPhieu=="Chờ thanh toán"&&replace is not null))throw RxError("MaLS","Chỉ kê ở lượt đang khám; sau hoàn tất chỉ soạn đơn thay thế trước xử lý tài chính/cấp thuốc.");
        if(replace is null)return;
        if(string.IsNullOrWhiteSpace(reason))throw RxError("LyDoThayThe","Ghi lý do thay đơn.");
        var old=db.QuerySingleOrDefault<ET_DONTHUOC>("SELECT * FROM dbo.DON_THUOC WITH(HOLDLOCK) WHERE MaDT=@replace AND MaLS=@MaLS AND MaBS=@MaBS AND TrangThai=N'Đã xác nhận'",new{replace,v.MaLS,v.MaBS},tx);
        if(old is null||replace==current)throw RxError("MaDTThayThe","Chọn đơn hiệu lực cùng lượt và bác sĩ.");
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DON_THUOC WITH(HOLDLOCK) WHERE MaDTThayThe=@replace AND TrangThai=N'Nháp' AND(@current IS NULL OR MaDT<>@current)",new{replace,current},tx)>0)throw RxError("MaDTThayThe","Đã có bản nháp thay đơn này; mở bản đang soạn để tiếp tục.");
    }
    private static void RequireAdditionalReason(IDbConnection db,IDbTransaction? tx,string maLS,string? current,string? replacement,string? note)
    {if(replacement is null&&string.IsNullOrWhiteSpace(note)&&db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DON_THUOC WITH(HOLDLOCK) WHERE MaLS=@maLS AND TrangThai=N'Đã xác nhận' AND(@current IS NULL OR MaDT<>@current)",new{maLS,current},tx)>0)throw RxError("GhiChu","Ghi lý do đơn bổ sung khi lượt đã có đơn hiệu lực.");}
    private static bool Processed(IDbConnection db,IDbTransaction? tx,string maLS)=>db.ExecuteScalar<int>("SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.HOA_DON WITH(HOLDLOCK) WHERE MaLS=@maLS) OR EXISTS(SELECT 1 FROM dbo.PHIEU_XUAT p WITH(HOLDLOCK) WHERE p.MaLS=@maLS AND p.TrangThai=N'Đã ghi sổ') OR EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_XUAT x WITH(HOLDLOCK) JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaCTDT=x.MaCTDT JOIN dbo.DON_THUOC d ON d.MaDT=c.MaDT WHERE d.MaLS=@maLS) THEN 1 ELSE 0 END",new{maLS},tx)==1;
    private static BanNhapDon RxSnapshot(IDbConnection db,IDbTransaction? tx,string id,RxVisitData v,NguCanhNghiepVu a)
    {var h=db.QuerySingleOrDefault<ET_DONTHUOC>("SELECT * FROM dbo.DON_THUOC WITH(HOLDLOCK) WHERE MaDT=@id AND MaLS=@MaLS",new{id,v.MaLS},tx)??throw DAL_QUYENNGHIEPVU.TuChoi();var rows=db.Query<ET_CHITIETDONTHUOC>("SELECT * FROM dbo.CHI_TIET_DON_THUOC WITH(HOLDLOCK) WHERE MaDT=@id ORDER BY MaCTDT",new{id},tx).ToArray();var own=a.CoQuyen(QuyenLamViec.Kham)&&v.MaBS==a.MaNV&&v.BacSiPhieu==a.MaNV&&h.MaBS==a.MaNV;var free=own&&v.KhongKeThuoc==0&&!Processed(db,tx,v.MaLS);return new(h,rows,v.MaBN,RxFingerprint(new{Header=h,Rows=rows}),free&&h.TrangThai=="Nháp"&&(VisitDraft(v)||v.TrangThai=="Đã xác nhận"&&v.TrangThaiPhieu=="Chờ thanh toán"&&h.MaDTThayThe is not null),free&&h.TrangThai=="Đã xác nhận"&&(VisitDraft(v)||v.TrangThaiPhieu=="Chờ thanh toán"));}
    private static (NguCanhNghiepVu,RxVisitData,BanNhapDon) OwnRx(IDbConnection db,IDbTransaction? tx,string id,NguCanhNghiepVu user)
    {var(a,v)=RxVisit(db,tx,Route(db,tx,id),user);var s=RxSnapshot(db,tx,id,v,a);if(s.Don.MaBS!=a.MaNV)throw DAL_QUYENNGHIEPVU.TuChoi();return(a,v,s);}
    private static string Route(IDbConnection db,IDbTransaction? tx,string id){DAL_QUYENNGHIEPVU.KiemTraMa(id);return db.QuerySingleOrDefault<string>("SELECT MaLS FROM dbo.DON_THUOC WITH(READUNCOMMITTED) WHERE MaDT=@id",new{id},tx)??throw DAL_QUYENNGHIEPVU.TuChoi();}
    private static (NguCanhNghiepVu,RxVisitData) RxVisit(IDbConnection db,IDbTransaction? tx,string maLS,NguCanhNghiepVu user)
    {DAL_QUYENNGHIEPVU.KiemTraMa(maLS);RxLock(db,tx,"RHM:Kham:LanKham:"+maLS);var a=RxDoctor(db,tx,user);var v=ReadRxVisit(db,tx,maLS);if(v.MaBS!=a.MaNV||v.BacSiPhieu!=a.MaNV)throw DAL_QUYENNGHIEPVU.TuChoi();return(a,v);}
    private static RxVisitData ReadRxVisit(IDbConnection db,IDbTransaction? tx,string maLS)=>db.QuerySingleOrDefault<RxVisitData>("SELECT l.*,p.MaBS BacSiPhieu,p.TrangThai TrangThaiPhieu,h.MaBN,b.HoTenBN,b.NgaySinh,b.DiUng,b.TienSuBenh FROM dbo.LICH_SU_KHAM_BENH l WITH(HOLDLOCK) JOIN dbo.PHIEU_KHAM_BENH p WITH(HOLDLOCK) ON p.MaPhieu=l.MaPhieu JOIN dbo.HO_SO_BENH_AN h WITH(HOLDLOCK) ON h.MaHS=p.MaHS JOIN dbo.BENH_NHAN b WITH(HOLDLOCK) ON b.MaBN=h.MaBN WHERE l.MaLS=@maLS",new{maLS},tx)??throw DAL_QUYENNGHIEPVU.TuChoi();
    private static NguCanhNghiepVu RxDoctor(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu user){var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);if(!a.CoQuyen(QuyenLamViec.Kham))throw DAL_QUYENNGHIEPVU.TuChoi();return a;}
    private static bool VisitDraft(RxVisitData v)=>v.TrangThai=="Nháp"&&v.TrangThaiPhieu is "Chờ khám" or "Đang khám";
    private static string RxFingerprint<T>(T value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private static void RxHash(string? token,string expected){if(token!=expected)throw new XungDotDuLieuKhamException();}
    private static LoiKhamException RxError(string field,string text)=>new(field,text);
    private static void RxLock(IDbConnection db,IDbTransaction? tx,string key){if(db.ExecuteScalar<int>("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; SELECT @r",new{key},tx)<0)throw new InvalidOperationException("Đơn đang được xử lý. Thử lại sau.");}
    private static void RxAudit(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu a,string action,string id,string? before,string? after)=>db.Execute("INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuTruoc,DuLieuSau) VALUES(@nk,@MaTK,SYSDATETIME(),@action,N'DON_THUOC',@id,N'Thành công',@before,@after)",new{nk=TaoMa(db,tx,"NHAT_KY_THAO_TAC","MaNK"),a.MaTK,action,id,before,after},tx);
    private const string DrugSql="SELECT t.MaThuoc,t.MaHH,h.TenHH TenThuoc,t.HamLuong,h.DonViTinh,t.DonGiaBan FROM dbo.THUOC t WITH(HOLDLOCK) JOIN dbo.HANG_HOA h WITH(HOLDLOCK) ON h.MaHH=t.MaHH ";
    private static Drug ActiveDrug(IDbConnection db,IDbTransaction? tx,string id){DAL_QUYENNGHIEPVU.KiemTraMa(id);var d=db.QuerySingleOrDefault<Drug>(DrugSql+"WHERE t.MaThuoc=@id AND h.TrangThai=N'Đang hoạt động'",new{id},tx)??throw RxError("MaThuoc","Thuốc không còn hoạt động.");if(string.IsNullOrWhiteSpace(d.TenThuoc)||string.IsNullOrWhiteSpace(d.HamLuong)||string.IsNullOrWhiteSpace(d.DonViTinh))throw RxError("MaThuoc","Thuốc thiếu tên/hàm lượng/đơn vị để chốt.");return d;}
    private sealed class Drug{public string MaThuoc{get;set;}="";public string MaHH{get;set;}="";public string TenThuoc{get;set;}="";public string HamLuong{get;set;}="";public string DonViTinh{get;set;}="";public decimal DonGiaBan{get;set;}}
    private sealed class RxVisitData:ET_LICHSUKHAMBENH{public string MaBN{get;set;}="";public string HoTenBN{get;set;}="";public string BacSiPhieu{get;set;}="";public string TrangThaiPhieu{get;set;}="";public DateTime? NgaySinh{get;set;}public string? DiUng{get;set;}public string? TienSuBenh{get;set;}}
}
