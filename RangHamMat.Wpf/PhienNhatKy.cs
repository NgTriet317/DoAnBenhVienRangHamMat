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
    internal static void BatDau()
    {
        if(PhienDangNhap.MaPhien!=Guid.Empty)return;
        var actor=new BUS_QUYENNGHIEPVU().LayNguCanh(PhienDangNhap.MaNV);
        PhienDangNhap.MaPhien=new BUS_NHATKY().BatDauPhien(actor);
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
        try
        {
            var actor=new NguCanhNghiepVu{MaNV=PhienDangNhap.MaNV,MaTK=PhienDangNhap.MaTK};
            var id=PhienDangNhap.MaPhien;
            await Task.Run(()=>new BUS_NHATKY().Heartbeat(id,actor));
        }
        catch(Exception ex){LoiPhien?.Invoke("Không thể cập nhật phiên: "+ex.Message);}
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
}
