using BUS;
using ET;

namespace RangHamMat;

/// <summary>Dùng cùng cách kiểm tra quyền và khởi tạo nhật ký khi đăng nhập hoặc mở lại ứng dụng.</summary>
internal sealed class DichVuDangNhap
{
    /// <summary>
    /// Kho lưu phiên ghi nhớ đã mã hóa cho tài khoản Windows hiện tại.
    /// </summary>
    private readonly KhoPhienDangNhap kho;

    /// <summary>
    /// Sử dụng kho phiên được cấp hoặc tạo kho mặc định khi không truyền vào.
    /// </summary>
    /// <param name="kho">Kho phiên tùy chọn dùng để đọc, lưu và xóa thông tin ghi nhớ.</param>
    internal DichVuDangNhap(KhoPhienDangNhap? kho = null) => this.kho = kho ?? new KhoPhienDangNhap();

    /// <summary>
    /// Đọc phiên ghi nhớ, đối chiếu tài khoản và vai trò hiện tại trước khi khôi phục định danh nghiệp vụ.
    /// </summary>
    /// <returns>True khi đã thiết lập lại phiên; false nếu không có phiên hợp lệ, đồng thời xóa bản ghi nhớ không còn khớp.</returns>
    internal bool ThuKhoiPhuc()
    {
        var saved = kho.Doc();
        if (saved is null) return false;
        var account = new BUS_TAIKHOAN().layTaiKhoanTheoMa(saved.MaTK);
        if (account is null || !saved.KhopTaiKhoan(account) || !ThuLayVaiTro(account, out var admin))
        {
            kho.Xoa();
            return false;
        }
        ThietLapPhien(account, admin);
        return true;
    }

    /// <summary>
    /// Thiết lập phiên của tài khoản hợp lệ rồi ghi nhớ đăng nhập; thu hồi phiên vừa tạo nếu lưu ghi nhớ thất bại.
    /// </summary>
    /// <param name="account">Tài khoản đã được nơi gọi xác thực mật khẩu; trạng thái và vai trò được kiểm tra lại tại đây.</param>
    internal void DangNhap(ET_TAIKHOAN account)
    {
        if (!account.TrangThai.Equals("Đang hoạt động", StringComparison.OrdinalIgnoreCase)
            || !ThuLayVaiTro(account, out var admin))
            throw new InvalidOperationException("Tài khoản bị khóa hoặc chưa được cấp vai trò hợp lệ.");
        ThietLapPhien(account, admin);
        try { kho.Luu(account); }
        catch
        {
            try { PhienNhatKy.KetThuc(); }
            finally { PhienDangNhap.XoaDinhDanh(); }
            throw;
        }
    }

    /// <summary>
    /// Xóa thông tin ghi nhớ để lần mở ứng dụng tiếp theo yêu cầu đăng nhập lại.
    /// </summary>
    internal void QuenPhien() => kho.Xoa();

    /// <summary>
    /// Chấp nhận vai trò Admin, Quản trị hoặc User theo danh mục vai trò hiện tại.
    /// </summary>
    /// <param name="account">Tài khoản có mã vai trò cần tra cứu.</param>
    /// <param name="admin">Nhận true nếu vai trò là Admin hoặc Quản trị.</param>
    /// <returns>True khi vai trò thuộc nhóm được phép đăng nhập.</returns>
    private static bool ThuLayVaiTro(ET_TAIKHOAN account, out bool admin)
    {
        var name = new BUS_VAITRO().layVaiTroTheoMa(account.MaVaiTro)?.TenVaiTro.Trim();
        admin = string.Equals(name, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Quản trị", StringComparison.OrdinalIgnoreCase);
        return admin || string.Equals(name, "User", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gán thông tin người dùng, tạo định danh nghiệp vụ và bắt đầu phiên nhật ký; xóa định danh nếu khởi tạo thất bại.
    /// </summary>
    /// <remarks>Phối hợp KhoiTaoDinhDanhNghiepVu và PhienNhatKy.BatDau để phiên thao tác có định danh và mã nhật ký.</remarks>
    /// <param name="account">Tài khoản được dùng làm nguồn định danh đã xác thực.</param>
    /// <param name="admin">Cho biết tài khoản đã được xác nhận thuộc vai trò quản trị.</param>
    private static void ThietLapPhien(ET_TAIKHOAN account, bool admin)
    {
        try
        {
            PhienDangNhap.MaNV = account.MaNV;
            PhienDangNhap.TenDangNhap = account.TenDangNhap;
            PhienDangNhap.IsAdmin = admin;
            PhienDangNhap.KhoiTaoDinhDanhNghiepVu(account.MaTK);
            PhienNhatKy.BatDau();
        }
        catch
        {
            PhienDangNhap.XoaDinhDanh();
            throw;
        }
    }
}
