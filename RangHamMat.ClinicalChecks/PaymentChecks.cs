using BUS;
using Dapper;
using ET;

namespace RangHamMat.ClinicalChecks;

// Các ca này cần commit thật: chỉ chạy trên DB P09Payments mới riêng, dọn bằng cách bỏ cả DB thử.
internal static class PaymentChecks
{
    internal static void Guard()
    { using var db = MigrationChecks.Open(); if (!db.Database.StartsWith("RHM_ClinicalWorkflow_Test_P09Payments_", StringComparison.Ordinal)) throw new InvalidOperationException("P09 cần database P09Payments riêng."); }
    private static void Need(bool ok, string message) => KhamVaKeHoachChecks.Need(ok, message);
    private static void Throws<T>(Action run) where T : Exception
    { try { run(); } catch (T) { return; } throw new Exception("Phải chặn bằng " + typeof(T).Name); }
    private static string Counts() { using var db = MigrationChecks.Open(); return db.ExecuteScalar<string>("SELECT CONCAT((SELECT COUNT(*) FROM dbo.HOA_DON),':',(SELECT COUNT(*) FROM dbo.CHI_TIET_HOA_DON),':',(SELECT COUNT(*) FROM dbo.PHIEU_XUAT),':',(SELECT COUNT(*) FROM dbo.CHI_TIET_PHIEU_XUAT),':',(SELECT COUNT(*) FROM dbo.THANH_TOAN),':',(SELECT COUNT(*) FROM dbo.BAN_GIAO_THUOC),':',(SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC),':',(SELECT SUM(SoLuongTon) FROM dbo.VW_TON_KHO))")!; }
    private static string FinancialCounts()=>string.Join(":",Counts().Split(':').Where((_,i)=>i!=6));
    internal sealed class Prepared : IDisposable
    {
        public Fixture F { get; }
        public NguCanhNghiepVu Actor { get; }
        public BangTinhPhi Fee { get; }
        public YeuCauThanhToan Request { get; }
        internal Prepared(bool drugs = true, bool second = false, decimal price = 400000, bool unicodeLots = false)
        {
            F = new Fixture();
            try
            {
                var warehouse = ThanhToanChecks.Prepare(F, drugs, second, servicePrice: price);
                if (drugs) warehouse = SeedStock(F, unicodeLots);
                Actor = F.User("C"); Fee = new BUS_THANHTOANTRONGOI(F.Transaction).LapBangTinh(F.Id("LSA"), drugs ? warehouse : null, Actor);
                Request = new() { MaLS = Fee.MaLS, MaKhoCapThuoc = Fee.MaKhoCapThuoc, MaYeuCau = Guid.NewGuid(), DauVanTayBangTinh = Fee.DauVanTay!, SoTienKhachDua = 500000 };
                F.Transaction.Commit();
            }
            catch { F.Dispose(); throw; }
        }
        public KetQuaThanhToan Pay(YeuCauThanhToan? request = null) => new BUS_THANHTOANTRONGOI().ThanhToan(request ?? Request, Actor);
        public void Dispose() => F.Dispose();
    }
    private static string SeedStock(Fixture f, bool unicodeLots)
    {
        f.Execute("""
            DECLARE @now datetime2(0)=SYSDATETIME(),@supplier nvarchar(50)=(SELECT TOP(1) MaNCC FROM dbo.NHA_CUNG_CAP ORDER BY MaNCC);
            INSERT dbo.KHO(MaKho,TenKho,MaNV,LoaiKho,TrangThai) VALUES(@p+'WP9',N'FIXTURE P09',@nv,N'Kho thuốc',N'Đang hoạt động');
            INSERT dbo.HOA_DON_NHAP(MaHDN,SoHoaDonNCC,MaNCC,MaNV,NgayHoaDon,TongTien,TrangThai) VALUES(@p+'HDNP9',@p,@supplier,@nv,DATEADD(day,-4,@now),0,N'Nháp');
            INSERT dbo.PHIEU_NHAP(MaPN,MaHDN,MaKho,MaNV,NgayNhap,TrangThai,LyDo) VALUES(@p+'PNP9',@p+'HDNP9',@p+'WP9',@nv,DATEADD(day,-3,@now),N'Nháp',N'FIXTURE');
            INSERT dbo.LO_HANG_HOA(MaLo,MaHH,MaNCC,MaKho,SoLo,NgayNhap,HanSuDung,TrangThai)
            SELECT @p+'L9'+CONVERT(nvarchar(3),DENSE_RANK() OVER(ORDER BY MaHH))+CASE WHEN @lotCount=1 THEN N'' WHEN n=1 THEN N'Á' WHEN n=2 THEN N'Z' ELSE N'B' END,MaHH,@supplier,@p+'WP9',N'FIXTURE P09',DATEADD(day,-3,@now),DATEADD(day,100,@now),N'Còn sử dụng'
            FROM(SELECT DISTINCT t.MaHH FROM dbo.DON_THUOC d JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaDT=d.MaDT JOIN dbo.THUOC t ON t.MaThuoc=c.MaThuoc WHERE d.MaLS=@ls AND d.TrangThai=N'Đã xác nhận') drugs
            CROSS JOIN(VALUES(1),(2),(3)) parts(n) WHERE n<=@lotCount;
            INSERT dbo.CHI_TIET_PHIEU_NHAP(MaCTPN,MaPN,MaHH,MaLo,SoLuong,DonGia)
            SELECT @p+'CP9'+CONVERT(nvarchar(3),ROW_NUMBER() OVER(ORDER BY MaHH,MaLo)),@p+'PNP9',MaHH,MaLo,@each,1 FROM dbo.LO_HANG_HOA WHERE MaKho=@p+'WP9';
            UPDATE dbo.PHIEU_NHAP SET TrangThai=N'Đã ghi sổ' WHERE MaPN=@p+'PNP9';
            """, new { p=f.Prefix, nv=f.Id("P"),ls=f.Id("LSA"),lotCount=unicodeLots?3:1,each=unicodeLots?4:100 });
        return f.Id("WP9");
    }
    private static T[] Pair<T>(Func<int, T> send)
    { using var start = new Barrier(2); return Task.WhenAll(Enumerable.Range(0, 2).Select(i => Task.Run(() => { if (!start.SignalAndWait(TimeSpan.FromSeconds(10))) throw new Exception("Worker chưa sẵn sàng"); return send(i); }))).GetAwaiter().GetResult(); }
    private static void Invariant(KetQuaThanhToan r)
    {
        using var db = MigrationChecks.Open();
        Need(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.HOA_DON WHERE MaLS=@MaLS AND PhienBanQuyTrinh=2 AND TrangThai=N'Đã xác nhận'", r) == 1, "Không đúng một hóa đơn xác nhận");
        Need(db.ExecuteScalar<decimal>("SELECT COALESCE(SUM(SoTien),0) FROM dbo.THANH_TOAN WHERE MaHD=@MaHD AND LoaiGiaoDich=N'Thu' AND TrangThai=N'Đã xác nhận'", r) == r.SoTienThu, "Thu thiếu hoặc thu lặp");
        Need(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.THANH_TOAN WHERE MaHD=@MaHD", r) == (r.SoTienThu == 0 ? 0 : 1), "Thừa giao dịch");
        Need(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BAN_GIAO_THUOC WHERE MaHD=@MaHD AND TrangThai=N'Chờ giao'", r) == r.CacDon.Count, "Thiếu bàn giao Chờ giao");
    }
    internal static int Run()
    {
        Guard(); var pass = 0; var fail = 0;
        foreach (var (name, run) in Cases()) { try { run(); pass++; Console.WriteLine("PASS: " + name); } catch (Exception ex) { fail++; Console.Error.WriteLine("FAIL: " + name + ": " + ex.Message + (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql ? " SQL="+sql.Number : "")); } }
        Console.WriteLine($"PaymentChecks: {pass} PASS, {fail} FAIL; fixture chỉ ở database P09Payments riêng."); return fail == 0 ? 0 : 1;
    }
    private static IEnumerable<(string, Action)> Cases()
    {
        yield return ("P09 alias mã yêu cầu đã trả hóa đơn vẫn khóa đúng đầu vào",()=>
        {using var p=new Prepared(false);var first=p.Pay();var alias=p.Request with {MaYeuCau=Guid.NewGuid()};var existing=p.Pay(alias);Need(existing.MaHD==first.MaHD&&existing.DaTonTai,"Không trả chứng từ đã thu");Throws<LoiKhamException>(()=>p.Pay(alias with {SoTienKhachDua=600000}));var before=Counts();Need(p.Pay(alias).MaHD==first.MaHD&&Counts()==before,"Alias thử lại thay đổi chứng từ/audit");});
        yield return ("P09 alias mã yêu cầu không tái dùng cho lượt khác sau trả chứng từ cũ",()=>
        {using var first=new Prepared(false);var original=first.Pay();var alias=first.Request with {MaYeuCau=Guid.NewGuid()};first.Pay(alias);using var other=new Prepared(false);var before=Counts();Throws<LoiKhamException>(()=>other.Pay(other.Request with {MaYeuCau=alias.MaYeuCau}));Need(Counts()==before,"Alias đã được dùng thanh toán lượt khác");first.F.ActAs("C");Need(first.Pay(alias).MaHD==original.MaHD,"Không thử lại được alias của lượt đầu");});
        yield return ("P09 nhiều lô có mã Unicode hợp lệ vẫn đối chiếu đúng nguồn",()=>
        {using var p=new Prepared(unicodeLots:true);Need(p.Fee.Thuoc.All(d=>d.CacLo.Count==3),"Chưa thử phân ba lô Unicode");var r=p.Pay();Need(r.SoTienThu==450000,"Sai tiền nhiều lô");Invariant(r);});
        yield return ("P09 phải thu450000 đưa500000 thu450000 thối50000, xuất đủ và chờ giao", () =>
        { using var p = new Prepared(); var r = p.Pay(); Need(r.SoTienThu == 450000 && r.SoTienKhachDua == 500000 && r.TienThua == 50000 && !r.DaTonTai && r.CacDon.Count == 1, "Sai tiền thu/thối/nguồn"); Invariant(r); using var db = MigrationChecks.Open(); Need(db.ExecuteScalar<decimal>("SELECT TienThua FROM dbo.THANH_TOAN WHERE MaTT=@MaTT", r) == 50000, "Không lưu tiền thối ngay INSERT"); });
        yield return ("P09 thiếu tiền không ghi bất kỳ bảng tài chính/tồn/nhật ký", () =>
        { using var p = new Prepared(); var before = Counts(); Throws<LoiKhamException>(() => p.Pay(p.Request with { SoTienKhachDua = 400000 })); Need(Counts() == before, "Thu thiếu để lại chứng từ"); });
        yield return ("P09 chuyển khoản đúng tiền, có mã và xác nhận đối chiếu", () =>
        { using var p = new Prepared(); var r = p.Request with { PhuongThuc = "Chuyển khoản", SoTienKhachDua = 450000 }; var before = Counts(); Throws<LoiKhamException>(() => p.Pay(r)); Throws<LoiKhamException>(() => p.Pay(r with { MaThamChieu = "FIXTURE-REF" })); Throws<LoiKhamException>(() => p.Pay(r with { MaThamChieu = "FIXTURE-REF", DaDoiChieuChuyenKhoan = true, SoTienKhachDua = 500000 })); Need(Counts() == before, "Chuyển khoản không đủ điều kiện đã ghi"); var result = p.Pay(r with { MaThamChieu = " FIXTURE-REF ", DaDoiChieuChuyenKhoan = true }); Invariant(result); using var db = MigrationChecks.Open(); Need(result.TienThua == 0 && db.ExecuteScalar<string>("SELECT MaThamChieu FROM dbo.THANH_TOAN WHERE MaTT=@MaTT", result) == "FIXTURE-REF", "Mất đối chiếu hoặc ghi thối chuyển khoản"); });
        yield return ("P09 cùng mã yêu cầu thử lại sau commit trả đúng kết quả đã lưu", () =>
        { using var p = new Prepared(); var first = p.Pay(); var before = Counts(); p.F.Connection.Close(); p.F.Connection.Open(); p.F.Connection.Execute("UPDATE dbo.THUOC SET DonGiaBan=DonGiaBan+10 WHERE MaThuoc=@id", new { id = p.Fee.Thuoc[0].MaThuoc }); var again = p.Pay(); Need(again.DaTonTai && again.MaHD == first.MaHD && again.MaTT == first.MaTT && again.TienThua == first.TienThua && Counts() == before, "Thử lại đã tính giá/thu/xuất mới"); Invariant(again); });
        yield return ("P09 cùng mã khác đầu vào bị từ chối, khác mã cùng lượt không thu lại", () =>
        { using var p = new Prepared(); var first = p.Pay(); var before = FinancialCounts(); Throws<LoiKhamException>(() => p.Pay(p.Request with { SoTienKhachDua = 600000 })); var other = p.Pay(p.Request with { MaYeuCau = Guid.NewGuid() }); Need(other.DaTonTai && other.MaHD == first.MaHD && FinancialCounts() == before, "Khác mã đã ghi bộ tài chính thứ hai"); });
        yield return ("P09 hai kết nối cùng mã và cùng lượt chỉ một bộ thu/xuất", () =>
        { using var p = new Prepared(); var result = Pair(_ => p.Pay()); Need(result.Select(r => r.MaHD).Distinct().Count() == 1 && result.Count(r => !r.DaTonTai) == 1, "Hai bộ cùng mã"); Invariant(result[0]); });
        yield return ("P09 hai kết nối khác mã cùng lượt chỉ một bộ thu/xuất", () =>
        { using var p = new Prepared(); var result = Pair(_ => p.Pay(p.Request with { MaYeuCau = Guid.NewGuid() })); Need(result.Select(r => r.MaHD).Distinct().Count() == 1 && result.Count(r => !r.DaTonTai) == 1, "Hai bộ khác mã"); Invariant(result[0]); });
        yield return ("P09 hai lượt khác kho riêng cùng thu không deadlock khi nhả khóa tồn",()=>
        {
            using var first=new Prepared();var principal1=Thread.CurrentPrincipal;using var second=new Prepared();var principal2=Thread.CurrentPrincipal;
            using var blocker=MigrationChecks.Open();using var tx=blocker.BeginTransaction(System.Data.IsolationLevel.Serializable);
            blocker.Query<string>("SELECT MaHH FROM dbo.HANG_HOA WITH(UPDLOCK,HOLDLOCK) WHERE MaHH IN(SELECT t.MaHH FROM dbo.THUOC t JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaThuoc=t.MaThuoc JOIN dbo.DON_THUOC d ON d.MaDT=c.MaDT WHERE d.MaLS IN @ids) ORDER BY MaHH",new{ids=new[]{first.Fee.MaLS,second.Fee.MaLS}},tx).ToArray();
            var tasks=new[]{Task.Run(()=>{Thread.CurrentPrincipal=principal1;return first.Pay();}),Task.Run(()=>{Thread.CurrentPrincipal=principal2;return second.Pay();})};
            var timer=System.Diagnostics.Stopwatch.StartNew();var waiting=0;
            while(timer.Elapsed<TimeSpan.FromSeconds(10)){waiting=first.F.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.dm_exec_requests WHERE database_id=DB_ID() AND session_id<>@@SPID AND blocking_session_id>0");if(waiting>=2)break;Thread.Sleep(20);}
            tx.Rollback();Need(waiting>=2,"Hai yêu cầu chưa tới điểm tranh chấp");var results=Task.WhenAll(tasks).GetAwaiter().GetResult();Need(results.Select(r=>r.MaHD).Distinct().Count()==2,"Gộp nhầm hai lượt");foreach(var r in results)Invariant(r);
        });
        yield return ("P09 giá đổi hoặc tồn đổi sau xem bảng tính phải xem lại, không ghi", () =>
        { using var p = new Prepared(); p.F.Connection.Execute("UPDATE dbo.THUOC SET DonGiaBan=DonGiaBan+1 WHERE MaThuoc=@id", new { id = p.Fee.Thuoc[0].MaThuoc }); var before = Counts(); Throws<XungDotDuLieuKhamException>(() => p.Pay()); Need(Counts() == before, "Giá đổi đã ghi"); });
        yield return ("P09 xuất lượt khác thay đổi tồn/token phải xem lại", () =>
        { using var p = new Prepared(); new BUS_HOADON().DispensePrescription(p.F.Id("DTH"), p.Request.MaKhoCapThuoc!, p.F.Id("P")); var before = Counts(); Throws<XungDotDuLieuKhamException>(() => p.Pay()); Need(Counts() == before, "Tồn đổi đã ghi bộ mới"); });
        yield return ("P09 thiếu tồn thuốc cuối đơn không xuất thuốc đầu hoặc ghi dở", () =>
        {
            using var p = new Prepared(); var drug=p.Fee.Thuoc.Last();var goods=p.F.Connection.ExecuteScalar<string>("SELECT MaHH FROM dbo.THUOC WHERE MaThuoc=@MaThuoc",drug);
            var available=new BUS_HOADON().GetStock(p.Request.MaKhoCapThuoc!,drug.MaDT).Rows.Cast<System.Data.DataRow>().Where(r=>Convert.ToString(r["MaHH"])==goods).Sum(r=>Convert.ToDecimal(r["TonKho"]));
            using(var tx=p.F.Connection.BeginTransaction())
            {p.F.Connection.Execute("INSERT dbo.DON_THUOC(MaDT,MaLS,MaBS,NgayKe,TrangThai) VALUES(@id,@ls,@doctor,DATEADD(minute,-5,SYSDATETIME()),N'Nháp');INSERT dbo.CHI_TIET_DON_THUOC(MaCTDT,MaDT,MaThuoc,LieuDung,SoLuong,SoNgayDung,HuongDan) VALUES(@line,@id,@drug,N'FIXTURE',@available,1,N'FIXTURE');UPDATE dbo.DON_THUOC SET TrangThai=N'Đã xác nhận' WHERE MaDT=@id",new{id=p.F.Id("DRAIN"),line=p.F.Id("DRAINLINE"),ls=p.F.Id("LSH"),doctor=p.F.Id("B"),drug=drug.MaThuoc,available},tx);tx.Commit();}
            new BUS_HOADON().DispensePrescription(p.F.Id("DRAIN"),p.Request.MaKhoCapThuoc!,p.F.Id("P"));var before=Counts();Throws<LoiKhamException>(()=>p.Pay());Need(Counts()==before,"Thiếu thuốc cuối nhưng đã trừ thuốc đầu");
        });
        yield return ("P09 lỗi INSERT thanh toán rollback cả xuất/HD/bàn giao và tồn", () =>
        { using var p = new Prepared(); var before = Counts(); p.F.Connection.Execute("CREATE TRIGGER dbo.CW_Test_PaymentFailure ON dbo.THANH_TOAN AFTER INSERT AS BEGIN THROW 52909,N'FIXTURE payment failure',1; END;"); try { Throws<LoiKhamException>(() => p.Pay()); Need(Counts() == before, "Lỗi thu để lại xuất/hóa đơn/tồn"); } finally { p.F.Connection.Execute("DROP TRIGGER dbo.CW_Test_PaymentFailure"); } Invariant(p.Pay()); });
        yield return ("P09 lỗi audit cuối rollback cả Thu/bàn giao/trạng thái phiếu",()=>
        {using var p=new Prepared(false);var before=Counts();p.F.Connection.Execute("CREATE TRIGGER dbo.CW_Test_AuditFailure ON dbo.NHAT_KY_THAO_TAC AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE HanhDong=N'Thanh toán trọn gói') THROW 52910,N'FIXTURE final audit failure',1;END;");try{Throws<LoiKhamException>(()=>p.Pay());Need(Counts()==before&&p.F.Connection.ExecuteScalar<string>("SELECT TrangThai FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@id",new{id=p.F.Id("PHA")})=="Chờ thanh toán","Lỗi cuối đã để lại thu hoặc phiếu hoàn tất");}finally{p.F.Connection.Execute("DROP TRIGGER dbo.CW_Test_AuditFailure");}});
        yield return ("P09 không thuốc hoàn tất, tổng0 có nguồn không tạo Thu0", () =>
        { using var p = new Prepared(false, price: 0); var r = p.Pay(p.Request with { SoTienKhachDua = 0 }); Invariant(r); Need(r.SoTienThu == 0 && r.MaTT is null && r.CacDon.Count == 0 && p.F.Connection.ExecuteScalar<string>("SELECT TrangThai FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@id", new { id = p.F.Id("PHA") }) == "Hoàn tất", "Sai hóa đơn0/không thuốc"); });
        yield return ("P09 mọi đơn hiệu lực đều xuất, nguồn snapshot không đổi theo tên danh mục", () =>
        { using var p = new Prepared(second: true); var oldName = p.Fee.Thuoc[0].NoiDung; p.F.Connection.Execute("UPDATE dbo.HANG_HOA SET TenHH=@newName WHERE MaHH=(SELECT MaHH FROM dbo.THUOC WHERE MaThuoc=@id)", new { id = p.Fee.Thuoc[0].MaThuoc, newName = "FIXTURE mới " + Guid.NewGuid().ToString("N") }); var r = p.Pay(); Invariant(r); Need(r.CacDon.Count == 2 && r.SoTienThu == 456000 && p.F.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.CHI_TIET_HOA_DON WHERE MaHD=@MaHD AND NoiDung=@oldName", new { r.MaHD, oldName }) > 0, "Thiếu đơn hoặc đổi tên thuốc chốt"); });
        yield return ("P09 quyền thu đọc lại SQL, Admin/bác sĩ/dược sĩ không được ký thu", () =>
        { using var p = new Prepared(); foreach (var role in new[] { "A", "N", "P", "X" }) { p.F.ActAs(role); var u = new BUS_QUYENNGHIEPVU().LayNguCanh(p.F.Id(role)) with { Quyen = QuyenLamViec.ThuTien }; Throws<TuChoiQuyenNghiepVuException>(() => new BUS_THANHTOANTRONGOI().ThanhToan(p.Request, u)); } p.F.ActAs("C"); p.F.Connection.Execute("UPDATE dbo.TAI_KHOAN SET TrangThai=N'Bị khóa' WHERE MaTK=@MaTK", p.Actor); Throws<TuChoiQuyenNghiepVuException>(() => p.Pay()); });
        yield return ("P09 hóa đơn v2 chặn mọi lối ghi cũ, lịch sử vẫn đọc được", () =>
        { using var p = new Prepared(); var r = p.Pay(); var before = Counts(); var b = new BUS_HOADON(); Throws<LoiKhamException>(() => b.RecordPayment(r.MaHD, p.Actor.MaNV, 1, "Hoàn", "Tiền mặt", "FIXTURE")); Throws<LoiKhamException>(() => b.ConfirmInvoice(r.MaHD)); Throws<LoiKhamException>(() => b.CancelInvoice(r.MaHD)); Throws<LoiKhamException>(() => b.CreateInvoice(r.MaLS, p.Actor.MaNV)); Throws<LoiKhamException>(() => b.DispensePrescription(r.CacDon[0].MaDT, p.Request.MaKhoCapThuoc!, p.Actor.MaNV)); Need(b.GetPayments(r.MaHD).Rows.Count == 1 && Counts() == before, "Lối cũ sửa chứng từ v2 hoặc mất lịch sử"); });
        yield return ("P09 mã yêu cầu không tái dùng cho lượt khác",()=>
        {using var first=new Prepared(false);first.Pay();using var other=new Prepared(false);var before=Counts();Throws<LoiKhamException>(()=>other.Pay(other.Request with {MaYeuCau=first.Request.MaYeuCau}));Need(Counts()==before,"Tái dùng mã yêu cầu cho lượt khác");});
        yield return ("P09 mã/token/tiền vượt giới hạn hoặc số lẻ không ghi",()=>
        {using var p=new Prepared(false);var before=Counts();foreach(var r in new[]{p.Request with {MaYeuCau=Guid.Empty},p.Request with {DauVanTayBangTinh="abc"},p.Request with {SoTienKhachDua=.5m},p.Request with {SoTienKhachDua=10000000000000000m},p.Request with {MaThamChieu=new string('x',101)}})Throws<LoiKhamException>(()=>p.Pay(r));Need(Counts()==before,"Đầu vào sai đã ghi");});
        yield return ("P09 từ chối transaction ngoài, không trả thành công trước commit",()=>
        {using var p=new Prepared(false);var before=Counts();using(var tx=p.F.Connection.BeginTransaction(System.Data.IsolationLevel.Serializable)){Throws<LoiKhamException>(()=>new BUS_THANHTOANTRONGOI(tx).ThanhToan(p.Request,p.Actor));tx.Rollback();}using(var ambient=new System.Transactions.TransactionScope()){Throws<LoiKhamException>(()=>p.Pay());}Need(Counts()==before,"Đầu mối tham gia transaction bên ngoài");});
        yield return ("P09 lỗi mở kết nối trả thông báo nghiệp vụ, giữ yêu cầu thử lại",()=>
        {using var p=new Prepared(false);var original=Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING");try{var b=new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(original){InitialCatalog="RHM_ClinicalWorkflow_Test_P09Payments_Missing_"+Guid.NewGuid().ToString("N"),ConnectTimeout=1,ConnectRetryCount=0};Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING",b.ConnectionString);Throws<LoiKhamException>(()=>p.Pay());}finally{Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING",original);}Invariant(p.Pay());});
        yield return ("P09 nguồn có lẻ đồng và hóa đơn cũ bắt đối chiếu, không thu thêm",()=>
        {using(var p=new Prepared(false,price:1.5m)){var before=Counts();Throws<LoiKhamException>(()=>p.Pay());Need(Counts()==before,"Thu nguồn lẻ đồng");}using(var p=new Prepared(false)){new BUS_HOADON().CreateInvoice(p.Fee.MaLS,p.Actor.MaNV);var before=Counts();Throws<LoiKhamException>(()=>p.Pay());Need(Counts()==before,"Thu lại lượt có hóa đơn lịch sử");}});
    }
}
