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
    private readonly Func<string, ET_TAIKHOAN?> layTaiKhoan;
    private readonly Func<ET_TAIKHOAN, (bool HopLe, bool QuanTri)> layVaiTro;
    private readonly Action<Guid?> batDauPhien;

    /// <summary>
    /// Sử dụng kho phiên được cấp hoặc tạo kho mặc định khi không truyền vào.
    /// </summary>
    /// <param name="kho">Kho phiên tùy chọn dùng để đọc, lưu và xóa thông tin ghi nhớ.</param>
    internal DichVuDangNhap(KhoPhienDangNhap? kho = null) : this(kho,
        maTK => new BUS_TAIKHOAN().layTaiKhoanTheoMa(maTK),
        account => { var valid = ThuLayVaiTro(account, out var admin); return (valid, admin); },
        PhienNhatKy.BatDau) { }

    /// <summary>Tách nguồn SQL khỏi luồng xác thực, định danh và kho DPAPI.</summary>
    internal DichVuDangNhap(KhoPhienDangNhap? kho, Func<string, ET_TAIKHOAN?> layTaiKhoan,
        Func<ET_TAIKHOAN, (bool HopLe, bool QuanTri)> layVaiTro, Action<Guid?> batDauPhien)
    {
        this.kho = kho ?? new KhoPhienDangNhap();
        this.layTaiKhoan = layTaiKhoan;
        this.layVaiTro = layVaiTro;
        this.batDauPhien = batDauPhien;
    }

    /// <summary>
    /// Đọc phiên ghi nhớ, đối chiếu tài khoản và vai trò hiện tại trước khi khôi phục định danh nghiệp vụ.
    /// </summary>
    /// <returns>True khi đã thiết lập lại phiên; false nếu không có phiên hợp lệ, đồng thời xóa bản ghi nhớ không còn khớp.</returns>
    internal bool ThuKhoiPhuc()
    {
        var saved = kho.Doc();
        if (saved is null) return false;
        var account = layTaiKhoan(saved.MaTK);
        if (account is null || !saved.KhopTaiKhoan(account))
        {
            kho.Xoa();
            return false;
        }
        var role = layVaiTro(account);
        if (!role.HopLe) { kho.Xoa(); return false; }
        try { ThietLapPhien(account, role.QuanTri, saved.MaPhien); }
        catch (UnauthorizedAccessException)
        {
            // Chỉ SQL được quyền đổi token mới nhất. File từ máy đã bị thay phiên không được đăng nhập lại.
            kho.Xoa();
            return false;
        }
        LuuPhienHayHoanTac(account);
        return true;
    }

    /// <summary>
    /// Thiết lập phiên của tài khoản hợp lệ rồi ghi nhớ đăng nhập; thu hồi phiên vừa tạo nếu lưu ghi nhớ thất bại.
    /// </summary>
    /// <param name="account">Tài khoản đã được nơi gọi xác thực mật khẩu; trạng thái và vai trò được kiểm tra lại tại đây.</param>
    internal void DangNhap(ET_TAIKHOAN account)
    {
        var role = layVaiTro(account);
        if (!account.TrangThai.Equals("Đang hoạt động", StringComparison.OrdinalIgnoreCase)
            || !role.HopLe)
            throw new InvalidOperationException("Tài khoản bị khóa hoặc chưa được cấp vai trò hợp lệ.");
        ThietLapPhien(account, role.QuanTri);
        LuuPhienHayHoanTac(account);
    }

    private void LuuPhienHayHoanTac(ET_TAIKHOAN account)
    {
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
    /// Chấp nhận vai trò còn tồn tại trong SQL; chỉ các tên quản trị được xác định mới bật cờ Admin.
    /// </summary>
    /// <param name="account">Tài khoản có mã vai trò cần tra cứu.</param>
    /// <param name="admin">Nhận true nếu vai trò là Admin, Quản trị hoặc Quản trị viên.</param>
    /// <returns>True khi tài khoản vẫn được gán một vai trò hợp lệ trong SQL.</returns>
    private static bool ThuLayVaiTro(ET_TAIKHOAN account, out bool admin)
    {
        var name = new BUS_VAITRO().layVaiTroTheoMa(account.MaVaiTro)?.TenVaiTro.Trim();
        admin = string.Equals(name, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Quản trị", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Quản trị viên", StringComparison.OrdinalIgnoreCase);
        return !string.IsNullOrWhiteSpace(name);
    }

    /// <summary>
    /// Gán thông tin người dùng, tạo định danh nghiệp vụ và bắt đầu phiên nhật ký; xóa định danh nếu khởi tạo thất bại.
    /// </summary>
    /// <remarks>Phối hợp KhoiTaoDinhDanhNghiepVu và PhienNhatKy.BatDau để phiên thao tác có định danh và mã nhật ký.</remarks>
    /// <param name="account">Tài khoản được dùng làm nguồn định danh đã xác thực.</param>
    /// <param name="admin">Cho biết tài khoản đã được xác nhận thuộc vai trò quản trị.</param>
    private void ThietLapPhien(ET_TAIKHOAN account, bool admin, Guid? phienGhiNho = null)
    {
        try
        {
            PhienDangNhap.MaNV = account.MaNV;
            PhienDangNhap.TenDangNhap = account.TenDangNhap;
            PhienDangNhap.IsAdmin = admin;
            PhienDangNhap.KhoiTaoDinhDanhNghiepVu(account.MaTK);
            batDauPhien(phienGhiNho);
        }
        catch
        {
            PhienDangNhap.XoaDinhDanh();
            throw;
        }
    }
}
