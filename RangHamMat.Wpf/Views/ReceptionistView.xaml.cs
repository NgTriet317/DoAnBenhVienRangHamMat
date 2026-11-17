using System;
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Input;
using RangHamMat.Repositories;
using RangHamMat.Models;
using System.IO;
using System.Text.Json;
using RangHamMat.Services;
using RangHamMat.ViewModels;

namespace RangHamMat.Views;

public partial class ReceptionistView : UserControl
{
    private readonly ReceptionistViewModel _vm;

    public ReceptionistView()
        : this(new ReceptionistViewModel(CreateRepository(), new GroqAiService())) { }

    private static IHospitalRepository CreateRepository()
    {
        string config = Path.Combine(AppContext.BaseDirectory, "AI", "Config", "ReceptionistSettings.json");
        var settings = File.Exists(config)
            ? JsonSerializer.Deserialize<GroqSettings>(File.ReadAllText(config),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            : null;
        return settings?.UseMockRepository == true
            ? new MockHospitalRepository() : new SqlHospitalRepository();
    }

    public ReceptionistView(ReceptionistViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
        _vm.Messages.CollectionChanged += ScrollMessages;
        _vm.PrintExistingSlipRequested += OpenExistingPrint;
        Unloaded += OnUnloaded;
    }

    private void ScrollMessages(object? sender, NotifyCollectionChangedEventArgs e)
        => Dispatcher.BeginInvoke(new Action(() => ChatScroll.ScrollToEnd()));

    private void ChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !_vm.SendCommand.CanExecute(null)) return;
        _vm.SendCommand.Execute(null);
        e.Handled = true;
    }

    private void OpenExistingPrint(string maPhieu)
    {
        try
        {
            // Dùng lại mẫu A5, xác thực SQL, bộ lọc quyền và In_Click của PhieuHuongDan.
            // Không đưa DataGrid bệnh nhân của màn hình nhân viên lên màn hình kiosk.
            var trangIn = new PhieuHuongDan();
            trangIn.InPhieuTheoMaTuAI(maPhieu);
        }
        catch (Exception ex)
        {
            _vm.ReportPrintError(ex.Message);
        }
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _vm.Messages.CollectionChanged -= ScrollMessages;
        _vm.PrintExistingSlipRequested -= OpenExistingPrint;
        _vm.Dispose();
    }
}
