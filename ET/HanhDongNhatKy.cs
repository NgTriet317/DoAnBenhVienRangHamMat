namespace ET;

/// <summary>
/// Cặp mã hành động lưu trong SQL và nhãn tiếng Việt cho bộ lọc, hiển thị nhật ký.
/// </summary>
public sealed record LuaChonHanhDongNhatKy(string Ma,string Ten);

/// <summary>Mã dùng để lọc/lưu SQL; nhãn tiếng Việt chỉ dùng khi trình bày.</summary>
public static class HanhDongNhatKy
{
    public static IReadOnlyList<LuaChonHanhDongNhatKy> LuaChon { get; }=Array.AsReadOnly(new LuaChonHanhDongNhatKy[]
    {
        new("","Tất cả hành động"),
        new("LOGIN_SUCCESS","Đăng nhập thành công"),new("LOGIN_FAILED","Đăng nhập thất bại"),new("LOGOUT","Đăng xuất"),
        new("CREATE","Thêm mới"),new("UPDATE","Cập nhật thông tin"),new("DELETE","Xóa"),
        new("DEACTIVATE","Ngừng sử dụng"),new("REACTIVATE","Sử dụng lại"),
        new("APPROVE","Phê duyệt"),new("CONFIRM","Xác nhận"),new("LOCK","Khóa"),new("UNLOCK","Mở khóa"),
        new("FINALIZE","Hoàn tất"),new("CANCEL","Hủy"),new("REASSIGN","Phân công lại"),
        new("ROLE_CHANGE","Thay đổi vai trò"),new("PERMISSION_CHANGE","Thay đổi quyền"),
        new("ACCOUNT_LOCK","Khóa tài khoản"),new("ACCOUNT_RESTORE","Khôi phục tài khoản")
    });
    /// <summary>
    /// Đổi mã hành động sang nhãn; giữ mã chưa biết để vẫn đọc được sự kiện cũ hoặc sự kiện chuyên biệt.
    /// </summary>
    public static string Nhan(string? ma)=>LuaChon.FirstOrDefault(x=>x.Ma==ma)?.Ten??ma??"";
}
