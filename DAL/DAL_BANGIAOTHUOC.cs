using System.Data;
using Dapper;
using System.Text.Json;
using ET;
namespace DAL;

public sealed class DAL_BANGIAOTHUOC : dbconnect
{
    private readonly IDbTransaction? callerTransaction;
    public DAL_BANGIAOTHUOC(IDbTransaction? transaction=null):base(transaction)=>callerTransaction=transaction;
    public ET_BANGIAOTHUOC? LayTheoDon(string maDT,NguCanhNghiepVu nguoiDung) => ThucHien((db,tx) =>
    {
        new DAL_KHAMDIEUTRI(tx).LayDonThuoc(maDT,nguoiDung);
        return db.QuerySingleOrDefault<ET_BANGIAOTHUOC>("SELECT * FROM dbo.BAN_GIAO_THUOC WHERE MaDT=@maDT",new{maDT},tx);
    });
    public HangDoiBanGiao LayHangDoi(BoLocBanGiao filter,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        RequirePharmacy(db,tx,user);
        if(filter is null || filter.Trang<1 || filter.Trang>1000000 || filter.KichThuocTrang is <1 or >100 ||
            filter.TrangThai is not("Chờ giao" or "Đã giao" or "Tất cả") || filter.TuKhoa?.Length>100 || filter.MaBN?.Length>50 || filter.MaLS?.Length>50)
            throw Error("BoLoc","Bộ lọc không hợp lệ; mỗi trang tối đa 100 đơn.");
        var term=string.IsNullOrWhiteSpace(filter.TuKhoa)?null:"%"+filter.TuKhoa.Trim().Replace("[","[[]").Replace("%","[%]").Replace("_","[_]")+"%";
        var where=Eligible+" AND(@MaBN IS NULL OR bn.MaBN=@MaBN) AND(@MaLS IS NULL OR d.MaLS=@MaLS) AND(@TrangThai=N'Tất cả' OR b.TrangThai=@TrangThai) AND(@term IS NULL OR bn.HoTenBN COLLATE Latin1_General_CI_AI LIKE @term OR bn.MaBN LIKE @term OR d.MaDT LIKE @term OR b.MaHD LIKE @term)";
        var args=new{filter.MaBN,filter.MaLS,filter.TrangThai,term,offset=(long)(filter.Trang-1)*filter.KichThuocTrang,take=filter.KichThuocTrang};
        var total=db.ExecuteScalar<int>("SELECT COUNT(*) "+Joins+" WHERE "+where,args,tx);
        var rows=db.Query<HangBanGiao>(Columns+Joins+" WHERE "+where+" ORDER BY h.NgayLap,d.MaDT OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY",args,tx).ToArray();
        return new HangDoiBanGiao(rows,total,filter.Trang,filter.KichThuocTrang);
    });
    public ChiTietBanGiao LayChiTiet(string maDT,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var actor=RequirePharmacy(db,tx,user);DAL_QUYENNGHIEPVU.KiemTraMa(maDT);
        // Đọc định tuyến ngắn trước khi khóa; không giữ S trên nguồn rồi chờ canonical lock.
        var visit=db.ExecuteScalar<string?>("SELECT MaLS FROM dbo.DON_THUOC WITH(READCOMMITTEDLOCK) WHERE MaDT=@maDT",new{maDT},tx);
        if(visit is null)throw Error("MaDT","Không có đơn đủ điều kiện bàn giao.");
        Lock(db,tx,"RHM:Kham:LanKham:"+visit);
        var row=ReadValid(db,tx,maDT);
        if(row.MaLS!=visit)throw Error("MaDT","Nguồn đơn đã đổi; tải lại trước khi xem/giao.");
        return new ChiTietBanGiao(row,new DAL_DONTHUOC(tx).LayBanInDon(maDT,actor));
    });
    public KetQuaBanGiao XacNhanGiao(string maDT,NguCanhNghiepVu user)
    {
        if(callerTransaction is null || callerTransaction.IsolationLevel!=IsolationLevel.Serializable)throw Error("BanGiao","Cần giao dịch Serializable của đầu mối bàn giao.");
        return ThucHien((db,tx)=>
        {
            var actor=RequirePharmacy(db,tx,user);DAL_QUYENNGHIEPVU.KiemTraMa(maDT);
            Lock(db,tx,"RHM:ThanhToan:Write");
            var visit=db.ExecuteScalar<string?>("SELECT MaLS FROM dbo.DON_THUOC WHERE MaDT=@maDT",new{maDT},tx);
            if(visit is null)throw Error("MaDT","Không có đơn đủ điều kiện bàn giao.");
            Lock(db,tx,"RHM:Kham:LanKham:"+visit);
            var row=ReadValid(db,tx,maDT);
            if(row.TrangThai=="Đã giao")return Result(row,true,AllGiven(db,tx,row.MaLS,row.MaHD));
            // Kiểm tra nội dung bản ký trước giao; không sửa đơn, giá, thu hay nguồn kho.
            new DAL_DONTHUOC(tx).LayBanInDon(maDT,actor);
            var changed=db.Execute("UPDATE dbo.BAN_GIAO_THUOC SET TrangThai=N'Đã giao',MaNVGiao=@MaNV,ThoiGianGiao=CONVERT(datetime2(0),SYSDATETIME()) WHERE MaDT=@maDT AND TrangThai=N'Chờ giao'",new{maDT,actor.MaNV},tx);
            if(changed!=1)throw Error("MaDT","Trạng thái bàn giao đã đổi; tải lại đơn.");
            row=ReadValid(db,tx,maDT);var complete=AllGiven(db,tx,row.MaLS,row.MaHD);
            if(complete)db.Execute("UPDATE p SET TrangThai=N'Hoàn tất' FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=p.MaPhieu WHERE l.MaLS=@MaLS",row,tx);
            var result=Result(row,false,complete);var audit=TaoMa(db,tx,"NHAT_KY_THAO_TAC","MaNK");
            db.Execute("INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,DuLieuSau,KetQua) VALUES(@audit,@MaTK,SYSDATETIME(),N'Bàn giao thuốc',N'DON_THUOC',@maDT,@data,N'Thành công')",new{audit,actor.MaTK,maDT,data=JsonSerializer.Serialize(result)},tx);
            return result;
        });
    }
    private static NguCanhNghiepVu RequirePharmacy(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu user)
    {var actor=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);if(!actor.CoQuyen(QuyenLamViec.GiaoThuoc))throw DAL_QUYENNGHIEPVU.TuChoi();return actor;}
    private static HangBanGiao ReadValid(IDbConnection db,IDbTransaction? tx,string id)
        =>db.QuerySingleOrDefault<HangBanGiao>(Columns+Joins+" WHERE b.MaDT=@id AND "+Eligible,new{id},tx)??throw Error("MaDT","Đơn chưa có bàn giao hợp lệ: cần bản ký, hóa đơn thu đủ và xuất đúng nguồn. Không xuất lại thuốc.");
    private static bool AllGiven(IDbConnection db,IDbTransaction? tx,string maLS,string maHD)=>db.ExecuteScalar<bool>("SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.DON_THUOC WHERE MaLS=@maLS AND TrangThai=N'Đã xác nhận') AND NOT EXISTS(SELECT 1 FROM dbo.DON_THUOC d WHERE d.MaLS=@maLS AND d.TrangThai=N'Đã xác nhận' AND NOT EXISTS(SELECT 1 FROM dbo.BAN_GIAO_THUOC b WHERE b.MaDT=d.MaDT AND b.MaHD=@maHD AND b.TrangThai=N'Đã giao')) THEN 1 ELSE 0 END AS bit)",new{maLS,maHD},tx);
    private static KetQuaBanGiao Result(HangBanGiao r,bool existing,bool complete)=>new(r.MaDT,r.MaLS,r.MaHD,r.MaPX,r.MaNVGiao??throw Error("MaDT","Thiếu người giao."),r.ThoiGianGiao??throw Error("MaDT","Thiếu giờ giao."),existing,complete);
    private static LoiKhamException Error(string field,string message)=>new(field,message);
    private static void Lock(IDbConnection db,IDbTransaction? tx,string key)
    {if(db.ExecuteScalar<int>("DECLARE @r int;EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;SELECT @r",new{key},tx)<0)throw Error("BanGiao","Đơn đang xử lý; thử lại đúng mã đơn.");}
    private const string Columns="SELECT d.MaDT,d.MaLS,bn.MaBN,bn.HoTenBN,bn.NgaySinh,d.NgayKe,b.MaHD,b.MaPX,b.TrangThai,b.MaNVGiao,n.HoTen TenNguoiGiao,b.ThoiGianGiao ";
    private const string Joins="""
        FROM dbo.BAN_GIAO_THUOC b WITH(HOLDLOCK) JOIN dbo.DON_THUOC d WITH(HOLDLOCK) ON d.MaDT=b.MaDT
        JOIN dbo.HOA_DON h WITH(HOLDLOCK) ON h.MaHD=b.MaHD JOIN dbo.PHIEU_XUAT x WITH(HOLDLOCK) ON x.MaPX=b.MaPX
        JOIN dbo.LICH_SU_KHAM_BENH l WITH(HOLDLOCK) ON l.MaLS=d.MaLS
        JOIN dbo.PHIEU_KHAM_BENH p WITH(HOLDLOCK) ON p.MaPhieu=l.MaPhieu
        JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS JOIN dbo.BENH_NHAN bn ON bn.MaBN=hs.MaBN
        LEFT JOIN dbo.NHAN_VIEN n ON n.MaNV=b.MaNVGiao
        """;
    private const string Eligible="""
        h.PhienBanQuyTrinh=2 AND h.TrangThai=N'Đã xác nhận' AND d.TrangThai=N'Đã xác nhận' AND l.TrangThai=N'Đã xác nhận'
        AND d.MaLS=h.MaLS AND x.MaLS=h.MaLS AND x.TrangThai=N'Đã ghi sổ'
        AND(h.SoTienPhaiThu=0 AND NOT EXISTS(SELECT 1 FROM dbo.THANH_TOAN t WITH(HOLDLOCK) WHERE t.MaHD=h.MaHD)
            OR h.SoTienPhaiThu>0 AND(SELECT COUNT(*) FROM dbo.THANH_TOAN t WITH(HOLDLOCK) WHERE t.MaHD=h.MaHD)=1
            AND EXISTS(SELECT 1 FROM dbo.THANH_TOAN t WITH(HOLDLOCK) WHERE t.MaHD=h.MaHD AND t.TrangThai=N'Đã xác nhận' AND t.LoaiGiaoDich=N'Thu' AND t.SoTien=h.SoTienPhaiThu))
        AND EXISTS(SELECT 1 FROM dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) WHERE c.MaDT=d.MaDT)
        AND EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_XUAT c WITH(HOLDLOCK) WHERE c.MaPX=b.MaPX)
        AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_XUAT z WITH(HOLDLOCK) LEFT JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaCTDT=z.MaCTDT
            WHERE z.MaPX=b.MaPX AND(c.MaDT IS NULL OR c.MaDT<>d.MaDT OR NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON i WITH(HOLDLOCK) WHERE i.MaHD=h.MaHD AND i.MaCTPX=z.MaCTPX)))
        AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) WHERE c.MaDT=d.MaDT AND c.SoLuong<>
            COALESCE((SELECT SUM(z.SoLuong) FROM dbo.CHI_TIET_PHIEU_XUAT z WITH(HOLDLOCK) WHERE z.MaPX=b.MaPX AND z.MaCTDT=c.MaCTDT),0))
        """;
}
