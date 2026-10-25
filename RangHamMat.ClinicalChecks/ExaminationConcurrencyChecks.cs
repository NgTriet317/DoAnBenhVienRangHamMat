using BUS;
using Dapper;
using ET;
using RangHamMat.ClinicalUiChecks;
namespace RangHamMat.ClinicalChecks;

internal static class ExaminationConcurrencyChecks
{
    internal static int Run()
    {
        using var db=MigrationChecks.Open();var prior=Thread.CurrentPrincipal;
        try
        {
            using var f=new ReceptionFixture();
            var receptionist=db.QuerySingle<(string NV,string TK)>("SELECT TOP(1) n.MaNV,t.MaTK FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV WHERE n.MaChucVu=N'CV004' AND n.TrangThai=N'Đang làm việc' AND t.TrangThai=N'Đang hoạt động' ORDER BY n.MaNV");
            PhienDangNhap.MaNV=receptionist.NV;PhienDangNhap.KhoiTaoDinhDanhNghiepVu(receptionist.TK);var receiver=new BUS_QUYENNGHIEPVU().LayNguCanh(receptionist.NV);
            var receipt=new BUS_TIEPNHAN().TiepNhan(new(){MaBN=f.Patient,MaDV=f.Service,MaBS=f.Doctor,MaPhong=f.Room,LyDoKham="FIXTURE concurrency P05"},receiver);
            PhienDangNhap.MaNV=f.Doctor;PhienDangNhap.KhoiTaoDinhDanhNghiepVu(f.Account);var actor=new BUS_QUYENNGHIEPVU().LayNguCanh(f.Doctor);
            static T[] Pair<T>(Func<int,T> action)
            {
                using var start=new Barrier(2);
                Task<T> Send(int i)=>Task.Run(()=>{if(!start.SignalAndWait(TimeSpan.FromSeconds(10)))throw new Exception("Không bắt đầu được hai worker.");return action(i);});
                return Task.WhenAll(Send(1),Send(2)).GetAwaiter().GetResult();
            }
            var starts=Pair(_=>new BUS_KHAMDIEUTRI().BatDauKham(receipt.MaPhieu,actor));
            if(starts[0]!=starts[1]||db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.LICH_SU_KHAM_BENH WHERE MaPhieu=@id",new{id=receipt.MaPhieu})!=1)throw new Exception("Hai cửa sổ tạo hai MaLS.");
            Console.WriteLine("PASS: Hai kết nối bắt đầu một phiếu trả cùng MaLS");
            var original=new BUS_KHAMDIEUTRI().LayBanNhap(starts[0],actor);
            var outcome=Pair(i=>
            {
                try{new BUS_KHAMDIEUTRI().LuuKham(KhamVaKeHoachChecks.Request(original) with {ChanDoan="FIXTURE cửa sổ "+i},actor);return "saved";}
                catch(XungDotDuLieuKhamException){return "conflict";}
            });
            if(outcome.Count(o=>o=="saved")!=1||outcome.Count(o=>o=="conflict")!=1)throw new Exception("Không chặn ghi đè đồng thời.");
            Console.WriteLine("PASS: Hai kết nối lưu cùng snapshot chỉ một commit, một xung đột đúng loại");
            if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.TINH_TRANG_BENH WHERE MaLS=@id",new{id=starts[0]})!=1)throw new Exception("Lưu dở hoặc nhân tình trạng.");
            Console.WriteLine("PASS: Tình trạng thuộc lần khám duy nhất, không lưu dở");
        }
        finally{Thread.CurrentPrincipal=prior;}
        Console.WriteLine("PASS: Fixture đồng thời P05 đã dọn sạch");return 0;
    }
}
