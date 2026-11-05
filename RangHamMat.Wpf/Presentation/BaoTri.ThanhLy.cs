using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

public partial class BaoTri
{
    private List<DongThanhLy> daThanhLy = new();

    private void NapThanhLy()
    {
        daThanhLy = DuLieuGiaoDien.Rows("THIET_BI").Where(row => UiRecord.Date(row, "NgayThanhLy") is not null)
            .Select(row => new DongThanhLy(row)).OrderByDescending(row => row.NgayThanhLy).ThenBy(row => row.MaTB).ToList();
        txtTabThanhLy.Text = $"Thanh lý ({daThanhLy.Count})";
        LocThanhLy();
    }

    private void LocThanhLy()
    {
        if (!sanSang) return;
        try
        {
            var tu = KiemTraNhapLieu.ReadDate(dpThanhLyTu);
            var den = KiemTraNhapLieu.ReadDate(dpThanhLyDen);
            if (tu > den) throw new InvalidOperationException("Ngày kết thúc bộ lọc thanh lý phải từ ngày bắt đầu trở đi.");
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

    private void LocThanhLy_ThayDoi(object sender, RoutedEventArgs e) => LocThanhLy();

    private void BoLocThanhLy_Click(object sender, RoutedEventArgs e)
    {
        sanSang = false;
        txtTimThanhLy.Clear();
        dpThanhLyTu.SelectedDate = dpThanhLyDen.SelectedDate = null;
        dpThanhLyTu.Text = dpThanhLyDen.Text = "";
        sanSang = true;
        LocThanhLy();
    }

    private void ThanhLy_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ChiTietThanhLy.DataContext = GridThanhLy.SelectedItem;
        ChiTietThanhLy.Visibility = GridThanhLy.SelectedItem is null ? Visibility.Collapsed : Visibility.Visible;
    }

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
