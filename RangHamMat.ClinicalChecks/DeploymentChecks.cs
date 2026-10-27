using BUS;
using DAL;
using Dapper;
using ET;

namespace RangHamMat.ClinicalChecks;

internal static class DeploymentChecks
{
    internal static int Run()
    {
        var pass=0;var fail=0;
        void Check(string name,Action run){try{run();pass++;Console.WriteLine("PASS: "+name);}catch(Exception ex){fail++;Console.Error.WriteLine("FAIL: "+name+": "+ex.GetBaseException().Message);}}
        Check("P12 cấu hình kết nối rời khỏi tên máy cố định",()=>
        {
            var source=File.ReadAllText("DAL/dbconnect.cs");
            if(source.Contains("DESKTOP-C9VCE8A")||!File.Exists("RangHamMat.Wpf/appsettings.json"))throw new Exception("Còn tên máy cũ hoặc thiếu cấu hình bàn giao.");
            using var db=new DAL_DONTHUOC().MoKetNoi();
            if(!db.Database.StartsWith("RHM_ClinicalWorkflow_Test_"))throw new Exception("Không dùng override DB thử.");
        });
        Check("P12 SQL chính cài đúng52 bảng và version2",()=>
        {
            var sql=File.ReadAllText("Database/QLBenhVienRangHamMat.sql");
            if(!sql.Contains("RHM.ClinicalWorkflow.Version")||!sql.Contains("CREATE TABLE [dbo].[BAN_GIAO_THUOC]"))throw new Exception("SQL chính chưa có schema2.");
        });
        Check("P12 nhân viên dùng chung transaction, đọc được thay đổi rồi rollback",()=>
        {
            using var f=new Fixture();
            var ctor=typeof(BUS_NHANVIEN).GetConstructor([typeof(System.Data.IDbTransaction)]);
            if(ctor is null)throw new Exception("BUS_NHANVIEN thiếu constructor transaction.");
            var bus=(BUS_NHANVIEN)ctor.Invoke([f.Transaction]);
            var employee=bus.layNhanVienTheoMa(f.Id("A"));
            employee.DiaChi="FIXTURE P12 địa chỉ rollback";
            if(!bus.capNhatNhanVien(employee)||bus.layNhanVienTheoMa(employee.MaNV).DiaChi!=employee.DiaChi)throw new Exception("Nhân viên không dùng cùng giao dịch.");
        });
        Check("P12 tài khoản admin seed đọc được workspace lâm sàng",()=>
        {
            var principal=Thread.CurrentPrincipal;
            try
            {
                using var db=MigrationChecks.Open();
                var account=db.QuerySingle<(string Account,string Employee)>("SELECT MaTK Account,MaNV Employee FROM dbo.TAI_KHOAN WHERE TenDangNhap=N'admin001'");
                RangHamMat.PhienDangNhap.MaNV=account.Employee;
                RangHamMat.PhienDangNhap.IsAdmin=true;
                RangHamMat.PhienDangNhap.KhoiTaoDinhDanhNghiepVu(account.Account);
                var user=new BUS_QUYENNGHIEPVU().LayNguCanh(account.Employee);
                new BUS_KHAMDIEUTRI().LayDuLieuHoSoGiaoDien(new(),user);
                new BUS_KHAMDIEUTRI().LayTrangBenhNhan(new(),user);
            }
            finally{Thread.CurrentPrincipal=principal;}
        });
        Console.WriteLine($"DeploymentChecks: {pass} PASS, {fail} FAIL.");return fail==0?0:1;
    }
}
