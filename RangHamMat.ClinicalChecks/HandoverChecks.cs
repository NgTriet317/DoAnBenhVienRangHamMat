using BUS;
using Dapper;
using ET;

namespace RangHamMat.ClinicalChecks;

internal static class HandoverChecks
{
    private static void Need(bool value,string message)=>KhamVaKeHoachChecks.Need(value,message);
    private static void Throws<T>(Action run) where T:Exception {try{run();}catch(T){return;}throw new Exception("Phải chặn bằng "+typeof(T).Name);}
    internal static NguCanhNghiepVu User(PaymentChecks.Prepared p,string role){p.F.ActAs(role);return new BUS_QUYENNGHIEPVU().LayNguCanh(p.F.Id(role));}
    private static string Financial(){using var db=MigrationChecks.Open();return db.ExecuteScalar<string>("SELECT CONCAT((SELECT COUNT(*) FROM dbo.HOA_DON),':',(SELECT COUNT(*) FROM dbo.CHI_TIET_HOA_DON),':',(SELECT COUNT(*) FROM dbo.PHIEU_XUAT),':',(SELECT COUNT(*) FROM dbo.CHI_TIET_PHIEU_XUAT),':',(SELECT COUNT(*) FROM dbo.THANH_TOAN),':',(SELECT SUM(SoLuongTon) FROM dbo.VW_TON_KHO))")!;}
    internal static int Run()
    {
        using(var db=MigrationChecks.Open())if(!db.Database.StartsWith("RHM_ClinicalWorkflow_Test_P10Handover_",StringComparison.Ordinal))throw new InvalidOperationException("P10 cần DB Handover mới riêng.");
        var pass=0;var fail=0;foreach(var(name,run)in Cases()){try{run();pass++;Console.WriteLine("PASS: "+name);}catch(Exception ex){fail++;Console.Error.WriteLine("FAIL: "+name+": "+ex.Message);}}Console.WriteLine($"HandoverChecks: {pass} PASS, {fail} FAIL; commit chỉ trên DB P10Handover riêng.");return fail==0?0:1;
    }
    private static IEnumerable<(string,Action)> Cases()
    {
        yield return("P10 chưa thu không có hàng chờ, không giao được",()=>
        {using var p=new PaymentChecks.Prepared();var u=User(p,"P");var b=new BUS_BANGIAOTHUOC();var id=p.Fee.Thuoc[0].MaDT;var before=Financial();Need(b.LayHangDoi(new(MaBN:p.Fee.MaBN),u).TongSo==0,"Đơn chưa thu xuất hiện hàng chờ");Throws<LoiKhamException>(()=>b.XacNhanGiao(id,u));Need(Financial()==before,"Giao chưa thu đổi tài chính/tồn");});
        yield return("P10 giao đúng người phiên, lặp trả đúng người/giờ cũ và không xuất lại",()=>
        {using var p=new PaymentChecks.Prepared();var paid=p.Pay();var u=User(p,"P");var b=new BUS_BANGIAOTHUOC();var before=Financial();var r=b.XacNhanGiao(paid.CacDon[0].MaDT,u);Need(!r.DaTonTai&&r.MaNVGiao==u.MaNV&&r.DaGiaoHet,"Sai người giao/thời điểm");var again=b.XacNhanGiao(r.MaDT,u);Need(again.DaTonTai&&again.ThoiGianGiao==r.ThoiGianGiao&&again.MaNVGiao==r.MaNVGiao,"Giao lại sửa người/giờ");Need(Financial()==before&&p.F.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaDoiTuong=@MaDT AND HanhDong=N'Bàn giao thuốc'",r)==1,"Giao thêm xuất/thu/audit");});
        yield return("P10 nhiều đơn chỉ hoàn tất phiếu khi giao hết",()=>
        {using var p=new PaymentChecks.Prepared(second:true);var paid=p.Pay();var u=User(p,"P");var b=new BUS_BANGIAOTHUOC();Need(!b.XacNhanGiao(paid.CacDon[0].MaDT,u).DaGiaoHet&&p.F.Connection.ExecuteScalar<string>("SELECT TrangThai FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@id",new{id=p.F.Id("PHA")})=="Chờ nhận thuốc","Hoàn tất quá sớm");Need(b.XacNhanGiao(paid.CacDon[1].MaDT,u).DaGiaoHet&&p.F.Connection.ExecuteScalar<string>("SELECT TrangThai FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@id",new{id=p.F.Id("PHA")})=="Hoàn tất","Không hoàn tất sau đủ đơn");});
        yield return("P10 hai kết nối giao cùng đơn chỉ một chuyển trạng thái",()=>
        {using var p=new PaymentChecks.Prepared();var paid=p.Pay();var u=User(p,"P");using var start=new Barrier(2);var tasks=Enumerable.Range(0,2).Select(_=>Task.Run(()=>{start.SignalAndWait(TimeSpan.FromSeconds(10));return new BUS_BANGIAOTHUOC().XacNhanGiao(paid.CacDon[0].MaDT,u);})).ToArray();var rows=Task.WhenAll(tasks).GetAwaiter().GetResult();Need(rows.Count(r=>!r.DaTonTai)==1&&rows[0].ThoiGianGiao==rows[1].ThoiGianGiao,"Giao hai lần");});
        yield return("P10 đọc chi tiết đồng thời xác nhận giao không đảo thứ tự khóa",()=>
        {
            using var p=new PaymentChecks.Prepared();var paid=p.Pay();var u=User(p,"P");var id=paid.CacDon[0].MaDT;
            using var db=MigrationChecks.Open();using var tx=db.BeginTransaction(System.Data.IsolationLevel.Serializable);var session=db.ExecuteScalar<int>("SELECT @@SPID",transaction:tx);
            db.Execute("DECLARE @r int;EXEC @r=sys.sp_getapplock @Resource=N'RHM:ThanhToan:Write',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=1000;IF @r<0 THROW 52921,N'Không giữ khóa writer',1;EXEC @r=sys.sp_getapplock @Resource=@visit,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=1000;IF @r<0 THROW 52921,N'Không giữ khóa lượt',1;SET LOCK_TIMEOUT 1000;",new{visit="RHM:Kham:LanKham:"+p.Fee.MaLS},tx);
            var reader=Task.Run(()=>new BUS_BANGIAOTHUOC().LayChiTiet(id,u));var timer=System.Diagnostics.Stopwatch.StartNew();var waiting=false;
            while(timer.Elapsed<TimeSpan.FromSeconds(10)){waiting=p.F.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.dm_exec_requests WHERE database_id=DB_ID() AND blocking_session_id=@session",new{session})>0;if(waiting)break;Thread.Sleep(20);}
            var written=false;try{new DAL.DAL_BANGIAOTHUOC(tx).XacNhanGiao(id,u);tx.Commit();written=true;}
            catch(Microsoft.Data.SqlClient.SqlException ex)when(ex.Number is 1222 or 1205){try{if(tx.Connection is not null)tx.Rollback();}catch{}}
            ChiTietBanGiao? detail=null;try{detail=reader.GetAwaiter().GetResult();}catch(InvalidOperationException){}
            Need(waiting&&written&&detail?.Luot.TrangThai=="Đã giao","Reader giữ khóa hàng rồi chờ application lock, chặn writer giao thuốc");
        });
        yield return("P10 quyền giao đọc lại SQL, cờ giả và tài khoản khóa bị chặn",()=>
        {using var p=new PaymentChecks.Prepared();var paid=p.Pay();foreach(var role in new[]{"A","C","N","X"}){var u=User(p,role) with {Quyen=QuyenLamViec.GiaoThuoc};Throws<TuChoiQuyenNghiepVuException>(()=>new BUS_BANGIAOTHUOC().XacNhanGiao(paid.CacDon[0].MaDT,u));Throws<TuChoiQuyenNghiepVuException>(()=>new BUS_BANGIAOTHUOC().LayHangDoi(new(),u));}var pharma=User(p,"P");p.F.Connection.Execute("UPDATE dbo.TAI_KHOAN SET TrangThai=N'Bị khóa' WHERE MaTK=@MaTK",pharma);Throws<TuChoiQuyenNghiepVuException>(()=>new BUS_BANGIAOTHUOC().XacNhanGiao(paid.CacDon[0].MaDT,pharma));});
        yield return("P10 hàng chờ phân trang/lọc đúng mã, chỉ trạng thái hợp lệ",()=>
        {using var p=new PaymentChecks.Prepared(second:true);p.Pay();var u=User(p,"P");var b=new BUS_BANGIAOTHUOC();var a=b.LayHangDoi(new(MaBN:p.Fee.MaBN,KichThuocTrang:1),u);var c=b.LayHangDoi(new(MaBN:p.Fee.MaBN,Trang:2,KichThuocTrang:1),u);Need(a.TongSo==2&&a.CacDon.Count==1&&c.CacDon.Count==1&&a.CacDon[0].MaDT!=c.CacDon[0].MaDT,"Sai phân trang");b.XacNhanGiao(a.CacDon[0].MaDT,u);Need(b.LayHangDoi(new(MaBN:p.Fee.MaBN),u).TongSo==1&&b.LayHangDoi(new(MaBN:p.Fee.MaBN,TrangThai:"Đã giao"),u).TongSo==1,"Sai lọc trạng thái");Throws<LoiKhamException>(()=>b.LayHangDoi(new(KichThuocTrang:101),u));});
        yield return("P10 xem hướng dẫn và tên chốt, không lấy lại danh mục mới",()=>
        {using var p=new PaymentChecks.Prepared();var paid=p.Pay();var original=p.Fee.Thuoc[0].NoiDung;p.F.Connection.Execute("UPDATE dbo.HANG_HOA SET TenHH=N'FIXTURE đổi tên' WHERE MaHH=(SELECT MaHH FROM dbo.THUOC WHERE MaThuoc=@id);UPDATE dbo.THUOC SET HamLuong=N'FIXTURE đổi hàm lượng',DonGiaBan=999 WHERE MaThuoc=@id",new{id=p.Fee.Thuoc[0].MaThuoc});var u=User(p,"P");var s=new BUS_BANGIAOTHUOC().LayChiTiet(paid.CacDon[0].MaDT,u);Need(s.Don.Nguon.CacDong.Any(d=>d.TenThuocChot+" "+d.HamLuongChot==original)&&s.Don.Nguon.CacDong.All(d=>!string.IsNullOrWhiteSpace(d.HuongDan)&&!string.IsNullOrWhiteSpace(d.DuongDung)),"Đổi chỉ định chốt hoặc thiếu hướng dẫn");});
        yield return("P10 lỗi audit cuối rollback giao và trạng thái phiếu",()=>
        {using var p=new PaymentChecks.Prepared();var paid=p.Pay();var u=User(p,"P");var before=Financial();p.F.Connection.Execute("CREATE TRIGGER dbo.CW_Test_HandoverFailure ON dbo.NHAT_KY_THAO_TAC AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE HanhDong=N'Bàn giao thuốc') THROW 52920,N'FIXTURE handover failure',1;END;");try{Throws<LoiKhamException>(()=>new BUS_BANGIAOTHUOC().XacNhanGiao(paid.CacDon[0].MaDT,u));Need(p.F.Connection.ExecuteScalar<string>("SELECT TrangThai FROM dbo.BAN_GIAO_THUOC WHERE MaDT=@id",new{id=paid.CacDon[0].MaDT})=="Chờ giao"&&p.F.Connection.ExecuteScalar<string>("SELECT TrangThai FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@id",new{id=p.F.Id("PHA")})=="Chờ nhận thuốc"&&Financial()==before,"Lỗi cuối để lại giao dở");}finally{p.F.Connection.Execute("DROP TRIGGER dbo.CW_Test_HandoverFailure");}});
        yield return("P10 thử lại thanh toán phản ánh đơn đã giao, không đổi tài chính",()=>
        {using var p=new PaymentChecks.Prepared();var paid=p.Pay();var u=User(p,"P");new BUS_BANGIAOTHUOC().XacNhanGiao(paid.CacDon[0].MaDT,u);p.F.ActAs("C");var before=Financial();var again=p.Pay();Need(again.CacDon.All(d=>Convert.ToString(d.GetType().GetProperty("TrangThaiGiao")?.GetValue(d))=="Đã giao")&&Financial()==before,"Vẫn báo chờ giao sau đã giao hoặc thu lại");});
        yield return("P10 bản in hóa đơn đọc mã đã commit, giữ ngày/giá/tiền chốt",()=>
        {using var p=new PaymentChecks.Prepared();var paid=p.Pay();var b=new BUS_THANHTOANTRONGOI();var first=b.LayBanInHoaDon(paid.MaHD,p.Actor);p.F.Connection.Execute("UPDATE dbo.THUOC SET DonGiaBan=DonGiaBan+1234");var again=b.LayBanInHoaDon(paid.MaHD,p.Actor);Need(first.HoaDon.MaHD==paid.MaHD&&first.HoaDon.NgayLap==again.HoaDon.NgayLap&&again.CacDong.Where(d=>d.MaCTPX is not null).Sum(d=>d.ThanhTien)==50000&&again.HoaDon.SoTienPhaiThu==450000&&again.SoTienKhachDua==500000&&again.TienThua==50000,"In lại định giá/thời gian mới");});
        yield return("P10 in lịch sử giữ phần lẻ và tiền còn phải thu",()=>
        {using var p=new PaymentChecks.Prepared(false,price:100000.25m);var f=new BUS_HOADON();var id=f.CreateInvoice(p.Fee.MaLS,p.Actor.MaNV);f.ConfirmInvoice(id);f.RecordPayment(id,p.Actor.MaNV,40000.10m,"Thu","Tiền mặt");var s=new BUS_THANHTOANTRONGOI().LayBanInHoaDon(id,p.Actor);Need(s.DaThu==40000.10m&&s.ConPhaiThu==60000.15m&&s.HoaDon.SoTienPhaiThu==100000.25m,"In lịch sử mất phần lẻ/sai đã thu");});
        yield return("P10 đọc bản in kiểm tra quyền và từ chối transaction chưa commit",()=>
        {using var p=new PaymentChecks.Prepared();var paid=p.Pay();var n=User(p,"N");Throws<TuChoiQuyenNghiepVuException>(()=>new BUS_THANHTOANTRONGOI().LayBanInHoaDon(paid.MaHD,n));p.F.ActAs("C");using var tx=p.F.Connection.BeginTransaction();Throws<LoiKhamException>(()=>new BUS_THANHTOANTRONGOI(tx).LayBanInHoaDon(paid.MaHD,p.Actor));tx.Rollback();});
    }
}
