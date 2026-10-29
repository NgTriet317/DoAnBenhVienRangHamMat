// AI bổ sung kiểm tra nhập liệu: đăng ký xử lý ngày gõ sai trên toàn bộ DatePicker khi ứng dụng khởi động.
// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung giải thích luồng khởi động: mở đăng nhập, nhận kết quả, mở MainWindow và kết thúc ứng dụng.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using RangHamMat;
using RangHamMat.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace wpftest
{
    /// <summary>
    /// Điểm khởi động WPF: điều phối hộp thoại đăng nhập, cửa sổ chính và thời điểm kết thúc ứng dụng.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Hiển thị hộp thoại đăng nhập; nếu người dùng đóng/hủy thì kết thúc ứng dụng, nếu đồng ý thì mở MainWindow và kết thúc khi cửa sổ chính đóng.
        /// </summary>
        private void App_Startup(object sender, StartupEventArgs e)
        {
            // AI bổ sung: bắt lỗi ngày nhập bằng bàn phím trên các DatePicker của ứng dụng.
            RangHamMat.Controls.KiemTraNhapLieu.Register();
            PhienDangNhap.KhoiTaoPhienUngDung();
            var login = new DangNhap();

            if (login.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }
        
    }

}
