using System.Windows.Threading;
using BUS;
using ET;

namespace RangHamMat;

/// <summary>Heartbeat mỗi phút, hết hạn sau 5 phút tại SQL; không suy diễn thời điểm crash.</summary>
internal static class PhienNhatKy
{
    /// <summary>
    /// Bộ hẹn giờ trên Dispatcher gửi heartbeat mỗi phút cho phiên nhật ký.
    /// </summary>
    private static DispatcherTimer? timer;
    /// <summary>
    /// Ngăn các lần gửi heartbeat chạy chồng nhau khi lần trước chưa kết thúc.
    /// </summary>
    private static bool running;
    /// <summary>
    /// Thông báo lỗi cập nhật phiên để cửa sổ chính hiển thị cho người dùng.
    /// </summary>
    public static event Action<string>? LoiPhien;
    /// <summary>
    /// Tạo phiên nhật ký nếu chưa có, gắn mã phiên vào định danh và bắt đầu bộ hẹn giờ heartbeat.
    /// </summary>
    internal static void BatDau(Guid? phienGhiNho = null)
    {
        if(PhienDangNhap.MaPhien!=Guid.Empty)return;
        var actor=new BUS_QUYENNGHIEPVU().LayNguCanh(PhienDangNhap.MaNV);
        var bus = new BUS_NHATKY();
        PhienDangNhap.MaPhien = phienGhiNho is Guid saved
            ? bus.KhoiPhucPhien(saved, actor) : bus.BatDauPhien(actor);
        PhienDangNhap.GanPhienAudit(PhienDangNhap.MaPhien);
        timer=new DispatcherTimer{Interval=TimeSpan.FromMinutes(1)};
        timer.Tick+=Tick;timer.Start();
    }
    /// <summary>
    /// Gửi heartbeat ở tác vụ nền cho phiên hiện tại; phát LoiPhien khi cập nhật thất bại và luôn nhả cờ đang chạy.
    /// </summary>
    private static async void Tick(object? sender,EventArgs e)
    {
        if(running||PhienDangNhap.MaPhien==Guid.Empty)return;
        running=true;
        var id=PhienDangNhap.MaPhien;var account=PhienDangNhap.MaTK;
        var actor=new NguCanhNghiepVu{MaNV=PhienDangNhap.MaNV,MaTK=account};
        var principal=new System.Security.Claims.ClaimsPrincipal(
            (Thread.CurrentPrincipal as System.Security.Claims.ClaimsPrincipal)?.Identities.Select(x=>x.Clone())
            ?? Enumerable.Empty<System.Security.Claims.ClaimsIdentity>());
        try
        {
            await Task.Run(()=>
            {
                var previous=Thread.CurrentPrincipal;
                try{Thread.CurrentPrincipal=principal;new BUS_NHATKY().Heartbeat(id,actor);}
                finally{Thread.CurrentPrincipal=previous;}
            });
        }
        catch(Exception ex){if(PhienDangNhap.MaPhien==id&&PhienDangNhap.MaTK==account)LoiPhien?.Invoke("Không thể cập nhật phiên: "+ex.Message);}
        finally{running=false;}
    }
    /// <summary>
    /// Ghi kết thúc phiên và luôn dừng bộ hẹn giờ, xóa mã phiên cùng claim nhật ký dù thao tác ghi lỗi.
    /// </summary>
    internal static void KetThuc()
    {
        if(PhienDangNhap.MaPhien==Guid.Empty)return;
        try { new BUS_NHATKY().KetThucPhien(PhienDangNhap.MaPhien); }
        finally
        {
            timer?.Stop();timer=null;PhienDangNhap.MaPhien=Guid.Empty;
            PhienDangNhap.GanPhienAudit(Guid.Empty);
        }
    }

    /// <summary>Dừng phiên đã bị SQL thu hồi; không gửi thêm heartbeat hay ghi đăng xuất bằng token cũ.</summary>
    internal static void BoPhien()
    {
        timer?.Stop();
        if (timer is not null) timer.Tick -= Tick;
        timer = null;
        PhienDangNhap.MaPhien = Guid.Empty;
        if (PhienDangNhap.DaDangNhap) PhienDangNhap.GanPhienAudit(Guid.Empty);
    }
}
