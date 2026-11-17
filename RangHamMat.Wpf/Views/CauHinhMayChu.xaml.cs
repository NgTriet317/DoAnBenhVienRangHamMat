using BUS;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Views;

/// <summary>
/// Cấu hình SQL Server trước màn hình đăng nhập. View chỉ tương tác qua BUS,
/// không trực tiếp mở kết nối hay xử lý SQL.
/// </summary>
public partial class CauHinhMayChu : Window
{
    private readonly BUS_CAUHINHCSDL nghiepVu;
    private readonly CancellationTokenSource huyXuLy = new();
    private bool dangXuLy;
    private readonly bool tuDongKhiMo;

    public CauHinhMayChu() : this(new BUS_CAUHINHCSDL()) { }

    public CauHinhMayChu(BUS_CAUHINHCSDL nghiepVu, bool tuDongKhiMo = true)
    {
        this.nghiepVu = nghiepVu;
        this.tuDongKhiMo = tuDongKhiMo;
        InitializeComponent();
    }

    private async void CauHinhMayChu_Loaded(object sender, RoutedEventArgs e)
    {
        cboDatabase.Text = "QLBenhVienRangHamMat";
        NapMayChu();
        var daLuu = nghiepVu.layCauHinhDaLuu();
        if (!string.IsNullOrWhiteSpace(daLuu))
        {
            try
            {
                var thongTin = nghiepVu.tachChuoiKetNoi(daLuu);
                cboMayChu.Text = thongTin.MayChu;
                cboDatabase.Text = thongTin.Database;
                chkWindows.IsChecked = thongTin.XacThucWindows;
                txtTaiKhoan.Text = thongTin.TaiKhoan;
                // Không hiện lại mật khẩu đã lưu trong form; auto-login sử dụng mã hóa DPAPI.
            }
            catch (ArgumentException) { }
        }
        else
        {
            cboMayChu.Text = ".";
        }

        if (tuDongKhiMo)
            await TimTuDongAsync();
        else
            DoiTrangThai(false, "Chọn máy chủ hoặc nhập địa chỉ rồi bấm Kiểm tra và Lưu và tiếp tục.");
    }

    private void CauHinhMayChu_Closed(object sender, EventArgs e)
    {
        huyXuLy.Cancel();
        // Không Dispose ở đây vì một lần mở kết nối async có thể vẫn dùng token.
    }

    private void XacThuc_Changed(object sender, RoutedEventArgs e)
    {
        if (pnlTaiKhoan is null) return;
        pnlTaiKhoan.Visibility = chkWindows.IsChecked == true
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void NapMayChu()
    {
        string hienTai = cboMayChu.Text;
        cboMayChu.ItemsSource = nghiepVu.layDanhSachMayChu();
        if (!string.IsNullOrWhiteSpace(hienTai)) cboMayChu.Text = hienTai;
    }

    private string LayChuoiKetNoi()
        => nghiepVu.taoChuoiKetNoi(cboMayChu.Text, cboDatabase.Text,
            chkWindows.IsChecked == true, txtTaiKhoan.Text, txtMatKhau.Password);

    private void DoiTrangThai(bool xuLy, string noiDung, bool thanhCong = false)
    {
        dangXuLy = xuLy;
        btnDoMayChu.IsEnabled = !xuLy;
        btnLayDatabase.IsEnabled = !xuLy;
        btnTuDong.IsEnabled = !xuLy;
        btnKiemTra.IsEnabled = !xuLy;
        btnLuu.IsEnabled = !xuLy;
        txtTrangThai.Text = noiDung;
        txtTrangThai.Foreground = new SolidColorBrush(
            thanhCong ? Color.FromRgb(19, 122, 78) : Color.FromRgb(93, 109, 130));
    }

    private void DoMayChu_Click(object sender, RoutedEventArgs e)
    {
        NapMayChu();
        DoiTrangThai(false,
            "Đã cập nhật SQL Server instance đăng ký trên máy. Nếu chưa thấy, hãy nhập tên máy chủ thủ công.");
    }

    private async void LayDatabase_Click(object sender, RoutedEventArgs e)
    {
        if (dangXuLy) return;
        try
        {
            string chuoi = LayChuoiKetNoi();
            DoiTrangThai(true, "Đang đọc danh sách database có quyền truy cập...");
            var names = await nghiepVu.layDanhSachDatabaseAsync(chuoi, huyXuLy.Token);
            string previous = cboDatabase.Text;
            cboDatabase.ItemsSource = names;
            cboDatabase.Text = names.Contains(previous, StringComparer.OrdinalIgnoreCase)
                ? previous : names.FirstOrDefault(x =>
                    x.Equals("QLBenhVienRangHamMat", StringComparison.OrdinalIgnoreCase)) ?? previous;
            DoiTrangThai(false, $"Đã tìm được {names.Length} database. Hãy chọn database có cấu trúc Răng Hàm Mặt.");
        }
        catch (OperationCanceledException)
        {
            // Cửa sổ đã đóng hoặc tác vụ bị hủy: không thao tác lên UI nữa.
        }
        catch (Exception ex)
        {
            DoiTrangThai(false, "Không tải được danh sách database: " + ex.Message);
        }
    }

    private async void KiemTra_Click(object sender, RoutedEventArgs e)
    {
        if (dangXuLy) return;
        try
        {
            string chuoi = LayChuoiKetNoi();
            DoiTrangThai(true, "Đang kết nối và kiểm tra các bảng nghiệp vụ...");
            await nghiepVu.kiemTraKetNoiAsync(chuoi, huyXuLy.Token);
            DoiTrangThai(false, "Kết nối thành công. Database có các bảng nghiệp vụ cần thiết.", true);
        }
        catch (OperationCanceledException)
        {
            // Cửa sổ đã đóng hoặc tác vụ bị hủy: không thao tác lên UI nữa.
        }
        catch (Exception ex)
        {
            DoiTrangThai(false, "Kết nối thất bại: " + ex.Message);
        }
    }

    private async void Luu_Click(object sender, RoutedEventArgs e)
    {
        if (dangXuLy) return;
        try
        {
            string chuoi = LayChuoiKetNoi();
            DoiTrangThai(true, "Đang kiểm tra kết nối và lưu cấu hình mã hóa...");
            await nghiepVu.luuVaSuDungAsync(chuoi, huyXuLy.Token);
            DialogResult = true;
        }
        catch (OperationCanceledException)
        {
            // Cửa sổ đã đóng hoặc tác vụ bị hủy: không thao tác lên UI nữa.
        }
        catch (Exception ex)
        {
            DoiTrangThai(false, "Không thể lưu cấu hình: " + ex.Message);
        }
    }

    /// <summary>
    /// Chỉ tự kết nối bằng Windows Authentication đến instance được nhận diện trên
    /// máy cục bộ. Có nhiều database hợp lệ thì yêu cầu người dùng chọn thủ công.
    /// </summary>
    private async Task TimTuDongAsync()
    {
        if (dangXuLy) return;
        try
        {
            DoiTrangThai(true, "Đang tự dò các SQL Server instance trên máy hiện tại...");
            NapMayChu();
            var danhSach = nghiepVu.layDanhSachMayChu();
            var phuHop = new List<string>();
            string? chuoiHopLe = null;
            foreach (string server in danhSach)
            {
                if (huyXuLy.IsCancellationRequested) return;
                try
                {
                    string chuoi = nghiepVu.taoChuoiKetNoi(server,
                        "QLBenhVienRangHamMat", true, "", "");
                    DoiTrangThai(true, "Đang thử SQL Server: " + server);
                    await nghiepVu.kiemTraKetNoiAsync(chuoi, huyXuLy.Token);
                    phuHop.Add(server);
                    chuoiHopLe = chuoi;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Instance không chạy/không có database/quyền đăng nhập: thử máy tiếp theo.
                }
            }

            if (phuHop.Count == 1 && chuoiHopLe is not null)
            {
                cboMayChu.Text = phuHop[0];
                chkWindows.IsChecked = true;
                cboDatabase.Text = "QLBenhVienRangHamMat";
                DoiTrangThai(false,
                    "Đã tìm thấy một database phù hợp. Hãy kiểm tra thông tin rồi bấm Lưu và tiếp tục.");
                return;
            }
            if (phuHop.Count > 1)
            {
                cboMayChu.Text = phuHop[0];
                chkWindows.IsChecked = true;
                cboDatabase.Text = "QLBenhVienRangHamMat";
                DoiTrangThai(false,
                    "Có nhiều SQL Server instance có database phù hợp: " +
                    string.Join(", ", phuHop) + ". Vui lòng chọn đúng máy chủ rồi bấm Lưu và tiếp tục.");
            }
            else
            {
                DoiTrangThai(false,
                    "Chưa tìm được database trên máy bằng Windows Authentication. " +
                    "Bạn có thể nhập SQL Server/tài khoản thủ công, hoặc chạy file Database/QLBenhVienRangHamMat.sql trong SSMS trước.");
            }
        }
        catch (OperationCanceledException)
        {
            // Cửa sổ đã đóng hoặc tác vụ bị hủy: không thao tác lên UI nữa.
        }
        catch (Exception ex)
        {
            DoiTrangThai(false, "Không thể tự cấu hình: " + ex.Message);
        }
    }

    private async void TuDong_Click(object sender, RoutedEventArgs e)
    {
        await TimTuDongAsync();
    }

    private void Thoat_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
