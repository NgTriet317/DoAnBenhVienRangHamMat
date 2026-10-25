using BUS;
using DAL;
using Dapper;
using ET;

namespace RangHamMat.ClinicalChecks;

internal static class TiepNhanChecks
{
    private static void Need(bool ok,string message) {if(!ok)throw new Exception(message);}
    internal static YeuCauTiepNhan Request(Fixture f)
    {
        var service=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE MaCK IS NOT NULL AND TrangThai=N'Đang hoạt động' ORDER BY MaDV");
        f.Execute("UPDATE dbo.NHAN_VIEN SET MaCK=(SELECT MaCK FROM dbo.DICH_VU WHERE MaDV=@service) WHERE MaNV=@doctor",new{service,doctor=f.Id("A")});
        var room=f.Scalar<string>("SELECT TOP(1) MaPhong FROM dbo.PHONG WHERE TrangThai=N'Đang hoạt động' AND LoaiPhong LIKE N'Phòng khám%' ORDER BY MaPhong");
        return new() {MaBN=f.Id("BN1"),MaDV=service,MaBS=f.Id("A"),MaPhong=room,LyDoKham="FIXTURE không dùng thật",LyDoDieuPhoiNgoaiLich="FIXTURE điều phối chưa có lịch"};
    }
    internal static void Schedule(Fixture f,YeuCauTiepNhan r,string status="Đã xác nhận",string attendance="Có mặt")
    {
        f.Execute("""
            INSERT dbo.CA_LAM(MaCa,GioBatDau,GioKetThuc,TrangThai) VALUES(@ca,'00:00:00','23:59:59',N'Đang hoạt động');
            INSERT dbo.LICH_LAM_VIEC(MaLLV,MaNV,MaCa,NgayLam,MaPhong,TrangThai,TrangThaiHienDien)
            VALUES(@id,@doctor,@ca,CONVERT(date,SYSDATETIME()),@room,@status,@attendance);
            """,new{ca=f.Id("CA"),id=f.Id("LLV"),doctor=r.MaBS,room=r.MaPhong,status,attendance});
    }
    private static void Invalid(Action run,string field)
    {try{run();}catch(LoiTiepNhanException ex)when(ex.Truong==field){return;}throw new Exception("Phải từ chối tại "+field);}
    private static void Denied(Action run)
    {try{run();}catch(TuChoiQuyenNghiepVuException){return;}throw new Exception("Phải từ chối quyền");}
    internal static IEnumerable<(string,Action<Fixture>)> Cases()
    {
        yield return ("Tiếp nhận chỉ liệt kê bác sĩ đúng chuyên khoa và tài khoản hoạt động",f=>
        {
            var r=Request(f); f.Execute("UPDATE dbo.NHAN_VIEN SET MaCK=(SELECT TOP(1) MaCK FROM dbo.CHUYEN_KHOA WHERE MaCK<>(SELECT MaCK FROM dbo.DICH_VU WHERE MaDV=@dv)) WHERE MaNV=@id",new{dv=r.MaDV,id=f.Id("B")});
            var rows=new BUS_TIEPNHAN(f.Transaction).LayBacSi(r.MaDV,f.User("R"));
            Need(rows.Any(v=>v.MaNV==r.MaBS) && rows.All(v=>v.MaNV!=f.Id("B")),"Trộn bác sĩ chuyên khoa");
            f.Execute("UPDATE dbo.TAI_KHOAN SET TrangThai=N'Đã khóa' WHERE MaNV=@id",new{id=r.MaBS});
            Need(new BUS_TIEPNHAN(f.Transaction).LayBacSi(r.MaDV,f.User("R")).All(v=>v.MaNV!=r.MaBS),"Tài khoản khóa còn được phân");
        });
        yield return ("Tiếp nhận tạo phiếu đúng bác sĩ, dùng hồ sơ cũ và không sinh MaLS",f=>
        {
            var r=Request(f); var user=f.User("R"); var result=new BUS_TIEPNHAN(f.Transaction).TiepNhan(r,user);
            Need(result.MaBN==r.MaBN && result.MaHS==f.Id("HS1") && result.MaBS==r.MaBS && result.TrangThai=="Chờ khám" && result.SoThuTu>0,"Sai ngữ cảnh tiếp nhận");
            var row=f.Connection.QuerySingle<ET_PHIEUKHAMBENH>("SELECT * FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@id",new{id=result.MaPhieu},f.Transaction);
            Need(row.MaNVTiepNhan==user.MaNV && row.MaDV==r.MaDV,"Không lấy NV từ phiên");
            Need(f.Scalar<int>("SELECT COUNT(*) FROM dbo.LICH_SU_KHAM_BENH WHERE MaPhieu=@id",new{id=result.MaPhieu})==0,"Tự tạo chẩn đoán");
            var queue=new BUS_KHAMDIEUTRI(f.Transaction).LayHangDoi(new(),f.User("A"));
            Need(queue.Any(q=>q.MaPhieu==result.MaPhieu),"Không đến hàng đợi bác sĩ đích");
        });
        yield return ("Tiếp nhận thiếu bác sĩ không lưu",f=>{var r=Request(f);Invalid(()=>new BUS_TIEPNHAN(f.Transaction).TiepNhan(r with {MaBS=""},f.User("R")),"MaBS");});
        yield return ("Tiếp nhận bác sĩ sai chuyên khoa bị chặn lại tại SQL",f=>{var r=Request(f);f.Execute("UPDATE dbo.NHAN_VIEN SET MaCK=NULL WHERE MaNV=@id",new{id=r.MaBS});Invalid(()=>new BUS_TIEPNHAN(f.Transaction).TiepNhan(r,f.User("R")),"MaBS");});
        yield return ("Không có lịch phải hiện rõ và có lý do điều phối",f=>
        {
            var r=Request(f);var u=f.User("R");var bus=new BUS_TIEPNHAN(f.Transaction);
            Need(bus.LayBacSi(r.MaDV,u).Single(v=>v.MaNV==r.MaBS).ThieuLich,"Thiếu lịch bị coi là sẵn sàng");
            Invalid(()=>bus.TiepNhan(r with {LyDoDieuPhoiNgoaiLich=null},u),"LyDoDieuPhoiNgoaiLich");
        });
        yield return ("Điều dưỡng không tự được điều phối ngoài lịch",f=>{var r=Request(f);Denied(()=>new BUS_TIEPNHAN(f.Transaction).TiepNhan(r,f.User("N")));});
        yield return ("Có lịch hợp lệ thì điều dưỡng được tiếp nhận và phòng phải khớp",f=>
        {
            var r=Request(f);Schedule(f,r); var result=new BUS_TIEPNHAN(f.Transaction).TiepNhan(r with {LyDoDieuPhoiNgoaiLich=null},f.User("N")); Need(result.MaBS==r.MaBS,"Lịch hợp lệ bị từ chối");
        });
        yield return ("Lịch đã xác định vắng thì không được điều phối vượt",f=>
        {
            var r=Request(f);Schedule(f,r,"Hủy","Nghỉ phép"); var u=f.User("R");Need(new BUS_TIEPNHAN(f.Transaction).LayBacSi(r.MaDV,u).All(v=>v.MaNV!=r.MaBS),"Đang nghỉ vẫn được chọn");Invalid(()=>new BUS_TIEPNHAN(f.Transaction).TiepNhan(r,u),"MaBS");
        });
        yield return ("Thu ngân và bác sĩ không tự tiếp nhận dù có DTO quyền giả",f=>{var r=Request(f);Denied(()=>new BUS_TIEPNHAN(f.Transaction).TiepNhan(r,f.User("C") with {Quyen=(QuyenLamViec)int.MaxValue}));});
        yield return ("Đổi quyền sau mở form vẫn bị từ chối",f=>{var r=Request(f);var user=f.User("R");f.Execute("UPDATE dbo.NHAN_VIEN SET MaChucVu=N'CV007' WHERE MaNV=@id",new{id=user.MaNV});Denied(()=>new BUS_TIEPNHAN(f.Transaction).TiepNhan(r,user));});
        yield return ("Guard form chung không lập phiếu bỏ qua đầu mối tiếp nhận",f=>
        {
            var u=f.User("R");Invalid(()=>new BUS_QUYENNGHIEPVU(f.Transaction).KiemTraGhiHoSo("PHIEU_KHAM_BENH",new Dictionary<string,object?> { ["MaNVTiepNhan"]=u.MaNV },null,false,u),"MaPhieu");
        });
        yield return ("Chưa có hồ sơ chỉ lập một lần và retry trực tiếp trả phiếu cũ",f=>
        {
            var r=Request(f);var id=f.Id("BN3");f.Execute("INSERT dbo.BENH_NHAN(MaBN,HoTenBN,TrangThai) VALUES(@id,N'FIXTURE CÙNG TÊN',N'Đang theo dõi')",new{id});
            var u=f.User("R");var bus=new BUS_TIEPNHAN(f.Transaction);var first=bus.TiepNhan(r with {MaBN=id},u);var second=bus.TiepNhan(r with {MaBN=id},u);
            Need(first==second && f.Scalar<int>("SELECT COUNT(*) FROM dbo.HO_SO_BENH_AN WHERE MaBN=@id",new{id})==1,"Trùng hồ sơ/phiếu khi bấm lại");
            Need(f.Scalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaDoiTuong=@id AND MaTK=@MaTK AND HanhDong=N'Tiếp nhận ngoài lịch' AND DuLieuSau LIKE N'%FIXTURE%'",new{id=first.MaPhieu,u.MaTK})==1,"Thiếu audit lý do điều phối");
        });
        yield return ("Lịch hẹn giữ nguyên khóa nguồn và retry không tạo hai phiếu",f=>
        {
            var r=Appointment(f,Request(f));var u=f.User("R");var bus=new BUS_TIEPNHAN(f.Transaction);var a=bus.TiepNhan(r,u);var b=bus.TiepNhan(r,u);
            Need(a==b && f.Scalar<int>("SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH WHERE MaLich=@MaLich AND TrangThai<>N'Hủy'",r)==1,"Tiếp nhận một lịch hai lần");
            Need(f.Scalar<string>("SELECT TrangThai FROM dbo.LICH_KHAM WHERE MaLich=@MaLich",r)=="Chờ khám","Không tích hợp trạng thái lịch");
        });
        yield return ("Lịch hẹn không chuyển sang bệnh nhân trùng tên khác",f=>{var r=Appointment(f,Request(f));Invalid(()=>new BUS_TIEPNHAN(f.Transaction).TiepNhan(r with {MaBN=f.Id("BN2")},f.User("R")),"MaLich");});
        yield return ("Bác sĩ có đơn nghỉ đã duyệt bị chặn dù còn lịch làm việc",f=>
        {
            var r=Request(f);Schedule(f,r);f.Execute("INSERT dbo.DON_XIN_NGHI(MaDonNghi,MaNV,TuNgay,DenNgay,LyDo,NgayGui,TrangThaiDuyet,MaNVDuyet,NgayDuyet) VALUES(@id,@doctor,CAST(SYSDATETIME() AS date),CAST(SYSDATETIME() AS date),N'FIXTURE',SYSDATETIME(),N'Đã duyệt',@reviewer,SYSDATETIME())",new{id=f.Id("NGHI"),doctor=r.MaBS,reviewer=f.Id("X")});
            Invalid(()=>new BUS_TIEPNHAN(f.Transaction).TiepNhan(r,f.User("R")),"MaBS");
        });
    }
    private static YeuCauTiepNhan Appointment(Fixture f,YeuCauTiepNhan r)
    {
        var id=f.Id("APPT");
        f.Execute("""
            INSERT dbo.LICH_KHAM(MaLich,MaBN,MaBS,MaCa,MaPhong,MaDV,NgayGioKham,ThoiLuongDuKien,UuTien,LyDoKham,TrangThai,NgayTao)
            VALUES(@id,@MaBN,@MaBS,(SELECT TOP(1) MaCa FROM dbo.CA_LAM ORDER BY MaCa),@MaPhong,@MaDV,DATEADD(minute,5,SYSDATETIME()),15,N'Bình thường',N'FIXTURE',N'Đã xác nhận',SYSDATETIME());
            """,new{id,r.MaBN,r.MaBS,r.MaPhong,r.MaDV});
        return r with {MaLich=id};
    }
}
