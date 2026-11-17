using System.Windows;
using System.Windows.Threading;
using BUS;
using ET;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat;

public partial class MainWindow
{
    private CancellationTokenSource? giamSatPhienCancellation;

    /// <summary>Buộc rời tài khoản cũ; App chuyển về đăng nhập dù biểu mẫu còn nháp.</summary>
    internal bool PhienBiThayThe { get; private set; }

    internal Action<string> ThongBaoMatPhien { get; set; } = message => MessageBox.Show(message,
        "Phiên đăng nhập", MessageBoxButton.OK, MessageBoxImage.Warning);
    internal Action XoaPhienGhiNho { get; set; } = () => new DichVuDangNhap().QuenPhien();

    private void KhoiTaoGiamSatPhien()
    {
        Loaded += (_, _) => BatDauGiamSatPhien();
        Closed += (_, _) => DungGiamSatPhien();
    }

    private void BatDauGiamSatPhien()
    {
        if (giamSatPhienCancellation is not null || PhienBiThayThe || !PhienDangNhap.DaDangNhap
            || PhienDangNhap.MaPhien == Guid.Empty) return;
        var source = new CancellationTokenSource();
        giamSatPhienCancellation = source;
        // Cố định database/tài khoản/token của phiên, độc lập cấu hình và chu kỳ Change Tracking.
        var connection = DAL.CauHinhKetNoi.LayChuoiKetNoi();
        _ = TheoDoiPhienDangNhapAsync(connection, PhienDangNhap.MaPhien, PhienDangNhap.MaTK, source.Token);
    }

    private async Task TheoDoiPhienDangNhapAsync(string connection, Guid session, string account, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var status = await new BUS_NHATKY().KiemTraPhienHienTaiAsync(connection, session, account, token);
                token.ThrowIfCancellationRequested();
                if (PhienDangNhap.MaPhien != session || PhienDangNhap.MaTK != account) return;
                if (status != TrangThaiPhienHienTai.DangHoatDong)
                {
                    BuocDangXuatDoMatPhien(status == TrangThaiPhienHienTai.DaDangNhapMayKhac
                        ? ThongBaoPhien.DangNhapMayKhac : "Phiên đăng nhập đã hết hiệu lực. Vui lòng đăng nhập lại.");
                    return;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested) return;
                // Mất mạng/lỗi SQL không phải bằng chứng token bị thu hồi. Giữ phiên và thử lại.
                ShowSessionError("Không thể kiểm tra phiên đăng nhập: " + ex.Message);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(2), token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
        }
    }

    private void DungGiamSatPhien()
    {
        var source = giamSatPhienCancellation;
        giamSatPhienCancellation = null;
        if (source is null) return;
        source.Cancel();
        source.Dispose();
    }

    internal void BuocDangXuatDoPhienBiThayThe() => BuocDangXuatDoMatPhien(ThongBaoPhien.DangNhapMayKhac);

    private void BuocDangXuatDoMatPhien(string message)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => BuocDangXuatDoMatPhien(message)), DispatcherPriority.Send);
            return;
        }
        if (PhienBiThayThe) return;
        PhienBiThayThe = true;
        daKhoaPhien = true;
        IsEnabled = false;
        giamSatTrangThai?.Stop();
        DungGiamSatPhien();
        DungDongBoDoMatPhien();
        try { XoaPhienGhiNho(); }
        catch (Exception ex) { ShowSessionError("Không thể xóa phiên ghi nhớ: " + ex.Message); }

        // Tháo toàn bộ nháp trước khi Closing/đổi principal chạy các guard điều hướng.
        // Không gọi Save/Discard: các thao tác đó có thể ghi hoặc đọc lại SQL sau khi bị thu hồi.
        var owned = ClinicalWindowLifecycle.Owned(this).ToArray();
        foreach (var child in owned)
        {
            child.IsEnabled = false;
            child.Content = null;
        }
        AppHost.Content = null;
        PhienNhatKy.BoPhien();
        PhienDangNhap.XoaDinhDanh();
        foreach (var child in owned.Reverse()) child.Close();
        // Hoàn tất thông báo trước Closed để Dispatcher không mở login bên trong modal thông báo.
        try { ThongBaoMatPhien(message); }
        finally { Close(); }
    }
}
