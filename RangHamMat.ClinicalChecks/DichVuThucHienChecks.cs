using BUS;
using ET;
namespace RangHamMat.ClinicalChecks;

internal static class DichVuThucHienChecks
{
    private static void Need(bool ok,string message)=>KhamVaKeHoachChecks.Need(ok,message);
    private static void Invalid(Action run)=>Throws<LoiKhamException>(run);
    private static void Denied(Action run)=>Throws<TuChoiQuyenNghiepVuException>(run);
    private static void Throws<T>(Action run) where T:Exception
    {try{run();}catch(T){return;}throw new Exception("Phải từ chối bằng "+typeof(T).Name);}
    internal static YeuCauThucHienDichVu Request(Fixture f,string? step=null)=>new(){MaLS=f.Id("LSA"),MaCTKH=step,MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY MaDV"),MaNVThucHien=f.Id("N"),ThoiGianThucHien=f.Scalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())"),SoLuong=1,DonGia=10000,KetQua="FIXTURE kết quả",LyDoPhatSinh=step is null?"FIXTURE ngoài kế hoạch":null};
    internal static YeuCauThucHienDichVu Update(BanNhapThucHien s)=>new(){MaDVThucHien=s.DichVu.MaDVThucHien,MaLS=s.DichVu.MaLS,MaCTKH=s.DichVu.MaCTKH,MaDV=s.DichVu.MaDV,MaNVThucHien=s.DichVu.MaNVThucHien,ThoiGianThucHien=s.DichVu.ThoiGianThucHien,SoLuong=s.DichVu.SoLuong,DonGia=s.DichVu.DonGia,KetQua=s.DichVu.KetQua,LyDoPhatSinh=s.LyDoPhatSinh,DauVanTay=s.DauVanTay};
    internal static BanNhapKeHoach Plan(Fixture f,NguCanhNghiepVu user,decimal qty=2)
    {var b=new BUS_KHAMDIEUTRI(f.Transaction);var p=b.LuuKeHoach(new(){MaLS=f.Id("LSA"),CacBuoc=[new(){MaDV=Request(f).MaDV,SoLuongDuKien=qty,DonGiaDuKien=10000,NoiDungBuoc="FIXTURE buổi sau"}]},user);return b.XacNhanKeHoach(p.KeHoach.MaKHDT,p.DauVanTay,user);}
    internal static IEnumerable<(string,Action<Fixture>)> Cases()
    {
        yield return("Quầy thu đọc nguồn phí không thấy kết quả/lý do chuyên môn",f=>
        {
            var id=SignedReadFixture(f);var b=new BUS_DICHVUTHUCHIEN(f.Transaction);
            foreach(var role in new[]{"C","R"})
            {var u=f.User(role);var direct=b.LayThucHien(id,u);var list=b.LayKhongGianThucHien(f.Id("LSH"),u).ThucTe.Single(r=>r.DichVu.MaDVThucHien==id);Need(direct.DichVu.KetQua is null&&direct.LyDoPhatSinh is null&&list.DichVu.KetQua is null&&list.LyDoPhatSinh is null,"Lộ kết quả hoặc lý do cho quầy thu");Need(direct.DichVu.ThanhTien==10000&&direct.DichVu.MaLS==f.Id("LSH"),"Mất nguồn thu cần thiết");}
        });
        yield return("Admin đọc trực tiếp dịch vụ phải có nhật ký đúng mã nguồn",f=>
        {var id=SignedReadFixture(f);var x=f.User("X");new BUS_DICHVUTHUCHIEN(f.Transaction).LayThucHien(id,x);Need(f.Scalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaTK=@MaTK AND MaDoiTuong=@id AND HanhDong=N'Tra cứu dịch vụ'",new{x.MaTK,id})==1,"Thiếu audit tra cứu trực tiếp");});
        yield return("Lý do mới nhất cùng giây qua biên mã 999/1000 không bị trả cũ",f=>
        {
            var a=f.User("A");var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var s=b.LuuThucHien(Request(f) with {LyDoPhatSinh="FIXTURE lý do đầu"},a);
            f.Execute("UPDATE dbo.NHAT_KY_THAO_TAC SET MaNK=N'NKTT999' WHERE MaDoiTuong=@id AND HanhDong=N'Lưu dịch vụ thực hiện'",new{id=s.DichVu.MaDVThucHien});
            b.LuuThucHien(Update(s) with {LyDoPhatSinh="FIXTURE lý do mới"},a);
            f.Execute("UPDATE dbo.NHAT_KY_THAO_TAC SET ThoiGian='2026-10-26T12:00:00' WHERE MaDoiTuong=@id AND HanhDong=N'Lưu dịch vụ thực hiện'",new{id=s.DichVu.MaDVThucHien});
            var latest=b.LayThucHien(s.DichVu.MaDVThucHien,a);Need(latest.LyDoPhatSinh=="FIXTURE lý do mới"&&latest.DauVanTay!=s.DauVanTay,"Trả lại lý do/dấu vân tay cũ tại biên số chữ số");
        });
        yield return("Điều dưỡng lưu dòng từ bước với lý do trống không đổi nguồn",f=>
        {var a=f.User("A");var p=Plan(f,a);var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var s=b.LuuThucHien(Request(f,p.CacBuoc[0].MaCTKH),a);var n=f.User("N");var saved=b.LuuThucHien(Update(s) with {LyDoPhatSinh="",KetQua="FIXTURE điều dưỡng từ bước"},n);Need(saved.DichVu.KetQua=="FIXTURE điều dưỡng từ bước","Lý do trống làm sai quyền điều dưỡng");});
        yield return("Nguồn bước và dịch vụ của dòng đã lưu không đổi ngầm",f=>
        {var a=f.User("A");var p=Plan(f,a);var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var s=b.LuuThucHien(Request(f,p.CacBuoc[0].MaCTKH),a);Invalid(()=>b.LuuThucHien(Update(s) with {MaCTKH=null,LyDoPhatSinh="FIXTURE"},a));Need(b.LayThucHien(s.DichVu.MaDVThucHien,a).DichVu.MaCTKH==p.CacBuoc[0].MaCTKH,"Nguồn đã đổi");});
        yield return("Khóa tài khoản sau mở dịch vụ chặn lưu và xác nhận",f=>
        {var a=f.User("A");var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var s=b.LuuThucHien(Request(f),a);f.Execute("UPDATE dbo.TAI_KHOAN SET TrangThai=N'Khóa' WHERE MaTK=@id",new{id=a.MaTK});Denied(()=>b.LuuThucHien(Update(s),a));Denied(()=>b.XacNhanThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,a));});
        yield return("Nguồn đã thu giữ nguyên ở buổi trước",f=>
        {
            var a=f.User("A");var p=Plan(f,a);var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var s=b.LuuThucHien(Request(f,p.CacBuoc[0].MaCTKH),a);s=b.XacNhanThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,a);
            f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id;DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=f.Id("DTA")});var clinical=new BUS_KHAMDIEUTRI(f.Transaction);var c=clinical.LuuKham(KhamVaKeHoachChecks.Request(clinical.LayBanNhap(f.Id("LSA"),a)) with {KhongKeThuoc=true,LyDoKhongKeThuoc="FIXTURE"},a);clinical.HoanTatKham(f.Id("LSA"),c.DauVanTay,a);
            f.Execute("INSERT dbo.HOA_DON(MaHD,MaLS,MaNV,NgayLap,TongTien,GiamGia,TrangThai) VALUES(@hd,@ls,@nv,SYSDATETIME(),0,0,N'Nháp'); INSERT dbo.CHI_TIET_HOA_DON(MaCTHD,MaHD,MaDVThucHien,NoiDung,ThanhTien) VALUES(@ct,@hd,@dv,N'FIXTURE',0); UPDATE dbo.HOA_DON SET TrangThai=N'Đã xác nhận',NgayXacNhan=SYSDATETIME() WHERE MaHD=@hd; INSERT dbo.THANH_TOAN(MaTT,MaHD,MaNV,NgayThanhToan,SoTien,LoaiGiaoDich,PhuongThuc,TrangThai) VALUES(@tt,@hd,@nv,SYSDATETIME(),10000,N'Thu',N'Tiền mặt',N'Đã xác nhận')",new{hd=f.Id("HD06"),ct=f.Id("CTHD06"),tt=f.Id("TT06"),ls=f.Id("LSA"),nv=f.Id("R"),dv=s.DichVu.MaDVThucHien});
            Need(b.LayThucHien(s.DichVu.MaDVThucHien,a).DaTinhPhi,"Không nhận nguồn đã thu");Invalid(()=>b.LuuThucHien(Update(s) with {DonGia=1},a));Invalid(()=>b.HuyThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,a));
            try{f.Execute("UPDATE dbo.CHI_TIET_KE_HOACH SET SoLuongDuKien=99 WHERE MaCTKH=@id",new{id=p.CacBuoc[0].MaCTKH});}catch(Microsoft.Data.SqlClient.SqlException ex)when(ex.Errors.Cast<Microsoft.Data.SqlClient.SqlError>().Any(e=>e.Number==52114)){return;}throw new Exception("Sửa được bước nguồn đã thu.");
        });
        yield return("Kế hoạch/nháp/hủy không tạo khoản thu; xác nhận một lần",f=>
        {var u=f.User("A");var p=Plan(f,u);var b=new BUS_DICHVUTHUCHIEN(f.Transaction);Need(b.LayKhongGianThucHien(f.Id("LSA"),u).GiaTriDaXacNhan==0,"Kế hoạch bị tính tiền");var s=b.LuuThucHien(Request(f,p.CacBuoc[0].MaCTKH),u);Need(b.LayKhongGianThucHien(f.Id("LSA"),u).GiaTriDaXacNhan==0,"Nháp bị tính");b.HuyThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,u);s=b.LuuThucHien(Request(f,p.CacBuoc[0].MaCTKH),u);b.XacNhanThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,u);b.XacNhanThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,u);Need(b.LayKhongGianThucHien(f.Id("LSA"),u).GiaTriDaXacNhan==10000,"Tính nhầm hoặc hai lần");Need(f.Scalar<int>("SELECT COUNT(*) FROM dbo.HOA_DON WHERE MaLS=@id",new{id=f.Id("LSA")})==0,"P06 tự tạo hóa đơn");});
        yield return("Phát sinh không có lý do bị chặn, có lý do audit giữ nguồn",f=>
        {var u=f.User("A");var b=new BUS_DICHVUTHUCHIEN(f.Transaction);Invalid(()=>b.LuuThucHien(Request(f) with {LyDoPhatSinh=null},u));var s=b.LuuThucHien(Request(f),u);Need(b.LayThucHien(s.DichVu.MaDVThucHien,u).LyDoPhatSinh=="FIXTURE ngoài kế hoạch","Mất lý do");});
        yield return("Chỉ bác sĩ phụ trách tạo và xác nhận, Admin không giả quyền",f=>
        {var a=f.User("A");var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var s=b.LuuThucHien(Request(f),a);Denied(()=>b.LuuThucHien(Request(f),f.User("B")));Denied(()=>b.XacNhanThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,f.User("X") with {Quyen=(QuyenLamViec)int.MaxValue}));});
        yield return("Điều dưỡng chỉ sửa kết quả/thời gian dòng được giao",f=>
        {var a=f.User("A");var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var s=b.LuuThucHien(Request(f),a);var n=f.User("N");var saved=b.LuuThucHien(Update(s) with {KetQua="FIXTURE điều dưỡng"},n);Need(saved.DichVu.KetQua=="FIXTURE điều dưỡng","Không lưu kết quả");Denied(()=>b.LuuThucHien(Update(saved) with {DonGia=1},n));Denied(()=>b.LuuThucHien(Update(saved) with {SoLuong=2},n));Denied(()=>b.LuuThucHien(Request(f),n));Denied(()=>b.XacNhanThucHien(saved.DichVu.MaDVThucHien,saved.DauVanTay,n));});
        yield return("Điều dưỡng khác không đọc hoặc sửa dòng không được giao",f=>
        {var a=f.User("A");var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var s=b.LuuThucHien(Request(f) with {MaNVThucHien=f.Id("A")},a);var n=f.User("N");Denied(()=>b.LayThucHien(s.DichVu.MaDVThucHien,n));Denied(()=>b.LuuThucHien(Update(s),n));Need(b.LayLuotThucHien(n).Count==0,"Lộ lượt chưa được giao");});
        yield return("Bước nháp, sai dịch vụ hoặc khác hồ sơ không thực hiện",f=>
        {var a=f.User("A");var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var p=new BUS_KHAMDIEUTRI(f.Transaction).LuuKeHoach(new(){MaLS=f.Id("LSA"),CacBuoc=[new(){MaDV=Request(f).MaDV,NoiDungBuoc="FIXTURE"}]},a);Invalid(()=>b.LuuThucHien(Request(f,p.CacBuoc[0].MaCTKH),a));p=new BUS_KHAMDIEUTRI(f.Transaction).XacNhanKeHoach(p.KeHoach.MaKHDT,p.DauVanTay,a);Invalid(()=>b.LuuThucHien(Request(f,p.CacBuoc[0].MaCTKH) with {MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE MaDV<>@id",new{id=Request(f).MaDV})},a));f.Execute("INSERT dbo.PHIEU_KHAM_BENH(MaPhieu,MaHS,MaPhong,MaBS,MaNVTiepNhan,SoThuTu,ThoiGianTiepNhan,LyDoKham,TrangThai) SELECT @id,MaHS,MaPhong,MaBS,MaNVTiepNhan,SoThuTu+1,ThoiGianTiepNhan,LyDoKham,TrangThai FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@source",new{id=f.Id("PHOTHER"),source=f.Id("PHU")});var other=f.User("B");var ls=new BUS_KHAMDIEUTRI(f.Transaction).BatDauKham(f.Id("PHOTHER"),other);Invalid(()=>b.LuuThucHien(Request(f,p.CacBuoc[0].MaCTKH) with {MaLS=ls},other));});
        yield return("Lượng đã xác nhận và nháp cùng giữ giới hạn bước",f=>
        {var a=f.User("A");var p=Plan(f,a,2);var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var r=Request(f,p.CacBuoc[0].MaCTKH);var s=b.LuuThucHien(r with {SoLuong=1.5m},a);Invalid(()=>b.LuuThucHien(r,a));b.HuyThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,a);s=b.LuuThucHien(r with {SoLuong=2},a);b.XacNhanThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,a);Invalid(()=>b.LuuThucHien(r,a));});
        yield return("Thời gian trước lượt hoặc tương lai bị chặn",f=>
        {var a=f.User("A");var b=new BUS_DICHVUTHUCHIEN(f.Transaction);Invalid(()=>b.LuuThucHien(Request(f) with {ThoiGianThucHien=DateTime.Today.AddYears(-1)},a));Invalid(()=>b.LuuThucHien(Request(f) with {ThoiGianThucHien=DateTime.Now.AddDays(1)},a));});
        yield return("Người làm phải có tài khoản/chức vụ hoạt động",f=>
        {var a=f.User("A");var b=new BUS_DICHVUTHUCHIEN(f.Transaction);Invalid(()=>b.LuuThucHien(Request(f) with {MaNVThucHien=f.Id("R")},a));f.Execute("UPDATE dbo.TAI_KHOAN SET TrangThai=N'Khóa' WHERE MaTK=@id",new{id=f.Id("TKN")});Invalid(()=>b.LuuThucHien(Request(f),a));});
        yield return("Snapshot cũ không ghi đè hoặc xác nhận nội dung đã đổi",f=>
        {var a=f.User("A");var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var s=b.LuuThucHien(Request(f),a);b.LuuThucHien(Update(s) with {KetQua="FIXTURE sửa"},a);Throws<XungDotDuLieuKhamException>(()=>b.LuuThucHien(Update(s),a));Throws<XungDotDuLieuKhamException>(()=>b.XacNhanThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,a));});
        yield return("Dịch vụ đã xác nhận không sửa, hủy hoặc đổi nguồn",f=>
        {var a=f.User("A");var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var s=b.LuuThucHien(Request(f),a);s=b.XacNhanThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,a);Invalid(()=>b.LuuThucHien(Update(s) with {DonGia=1},a));Invalid(()=>b.HuyThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,a));});
        yield return("Kết quả rỗng không xác nhận dịch vụ",f=>
        {var a=f.User("A");var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var s=b.LuuThucHien(Request(f) with {KetQua=null},a);Invalid(()=>b.XacNhanThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,a));});
        yield return("Hai buổi cùng hồ sơ dùng phần còn lại, nguồn phí tách lượt",f=>
        {var a=f.User("A");var p=Plan(f,a,2);var b=new BUS_DICHVUTHUCHIEN(f.Transaction);var s=b.LuuThucHien(Request(f,p.CacBuoc[0].MaCTKH),a);b.XacNhanThucHien(s.DichVu.MaDVThucHien,s.DauVanTay,a);var bb=f.User("B");var t=b.LuuThucHien(Request(f,p.CacBuoc[0].MaCTKH) with {MaLS=f.Id("LSB")},bb);b.XacNhanThucHien(t.DichVu.MaDVThucHien,t.DauVanTay,bb);Need(b.LayKhongGianThucHien(f.Id("LSB"),bb).GiaTriDaXacNhan==10000,"Buổi hai cộng phí buổi trước");Need(b.LayKhongGianThucHien(f.Id("LSB"),bb).DuKien.Single(x=>x.MaCTKH==p.CacBuoc[0].MaCTKH).ConLai==0,"Không trừ phần đã làm");});
    }
    private static string SignedReadFixture(Fixture f)
    {
        var id=f.Id("READ06");f.Execute("INSERT dbo.DICH_VU_THUC_HIEN(MaDVThucHien,MaLS,MaDV,MaNVThucHien,ThoiGianThucHien,SoLuong,DonGia,KetQua,TrangThai) VALUES(@id,@ls,@dv,@nv,SYSDATETIME(),1,10000,N'FIXTURE kết quả riêng',N'Đã xác nhận'); INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuSau) VALUES(@nk,@tk,SYSDATETIME(),N'Lưu dịch vụ thực hiện',N'DICH_VU_THUC_HIEN',@id,N'Thành công',N'{\"LyDoPhatSinh\":\"FIXTURE lý do riêng\"}')",new{id,ls=f.Id("LSH"),dv=Request(f).MaDV,nv=f.Id("B"),nk=f.Id("READAUDIT"),tk=f.Id("TKB")});return id;
    }
}
