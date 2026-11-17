using System.Windows;

namespace RangHamMat.Services;

public static partial class DongBoManHinh
{
    private sealed class WindowRegistration(Window window,string[] tables,Func<CancellationToken,Task<Action>> prepare,Func<bool>? busy)
    {
        internal Window Window {get;}=window;
        internal string[] Tables {get;}=tables;
        internal Func<CancellationToken,Task<Action>> Prepare {get;}=prepare;
        internal Func<bool>? Busy {get;}=busy;
        internal long Generation;
        internal long Applied=-1;
    }
    private static readonly List<WindowRegistration> activeWindows=new();

    /// <summary>Chỉ đăng ký phần danh sách độc lập, không được áp dụng lại dữ liệu vào biểu mẫu đang nhập.</summary>
    public static void RegisterPreparedWindow(Window window,string[] tables,Func<CancellationToken,Task<Action>> prepare,Func<bool>? busy=null)
    {
        var registration=new WindowRegistration(window,tables,prepare,busy);
        window.Loaded+=(_,_)=>
        {
            registration.Generation++;
            if(!activeWindows.Contains(registration))activeWindows.Add(registration);
            RefreshRequested?.Invoke(tables);
        };
        window.Closed+=(_,_)=>{registration.Generation++;activeWindows.Remove(registration);};
        if(window.IsLoaded){activeWindows.Add(registration);RefreshRequested?.Invoke(tables);}
    }
    private static bool OwnedBy(Window child,Window main)
    {
        for(var owner=child.Owner;owner is not null;owner=owner.Owner)if(ReferenceEquals(owner,main))return true;
        return false;
    }
    internal static async Task RefreshWindowsAsync(MainWindow main,IReadOnlyCollection<string> tables,CancellationToken token)
    {
        foreach(var r in activeWindows.Where(r=>r.Window.IsVisible&&OwnedBy(r.Window,main)).ToArray())
        {
            if(!tables.Contains("*")&&!r.Tables.Intersect(tables,StringComparer.OrdinalIgnoreCase).Any())continue;
            var stale=r.Applied<0||observedTables.Any(p=>p.Value>r.Applied&&(p.Key=="*"||r.Tables.Contains(p.Key,StringComparer.OrdinalIgnoreCase)));
            if(!stale||r.Busy?.Invoke()==true)continue;
            var generation=r.Generation;
            var stamp=observedSequence;
            var action=await r.Prepare(token);
            token.ThrowIfCancellationRequested();
            if(!activeWindows.Contains(r)||r.Generation!=generation||!r.Window.IsVisible||r.Busy?.Invoke()==true)continue;
            var restore=CaptureSources(r.Window);
            try{ApplyPrepared(action);r.Applied=stamp;}
            catch{ApplyPrepared(restore);throw;}
        }
    }
}
