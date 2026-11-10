using System.Windows.Threading;
using BUS;
using ET;

namespace RangHamMat;

/// <summary>Heartbeat mỗi phút, hết hạn sau 5 phút tại SQL; không suy diễn thời điểm crash.</summary>
internal static class PhienNhatKy
{
    private static DispatcherTimer? timer;
    private static bool running;
    public static event Action<string>? LoiPhien;
    internal static void BatDau()
    {
        if(PhienDangNhap.MaPhien!=Guid.Empty)return;
        var actor=new BUS_QUYENNGHIEPVU().LayNguCanh(PhienDangNhap.MaNV);
        PhienDangNhap.MaPhien=new BUS_NHATKY().BatDauPhien(actor);
        PhienDangNhap.GanPhienAudit(PhienDangNhap.MaPhien);
        timer=new DispatcherTimer{Interval=TimeSpan.FromMinutes(1)};
        timer.Tick+=Tick;timer.Start();
    }
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
