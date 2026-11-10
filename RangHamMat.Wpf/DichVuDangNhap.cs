using BUS;
using ET;

namespace RangHamMat;

/// <summary>Dùng cùng cách kiểm tra quyền và khởi tạo nhật ký khi đăng nhập hoặc mở lại ứng dụng.</summary>
internal sealed class DichVuDangNhap
{
    private readonly KhoPhienDangNhap kho;

    internal DichVuDangNhap(KhoPhienDangNhap? kho = null) => this.kho = kho ?? new KhoPhienDangNhap();

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

    internal void QuenPhien() => kho.Xoa();

    private static bool ThuLayVaiTro(ET_TAIKHOAN account, out bool admin)
    {
        var name = new BUS_VAITRO().layVaiTroTheoMa(account.MaVaiTro)?.TenVaiTro.Trim();
        admin = string.Equals(name, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Quản trị", StringComparison.OrdinalIgnoreCase);
        return admin || string.Equals(name, "User", StringComparison.OrdinalIgnoreCase);
    }

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
