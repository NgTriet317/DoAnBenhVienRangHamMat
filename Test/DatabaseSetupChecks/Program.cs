using System.Reflection;
using Microsoft.Win32;

// Không kết nối SQL và không chạm nhánh registry của ứng dụng đang cài.
const BindingFlags Internal = BindingFlags.Instance | BindingFlags.NonPublic;
string path = @"Software\RangHamMat\Tests\DatabaseSetup-" + Guid.NewGuid().ToString("N");
int passed = 0;
try
{
    var assembly = Assembly.Load("RangHamMat.Wpf");
    var type = assembly.GetType("RangHamMat.DichVuKhoiDongCSDL");
    Check(type is not null, "Có dịch vụ bắt buộc xác nhận SQL theo lần cài đặt");
    object Create() => Activator.CreateInstance(type!, Internal, null, [path], null)!;
    var service = Create();
    int restores = 0;
    Task<bool> Restore(bool connected)
    {
        restores++;
        return Task.FromResult(connected);
    }
    async Task<bool> NeedsSetup(bool connected) => await (Task<bool>)type!
        .GetMethod("CanHienCauHinhAsync", Internal)!.Invoke(service,
            [(Func<Task<bool>>)(() => Restore(connected))])!;
    void Confirm() => type!.GetMethod("XacNhanCauHinh", Internal)!.Invoke(service, null);
    void Write(object value, RegistryValueKind kind = RegistryValueKind.DWord)
    {
        using var key = Registry.CurrentUser.CreateSubKey(path);
        key.SetValue("CanCauHinhDatabase", value, kind);
    }

    Check(await NeedsSetup(true) && restores == 0,
        "Thiếu cờ: vẫn yêu cầu chọn SQL dù cấu hình có thể kết nối");
    Write(1);
    Check(await NeedsSetup(true) && restores == 0,
        "Cài mới: không thử tự kết nối trước khi người dùng xác nhận");
    service = Create();
    Check(await NeedsSetup(true) && restores == 0,
        "Đóng/hủy rồi mở lại vẫn yêu cầu cấu hình");

    Confirm();
    using (var key = Registry.CurrentUser.OpenSubKey(path))
        Check(key!.GetValue("CanCauHinhDatabase") is 0,
            "Lưu thành công mới ghi nhận đã xác nhận");
    service = Create();
    Check(!await NeedsSetup(true) && restores == 1,
        "Mở lại sau xác nhận: tự dùng kết nối hợp lệ");
    Check(await NeedsSetup(false) && restores == 2,
        "Kết nối đã xác nhận nhưng hỏng vẫn mở cấu hình");

    // Tái tạo tác động của MSI: xóa đúng nhánh cài đặt, rồi cài lại DWORD 1.
    Registry.CurrentUser.DeleteSubKeyTree(path);
    Write(1);
    service = Create();
    Check(await NeedsSetup(true) && restores == 2,
        "Gỡ/cài lại cùng phiên bản: bắt xác nhận lại dù kết nối cũ còn hợp lệ");
    Write("0", RegistryValueKind.String);
    Check(await NeedsSetup(true) && restores == 2,
        "Cờ sai kiểu không làm bỏ qua xác nhận");
    Write(3);
    Check(await NeedsSetup(true) && restores == 2,
        "Cờ khác 0 vẫn yêu cầu cấu hình");

    Write(0);
    var failure = new IOException("Lỗi nguồn kết nối kiểm thử");
    try
    {
        await (Task<bool>)type!.GetMethod("CanHienCauHinhAsync", Internal)!.Invoke(service,
            [(Func<Task<bool>>)(() => Task.FromException<bool>(failure))])!;
        throw new Exception("Lỗi khôi phục không được nuốt thành kết nối thành công.");
    }
    catch (IOException error) when (ReferenceEquals(error, failure))
    {
        Check(true, "Lỗi khôi phục được phát lại cho App xử lý");
    }

    Console.WriteLine($"RESULT: {passed} passed");
    return 0;
}
catch (Exception error)
{
    Console.WriteLine("FAIL: " + error.GetBaseException().Message);
    return 1;
}
finally
{
    Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
}

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    passed++;
    Console.WriteLine("PASS " + message);
}
