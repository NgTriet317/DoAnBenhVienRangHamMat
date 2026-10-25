using BUS;
using Dapper;
using ET;

namespace RangHamMat.ClinicalChecks;

/// <summary>Hai kết nối thật, fixture phải commit để cả hai quầy nhìn thấy; dọn theo mã GUID trong finally.</summary>
internal static class TiepNhanConcurrencyChecks
{
    internal static int Run()
    {
        using var db=MigrationChecks.Open();
        var prefix="CWTN"+Guid.NewGuid().ToString("N");
        var prior=Thread.CurrentPrincipal;
        var data=db.QuerySingle<(string Service,string Room,string Specialty)>("""
            SELECT TOP(1) d.MaDV,p.MaPhong,d.MaCK FROM dbo.DICH_VU d JOIN dbo.CHUYEN_KHOA c ON c.MaCK=d.MaCK
            JOIN dbo.PHONG p ON p.MaKhoa=c.MaKhoa WHERE d.TrangThai=N'Đang hoạt động'
              AND p.TrangThai=N'Đang hoạt động' AND p.LoaiPhong LIKE N'Phòng khám%' ORDER BY d.MaDV,p.MaPhong
            """);
        try
        {
            using(var setup=db.BeginTransaction())
            {
                db.Execute("""
                    DECLARE @role nvarchar(50)=(SELECT MaVaiTro FROM dbo.VAI_TRO WHERE TenVaiTro=N'User');
                    INSERT dbo.NHAN_VIEN(MaNV,HoTen,MaChucVu,MaCK,NgayVaoLam,TrangThai)
                    VALUES(@prefix+'A',N'FIXTURE TIẾP NHẬN',N'CV002',@specialty,'2020-01-01',N'Đang làm việc'),
                          (@prefix+'R',N'FIXTURE TIẾP NHẬN',N'CV004',@specialty,'2020-01-01',N'Đang làm việc');
                    INSERT dbo.TAI_KHOAN(MaTK,TenDangNhap,MatKhauHash,MaVaiTro,MaNV,TrangThai)
                    SELECT @prefix+'TK'+v.s,@prefix+v.s,N'FIXTURE-NOT-A-LOGIN',@role,@prefix+v.s,N'Đang hoạt động' FROM(VALUES('A'),('R'))v(s);
                    INSERT dbo.BENH_NHAN(MaBN,HoTenBN,TrangThai)
                    SELECT @prefix+v.s,N'FIXTURE CÙNG TÊN',N'Đang theo dõi' FROM(VALUES('BN1'),('BN2'),('BN3'),('BN4'))v(s);
                    INSERT dbo.CA_LAM(MaCa,GioBatDau,GioKetThuc,TrangThai) VALUES(@prefix+'CA','00:00:00','23:59:59',N'Đang hoạt động');
                    INSERT dbo.LICH_LAM_VIEC(MaLLV,MaNV,MaCa,NgayLam,MaPhong,TrangThai,TrangThaiHienDien)
                    VALUES(@prefix+'LLV',@prefix+'A',@prefix+'CA',CAST(SYSDATETIME() AS date),@room,N'Đã xác nhận',N'Có mặt');
                    INSERT dbo.LICH_KHAM(MaLich,MaBN,MaBS,MaCa,MaPhong,MaDV,NgayGioKham,ThoiLuongDuKien,UuTien,LyDoKham,TrangThai,NgayTao)
                    VALUES(@prefix+'APPT',@prefix+'BN3',@prefix+'A',@prefix+'CA',@room,@service,DATEADD(minute,2,SYSDATETIME()),15,N'Bình thường',N'FIXTURE',N'Đã xác nhận',SYSDATETIME());
                    """,new{prefix,specialty=data.Specialty,room=data.Room,service=data.Service},setup);
                setup.Commit();
            }
            PhienDangNhap.MaNV=prefix+"R";PhienDangNhap.KhoiTaoDinhDanhNghiepVu(prefix+"TKR");
            var user=new BUS_QUYENNGHIEPVU().LayNguCanh(prefix+"R");
            YeuCauTiepNhan Request(string patient)=>new() {MaBN=prefix+patient,MaDV=data.Service,MaBS=prefix+"A",MaPhong=data.Room,LyDoKham="FIXTURE không dùng thật"};
            KetQuaTiepNhan[] Pair(YeuCauTiepNhan first,YeuCauTiepNhan second)
            {
                using var start=new Barrier(2);
                Task<KetQuaTiepNhan> Send(YeuCauTiepNhan request)=>Task.Run(()=>
                {
                    if(!start.SignalAndWait(TimeSpan.FromSeconds(10)))throw new Exception("Hai quầy chưa cùng bắt đầu.");
                    return new BUS_TIEPNHAN().TiepNhan(request,user);
                });
                return Task.WhenAll(Send(first),Send(second)).GetAwaiter().GetResult();
            }
            var direct=Pair(Request("BN1"),Request("BN2"));
            if(direct[0].MaPhieu==direct[1].MaPhieu || direct[0].SoThuTu==direct[1].SoThuTu || Math.Abs(direct[0].SoThuTu-direct[1].SoThuTu)!=1)
                throw new Exception("Hai quầy cấp trùng hoặc sai số cùng ngày/phòng.");
            Console.WriteLine("PASS: Hai quầy/hai kết nối cùng ngày/phòng cấp hai số liên tiếp khác nhau");
            var appointment=Request("BN3") with {MaLich=prefix+"APPT"};var same=Pair(appointment,appointment);
            if(same[0]!=same[1])throw new Exception("Một lịch sinh hai lượt tiếp nhận.");
            Console.WriteLine("PASS: Hai quầy nhận cùng lịch trả cùng một MaPhieu");
            if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.HO_SO_BENH_AN WHERE MaBN LIKE @pattern",new{pattern=prefix+"%"})!=3 ||
               db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH WHERE MaNVTiepNhan=@receiver",new{receiver=user.MaNV})!=3)
                throw new Exception("Trùng hoặc mất hồ sơ/phiếu trong cạnh tranh.");
            Console.WriteLine("PASS: Chỉ lập ba hồ sơ/ba phiếu cho ba BN trùng tên, chọn theo mã");
            var blocked=false;
            using(var caller=MigrationChecks.Open())
            using(var tx=caller.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                new BUS_TIEPNHAN(tx).TiepNhan(Request("BN4"),user);
                using var changer=MigrationChecks.Open();
                try {changer.Execute("SET LOCK_TIMEOUT 700; UPDATE dbo.LICH_LAM_VIEC SET TrangThai=N'Hủy' WHERE MaLLV=@id",new{id=prefix+"LLV"},commandTimeout:5);}
                catch(Microsoft.Data.SqlClient.SqlException ex)when(ex.Number==1222){blocked=true;}
                finally{tx.Rollback();}
            }
            db.Execute("UPDATE dbo.LICH_LAM_VIEC SET TrangThai=N'Đã xác nhận' WHERE MaLLV=@id",new{id=prefix+"LLV"});
            if(!blocked)throw new Exception("ReadCommitted cho đổi lịch sau kiểm tra nhưng trước commit tiếp nhận.");
            Console.WriteLine("PASS: Transaction ReadCommitted giữ khóa lịch đến hết tiếp nhận");
        }
        finally
        {
            Thread.CurrentPrincipal=prior;
            using var cleanup=db.BeginTransaction();
            db.Execute("""
                DELETE dbo.NHAT_KY_THAO_TAC WHERE MaTK IN(@prefix+'TKA',@prefix+'TKR');
                DELETE dbo.PHIEU_KHAM_BENH WHERE MaNVTiepNhan=@prefix+'R';
                DELETE dbo.HO_SO_BENH_AN WHERE MaBN IN(@prefix+'BN1',@prefix+'BN2',@prefix+'BN3',@prefix+'BN4');
                DELETE dbo.LICH_KHAM WHERE MaLich=@prefix+'APPT';
                DELETE dbo.BENH_NHAN WHERE MaBN IN(@prefix+'BN1',@prefix+'BN2',@prefix+'BN3',@prefix+'BN4');
                DELETE dbo.LICH_LAM_VIEC WHERE MaLLV=@prefix+'LLV';
                DELETE dbo.CA_LAM WHERE MaCa=@prefix+'CA';
                DELETE dbo.TAI_KHOAN WHERE MaTK IN(@prefix+'TKA',@prefix+'TKR');
                DELETE dbo.NHAN_VIEN WHERE MaNV IN(@prefix+'A',@prefix+'R');
                """,new{prefix},cleanup);
            cleanup.Commit();
        }
        Console.WriteLine("PASS: Fixture commit cho kiểm tra đồng thời đã dọn sạch");
        return 0;
    }
}
