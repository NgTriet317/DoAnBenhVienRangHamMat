// AI bổ sung kiểm tra nhập liệu: đăng ký xử lý ngày gõ sai trên toàn bộ DatePicker khi ứng dụng khởi động.
// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung giải thích luồng khởi động: mở đăng nhập, nhận kết quả, mở MainWindow và kết thúc ứng dụng.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using RangHamMat;
using RangHamMat.Views;
using BUS;
using System.Windows;

namespace wpftest
{
    /// <summary>
    /// Điểm khởi động WPF: điều phối hộp thoại đăng nhập, cửa sổ chính và thời điểm kết thúc ứng dụng.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Dịch vụ dùng chung cho khôi phục đăng nhập, đăng nhập mới và xóa phiên đã ghi nhớ.
        /// </summary>
        private readonly DichVuDangNhap dichVuDangNhap;

        /// <summary>
        /// Khởi tạo ứng dụng với kho ghi nhớ đăng nhập mặc định.
        /// </summary>
        public App() : this(new DichVuDangNhap()) { }

        /// <summary>
        /// Nhận dịch vụ đăng nhập để dùng nhất quán khi chuyển giữa hộp thoại và cửa sổ chính.
        /// </summary>
        /// <param name="dichVuDangNhap">Dịch vụ xác thực và quản lý phiên ghi nhớ của ứng dụng.</param>
        internal App(DichVuDangNhap dichVuDangNhap) => this.dichVuDangNhap = dichVuDangNhap;

        /// <summary>
        /// Khôi phục tài khoản đã ghi nhớ hoặc hiển thị đăng nhập khi chưa có phiên hợp lệ.
        /// </summary>
        private void App_Startup(object sender, StartupEventArgs e)
        {
            // AI bổ sung: bắt lỗi ngày nhập bằng bàn phím trên các DatePicker của ứng dụng.
            RangHamMat.Controls.KiemTraNhapLieu.Register();
            PhienDangNhap.KhoiTaoPhienUngDung();
            bool restored = false;
            try { restored = dichVuDangNhap.ThuKhoiPhuc(); }
            catch (System.Exception ex)
            {
                MessageBox.Show("Không thể khôi phục đăng nhập: " + ex.Message,
                    "Phiên đăng nhập", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            if (restored) MoManHinhChinh();
            else HienDangNhap();
        }

        /// <summary>
        /// Mở hộp thoại đăng nhập; kết thúc ứng dụng khi người dùng đóng mà chưa đăng nhập thành công.
        /// </summary>
        private void HienDangNhap()
        {
            var login = new DangNhap(dichVuDangNhap);
            MainWindow = login;
            if (login.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
            MoManHinhChinh();
        }

        /// <summary>
        /// Đồng bộ trạng thái nghỉ trước khi mở cửa sổ chính, rồi nối luồng đăng xuất về hộp thoại đăng nhập.
        /// </summary>
        /// <remarks>Nếu xóa phiên ghi nhớ thất bại, sự kiện Closing hủy đóng cửa sổ để người dùng còn xử lý được lỗi.</remarks>
        private void MoManHinhChinh()
        {
            // Khi mở ứng dụng, áp dụng các đơn đã tới ngày có hiệu lực trong lúc ứng dụng tắt.
            try
            {
                new BUS_NHANVIEN().dongBoTrangThaiNghi();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Không thể đồng bộ trạng thái nghỉ của nhân viên: " + ex.Message,
                    "Lỗi đồng bộ nhân sự", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            // Giữ tiến trình trong lúc đổi cửa sổ chính sang hộp thoại đăng nhập.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            mainWindow.Closing += (_, e) =>
            {
                if (e.Cancel || !mainWindow.YeuCauDangXuat) return;
                try { dichVuDangNhap.QuenPhien(); }
                catch (System.Exception ex)
                {
                    e.Cancel = true;
                    MessageBox.Show("Không thể xóa phiên đăng nhập đã lưu: " + ex.Message,
                        "Đăng xuất", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            mainWindow.Closed += (_, _) =>
            {
                if (!mainWindow.YeuCauDangXuat)
                {
                    Shutdown();
                    return;
                }
                PhienDangNhap.XoaDinhDanh();
                Dispatcher.BeginInvoke(new System.Action(HienDangNhap));
            };
            mainWindow.Show();
        }
        
    }

}
