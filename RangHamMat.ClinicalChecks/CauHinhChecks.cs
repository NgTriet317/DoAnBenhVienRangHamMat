using System.Reflection;
using BUS;
using DAL;
using Dapper;
using ET;

namespace RangHamMat.ClinicalChecks;

internal static class CauHinhChecks
{
    internal static IEnumerable<(string, Action<Fixture>)> Cases()
    {
        foreach (var name in MigrationChecks.NewTables)
        {
            var suffix = name.Replace("_", "");
            yield return ($"ET/DAL/BUS cho {name}", f =>
            {
                Need(typeof(dbconnect).Assembly.GetType("DAL.DAL_" + suffix) is not null, "Thiếu DAL");
                Need(typeof(BUS_QUYENNGHIEPVU).Assembly.GetType("BUS.BUS_" + suffix) is not null, "Thiếu BUS");
            });
        }
        yield return ("Danh mục ghi/đọc qua BUS và audit đúng tài khoản", f =>
        {
            var user = f.User("X");
            var item = new ET_DMTINHTRANGBENH { TenTinhTrang="FIXTURE không dùng thật", TrangThai="Đang áp dụng" };
            Save(f, item, user);
            Need(!string.IsNullOrEmpty(item.MaDMTT), "Chưa cấp mã");
            var rows = (IReadOnlyList<ET_DMTINHTRANGBENH>)Call(f, item, "LayDanhSach", user)!;
            Need(rows.Any(r => r.MaDMTT == item.MaDMTT && r.TenTinhTrang == item.TenTinhTrang), "Roundtrip mất dữ liệu");
            Need(f.Scalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaDoiTuong=@id AND MaTK=@actor AND DuLieuSau LIKE N'%FIXTURE%'", new { id=item.MaDMTT, actor=user.MaTK })==1, "Thiếu audit");
            Need(f.Scalar<string?>("SELECT CONVERT(nvarchar(128),SESSION_CONTEXT(N'RHM.MaTK'))") is null, "Rò actor session context");
        });
        yield return ("Thu ngân không được sửa cấu hình hỗ trợ", f =>
        {
            var user=f.User("C");
            ExpectDenied(() => Save(f,new ET_NHOMTHUOCKEDON { TenNhom="FIXTURE",TrangThai="Đang áp dụng" },user));
        });
        yield return ("Cấu hình kiểm tra lại tài khoản đã khóa", f =>
        {
            var user=f.User("X");
            f.Execute("UPDATE dbo.TAI_KHOAN SET TrangThai=N'Đã khóa' WHERE MaTK=@MaTK",new{user.MaTK});
            ExpectDenied(() => Save(f,new ET_NHOMTHUOCKEDON { TenNhom="FIXTURE",TrangThai="Đang áp dụng" },user));
        });
        yield return ("Cấu hình nháp không nhận chữ ký do client cung cấp", f =>
        {
            var user=f.User("X");
            var drug=f.Scalar<string>("SELECT TOP(1) MaThuoc FROM dbo.THUOC ORDER BY MaThuoc");
            try
            {
                Save(f,new ET_CAUHINHTHUOCKEDON { MaThuoc=drug,PhienBan=1,TrangThai="Đã duyệt",MaNguoiDuyet=f.Id("A"),NgayDuyet=DateTime.Today },user);
                throw new Exception("Chấp nhận giả chữ ký");
            }
            catch(InvalidOperationException ex) when(ex.Message.StartsWith("Chỉ lưu cấu hình nháp")) { }
        });
        yield return ("Cập nhật cấu hình không ghi đè snapshot đã cũ", f =>
        {
            var user=f.User("X");
            var item=new ET_NHOMTHUOCKEDON { TenNhom="FIXTURE",TrangThai="Đang áp dụng" };
            Save(f,item,user);
            var old=new ET_NHOMTHUOCKEDON { MaNhomThuoc=item.MaNhomThuoc,TenNhom=item.TenNhom,TrangThai=item.TrangThai };
            f.Execute("EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@actor; UPDATE dbo.NHOM_THUOC_KE_DON SET TenNhom=N'FIXTURE changed' WHERE MaNhomThuoc=@MaNhomThuoc; EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=NULL;",new{item.MaNhomThuoc,actor=user.MaTK});
            try { Save(f,item,user,old); throw new Exception("Ghi đè bản thay đổi"); }
            catch(InvalidOperationException ex) when(ex.Message.StartsWith("Bản ghi đã thay đổi")) { }
        });
    }

    internal static void Save<T>(Fixture f,T item,NguCanhNghiepVu user,T? expected=default) where T:class
        => Call(f,item,"LuuNhap",item,user,expected);

    private static object? Call<T>(Fixture f,T item,string method,params object?[] values)
    {
        var type=typeof(BUS_QUYENNGHIEPVU).Assembly.GetType("BUS.BUS_"+typeof(T).Name[3..])
            ?? throw new InvalidOperationException("Thiếu BUS_"+typeof(T).Name[3..]);
        var service=Activator.CreateInstance(type,f.Transaction)!;
        try { return type.GetMethod(method)!.Invoke(service,values); }
        catch(TargetInvocationException ex) when(ex.InnerException is not null)
        { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); throw; }
    }
    private static void ExpectDenied(Action action)
    { try { action(); } catch(TuChoiQuyenNghiepVuException) { return; } throw new Exception("Phải từ chối bằng lỗi quyền"); }
    internal static void Need(bool condition,string name) { if(!condition)throw new Exception(name); }
}
