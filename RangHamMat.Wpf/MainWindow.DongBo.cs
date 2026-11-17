using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BUS;
using RangHamMat.Controls;
using RangHamMat.Services;

namespace RangHamMat;

public partial class MainWindow
{
    private CancellationTokenSource? dongBoCancellation;
    private readonly SemaphoreSlim dongBoGate=new(1,1);
    private readonly TrangThaiDongBo dongBoPending=new();
    private readonly TextBlock dongBoStatus=new(){Foreground=Brushes.DimGray,Background=Brushes.White,
        FontSize=11,Margin=new Thickness(24,0,24,4),VerticalAlignment=VerticalAlignment.Bottom,
        HorizontalAlignment=HorizontalAlignment.Left,Text="Đồng bộ: đang khởi động…",IsHitTestVisible=false};
    private long? dongBoVersion;
    private string? dongBoDatabase;
    private string dongBoConnection="";
    private bool dongBoClosed;
    private long dongBoInteraction;

    private void KhoiTaoDongBo()
    {
        if(Content is Panel panel){Panel.SetZIndex(dongBoStatus,40);panel.Children.Add(dongBoStatus);}
        Loaded+=(_,_)=>BatDauDongBo();
        Closed+=(_,_)=>{dongBoClosed=true;HuyDongBo();PhienDangNhap.DinhDanhThayDoi-=DoiPhienDongBo;DongBoManHinh.RefreshRequested-=YeuCauDongBoManHinh;};
        PhienDangNhap.DinhDanhThayDoi+=DoiPhienDongBo;
        DongBoManHinh.RefreshRequested+=YeuCauDongBoManHinh;
        PreviewMouseDown+=(_,_)=>dongBoInteraction++;
        PreviewKeyDown+=(_,_)=>dongBoInteraction++;
    }
    private void YeuCauDongBoManHinh(string[] tables)=>dongBoPending.GhiNhan(tables);
    private void HuyDongBo()
    {
        var previous=dongBoCancellation;dongBoCancellation=null;
        if(previous is not null){previous.Cancel();previous.Dispose();}
    }
    internal void DungDongBoDoMatPhien(){dongBoClosed=true;HuyDongBo();}
    private void DoiPhienDongBo(){HuyDongBo();if(IsLoaded&&!dongBoClosed)BatDauDongBo();}
    private void BatDauDongBo()
    {
        HuyDongBo();
        var source=new CancellationTokenSource();dongBoCancellation=source;
        dongBoVersion=null;dongBoDatabase=null;dongBoPending.Xoa();
        if(!PhienDangNhap.DaDangNhap)return;
        _=TheoDoiDongBoAsync(source);
    }
    private async Task TheoDoiDongBoAsync(CancellationTokenSource source)
    {
        var token=source.Token;
        try
        {
            var config=CauHinhDongBo.Read();
            if(!config.Bat){dongBoStatus.Text="Đồng bộ tự động đã tắt";return;}
            var delay=config.ChuKy;
            while(!token.IsCancellationRequested)
            {
                try
                {
                    await dongBoGate.WaitAsync(token);
                    try
                    {
                        var connection=DAL.CauHinhKetNoi.LayChuoiKetNoi();
                        if(connection!=dongBoConnection){dongBoConnection=connection;dongBoVersion=null;dongBoDatabase=null;dongBoPending.Xoa();}
                        var batch=await new BUS_DONGBO(connection).DocThayDoiAsync(dongBoVersion,token);
                        token.ThrowIfCancellationRequested();
                        if(dongBoDatabase is not null && dongBoDatabase!=batch.DatabaseIdentity)
                        {
                            batch=await new BUS_DONGBO(connection).DocThayDoiAsync(null,token);
                            token.ThrowIfCancellationRequested();
                        }
                        dongBoDatabase=batch.DatabaseIdentity;dongBoVersion=batch.Version;
                        DongBoManHinh.ObserveChanges(batch.Tables);
                        dongBoPending.GhiNhan(batch.Tables);
                        await ApDungDongBoAsync(token);
                    }
                    finally{dongBoGate.Release();}
                    delay=config.ChuKy;
                }
                catch(OperationCanceledException)when(token.IsCancellationRequested){break;}
                catch(Exception ex)
                {
                    if(token.IsCancellationRequested)break;
                    dongBoStatus.Text="Đồng bộ tạm dừng: "+ex.Message;
                    dongBoStatus.ToolTip="Kiểm tra kết nối, quyền SQL và phần cài Change Tracking trong file SQL chính. Dữ liệu đang xem được giữ; hệ thống sẽ thử lại.";
                    delay=TimeSpan.FromSeconds(Math.Min(30,Math.Max(config.ChuKy.TotalSeconds,delay.TotalSeconds*2)));
                }
                await Task.Delay(delay,token);
            }
        }
        catch(OperationCanceledException)when(token.IsCancellationRequested){}
        catch(Exception ex){if(!token.IsCancellationRequested)dongBoStatus.Text="Chưa bật đồng bộ: "+ex.Message;}
    }
    private async Task ApDungDongBoAsync(CancellationToken token)
    {
        if(!dongBoPending.CoThayDoi){dongBoStatus.Text="Đồng bộ: đang theo dõi thay đổi";return;}
        var safeWindowTables=dongBoPending.Chup().Keys.ToArray();
        await DongBoManHinh.RefreshWindowsAsync(this,safeWindowTables,token);
        token.ThrowIfCancellationRequested();
        if(DongBoManHinh.IsBusy(this))
        {dongBoStatus.Text="Có dữ liệu mới; lưu hoặc kết thúc thao tác để cập nhật";return;}
        var snapshot=dongBoPending.Chup();var tables=snapshot.Keys.ToArray();
        var content=AppHost.Content;var interaction=dongBoInteraction;
        Action? apply=null;
        if(DongBoManHinh.NeedsCache(tables))apply=await DuLieuGiaoDien.PrepareRefreshAsync(token);
        var prepared=await DongBoManHinh.PrepareAsync(tables,token);
        token.ThrowIfCancellationRequested();
        if(content!=AppHost.Content || interaction!=dongBoInteraction || DongBoManHinh.IsBusy(this))return;
        try{DongBoManHinh.ApplyPrepared(()=>{apply?.Invoke();prepared();});}
        catch(DongBoDaDoiManHinhException){return;}
        await DongBoManHinh.RefreshAsync(tables);
        token.ThrowIfCancellationRequested();
        if(apply is null && DongBoManHinh.AffectsCache(tables))DuLieuGiaoDien.InvalidateForSync();
        dongBoPending.XacNhan(snapshot);
        dongBoStatus.Text="Đã cập nhật dữ liệu · "+DateTime.Now.ToString("HH:mm:ss");
        dongBoStatus.ToolTip=null;
    }
    internal async Task TaiLaiDongBoAsync()
    {
        if(DongBoManHinh.IsBusy(this))
        {dongBoStatus.Text="Lưu hoặc kết thúc nhập liệu trước khi tải lại.";return;}
        await dongBoGate.WaitAsync();
        try
        {
            // Manual reload hoạt động cả khi CT chưa được cài hoặc đồng bộ tự động bị tắt.
            DongBoManHinh.ObserveChanges(["*"]);
            dongBoPending.GhiNhan(["*"]);
            await ApDungDongBoAsync(dongBoCancellation?.Token??CancellationToken.None);
        }
        finally{dongBoGate.Release();}
    }
}
