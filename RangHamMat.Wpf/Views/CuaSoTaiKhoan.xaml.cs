using BUS;
using ET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Views;

// Biểu mẫu nhúng trực tiếp vào tab Tài khoản hoặc Vai trò, không mở Window/ShowDialog.
public partial class CuaSoTaiKhoan : UserControl
{
    private readonly BUS_TAIKHOAN busTaiKhoan = new();
    private readonly BUS_VAITRO busVaiTro = new();
    private readonly BUS_NHANVIEN busNhanVien = new();
    private readonly BUS_KHOA busKhoa = new();
    private readonly BUS_PHONG busPhong = new();
    private List<ET_PHONG> danhSachPhong = new();
    private bool dangNapPhamVi;
    private readonly BUS_QUANLYTAIKHOAN busQuanLy = new();
    private ET_TAIKHOAN? taiKhoanCu;
    private ET_VAITRO? vaiTroCu;
    private bool dangSuaTaiKhoan;

    public event Action<string>? LuuThanhCong;
    public event Action? HuyThayDoi;
    public bool DaLuu { get; private set; }
    public string MaVuaLuu { get; private set; } = "";

    public CuaSoTaiKhoan() => InitializeComponent();

    public void MoTaiKhoan(string? maTK = null)
    {
        DaLuu = false;
        MaVuaLuu = "";
        dangSuaTaiKhoan = !string.IsNullOrWhiteSpace(maTK);
        vaiTroCu = null;
        pnlTaiKhoan.Visibility = Visibility.Visible;
        pnlVaiTro.Visibility = Visibility.Collapsed;
        lblTieuDe.Text = dangSuaTaiKhoan ? "Sửa thông tin tài khoản" : "Thêm tài khoản mới";
        lblHuongDan.Text = dangSuaTaiKhoan
            ? "Chỉnh sửa trực tiếp. Trạng thái chỉ đổi qua nút Khóa / Khôi phục bên dưới."
            : "Tạo mới bằng chính biểu mẫu này; trạng thái mặc định đang hoạt động.";
        lblLoi.Text = "";

        taiKhoanCu = dangSuaTaiKhoan
            ? busTaiKhoan.layTaiKhoanTheoMa(maTK!) ?? throw new InvalidOperationException("Tài khoản không còn tồn tại.")
            : null;

        cboVaiTro.Items.Clear();
        foreach (var vt in busVaiTro.layToanBoVaiTro().OrderBy(x => x.TenVaiTro))
            cboVaiTro.Items.Add(new ComboBoxItem { Content = vt.TenVaiTro, Tag = vt.MaVaiTro });

        cboNhanVien.Items.Clear();
        var daCoTaiKhoan = busTaiKhoan.layToanBoTaiKhoan()
            .Where(x => x.MaTK != maTK)
            .Select(x => x.MaNV)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var nv in busNhanVien.layToanBoNhanVien().OrderBy(x => x.HoTen))
            if (!daCoTaiKhoan.Contains(nv.MaNV))
                cboNhanVien.Items.Add(new ComboBoxItem
                {
                    Content = nv.HoTen + " (" + nv.MaNV + ")",
                    Tag = nv.MaNV
                });

        txtMaTaiKhoan.Text = taiKhoanCu?.MaTK ?? "Tự sinh khi lưu";
        txtTenDangNhap.Text = taiKhoanCu?.TenDangNhap ?? "";
        // Chọn khoa hoặc phòng; khi theo phòng, khoa dùng để lọc và không lưu cùng MaPhong.
        dangNapPhamVi = true;
        try
        {
            cboKhoa.Items.Clear();
            foreach (var khoa in busKhoa.layToanBoKhoa().OrderBy(x => x.TenKhoa))
                cboKhoa.Items.Add(new ComboBoxItem
                {
                    Content = khoa.TenKhoa + " (" + khoa.MaKhoa + ")",
                    Tag = khoa.MaKhoa
                });

            danhSachPhong = busPhong.layToanBoPhong().ToList();
            var phongDaGan = danhSachPhong.FirstOrDefault(x => x.MaPhong == taiKhoanCu?.MaPhong);
            var maKhoaDeChon = taiKhoanCu?.MaKhoa ?? phongDaGan?.MaKhoa;
            rbTheoPhong.IsChecked = !string.IsNullOrWhiteSpace(taiKhoanCu?.MaPhong);
            rbTheoKhoa.IsChecked = !string.IsNullOrWhiteSpace(taiKhoanCu?.MaKhoa);
            rbKhongLienKet.IsChecked = string.IsNullOrWhiteSpace(taiKhoanCu?.MaKhoa)
                && string.IsNullOrWhiteSpace(taiKhoanCu?.MaPhong);
            ChonCombo(cboKhoa, maKhoaDeChon);
            NapPhongTheoKhoa(taiKhoanCu?.MaPhong);
        }
        finally { dangNapPhamVi = false; }
        CapNhatPhamVi();
        lblTrangThai.Text = taiKhoanCu?.TrangThai ?? "Đang hoạt động";
        lblTrangThai.Foreground = string.Equals(lblTrangThai.Text, "Đang hoạt động", StringComparison.OrdinalIgnoreCase)
            ? Brushes.ForestGreen : Brushes.Firebrick;
        pwdMatKhau.Clear();
        pwdXacNhan.Clear();
        ChonCombo(cboVaiTro, taiKhoanCu?.MaVaiTro);
        ChonCombo(cboNhanVien, taiKhoanCu?.MaNV);
        lblMatKhau.Text = dangSuaTaiKhoan
            ? "Nếu không muốn đổi mật khẩu, để trống cả hai ô. Khi đổi phải có ít nhất 8 ký tự gồm chữ và số."
            : "Tài khoản mới cần mật khẩu ít nhất 8 ký tự, gồm chữ và số.";
        btnLuu.IsEnabled = PhienDangNhap.IsAdmin;
    }

    public void MoVaiTro(string? maVaiTro = null)
    {
        DaLuu = false;
        MaVuaLuu = "";
        taiKhoanCu = null;
        pnlTaiKhoan.Visibility = Visibility.Collapsed;
        pnlVaiTro.Visibility = Visibility.Visible;
        lblTieuDe.Text = string.IsNullOrWhiteSpace(maVaiTro) ? "Thêm vai trò mới" : "Sửa thông tin vai trò";
        lblHuongDan.Text = "Thêm và sửa dùng chung một biểu mẫu, không có cửa sổ bật lên.";
        lblLoi.Text = "";

        vaiTroCu = string.IsNullOrWhiteSpace(maVaiTro) ? null :
            busVaiTro.layVaiTroTheoMa(maVaiTro) ?? throw new InvalidOperationException("Vai trò không còn tồn tại.");
        txtMaVaiTro.Text = vaiTroCu?.MaVaiTro ?? "Tự sinh khi lưu";
        txtTenVaiTro.Text = vaiTroCu?.TenVaiTro ?? "";
        txtMoTa.Text = vaiTroCu?.MoTa ?? "";
        btnLuu.IsEnabled = PhienDangNhap.IsAdmin;
    }

    // Mọi khoa/phòng đều lấy từ BUS: cho phép hiển thị cả liên kết lịch sử khi sửa.
    private void NapPhongTheoKhoa(string? maPhong = null)
    {
        var maKhoa = (cboKhoa.SelectedItem as ComboBoxItem)?.Tag as string;
        cboPhong.Items.Clear();
        if (string.IsNullOrWhiteSpace(maKhoa)) return;
        foreach (var phong in danhSachPhong
            .Where(x => string.Equals(x.MaKhoa, maKhoa, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.TenPhong))
            cboPhong.Items.Add(new ComboBoxItem
            {
                Content = phong.TenPhong + " (" + phong.MaPhong + ")",
                Tag = phong.MaPhong
            });
        ChonCombo(cboPhong, maPhong);
    }

    private void CapNhatPhamVi()
    {
        if (cboKhoa is null || cboPhong is null || pnlChonPhong is null) return;
        bool theoKhoa = rbTheoKhoa.IsChecked == true;
        bool theoPhong = rbTheoPhong.IsChecked == true;
        pnlChonKhoa.Visibility = theoKhoa || theoPhong ? Visibility.Visible : Visibility.Collapsed;
        pnlChonPhong.Visibility = theoPhong ? Visibility.Visible : Visibility.Collapsed;
        lblChonKhoa.Text = theoPhong ? "Chọn khoa để lọc phòng" : "Khoa liên kết";
        cboKhoa.IsEnabled = theoKhoa || theoPhong;
        cboPhong.IsEnabled = theoPhong;
    }

    private void PhamVi_Checked(object sender, RoutedEventArgs e)
    {
        if (dangNapPhamVi) return;
        CapNhatPhamVi();
    }

    private void cboKhoa_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dangNapPhamVi || cboPhong is null) return;
        NapPhongTheoKhoa();
    }

    private static void ChonCombo(ComboBox combo, string? ma)
    {
        combo.SelectedIndex = -1;
        foreach (var item in combo.Items.OfType<ComboBoxItem>())
            if (string.Equals(item.Tag as string, ma, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                break;
            }
    }

    private static string MaNguoiSua() => (Thread.CurrentPrincipal as ClaimsPrincipal)?
        .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    private void Luu_Click(object sender, RoutedEventArgs e)
    {
        if (!PhienDangNhap.IsAdmin) return;
        lblLoi.Text = "";
        btnLuu.IsEnabled = false;
        try
        {
            if (pnlTaiKhoan.Visibility == Visibility.Visible)
            {
                var matKhau = pwdMatKhau.Password;
                if (matKhau != pwdXacNhan.Password)
                    throw new InvalidOperationException("Mật khẩu xác nhận không khớp.");
                if (!dangSuaTaiKhoan && matKhau.Length == 0)
                    throw new InvalidOperationException("Tài khoản mới phải có mật khẩu.");
                if (cboVaiTro.SelectedItem is not ComboBoxItem vt || cboNhanVien.SelectedItem is not ComboBoxItem nv)
                    throw new InvalidOperationException("Chọn nhân viên và vai trò trước khi lưu.");

                // Theo ràng buộc CK_TAI_KHOAN_1, mỗi tài khoản có MaKhoa HOẶC MaPhong.
                string? maKhoaLienKet = null;
                string? maPhongLienKet = null;
                if (rbTheoKhoa.IsChecked == true)
                {
                    maKhoaLienKet = (cboKhoa.SelectedItem as ComboBoxItem)?.Tag as string;
                    if (string.IsNullOrWhiteSpace(maKhoaLienKet))
                        throw new InvalidOperationException("Vui lòng chọn khoa liên kết.");
                }
                else if (rbTheoPhong.IsChecked == true)
                {
                    if (cboKhoa.SelectedItem is not ComboBoxItem)
                        throw new InvalidOperationException("Vui lòng chọn khoa để lọc phòng.");
                    maPhongLienKet = (cboPhong.SelectedItem as ComboBoxItem)?.Tag as string;
                    if (string.IsNullOrWhiteSpace(maPhongLienKet))
                        throw new InvalidOperationException("Vui lòng chọn phòng liên kết.");
                }

                var entity = new ET_TAIKHOAN
                {
                    MaTK = taiKhoanCu?.MaTK ?? "",
                    TenDangNhap = txtTenDangNhap.Text.Trim(),
                    MatKhauHash = taiKhoanCu?.MatKhauHash ?? "",
                    MaVaiTro = vt.Tag as string ?? "",
                    MaNV = nv.Tag as string ?? "",
                    MaKhoa = maKhoaLienKet,
                    MaPhong = maPhongLienKet,
                    // Không cho lưu thông tin chung tự ý thay trạng thái.
                    TrangThai = taiKhoanCu?.TrangThai ?? "Đang hoạt động"
                };
                busQuanLy.luuTaiKhoan(MaNguoiSua(), entity, taiKhoanCu, matKhau);
                MaVuaLuu = entity.MaTK;
                pwdMatKhau.Clear();
                pwdXacNhan.Clear();
            }
            else
            {
                var entity = new ET_VAITRO
                {
                    MaVaiTro = vaiTroCu?.MaVaiTro ?? "",
                    TenVaiTro = txtTenVaiTro.Text.Trim(),
                    MoTa = txtMoTa.Text.Trim()
                };
                busQuanLy.luuVaiTro(MaNguoiSua(), entity, vaiTroCu);
                MaVuaLuu = entity.MaVaiTro;
            }
            DaLuu = true;
            LuuThanhCong?.Invoke(MaVuaLuu);
        }
        catch (Exception ex)
        {
            lblLoi.Text = ex.Message;
        }
        finally
        {
            btnLuu.IsEnabled = PhienDangNhap.IsAdmin;
        }
    }

    private void Huy_Click(object sender, RoutedEventArgs e)
    {
        lblLoi.Text = "";
        HuyThayDoi?.Invoke();
    }
}
