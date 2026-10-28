using ET;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
namespace DAL;
public sealed partial class DAL_KHAMDIEUTRI
{
    public DuLieuKhongGianKham LayKhongGianKham(string maLS,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var(_,v)=OwnedVisit(db,tx,maLS,user,false);var s=ClinicalSnapshot(db,tx,v);
        var patient=db.QuerySingle<ET_BENHNHAN>("SELECT * FROM dbo.BENH_NHAN WITH(HOLDLOCK) WHERE MaBN=@MaBN",v,tx);
        var services=db.Query<ET_DICHVU>("SELECT d.* FROM dbo.DICH_VU d WHERE d.TrangThai=N'Đang hoạt động' OR EXISTS(SELECT 1 FROM dbo.CHI_TIET_KE_HOACH c JOIN dbo.KE_HOACH_DIEU_TRI k ON k.MaKHDT=c.MaKHDT WHERE k.MaLS=@maLS AND c.MaDV=d.MaDV) ORDER BY d.TenDV,d.MaDV",new{maLS},tx).ToArray();
        var plans=db.Query<ET_KEHOACHDIEUTRI>("SELECT * FROM dbo.KE_HOACH_DIEU_TRI WITH(HOLDLOCK) WHERE MaLS=@maLS ORDER BY NgayLap,MaKHDT",new{maLS},tx).ToArray();
        var reference=db.Query<ET_DMTINHTRANGBENH>("SELECT * FROM dbo.DM_TINH_TRANG_BENH WHERE MaDMTT LIKE N'REF28[_]%' AND TrangThai=N'Đang áp dụng' ORDER BY TenTinhTrang",transaction:tx).ToArray();
        return new DuLieuKhongGianKham(s,patient,services,plans,reference);
    });
    public string BatDauKham(string maPhieu,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        ClinicalLock(db,tx,"Phieu:"+ValidCode(maPhieu));
        var actor=Doctor(db,tx,user);
        var p=db.QuerySingleOrDefault<ET_PHIEUKHAMBENH>("SELECT * FROM dbo.PHIEU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaPhieu=@maPhieu",new{maPhieu},tx);
        if(p is null || !actor.LaAdmin && p.MaBS!=actor.MaNV)throw DAL_QUYENNGHIEPVU.TuChoi();
        var old=db.QuerySingleOrDefault<ET_LICHSUKHAMBENH>("SELECT * FROM dbo.LICH_SU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaPhieu=@maPhieu",new{maPhieu},tx);
        if(old is not null)
        {
            if(!actor.LaAdmin && old.MaBS!=actor.MaNV)throw DAL_QUYENNGHIEPVU.TuChoi();
            if(old.TrangThai=="Nháp" && p.TrangThai=="Chờ khám")db.Execute("UPDATE dbo.PHIEU_KHAM_BENH SET TrangThai=N'Đang khám' WHERE MaPhieu=@maPhieu",new{maPhieu},tx);
            return old.MaLS;
        }
        var now=ClinicalNow(db,tx);
        if(p.TrangThai!="Chờ khám" || p.ThoiGianTiepNhan>now)throw KhamError("MaPhieu","Chỉ bắt đầu phiếu chờ khám có thời gian hợp lệ.");
        var visit=new ET_LICHSUKHAMBENH {MaLS=TaoMa(db,tx,"LICH_SU_KHAM_BENH","MaLS"),MaPhieu=p.MaPhieu,MaBS=p.MaBS!,MaPhong=p.MaPhong,ThoiGianKham=now,TrieuChung="",ChanDoan="",KeHoachXuLy="",TrangThai="Nháp"};
        KiemTra<ET_LICHSUKHAMBENH>(db,tx,"LICH_SU_KHAM_BENH",visit,null,null);
        db.Execute("INSERT dbo.LICH_SU_KHAM_BENH(MaLS,MaPhieu,MaBS,MaPhong,ThoiGianKham,TrieuChung,ChanDoan,KeHoachXuLy,TrangThai,NgayXacNhan,GhiChu,KhongKeThuoc,LyDoKhongKeThuoc) VALUES(@MaLS,@MaPhieu,@MaBS,@MaPhong,@ThoiGianKham,@TrieuChung,@ChanDoan,@KeHoachXuLy,@TrangThai,@NgayXacNhan,@GhiChu,@KhongKeThuoc,@LyDoKhongKeThuoc)",visit,tx);
        db.Execute("UPDATE dbo.PHIEU_KHAM_BENH SET TrangThai=N'Đang khám' WHERE MaPhieu=@maPhieu",new{maPhieu},tx);
        ClinicalAudit(db,tx,actor,"Bắt đầu khám","LICH_SU_KHAM_BENH",visit.MaLS,null,null);
        return visit.MaLS;
    });

    public BanNhapKham LayBanNhap(string maLS,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {var (_,v)=OwnedVisit(db,tx,maLS,user,false);return ClinicalSnapshot(db,tx,v);});

    public BanNhapKham LuuKham(YeuCauLuuKham request,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor,visit)=OwnedVisit(db,tx,request.MaLS,user,true);
        var original=ClinicalSnapshot(db,tx,visit);RequireFingerprint(request.DauVanTay,original.DauVanTay);
        if(request.TinhTrang is null || request.TinhTrang.Count>100)throw KhamError("TinhTrang","Mỗi lần lưu tối đa 100 tình trạng.");
        if(request.KhongKeThuoc && string.IsNullOrWhiteSpace(request.LyDoKhongKeThuoc))throw KhamError("LyDoKhongKeThuoc","Nhập lý do không kê thuốc.");
        if(request.KhongKeThuoc && db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DON_THUOC WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@id AND TrangThai IN(N'Nháp',N'Đã xác nhận')",new{id=visit.MaLS},tx)>0)throw KhamError("DonThuoc","Xử lý đơn nháp/đơn hiệu lực trước khi chọn không kê thuốc.");
        var old=original.TinhTrang.ToDictionary(t=>t.MaTTB,StringComparer.Ordinal);
        var incoming=new List<ET_TINHTRANGBENH>();var keys=new HashSet<string>(StringComparer.Ordinal);
        foreach(var r in request.TinhTrang)
        {
            if(r is null)throw KhamError("TinhTrang","Dòng tình trạng không hợp lệ.");
            ET_TINHTRANGBENH? current=null;
            if(!string.IsNullOrEmpty(r.MaTTB) && (!old.TryGetValue(r.MaTTB,out current)||!keys.Add(r.MaTTB)))throw DAL_QUYENNGHIEPVU.TuChoi();
            var name=r.LoaiTinhTrang?.Trim()??"";
            var row=new ET_TINHTRANGBENH {MaTTB=current?.MaTTB??"",MaLS=visit.MaLS,MaDMTT=current?.MaDMTT,LoaiTinhTrang=name,MoTaTinhTrang=string.IsNullOrWhiteSpace(r.MoTaTinhTrang)?name:r.MoTaTinhTrang.Trim(),GhiChu=r.GhiChu};
            Required(row.LoaiTinhTrang,"TinhTrang","Nhập tình trạng bệnh.");
            KiemTra(db,tx,"TINH_TRANG_BENH",row,current,null);
            incoming.Add(row);
        }
        foreach(var removed in old.Values.Where(t=>!keys.Contains(t.MaTTB)))
        {
            if(ConditionHasPlan(db,tx,removed.MaTTB))throw KhamError("TinhTrang","Không xóa tình trạng đang là nguồn kế hoạch điều trị.");
        }
        visit.TrieuChung=request.TrieuChung;visit.ChanDoan=request.ChanDoan;visit.KeHoachXuLy=request.KeHoachXuLy;visit.GhiChu=request.GhiChu;visit.KhongKeThuoc=request.KhongKeThuoc?1:0;visit.LyDoKhongKeThuoc=request.KhongKeThuoc?request.LyDoKhongKeThuoc:null;
        KiemTra<ET_LICHSUKHAMBENH>(db,tx,"LICH_SU_KHAM_BENH",visit,null,null);
        // Hoàn tất validation trước DML, kể cả khi transaction ngoài bắt lỗi rồi tiếp tục công việc khác.
        foreach(var removed in old.Values.Where(t=>!keys.Contains(t.MaTTB)))db.Execute("DELETE dbo.TINH_TRANG_BENH WHERE MaTTB=@MaTTB",removed,tx);
        foreach(var row in incoming)
            if(string.IsNullOrEmpty(row.MaTTB))
            {
                row.MaTTB=TaoMa(db,tx,"TINH_TRANG_BENH","MaTTB");
                db.Execute("INSERT dbo.TINH_TRANG_BENH(MaTTB,MaLS,LoaiTinhTrang,MoTaTinhTrang,GhiChu,MaDMTT) VALUES(@MaTTB,@MaLS,@LoaiTinhTrang,@MoTaTinhTrang,@GhiChu,@MaDMTT)",row,tx);
            }
            else db.Execute("UPDATE dbo.TINH_TRANG_BENH SET LoaiTinhTrang=@LoaiTinhTrang,MoTaTinhTrang=@MoTaTinhTrang,GhiChu=@GhiChu,MaDMTT=@MaDMTT WHERE MaTTB=@MaTTB",row,tx);
        db.Execute("UPDATE dbo.LICH_SU_KHAM_BENH SET TrieuChung=@TrieuChung,ChanDoan=@ChanDoan,KeHoachXuLy=@KeHoachXuLy,GhiChu=@GhiChu,KhongKeThuoc=@KhongKeThuoc,LyDoKhongKeThuoc=@LyDoKhongKeThuoc WHERE MaLS=@MaLS",visit,tx);
        var saved=ClinicalSnapshot(db,tx,visit);ClinicalAudit(db,tx,actor,"Lưu nháp khám","LICH_SU_KHAM_BENH",visit.MaLS,original.DauVanTay,saved.DauVanTay);return saved;
    });

    public BanNhapKham HoanTatKham(string maLS,string dauVanTay,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var (actor,visit)=OwnedVisit(db,tx,maLS,user,false);var s=ClinicalSnapshot(db,tx,visit);
        if(visit.TrangThai=="Đã xác nhận")return s;
        if(!s.DuocSua)throw DAL_QUYENNGHIEPVU.TuChoi();RequireFingerprint(dauVanTay,s.DauVanTay);
        Required(visit.TrieuChung,"TrieuChung","Nhập triệu chứng trước khi hoàn tất.");Required(visit.ChanDoan,"ChanDoan","Nhập chẩn đoán trước khi hoàn tất.");Required(visit.KeHoachXuLy,"KeHoachXuLy","Nhập xử trí trước khi hoàn tất.");
        if(s.TinhTrang.Count==0)throw KhamError("TinhTrang","Ghi ít nhất một tình trạng; có thể mô tả thủ công.");
        var orders=db.Query<ET_DONTHUOC>("SELECT * FROM dbo.DON_THUOC WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@maLS",new{maLS},tx).ToArray();
        if(orders.Any(o=>o.TrangThai=="Nháp"))throw KhamError("DonThuoc","Còn đơn nháp cần ký hoặc hủy.");
        if(visit.KhongKeThuoc==1){Required(visit.LyDoKhongKeThuoc,"LyDoKhongKeThuoc","Nhập lý do không kê thuốc.");if(orders.Any(o=>o.TrangThai=="Đã xác nhận"))throw KhamError("DonThuoc","Đơn hiệu lực không đồng thời với không kê thuốc.");}
        else if(!orders.Any(o=>o.TrangThai=="Đã xác nhận"))throw KhamError("DonThuoc","Ký đơn hiệu lực hoặc chọn không kê thuốc và ghi lý do.");
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DICH_VU_THUC_HIEN WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@maLS AND TrangThai=N'Nháp'",new{maLS},tx)>0)throw KhamError("DichVu","Còn dịch vụ nháp cần xử lý.");
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.KE_HOACH_DIEU_TRI WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@maLS AND TrangThai=N'Nháp'",new{maLS},tx)>0)throw KhamError("KeHoach","Duyệt hoặc hủy kế hoạch nháp trước khi hoàn tất.");
        var now=ClinicalNow(db,tx);if(now<visit.ThoiGianKham)throw KhamError("ThoiGianKham","Giờ hệ thống trước thời gian bắt đầu khám.");
        db.Execute("UPDATE dbo.LICH_SU_KHAM_BENH SET TrangThai=N'Đã xác nhận',NgayXacNhan=@now WHERE MaLS=@maLS; UPDATE dbo.PHIEU_KHAM_BENH SET TrangThai=N'Chờ thanh toán' WHERE MaPhieu=@receipt",new{maLS,now,receipt=visit.MaPhieu},tx);
        visit.TrangThai="Đã xác nhận";visit.NgayXacNhan=now;visit.TrangThaiPhieu="Chờ thanh toán";
        var saved=ClinicalSnapshot(db,tx,visit);ClinicalAudit(db,tx,actor,"Hoàn tất chuyên môn","LICH_SU_KHAM_BENH",maLS,s.DauVanTay,saved.DauVanTay);return saved;
    });

    private static NguCanhNghiepVu Doctor(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu user)
    {var current=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);if(!current.CoQuyen(QuyenLamViec.Kham))throw DAL_QUYENNGHIEPVU.TuChoi();return current;}
    private static (NguCanhNghiepVu,VisitRead) OwnedVisit(IDbConnection db,IDbTransaction? tx,string id,NguCanhNghiepVu user,bool draft)
    {
        ClinicalLock(db,tx,"LanKham:"+ValidCode(id));var actor=Doctor(db,tx,user);
        var v=db.QuerySingleOrDefault<VisitRead>("SELECT l.*,p.MaHS,p.MaBS BacSiTiepNhan,p.TrangThai TrangThaiPhieu,h.MaBN FROM dbo.LICH_SU_KHAM_BENH l WITH(UPDLOCK,HOLDLOCK) JOIN dbo.PHIEU_KHAM_BENH p WITH(UPDLOCK,HOLDLOCK) ON p.MaPhieu=l.MaPhieu JOIN dbo.HO_SO_BENH_AN h WITH(HOLDLOCK) ON h.MaHS=p.MaHS WHERE l.MaLS=@id AND (@LaAdmin=1 OR (l.MaBS=@MaNV AND p.MaBS=@MaNV))",new{id,actor.MaNV,actor.LaAdmin},tx);
        if(v is null || draft&&!Editable(v))throw DAL_QUYENNGHIEPVU.TuChoi();return(actor,v);
    }
    private static bool Editable(VisitRead v)=>v.TrangThai=="Nháp" && v.TrangThaiPhieu is "Chờ khám" or "Đang khám";
    private static BanNhapKham ClinicalSnapshot(IDbConnection db,IDbTransaction? tx,VisitRead v)
    {
        var conditions=db.Query<ET_TINHTRANGBENH>("SELECT * FROM dbo.TINH_TRANG_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@MaLS ORDER BY MaTTB",v,tx).ToArray();
        return new(v,conditions,Fingerprint(new{Visit=(ET_LICHSUKHAMBENH)v,Conditions=conditions}),Editable(v),v.MaBN,v.MaHS);
    }
    private static string Fingerprint<T>(T value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private static void RequireFingerprint(string? supplied,string expected){if(string.IsNullOrEmpty(supplied)||!string.Equals(supplied,expected,StringComparison.Ordinal))throw new XungDotDuLieuKhamException();}
    private static string ValidCode(string id){DAL_QUYENNGHIEPVU.KiemTraMa(id);return id;}
    private static DateTime ClinicalNow(IDbConnection db,IDbTransaction? tx)=>db.ExecuteScalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())",transaction:tx);
    private static LoiKhamException KhamError(string field,string message)=>new(field,message);
    private static void Required(string? value,string field,string message){if(string.IsNullOrWhiteSpace(value))throw KhamError(field,message);}
    private static void ClinicalLock(IDbConnection db,IDbTransaction? tx,string source)
    {if(db.ExecuteScalar<int>("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; SELECT @r",new{resource="RHM:Kham:"+source},tx)<0)throw new InvalidOperationException("Dữ liệu đang được lưu ở cửa sổ khác. Vui lòng thử lại.");}
    private static bool ConditionHasPlan(IDbConnection db,IDbTransaction? tx,string id)=>db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.KE_HOACH_DIEU_TRI WITH(UPDLOCK,HOLDLOCK) WHERE MaTTBNguon=@id",new{id},tx)>0;
    private static void ClinicalAudit(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu user,string action,string table,string id,string? before,string? after)
    {db.Execute("INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuTruoc,DuLieuSau) VALUES(@nk,@MaTK,SYSDATETIME(),@action,@table,@id,N'Thành công',@before,@after)",new{nk=TaoMa(db,tx,"NHAT_KY_THAO_TAC","MaNK"),user.MaTK,action,table,id,before,after},tx);}
}
