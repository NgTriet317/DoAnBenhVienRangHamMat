using System.Windows;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Views;
public partial class DonThuoc
{
    private void RefreshHistoryPage()
    {
        try
        {
            Feedback.Text = "";
            var filter = new BoLocDanhSachHoSo
            {
                MaBN = PatientId, MaLS = visitId, TuKhoa = SearchBox.Text,
                TrangThai = HoTroGiaoDien.Choice(StatusFilter), Trang = historyPage, KichThuocTrang = 25
            };
            var page = history.LayTrangLichSuDon(filter, actor);
            var pages = Math.Max(1, (int)Math.Ceiling(page.TongSo / 25d));
            if (historyPage > pages)
            { historyPage = pages; page = history.LayTrangLichSuDon(filter with { Trang = historyPage }, actor); }
            var rows = page.CacDong.Select(row => new IssuedPrescriptionCard(row)).ToArray();
            // Các lựa chọn chỉ lấy từ trang đã được DAL kiểm tra quyền, không dùng cache bệnh nhân toàn cục.
            var choices = rows.Select(row => new LuaChonDanhMuc(row.PatientId, row.Patient)).DistinctBy(row => row.Value).ToList();
            if (PatientId is not null && choices.All(choice => choice.Value != PatientId))
                choices.Insert(0, new(PatientId, "Bệnh nhân " + PatientId));
            choices.Insert(0, new("", "Tất cả trong phạm vi được phép"));
            PatientFilter.ItemsSource = choices;
            PatientFilter.SelectedValue = PatientId ?? "";
            PrescriptionList.ItemsSource = rows;
            PrescriptionList.SelectedItem = rows.FirstOrDefault(row => row.Code == selectedId);
            RecordCount.Text = $"{page.TongSo} đơn thuốc · trang {historyPage}/{pages}";
            ListEmpty.Visibility = rows.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            PrescriptionPages.Visibility = Visibility.Visible;
            PreviousPrescriptionPage.IsEnabled = historyPage > 1;
            NextPrescriptionPage.IsEnabled = historyPage < pages;
        }
        catch (InvalidOperationException ex)
        {
            PrescriptionList.ItemsSource = Array.Empty<IssuedPrescriptionCard>();
            PatientFilter.ItemsSource = Array.Empty<LuaChonDanhMuc>();
            RecordCount.Text = "Không tải được danh sách đơn thuốc";
            ListEmpty.Visibility = Visibility.Visible;
            PrescriptionPages.Visibility = Visibility.Collapsed;
            Feedback.Text = ClinicalFormInput.Message(ex);
        }
        finally { updating = false; }
        ShowSelected();
    }
    private void PreviousPrescription_Click(object sender, RoutedEventArgs e)
    { if (MustKeepSource()) return; historyPage = Math.Max(1, historyPage - 1); Refresh(); }
    private void NextPrescription_Click(object sender, RoutedEventArgs e)
    { if (MustKeepSource()) return; historyPage++; Refresh(); }
}
