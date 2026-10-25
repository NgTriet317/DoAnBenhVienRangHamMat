using System.Reflection;
using Dapper;
using ET;
using Microsoft.Data.SqlClient;

namespace RangHamMat.ClinicalChecks;

internal static class MigrationChecks
{
    internal static readonly string[] NewTables = ["DM_TINH_TRANG_BENH", "MAU_KE_HOACH_DIEU_TRI", "CHI_TIET_MAU_KE_HOACH", "NHOM_THUOC_KE_DON", "CAU_HINH_THUOC_KE_DON", "BAN_GIAO_THUOC"];
    internal static readonly Dictionary<string, string[]> NewColumns = new()
    {
        ["TINH_TRANG_BENH"]=["MaDMTT","ViTriRang"],
        ["KE_HOACH_DIEU_TRI"]=["MaTTBNguon","MaMauKH","PhienBanMau"],
        ["CHI_TIET_KE_HOACH"]=["NoiDungBuoc"],
        ["LICH_SU_KHAM_BENH"]=["KhongKeThuoc","LyDoKhongKeThuoc"],
        ["DON_THUOC"]=["MaDTThayThe"],
        ["CHI_TIET_DON_THUOC"]=["DuongDung","TenThuocChot","HamLuongChot","DonViTinhChot","PhienBanHuongDan"],
        ["HOA_DON"]=["PhienBanQuyTrinh","MaYeuCau","DauVanTayYeuCau"],
        ["THANH_TOAN"]=["SoTienKhachDua","TienThua","MaThamChieu"]
    };

    internal static SqlConnection Open()
    {
        var value=Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING");
        if(string.IsNullOrWhiteSpace(value) || !new SqlConnectionStringBuilder(value).InitialCatalog.StartsWith("RHM_ClinicalWorkflow_Test_",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("MigrationChecks chỉ chạy trên bản sao RHM_ClinicalWorkflow_Test_* đã xác nhận.");
        var connection=new SqlConnection(value); connection.Open(); return connection;
    }

    public static int Run()
    {
        using var db=Open();
        return CheckContract(db);
    }

    internal static int CheckContract(SqlConnection db)
    {
        var failed=0; var passed=0;
        void Check(bool ok,string name) { if(ok){Console.WriteLine("PASS: "+name);passed++;}else{Console.Error.WriteLine("FAIL: "+name);failed++;} }
        Check(db.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0")==52,"Schema có 46 bảng cũ và đúng 6 bảng bổ sung");
        foreach(var table in NewTables)
        {
            Check(db.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.tables WHERE object_id=OBJECT_ID(@name)",new{name="dbo."+table})==1,"SQL table "+table);
            var entity=typeof(ET_BENHNHAN).Assembly.GetType("ET.ET_"+table.Replace("_",""));
            Check(entity?.GetCustomAttribute<Dapper.Contrib.Extensions.TableAttribute>(false)?.Name==table,"ET ánh xạ "+table);
        }
        foreach(var (table, columns) in NewColumns)
        {
            var actual=db.Query<string>("SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(@name)",new{name="dbo."+table}).ToHashSet();
            Check(columns.All(actual.Contains),"SQL thêm đủ cột "+table);
            var entity=typeof(ET_BENHNHAN).Assembly.GetType("ET.ET_"+table.Replace("_",""));
            Check(columns.All(name=>entity?.GetProperty(name) is not null),"ET thêm đủ cột "+table);
        }
        Check(db.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.extended_properties WHERE class=0 AND name=N'RHM.ClinicalWorkflow.Version' AND TRY_CONVERT(int,value)=2")==1,"Version guard đã ghi phiên bản 2");
        Console.WriteLine($"Migration contract: {passed} PASS, {failed} FAIL.");
        return failed==0?0:1;
    }
}
