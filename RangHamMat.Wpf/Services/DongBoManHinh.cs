using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Services;

internal sealed class DongBoDaDoiManHinhException : InvalidOperationException;

/// <summary>Đăng ký làm mới tường minh; giữ biểu mẫu và giao dịch đang xử lý.</summary>
public static class DongBoManHinh
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr window);
    private sealed record Registration(UserControl Control, string[] Tables, Action Refresh, Func<bool>? Busy, bool Cache,
        Func<CancellationToken,Task<Action>>? Prepare=null,Func<Task>? AsyncRefresh=null)
    { public long Generation {get;set;} public long Applied {get;set;} }
    private static readonly List<Registration> active = new();
    private static long observedSequence;
    private static readonly Dictionary<string,long> observedTables=new(StringComparer.OrdinalIgnoreCase);
    internal static event Action<string[]>? RefreshRequested;
    internal static void ObserveChanges(IReadOnlyCollection<string> tables)
    {
        foreach(var table in tables)observedTables[table]=++observedSequence;
    }
    private static void RequestStale(Registration r)
    {
        if(!r.Control.IsVisible)return;
        var stale=observedTables.Where(p=>p.Value>r.Applied && Relevant(r,[p.Key])).Select(p=>p.Key).ToArray();
        if(stale.Length>0)RefreshRequested?.Invoke(stale);
    }
    private static readonly AsyncLocal<bool> automatic=new();
    public static bool DangCapNhatTuDong { get=>automatic.Value; private set=>automatic.Value=value; }
    public static void ThrowIfAutomaticRefresh(Exception error)
    {
        if(DangCapNhatTuDong)throw new InvalidOperationException(error.Message,error);
    }
    public static void Register(UserControl control, string[] tables, Action refresh, Func<bool>? busy = null)
        => RegisterCore(new(control, tables, refresh, busy, false));
    public static void RegisterPrepared(UserControl control,string[] tables,Func<CancellationToken,Task<Action>> prepare,Func<bool>? busy=null)
        => RegisterCore(new(control,tables,()=>{},busy,false,prepare));
    public static void RegisterAsync(UserControl control,string[] tables,Func<Task> refresh,Func<bool>? busy=null)
        => RegisterCore(new(control,tables,()=>{},busy,false,null,refresh));
    internal static async Task RefreshAsync(IReadOnlyCollection<string> tables)
    {
        Refresh(tables);
        DangCapNhatTuDong=true;
        try
        {
            foreach(var r in active.Where(r=>r.Control.IsVisible && Relevant(r,tables) && r.AsyncRefresh is not null).ToArray())
            {
                var restore=CaptureSources(r.Control);
                try{await r.AsyncRefresh!();r.Applied=observedSequence;}catch{restore();throw;}
            }
        }
        finally{DangCapNhatTuDong=false;}
    }
    internal static async Task<Action> PrepareAsync(IReadOnlyCollection<string> tables,CancellationToken token)
    {
        var actions=new List<Action>();
        foreach(var r in active.Where(r=>r.Control.IsVisible && Relevant(r,tables) && r.Prepare is not null).ToArray())
        {
            var generation=r.Generation;var action=await r.Prepare!(token);
            actions.Add(()=>
            {
                if(!active.Contains(r)||r.Generation!=generation||!r.Control.IsVisible)
                    throw new DongBoDaDoiManHinhException();
                var restore=CaptureSources(r.Control);
                try{action();r.Applied=observedSequence;}catch{restore();throw;}
            });
        }
        return ()=>{foreach(var action in actions)action();};
    }
    internal static void RegisterCache(UserControl control, Action refresh)
        => RegisterCore(new(control, CacheTables(control.GetType().Name), refresh, null, true));
    private static readonly string[] sharedTables=["BENH_NHAN","HO_SO_BENH_AN","PHIEU_KHAM_BENH","LICH_KHAM","LICH_SU_KHAM_BENH",
        "TINH_TRANG_BENH","KE_HOACH_DIEU_TRI","CHI_TIET_KE_HOACH","DICH_VU_THUC_HIEN","DON_THUOC","CHI_TIET_DON_THUOC",
        "NHAN_VIEN","PHONG","CA_LAM","DICH_VU","THUOC","HANG_HOA","KHO","LO_HANG_HOA","HOA_DON","CHI_TIET_HOA_DON",
        "THANH_TOAN","PHIEU_XUAT","CHI_TIET_PHIEU_XUAT","BAO_TRI_THIET_BI","THIET_BI","NHA_CUNG_CAP","HOA_DON_NHAP"];
    private static string[] CacheTables(string name)=>name switch
    {
        "NhaCungCap"=>["NHA_CUNG_CAP","HOA_DON_NHAP"],
        "BaoTri"=>["BAO_TRI_THIET_BI","THANH_LY_THIET_BI","THIET_BI","NHAN_VIEN"],
        "LichKham"=>["LICH_KHAM","NHAN_VIEN","PHONG","CA_LAM","LICH_LAM_VIEC","BENH_NHAN","DICH_VU"],
        "KhamDieuTri"=>["PHIEU_KHAM_BENH","LICH_SU_KHAM_BENH","BENH_NHAN","HO_SO_BENH_AN","NHAN_VIEN","PHONG","DICH_VU","LICH_KHAM"],
        _=>sharedTables
    };
    internal static bool AffectsCache(IReadOnlyCollection<string> tables)
        =>tables.Contains("*")||sharedTables.Intersect(tables,StringComparer.OrdinalIgnoreCase).Any();
    private static void RegisterCore(Registration registration)
    {
        registration.Applied=observedSequence;
        registration.Control.Loaded += (_,_) => { registration.Generation++;if (!active.Contains(registration)) active.Add(registration);RequestStale(registration); };
        registration.Control.Unloaded += (_,_) => {registration.Generation++;active.Remove(registration);};
        registration.Control.IsVisibleChanged+=(_,_)=>RequestStale(registration);
        if(registration.Control.IsLoaded)active.Add(registration);
    }
    private static bool Relevant(Registration r, IReadOnlyCollection<string> tables)
    {
        if((r.Cache||r.Tables.Contains("*")) && tables.All(t=>t is "PHIEN_DANG_NHAP" or "NHAT_KY_THAO_TAC" or "NHAT_KY_PHIEN"))return false;
        return tables.Contains("*") || r.Tables.Contains("*") || r.Tables.Intersect(tables,StringComparer.OrdinalIgnoreCase).Any();
    }
    internal static bool NeedsCache(IReadOnlyCollection<string> tables)
        => active.Any(r=>r.Cache && r.Control.IsVisible && Relevant(r,tables));
    internal static void ApplyPrepared(Action apply)
    {
        var previous=DangCapNhatTuDong;DangCapNhatTuDong=true;
        try{apply();}finally{DangCapNhatTuDong=previous;}
    }
    private static Action CaptureSources(UserControl control)
    {
        var sources=ClinicalPendingWork.Nodes(control).OfType<ItemsControl>()
            .Select(c=>(Control:c,Source:c.ItemsSource,Selected:(c as Selector)?.SelectedItem)).ToArray();
        var context=control.DataContext;
        return ()=>
        {
            control.DataContext=context;
            foreach(var old in sources)
            {
                try
                {
                    if(!ReferenceEquals(old.Control.ItemsSource,old.Source))old.Control.ItemsSource=old.Source;
                    if(old.Control is Selector selector)selector.SelectedItem=old.Selected;
                }
                catch{/* Giữ lỗi gốc khi phục hồi lựa chọn. */}
            }
        };
    }

    internal static bool IsBusy(MainWindow owner)
    {
        // ShowDialog vô hiệu hóa owner; cửa sổ lịch sử chỉ đọc vẫn được theo dõi bình thường.
        if(!owner.IsEnabled)return true;
        var handle=new WindowInteropHelper(owner).Handle;
        if(handle!=IntPtr.Zero&&!IsWindowEnabled(handle))return true;
        var windows=Application.Current.Windows.OfType<Window>().Where(w=>w.IsVisible).ToArray();
        if(windows.Any(w=>w!=owner && (w.GetType().Name is "KeDonThuocWindow" ||
            w.Content is UserControl f && (f.GetType().Name.StartsWith("CuaSo",StringComparison.Ordinal) || f.GetType().Name.StartsWith("Them",StringComparison.Ordinal)))))return true;
        if(owner.AppHost.Content is AppWorkspace workspace && workspace.DongBoDangNhapLieu) return true;
        var nodes = windows.Cast<DependencyObject>().Append(owner).Distinct().SelectMany(ClinicalPendingWork.Nodes).Distinct().ToArray();
        if(nodes.OfType<IClinicalPendingWork>().Any(w=>w.HasChanges || w.MustStay))return true;
        if(nodes.OfType<UserControl>().Any(f=>BieuMauHoSo.IsClinicalForm(f) && BieuMauHoSo.HasInputChanges(f)))return true;
        if(nodes.OfType<DataGridRow>().Any(r=>r.IsEditing))return true;
        if(active.Any(r=>r.Control.IsVisible && (r.Busy?.Invoke()??false)))return true;
        // Giữ cả nội dung gõ đang nằm ở binding UpdateSourceTrigger=LostFocus.
        return Keyboard.FocusedElement is TextBoxBase text && !text.IsReadOnly;
    }

    internal static void Refresh(IReadOnlyCollection<string> tables)
    {
        var targets = active.Where(r=>r.Control.IsVisible && Relevant(r,tables)).ToArray();
        DangCapNhatTuDong=true;
        try
        {
            foreach(var r in targets)
            {
                if(r.Busy?.Invoke()==true)throw new InvalidOperationException("Có dữ liệu mới; kết thúc nhập liệu để cập nhật.");
                var sources=ClinicalPendingWork.Nodes(r.Control).OfType<ItemsControl>()
                    .Select(c=>(Control:c,Source:c.ItemsSource,Selected:(c as Selector)?.SelectedItem)).ToArray();
                try{r.Refresh();if(r.Prepare is null && r.AsyncRefresh is null)r.Applied=observedSequence;}
                catch
                {
                    foreach(var old in sources)
                    {
                        try
                        {
                            if(!ReferenceEquals(old.Control.ItemsSource,old.Source))old.Control.ItemsSource=old.Source;
                            if(old.Control is Selector selector)selector.SelectedItem=old.Selected;
                        }
                        catch{/* Ưu tiên giữ lỗi tải gốc; không biến lỗi phục hồi selection thành lỗi khác. */}
                    }
                    throw;
                }
            }
        }
        finally { DangCapNhatTuDong=false; }
    }

    /// <summary>Lỗi đọc tự động được trả về bộ điều phối, không mở hộp thoại mỗi nhịp.</summary>
    public static MessageBoxResult ShowMessage(string message, string caption = "Thông báo",
        MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
    {
        if(DangCapNhatTuDong) throw new InvalidOperationException(message);
        return MessageBox.Show(message,caption,buttons,icon);
    }
}
