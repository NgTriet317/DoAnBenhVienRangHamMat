using BUS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace RangHamMat;

public partial class MainWindow
{
    private readonly BUS_PHANQUYENVAITRO busKiemTraQuyen = new();
    private HashSet<string>? quyenPhanHeDaTai;
    private string maTaiKhoanDaTai = "";
    private int phienBanQuyenDaTai = -1;
    private DispatcherTimer? giamSatTrangThai;
    private bool daKhoaPhien;
    private DateTime lanKiemTraTrangThai = DateTime.MinValue;

    private static string MaTaiKhoanHienTai()
        => (Thread.CurrentPrincipal as ClaimsPrincipal)?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    // Kiểm tra quyền theo tài khoản và trạng thái đang hoạt động trong SQL.
    private bool CoQuyenMoPhanHe(string module)
    {
        if (daKhoaPhien) return false;
        var maTK = MaTaiKhoanHienTai();
        if (maTK.Length == 0) return false;
        try
        {
            // Không chỉ dựa vào quyền cache: tối đa 2 giây sẽ kiểm tra lại trạng thái SQL.
            if (DateTime.UtcNow - lanKiemTraTrangThai >= TimeSpan.FromSeconds(2))
            {
                lanKiemTraTrangThai = DateTime.UtcNow;
                if (!busKiemTraQuyen.taiKhoanConHoatDong(maTK))
                {
                    KetThucPhienBiKhoa();
                    return false;
                }
            }
            if (quyenPhanHeDaTai == null || maTK != maTaiKhoanDaTai
                || phienBanQuyenDaTai != BUS_PHANQUYENVAITRO.PhienBan)
            {
                quyenPhanHeDaTai = busKiemTraQuyen.layQuyenTheoTaiKhoan(maTK)
                    .ToHashSet(StringComparer.Ordinal);
                maTaiKhoanDaTai = maTK;
                phienBanQuyenDaTai = BUS_PHANQUYENVAITRO.PhienBan;
            }
            return quyenPhanHeDaTai.Contains(module);
        }
        catch (Exception)
        {
            // Không cấp quyền mặc định khi không đọc được SQL.
            quyenPhanHeDaTai = new HashSet<string>(StringComparer.Ordinal);
            maTaiKhoanDaTai = maTK;
            phienBanQuyenDaTai = BUS_PHANQUYENVAITRO.PhienBan;
            return false;
        }
    }

    internal void LamMoiDanhSachPhanHe()
    {
        quyenPhanHeDaTai = null;
        if (IsLoaded && !daKhoaPhien) FilterApps();
    }

    private void KetThucPhienBiKhoa()
    {
        if (daKhoaPhien) return;
        daKhoaPhien = true;
        giamSatTrangThai?.Stop();
        IsEnabled = false;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            MessageBox.Show("Tài khoản của bạn đã bị khóa. Phiên làm việc đã bị ngắt."
                + " Vui lòng đăng nhập lại sau khi được khôi phục.",
                "Tài khoản đã khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            Application.Current.Shutdown();
        }), DispatcherPriority.Normal);
    }

    // Cứ 6 giây đọc SQL: khóa từ máy khác làm hết hiệu lực phiên đang mở.
    // Không xóa các quyền của vai trò, chỉ ngắt phiên tài khoản bị khóa.
    private void BatDauGiamSatTaiKhoan()
    {
        giamSatTrangThai = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(6)
        };
        giamSatTrangThai.Tick += (_, _) =>
        {
            if (daKhoaPhien) return;
            var maTK = MaTaiKhoanHienTai();
            if (maTK.Length == 0) return;
            try
            {
                if (!busKiemTraQuyen.taiKhoanConHoatDong(maTK))
                {
                    KetThucPhienBiKhoa();
                    return;
                }

                // Đồng bộ quyền trên thiết bị này nếu người quản trị cấp/thu quyền ở máy khác.
                var quyenMoi = busKiemTraQuyen.layQuyenTheoTaiKhoan(maTK)
                    .ToHashSet(StringComparer.Ordinal);
                if (quyenPhanHeDaTai == null || maTK != maTaiKhoanDaTai ||
                    !quyenMoi.SetEquals(quyenPhanHeDaTai))
                {
                    quyenPhanHeDaTai = quyenMoi;
                    maTaiKhoanDaTai = maTK;
                    phienBanQuyenDaTai = BUS_PHANQUYENVAITRO.PhienBan;
                    if (IsLoaded) FilterApps();
                }
            }
            catch (Exception)
            {
                // Mất kết nối: không dùng quyền đã cache để mở phân hệ mới.
                quyenPhanHeDaTai = new HashSet<string>(StringComparer.Ordinal);
                maTaiKhoanDaTai = maTK;
                phienBanQuyenDaTai = BUS_PHANQUYENVAITRO.PhienBan;
                if (IsLoaded) FilterApps();
            }
        };
        Closed += (_, _) => giamSatTrangThai?.Stop();
        giamSatTrangThai.Start();
    }
}
