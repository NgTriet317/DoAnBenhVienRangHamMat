using System.Reflection;
using BUS;
using ET;
namespace RangHamMat.ClinicalChecks;
internal static class ExperienceChecks
{
    private static void Need(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static object Resolve(Fixture f,string user="A",string? bn=null,string? hs=null,string? receipt=null,string? visit=null,string? rx=null,string? invoice=null)
    {
        var method=typeof(BUS_KHAMDIEUTRI).GetMethod("XacMinhNguCanh")??throw new InvalidOperationException("Chưa có nguồn kiểm tra đủ liên kết hồ sơ/chứng từ.");
        var value=Activator.CreateInstance(method.GetParameters()[0].ParameterType)!;
        foreach(var p in new[]{("MaBN",bn),("MaHS",hs),("MaPhieu",receipt),("MaLS",visit),("MaDT",rx),("MaHD",invoice)})value.GetType().GetProperty(p.Item1)!.SetValue(value,p.Item2);
        try{return method.Invoke(new BUS_KHAMDIEUTRI(f.Transaction),[value,f.User(user)])!;}catch(TargetInvocationException ex)when(ex.InnerException is not null){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();throw;}
    }
    private static string? Get(object value,string key)=>(string?)value.GetType().GetProperty(key)!.GetValue(value);
    private static void Denied(Action action){try{action();}catch(TuChoiQuyenNghiepVuException){return;}throw new InvalidOperationException("Nguồn sai liên kết vẫn được mở.");}
    internal static IEnumerable<(string Name,Action<Fixture> Run)> Cases()
    {
        yield return("P11 liên kết đơn suy đúng BN/hồ sơ/phiếu/lượt",f=>{var c=Resolve(f,rx:f.Id("DTA"));Need(Get(c,"MaBN")==f.Id("BN1")&&Get(c,"MaLS")==f.Id("LSA")&&Get(c,"MaHS")==f.Id("HS1"),"Suy sai nguồn đơn");});
        yield return("P11 mã hồ sơ/phiếu khác BN bị từ chối",f=>{Denied(()=>Resolve(f,bn:f.Id("BN2"),hs:f.Id("HS1")));Denied(()=>Resolve(f,hs:f.Id("HS2"),visit:f.Id("LSA")));});
        yield return("P11 đơn/lượt khác nhau không tự chọn nguồn",f=>Denied(()=>Resolve(f,visit:f.Id("LSB"),rx:f.Id("DTA"))));
        yield return("P11 mã không tồn tại hoặc quá dài bị từ chối",f=>{Denied(()=>Resolve(f,invoice:f.Id("MISSING")));Denied(()=>Resolve(f,receipt:new string('x',51)));});
        yield return("P11 bác sĩ không mở nháp bác sĩ khác",f=>Denied(()=>Resolve(f,user:"B",rx:f.Id("DTA"))));
        yield return("P11 chỉ suy định danh, không ghi tài chính/kho",f=>{var before=f.Scalar<int>("SELECT COUNT(*) FROM dbo.HOA_DON");Resolve(f,bn:f.Id("BN1"),visit:f.Id("LSA"));Need(before==f.Scalar<int>("SELECT COUNT(*) FROM dbo.HOA_DON"),"Điều hướng ghi hóa đơn");});
        yield return("P11 danh sách mã lượt hóa đơn kiểm quyền tại SQL không cần toàn bộ hồ sơ",f=>
        {
            f.Execute("INSERT dbo.HOA_DON(MaHD,MaLS,MaNV,NgayLap,TongTien,GiamGia,TrangThai) VALUES(@hd,@ls,@actor,SYSDATETIME(),0,0,N'Nháp')",new{hd=f.Id("HDA"),ls=f.Id("LSA"),actor=f.Id("A")});
            var method=typeof(BUS_KHAMDIEUTRI).GetMethod("LayMaLuotHoaDon")??throw new InvalidOperationException("Tra cứu hóa đơn vẫn cần snapshot toàn bộ hồ sơ.");var bus=new BUS_KHAMDIEUTRI(f.Transaction);
            var own=(IReadOnlyList<string>)method.Invoke(bus,[f.Id("LSA"),f.User("A")])!;Need(own.SequenceEqual(new[]{f.Id("LSA")}),"Sai phạm vi lượt hóa đơn");
            var denied=(IReadOnlyList<string>)method.Invoke(bus,[f.Id("LSA"),f.User("B")])!;Need(denied.Count==0,"Bác sĩ khác đọc hóa đơn của nháp");
        });
        yield return("P11 lịch sử đơn phân trang giữ dòng và định danh theo quyền",f=>
        {var page=new BUS_KHAMDIEUTRI(f.Transaction).LayTrangLichSuDon(new(){MaBN=f.Id("BN1"),MaLS=f.Id("LSA"),KichThuocTrang=1},f.User("A"));Need(page.TongSo==1&&page.CacDong.Count==1&&page.CacDong[0].MaDT==f.Id("DTA")&&page.CacDong[0].MaBN==f.Id("BN1")&&page.CacDong[0].CacDong.Count==1,"Sai trang đơn/dòng/BN");});
        yield return("P11 lịch sử đơn không tin cờ quyền thu ngân và không đọc nháp bác sĩ khác",f=>
        {var bus=new BUS_KHAMDIEUTRI(f.Transaction);var cash=f.User("C") with{LaAdmin=true,Quyen=QuyenLamViec.Kham|QuyenLamViec.TiepNhan};Denied(()=>bus.LayTrangLichSuDon(new(){},cash));var page=bus.LayTrangLichSuDon(new(){MaLS=f.Id("LSA")},f.User("B"));Need(page.TongSo==0,"Bác sĩ khác thấy đơn nháp");});
        yield return("P11 lịch sử đơn tìm ký tự wildcard theo nghĩa đen",f=>
        {var bus=new BUS_KHAMDIEUTRI(f.Transaction);var actor=f.User("A");Need(bus.LayTrangLichSuDon(new(){TuKhoa="%_["},actor).TongSo==0,"Wildcard mở rộng phạm vi đơn");});
        yield return("P11 hàng phân trang điều dưỡng không lộ hoặc tìm theo chẩn đoán nháp",f=>
        {
            var actor=f.User("A");var services=new BUS_DICHVUTHUCHIEN(f.Transaction);services.LuuThucHien(new(){MaLS=f.Id("LSA"),MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY MaDV"),MaNVThucHien=f.Id("N"),ThoiGianThucHien=f.Scalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())"),SoLuong=1,DonGia=0,LyDoPhatSinh="FIXTURE"},actor);
            var secret="FIXTURE PHI DOCTOR ONLY "+f.Prefix;f.Execute("UPDATE dbo.LICH_SU_KHAM_BENH SET ChanDoan=@secret WHERE MaLS=@id",new{secret,id=f.Id("LSA")});
            var nurse=f.User("N");var bus=new BUS_KHAMDIEUTRI(f.Transaction);var page=bus.LayTrangLanKham(new(){MaLS=f.Id("LSA")},nurse);
            Need(page.CacDong.Count==1&&page.CacDong[0].ChanDoan=="","Điều dưỡng đọc được chẩn đoán nháp trong metadata");
            Need(bus.LayTrangLanKham(new(){MaLS=f.Id("LSA"),TuKhoa=secret},nurse).TongSo==0,"Tìm kiếm suy ra chẩn đoán nháp bị ẩn");
        });
        yield return("P11 SQL phân trang 1000 BN/10000 lượt và cache UI có giới hạn",f=>
        {
            var bus=new BUS_KHAMDIEUTRI(f.Transaction);var pageMethod=typeof(BUS_KHAMDIEUTRI).GetMethod("LayTrangBenhNhan")??throw new InvalidOperationException("Chưa phân trang BN tại SQL.");
            var visitsMethod=typeof(BUS_KHAMDIEUTRI).GetMethod("LayTrangLanKham")??throw new InvalidOperationException("Chưa phân trang lượt tại SQL.");
            var cacheMethod=typeof(BUS_KHAMDIEUTRI).GetMethod("LayDuLieuHoSoGiaoDien")??throw new InvalidOperationException("Cache UI chưa có giới hạn.");
            Console.WriteLine("P11 LOAD setup start");SeedLarge(f);Console.WriteLine("P11 LOAD setup complete");var actor=f.User("A");
            object Page(MethodInfo method,int number)
            {Console.WriteLine($"P11 LOAD {method.Name} page {number}");var filter=Activator.CreateInstance(method.GetParameters()[0].ParameterType)!;filter.GetType().GetProperty("TuKhoa")!.SetValue(filter,"FIXTURE P11 LOAD");filter.GetType().GetProperty("Trang")!.SetValue(filter,number);filter.GetType().GetProperty("KichThuocTrang")!.SetValue(filter,25);return method.Invoke(bus,[filter,actor])!;}
            object[] Rows(object page)=>((System.Collections.IEnumerable)page.GetType().GetProperty("CacDong")!.GetValue(page)!).Cast<object>().ToArray();
            var watch=System.Diagnostics.Stopwatch.StartNew();var first=Page(pageMethod,1);var last=Page(pageMethod,40);var visits=Page(visitsMethod,400);watch.Stop();
            Need((int)first.GetType().GetProperty("TongSo")!.GetValue(first)! ==1000&&Rows(first).Length==25&&Rows(last).Length==25,"Thiếu hoặc tải toàn bộ BN");
            Need((int)visits.GetType().GetProperty("TongSo")!.GetValue(visits)! ==10000&&Rows(visits).Length==25,"Thiếu hoặc tải toàn bộ lượt");
            Console.WriteLine("P11 LOAD UI snapshot");var cType=cacheMethod.GetParameters()[0].ParameterType;var snapshot=(DuLieuHoSoTheoQuyen)cacheMethod.Invoke(bus,[Activator.CreateInstance(cType)!,actor])!;
            Need(snapshot.CacBang["BENH_NHAN"].Length<=50&&snapshot.CacBang["LICH_SU_KHAM_BENH"].Length<=100,"Cache UI tải toàn bộ dữ liệu lớn");
            var receiptIds=snapshot.CacBang["PHIEU_KHAM_BENH"].Cast<ET_PHIEUKHAMBENH>().Select(p=>p.MaPhieu).ToHashSet();
            Need(snapshot.CacBang["LICH_SU_KHAM_BENH"].Cast<ET_LICHSUKHAMBENH>().All(l=>receiptIds.Contains(l.MaPhieu)),"Snapshot giới hạn làm mất phiếu cha");
            var selected=new NguCanhHoSo{MaBN=f.Id("LB1000"),MaLS=f.Id("LL10000")};var exact=(DuLieuHoSoTheoQuyen)cacheMethod.Invoke(bus,[selected,actor])!;
            Need(exact.CacBang["BENH_NHAN"].Length==1&&exact.CacBang["LICH_SU_KHAM_BENH"].Cast<ET_LICHSUKHAMBENH>().Single().MaLS==selected.MaLS,"Không mở đúng lượt ngoài snapshot đầu");
            Console.WriteLine($"P11 SQL LOAD: 1000BN/10000LS; pages25; cacheBN{snapshot.CacBang["BENH_NHAN"].Length}/LS{snapshot.CacBang["LICH_SU_KHAM_BENH"].Length}; three queries {watch.ElapsedMilliseconds}ms (máy thử).");
        });
    }
    private static void SeedLarge(Fixture f)=>f.Execute("""
        DECLARE @now datetime2(0)=DATEADD(minute,-30,SYSDATETIME()),@room nvarchar(50)=(SELECT TOP(1) MaPhong FROM dbo.PHONG ORDER BY MaPhong);
        SELECT TOP(10000) CONVERT(int,ROW_NUMBER() OVER(ORDER BY a.object_id,b.object_id)) n INTO #P11Rows FROM sys.all_objects a CROSS JOIN sys.all_objects b;
        INSERT dbo.BENH_NHAN(MaBN,HoTenBN,NgaySinh,TrangThai)
          SELECT @prefix+'LB'+CONVERT(nvarchar(10),n),N'FIXTURE P11 LOAD '+CONVERT(nvarchar(10),n),'1990-01-01',N'Đang theo dõi' FROM #P11Rows WHERE n<=1000;
        INSERT dbo.HO_SO_BENH_AN(MaHS,MaBN,NgayLap,LyDoKhamBanDau,TrangThai)
          SELECT @prefix+'LH'+CONVERT(nvarchar(10),n),@prefix+'LB'+CONVERT(nvarchar(10),n),DATEADD(day,-1,@now),N'FIXTURE',N'Đang theo dõi' FROM #P11Rows WHERE n<=1000;
        INSERT dbo.PHIEU_KHAM_BENH(MaPhieu,MaHS,MaPhong,MaBS,MaNVTiepNhan,SoThuTu,ThoiGianTiepNhan,LyDoKham,TrangThai)
          SELECT @prefix+'LP'+CONVERT(nvarchar(10),n),@prefix+'LH'+CONVERT(nvarchar(10),(n-1)%1000+1),@room,@prefix+'A',@prefix+'R',10000+n,@now,N'FIXTURE',N'Đang khám' FROM #P11Rows;
        INSERT dbo.LICH_SU_KHAM_BENH(MaLS,MaPhieu,MaBS,MaPhong,ThoiGianKham,TrieuChung,ChanDoan,KeHoachXuLy,TrangThai)
          SELECT @prefix+'LL'+CONVERT(nvarchar(10),n),@prefix+'LP'+CONVERT(nvarchar(10),n),@prefix+'A',@room,DATEADD(minute,1,@now),N'FIXTURE',N'FIXTURE',N'FIXTURE',N'Nháp' FROM #P11Rows;
        DROP TABLE #P11Rows;
        """,new{prefix=f.Prefix});
}
