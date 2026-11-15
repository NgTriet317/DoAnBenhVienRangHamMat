using System.Data;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RangHamMat.Views;

/// <summary>Giữ dòng theo khóa tường minh; dòng biến mất để trống, không chuyển sang đối tượng khác.</summary>
internal static class DongBoLuaChon
{
    public static void Giu<T>(Selector selector, Func<T, string> key, Action refresh)
    {
        string? selected = selector.SelectedItem is T value ? key(value) : null;
        refresh();
        selector.SelectedItem = selected is null ? null : selector.Items.OfType<T>().FirstOrDefault(x => key(x) == selected);
    }

    public static void GiuDong(Selector selector, string[] columns, Action refresh)
        => Giu<DataRowView>(selector, row => string.Join("\u001f", columns.Select(c => Convert.ToString(row[c]))), refresh);

    public static string? GiaTriCombo(ComboBox combo) => Convert.ToString(combo.SelectedValue);

    public static void ChonNhan(ComboBox combo, string? label)
    {
        combo.SelectedItem = combo.Items.Cast<object>().FirstOrDefault(x =>
            Convert.ToString(x is ComboBoxItem item ? item.Content : x) == label);
    }
}

internal static class DongBoChuanBi
{
    // Giữ cách tải chủ động cũ; nhịp tự động chỉ được đọc nhân viên.
    internal static IEnumerable<ET.ET_NHANVIEN> NhanVien(BUS.BUS_NHANVIEN bus)
        =>Services.DongBoManHinh.DangCapNhatTuDong?bus.DocDanhSachKhongGhi():bus.layToanBoNhanVien();
    // Chỉ read dữ liệu POCO/DataTable ở nền. apply chạy trên Dispatcher do bộ điều phối quản lý.
    public static async Task<Action> Doc<T>(Func<T> read, Action<T> apply, CancellationToken token)
    {
        var principal=new System.Security.Claims.ClaimsPrincipal(
            (Thread.CurrentPrincipal as System.Security.Claims.ClaimsPrincipal)?.Identities.Select(i=>i.Clone())
            ?? Enumerable.Empty<System.Security.Claims.ClaimsIdentity>());
        var data = await Task.Run(() =>
        {
            var previous=Thread.CurrentPrincipal;
            try{Thread.CurrentPrincipal=principal;token.ThrowIfCancellationRequested();return read();}
            finally{Thread.CurrentPrincipal=previous;}
        }, token);
        token.ThrowIfCancellationRequested();
        return () => apply(data);
    }
}
