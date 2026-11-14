using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Phần mở rộng của màn hình bảo trì để đọc, lọc và mở biểu mẫu thanh lý thiết bị.
/// </summary>
public partial class BaoTri
{
    /// <summary>
    /// Danh sách thiết bị đã thanh lý được nạp để lọc tại giao diện và giữ lựa chọn theo mã thiết bị.
    /// </summary>
    private List<DongThanhLy> daThanhLy = new();

    /// <summary>
    /// Nạp thiết bị có ngày thanh lý, sắp mới nhất trước, cập nhật nhãn tab rồi áp dụng bộ lọc hiện tại.
    /// </summary>
    private void NapThanhLy()
    {
        daThanhLy = DuLieuGiaoDien.Rows("THIET_BI").Where(row => UiRecord.Date(row, "NgayThanhLy") is not null)
            .Select(row => new DongThanhLy(row)).OrderByDescending(row => row.NgayThanhLy).ThenBy(row => row.MaTB).ToList();
        txtTabThanhLy.Text = $"Thanh lý ({daThanhLy.Count})";
        LocThanhLy();
    }

    /// <summary>
    /// Lọc theo khoảng ngày và từ khóa, giữ lại lựa chọn nếu còn và tính tổng giá trị của các dòng đang hiển thị.
    /// </summary>
    private void LocThanhLy()
    {
        if (!sanSang) return;
        try
        {
            if(!ReadFilterDates(dpThanhLyTu,dpThanhLyDen,out var tu,out var den))return;
            var ma = (GridThanhLy.SelectedItem as DongThanhLy)?.MaTB;
            var query = txtTimThanhLy.Text.Trim();
            var rows = daThanhLy.Where(row => (tu is null || row.NgayThanhLy.Date >= tu.Value.Date)
                && (den is null || row.NgayThanhLy.Date <= den.Value.Date)
                && row.TuKhoa.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();
            GridThanhLy.ItemsSource = rows;
            GridThanhLy.SelectedItem = rows.FirstOrDefault(row => row.MaTB == ma);
            KhongCoThanhLy.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            var tong = rows.Sum(row => row.GiaTri).ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
            txtSoThanhLy.Text = $"{rows.Count} / {daThanhLy.Count} thiết bị đã thanh lý · Tổng giá trị hiển thị: {tong} đ";
            txtLoi.Text = "";
        }
        catch (InvalidOperationException ex) { txtLoi.Text = ex.Message; }
    }

    /// <summary>
    /// Áp dụng lại bộ lọc thanh lý khi từ khóa hoặc ngày thay đổi.
    /// </summary>
    private void LocThanhLy_ThayDoi(object sender, RoutedEventArgs e) => LocThanhLy();

    /// <summary>
    /// Chặn sự kiện trong lúc xóa từ khóa và khoảng ngày, sau đó lọc lại danh sách thanh lý.
    /// </summary>
    private void BoLocThanhLy_Click(object sender, RoutedEventArgs e)
    {
        sanSang = false;
        txtTimThanhLy.Clear();
        dpThanhLyTu.SelectedDate = dpThanhLyDen.SelectedDate = null;
        dpThanhLyTu.Text = dpThanhLyDen.Text = "";
        sanSang = true;
        LocThanhLy();
    }

    /// <summary>
    /// Hiển thị chi tiết của dòng thanh lý đang chọn hoặc ẩn vùng chi tiết khi chưa chọn.
    /// </summary>
    private void ThanhLy_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ChiTietThanhLy.DataContext = GridThanhLy.SelectedItem;
        ChiTietThanhLy.Visibility = GridThanhLy.SelectedItem is null ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// Mở biểu mẫu thanh lý cho thiết bị đang chọn khi có quyền; sau khi lưu chuyển sang tab thanh lý và chọn bản ghi vừa lưu.
    /// </summary>
    private void ThanhLy_Click(object sender, RoutedEventArgs e)
    {
        var workspace = HoTroGiaoDien.Workspace(this);
        if (workspace is null || !workspace.CanOpen("BaoTri") || GridThietBiHu.SelectedItem is not DongThietBiBaoTri dong) return;
        var form = new CuaSoThanhLy(dong.Record);
        form.Unloaded += (_, _) =>
        {
            if (BieuMauHoSo.GetSaved(form) is not object saved) return;
            BoLocThanhLy_Click(this, new RoutedEventArgs());
            CacDanhSach.SelectedIndex = 2;
            GridThanhLy.SelectedItem = daThanhLy.FirstOrDefault(row => row.MaTB == UiRecord.Text(saved, "MaTB"));
            if (GridThanhLy.SelectedItem is object selected) GridThanhLy.ScrollIntoView(selected);
        };
        workspace.ShowModal(form);
    }
}
