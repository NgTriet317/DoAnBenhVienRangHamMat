using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Printing;
using BUS;

namespace RangHamMat.Views;

/// <summary>
/// In hướng dẫn sau khi nhân viên đã tạo PHIEU_KHAM_BENH ở nghiệp vụ tiếp nhận.
/// Không ghi đè trạng thái tiếp nhận/khám, không tạo bản ghi phiếu khám trùng.
/// </summary>
public partial class PhieuHuongDan : UserControl
{
    // Kích thước A5 dọc (148 x 210 mm), đổi mm sang đơn vị WPF 96 DPI.
    private const double RongGiayA5 = 148.0 * 96.0 / 25.4;
    private const double CaoGiayA5 = 210.0 * 96.0 / 25.4;

    private readonly BUS_PHIEUHUONGDAN busPhieu = new();
    private DataTable danhSachPhieu = new();
    private DataRowView? phieuDangChon;
    private bool dangTai;
    private bool daTai;
    private string? maPhieuCanChon;

    public PhieuHuongDan()
    {
        InitializeComponent();
        Loaded += PhieuHuongDan_Loaded;
        viewerPhieu.Document = TaoTrangTrong();
    }

    /// <summary>Cho màn hình tiếp nhận/màn hình chính chọn phiếu theo mã khi điều hướng tới đây.</summary>
    public void ChonPhieu(string maPhieu)
    {
        maPhieuCanChon = maPhieu;
        if (daTai) ChonPhieuTrongBang();
    }

    private void PhieuHuongDan_Loaded(object sender, RoutedEventArgs e)
    {
        if (daTai) return;
        hienThiDuLieu();
    }

    private void hienThiDuLieu()
    {
        string? maTruoc = maPhieuCanChon ?? phieuDangChon?.Row["MaPhieu"]?.ToString();
        dangTai = true;
        try
        {
            // Phạm vi SQL: quản trị viên đọc tất cả, người khác chỉ phiếu do mình tiếp nhận hoặc được phân công khám.
            danhSachPhieu = busPhieu.layDanhSachPhieu(PhienDangNhap.MaNV, PhienDangNhap.IsAdmin);
            gridPhieu.ItemsSource = danhSachPhieu.DefaultView;
            cboTrangThai.Items.Clear();
            cboTrangThai.Items.Add("Tất cả trạng thái");
            foreach (string trangThai in danhSachPhieu.AsEnumerable()
                         .Select(x => x["TrangThai"]?.ToString() ?? "")
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Distinct(StringComparer.CurrentCultureIgnoreCase)
                         .OrderBy(x => x))
                cboTrangThai.Items.Add(trangThai);
            cboTrangThai.SelectedIndex = 0;
            daTai = true;
            lblThongBao.Text = "Danh sách đã được đồng bộ từ phiếu tiếp nhận.";
        }
        catch (Exception ex)
        {
            lblThongBao.Text = "Không thể tải dữ liệu: " + ex.Message;
            MessageBox.Show(ex.Message, "Phiếu hướng dẫn", MessageBoxButton.OK, MessageBoxImage.Error);
            daTai = false;
        }
        finally { dangTai = false; }

        if (!daTai) return;
        locDuLieu();
        if (!string.IsNullOrWhiteSpace(maTruoc))
        {
            maPhieuCanChon = maTruoc;
            ChonPhieuTrongBang();
        }
    }

    private void ChonPhieuTrongBang()
    {
        if (string.IsNullOrWhiteSpace(maPhieuCanChon)) return;
        // Nếu được mở từ hồ sơ khác ngày, bỏ giới hạn "Hôm nay" để tìm đúng phiếu.
        chkHomNay.IsChecked = false;
        if (cboTrangThai.Items.Count > 0) cboTrangThai.SelectedIndex = 0;
        txtTimKiem.Text = "";
        locDuLieu();
        bool timThay = false;
        foreach (DataRowView item in danhSachPhieu.DefaultView)
        {
            if (!string.Equals(item["MaPhieu"]?.ToString(), maPhieuCanChon, StringComparison.OrdinalIgnoreCase))
                continue;
            gridPhieu.SelectedItem = item;
            gridPhieu.ScrollIntoView(item);
            timThay = true;
            break;
        }
        if (!timThay)
            lblThongBao.Text = "Không tìm thấy phiếu được yêu cầu hoặc tài khoản không có quyền xem phiếu này.";
        maPhieuCanChon = null;
    }

    private void locDuLieu()
    {
        if (!daTai || dangTai || danhSachPhieu.Columns.Count == 0) return;

        string tim = Regex.Replace(txtTimKiem.Text.Trim(), @"[^\p{L}\p{N}\s._-]", "");
        var boLoc = new List<string>();
        if (tim.Length > 0)
        {
            string anToan = tim.Replace("'", "''");
            boLoc.Add($"(MaPhieu LIKE '%{anToan}%' OR MaBN LIKE '%{anToan}%' " +
                      $"OR HoTenBN LIKE '%{anToan}%' OR TenPhong LIKE '%{anToan}%')");
        }
        if (cboTrangThai.SelectedIndex > 0 && cboTrangThai.SelectedItem is string trangThai)
            boLoc.Add("TrangThai = '" + trangThai.Replace("'", "''") + "'");
        if (chkHomNay.IsChecked == true)
        {
            string homNay = DateTime.Today.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            string ngayMai = DateTime.Today.AddDays(1).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            boLoc.Add($"ThoiGianTiepNhan >= #{homNay}# AND ThoiGianTiepNhan < #{ngayMai}#");
        }

        danhSachPhieu.DefaultView.RowFilter = string.Join(" AND ", boLoc);
        lblSoLuong.Text = $"Hiển thị {danhSachPhieu.DefaultView.Count} / {danhSachPhieu.Rows.Count} phiếu";
    }

    private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e) => locDuLieu();
    private void cboTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e) => locDuLieu();
    private void chkHomNay_Changed(object sender, RoutedEventArgs e) => locDuLieu();
    private void LamMoi_Click(object sender, RoutedEventArgs e) => hienThiDuLieu();

    private void gridPhieu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        phieuDangChon = gridPhieu.SelectedItem as DataRowView;
        if (phieuDangChon is null)
        {
            btnIn.IsEnabled = btnMoKham.IsEnabled = false;
            lblMaPhieu.Text = "Chưa chọn phiếu";
            lblTrangThai.Text = "";
            viewerPhieu.Document = TaoTrangTrong();
            return;
        }

        lblMaPhieu.Text = $"{phieuDangChon["MaPhieu"]} · {phieuDangChon["HoTenBN"]}";
        string trangThai = phieuDangChon["TrangThai"]?.ToString() ?? "";
        lblTrangThai.Text = trangThai;
        bool daHuy = trangThai.Contains("hủy", StringComparison.CurrentCultureIgnoreCase) ||
                     trangThai.Contains("huy", StringComparison.CurrentCultureIgnoreCase);
        btnIn.IsEnabled = !daHuy;
        btnMoKham.IsEnabled = !daHuy;
        viewerPhieu.Document = TaoTaiLieu(phieuDangChon);
        lblThongBao.Text = daHuy
            ? "Phiếu đã hủy: không phát phiếu hướng dẫn hoặc chuyển khám từ màn hình này."
            : "Bản xem trước dùng mẫu A5 đã duyệt. Khi lưu PDF, chọn Microsoft Print to PDF và xác nhận khổ giấy A5.";
    }

    private static FixedDocument TaoTrangTrong()
    {
        var trang = new FixedPage
        {
            Width = RongGiayA5,
            Height = CaoGiayA5,
            Background = Brushes.White
        };
        var nhac = new TextBlock
        {
            Text = "Chọn một phiếu tiếp nhận từ danh sách bên trái để xem hướng dẫn.",
            Width = RongGiayA5 - 60,
            FontSize = 15,
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        };
        FixedPage.SetLeft(nhac, 30);
        FixedPage.SetTop(nhac, 50);
        trang.Children.Add(nhac);
        return TaoTaiLieuCoDinh(trang);
    }

    // FixedDocument giữ nguyên chiều ngang A5, không để FlowDocumentPageViewer
    // điều chỉnh vùng dàn trang khiến phần bên phải của khung bị cắt.
    private static FixedDocument TaoTaiLieuCoDinh(FixedPage trang)
    {
        var taiLieu = new FixedDocument();
        var noiDungTrang = new PageContent();
        ((IAddChild)noiDungTrang).AddChild(trang);
        taiLieu.Pages.Add(noiDungTrang);
        return taiLieu;
    }

    /// <summary>
    /// Mẫu A5 đã duyệt: trắng đen như phiếu hóa đơn, khung mảnh, bảng nhãn/nội dung
    /// và phần hướng dẫn di chuyển. Dùng chung cho xem trước lẫn in/PDF.
    /// </summary>
    private static FixedDocument TaoTaiLieu(DataRowView phieu,
        double rongTrang = RongGiayA5, double caoTrang = CaoGiayA5)
    {
        string Lay(string cot) => phieu[cot] == DBNull.Value ||
            string.IsNullOrWhiteSpace(phieu[cot]?.ToString())
                ? "Chưa cập nhật" : phieu[cot]!.ToString()!;
        string Ngay(string cot) => phieu[cot] == DBNull.Value
            ? "Chưa có lịch hẹn"
            : Convert.ToDateTime(phieu[cot], CultureInfo.InvariantCulture)
                .ToString("HH:mm - dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));

        // Màu trung tính để in trắng đen rõ nét, không lệ thuộc resource/theme của ứng dụng.
        var den = Brushes.Black;
        var xamNhat = new SolidColorBrush(Color.FromRgb(238, 238, 238));
        var xamNhan = new SolidColorBrush(Color.FromRgb(248, 248, 248));
        var font = new FontFamily("Times New Roman");
        const double leTrang = 13;
        const double leKhung = 14;
        double rongKhung = Math.Max(330, rongTrang - 2 * leTrang - 4);
        double caoKhung = Math.Max(460, caoTrang - 2 * leTrang - 8);
        double rongNoiDung = rongKhung - 2 * leKhung - 2;

        // Tạo một trang cố định thay vì FlowDocument, dùng chung cho xem trước và in.
        // Nhờ vậy cả hai cột của bảng tiếp nhận nằm trọn trong khổ A5.

        TextBlock Chu(string noiDung, double coChu = 12.5,
            FontWeight? doDam = null, TextAlignment canLe = TextAlignment.Left)
        {
            return new TextBlock
            {
                Text = noiDung,
                FontFamily = font,
                FontSize = coChu,
                FontWeight = doDam ?? FontWeights.Normal,
                TextAlignment = canLe,
                TextWrapping = TextWrapping.Wrap,
                Foreground = den
            };
        }

        Border Vien(UIElement con, Thickness dayVien, Brush? nen = null,
            Thickness? canTrong = null)
        {
            return new Border
            {
                Child = con,
                Background = nen ?? Brushes.White,
                BorderBrush = den,
                BorderThickness = dayVien,
                Padding = canTrong ?? new Thickness(0)
            };
        }

        Border TieuDeMuc(string noiDung)
        {
            var tieuDe = Chu(noiDung, 14, FontWeights.Bold);
            return Vien(tieuDe, new Thickness(0.8), xamNhat,
                new Thickness(10, 4, 8, 4));
        }

        var noiDungPhieu = new StackPanel { Width = rongNoiDung };

        var tenCoSo = Chu("CƠ SỞ KHÁM RĂNG HÀM MẶT", 15, FontWeights.Bold,
            TextAlignment.Center);
        noiDungPhieu.Children.Add(tenCoSo);
        var dongMoTa = Chu("Khoa Răng Hàm Mặt – Khám và điều trị chuyên sâu",
            11.5, null, TextAlignment.Center);
        dongMoTa.Margin = new Thickness(0, 2, 0, 8);
        noiDungPhieu.Children.Add(dongMoTa);
        noiDungPhieu.Children.Add(new Border
        {
            BorderBrush = den, BorderThickness = new Thickness(0, 0, 0, 1),
            Margin = new Thickness(4, 0, 4, 13)
        });

        var tieuDeChinh = Chu("PHIẾU HƯỚNG DẪN KHÁM BỆNH", 19.5,
            FontWeights.Bold, TextAlignment.Center);
        tieuDeChinh.Margin = new Thickness(0, 0, 0, 5);
        noiDungPhieu.Children.Add(tieuDeChinh);
        var maPhieu = Chu("", 14, null, TextAlignment.Center);
        maPhieu.Inlines.Add(new Run("Mã phiếu: "));
        maPhieu.Inlines.Add(new Run(Lay("MaPhieu")) { FontWeight = FontWeights.Bold });
        maPhieu.Margin = new Thickness(0, 0, 0, 17);
        noiDungPhieu.Children.Add(maPhieu);

        // Mỗi dòng chỉ gồm hai ô: tên trường bên trái và nội dung lấy từ SQL bên phải.
        noiDungPhieu.Children.Add(TieuDeMuc("I. THÔNG TIN TIẾP NHẬN"));
        var bang = new Grid { Margin = new Thickness(7, 7, 7, 12) };
        bang.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(0.34, GridUnitType.Star)
        });
        bang.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(0.66, GridUnitType.Star)
        });
        void ThemDong(string nhan, string giaTri)
        {
            int hang = bang.RowDefinitions.Count;
            bang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var textNhan = Chu(nhan, 12.2, FontWeights.Bold);
            var oNhan = Vien(textNhan, new Thickness(0.7), xamNhan,
                new Thickness(8, 4, 5, 4));
            Grid.SetRow(oNhan, hang);
            Grid.SetColumn(oNhan, 0);
            bang.Children.Add(oNhan);
            var textGiaTri = Chu(giaTri, 12.2);
            var oGiaTri = Vien(textGiaTri, new Thickness(0.7), Brushes.White,
                new Thickness(8, 4, 6, 4));
            Grid.SetRow(oGiaTri, hang);
            Grid.SetColumn(oGiaTri, 1);
            bang.Children.Add(oGiaTri);
        }
        ThemDong("Bệnh nhân", Lay("HoTenBN"));
        ThemDong("Mã bệnh nhân", Lay("MaBN"));
        ThemDong("Số thứ tự", Lay("SoThuTu"));
        ThemDong("Khoa khám", Lay("TenKhoa"));
        ThemDong("Phòng khám", Lay("TenPhong"));
        ThemDong("Bác sĩ", Lay("TenBacSi"));
        ThemDong("Dịch vụ", Lay("TenDichVu"));
        ThemDong("Thời gian tiếp nhận", Ngay("ThoiGianTiepNhan"));
        ThemDong("Lịch hẹn", Ngay("NgayGioKham"));
        noiDungPhieu.Children.Add(bang);

        noiDungPhieu.Children.Add(TieuDeMuc("II. HƯỚNG DẪN DI CHUYỂN"));
        var huongDan = new StackPanel { Margin = new Thickness(10, 11, 9, 10) };
        void Buoc(int so, string noiDung)
        {
            var hang = new Grid { Margin = new Thickness(0, 0, 0, 7) };
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hang.Children.Add(Chu(so + ".", 12.5, FontWeights.Bold));
            var chiTiet = Chu(noiDung, 12.5);
            Grid.SetColumn(chiTiet, 1);
            hang.Children.Add(chiTiet);
            huongDan.Children.Add(hang);
        }
        Buoc(1, $"Vui lòng đến {Lay("TenPhong")} thuộc {Lay("TenKhoa")} để chờ khám.");
        Buoc(2, $"Giữ phiếu và chú ý số thứ tự {Lay("SoThuTu")}; xuất trình phiếu khi được gọi.");
        Buoc(3, "Nếu phòng khám, bác sĩ hoặc thời gian có thay đổi, vui lòng liên hệ quầy tiếp nhận để được xác nhận.");
        Buoc(4, "Nếu cần hỗ trợ khẩn cấp khi chờ khám, vui lòng thông báo ngay cho nhân viên y tế.");
        noiDungPhieu.Children.Add(Vien(huongDan, new Thickness(0.8, 0, 0.8, 0.8)));

        var chuKy = Chu("", 12.5, null, TextAlignment.Right);
        chuKy.Margin = new Thickness(0, 17, 0, 10);
        chuKy.FontStyle = FontStyles.Italic;
        chuKy.Inlines.Add(new Run("Nhân viên tiếp nhận: ") { FontWeight = FontWeights.Bold });
        chuKy.Inlines.Add(new Run(Lay("TenNhanVienTiepNhan")));
        noiDungPhieu.Children.Add(chuKy);
        var vachKy = new Border
        {
            BorderBrush = den, BorderThickness = new Thickness(0, 0, 0, 0.7),
            Width = rongNoiDung * 0.42,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 15)
        };
        noiDungPhieu.Children.Add(vachKy);

        var luuY = Chu("", 11.3);
        luuY.FontStyle = FontStyles.Italic;
        luuY.Inlines.Add(new Run("Lưu ý: ") { FontWeight = FontWeights.Bold });
        luuY.Inlines.Add(new Run("Phiếu dùng để hướng dẫn di chuyển, không thay thế hồ sơ bệnh án hoặc đơn thuốc."));
        noiDungPhieu.Children.Add(luuY);

        // Viewbox chỉ thu nhỏ khi tên phòng, dịch vụ hoặc nội dung quá dài.
        // Nhờ vậy bản xem trước và bản PDF luôn là một mặt A5, không cắt dòng.
        var coGian = new Viewbox
        {
            Child = noiDungPhieu,
            Width = rongNoiDung,
            MaxHeight = caoKhung - 2 * leKhung - 2,
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var vienNgoai = new Border
        {
            BorderBrush = den,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            Width = rongKhung,
            Height = caoKhung,
            Padding = new Thickness(leKhung),
            Child = coGian
        };
        var trang = new FixedPage
        {
            Width = rongTrang,
            Height = caoTrang,
            Background = Brushes.White
        };
        FixedPage.SetLeft(vienNgoai, leTrang + 2);
        FixedPage.SetTop(vienNgoai, leTrang + 2);
        trang.Children.Add(vienNgoai);
        return TaoTaiLieuCoDinh(trang);
    }

    private void In_Click(object sender, RoutedEventArgs e)
    {
        if (phieuDangChon is null || !btnIn.IsEnabled) return;
        try
        {
            var hopThoai = new PrintDialog();
            // Đề xuất A5 dọc trước khi mở hộp thoại, người dùng có thể đổi khổ giấy.
            var cauHinhIn = hopThoai.PrintTicket;
            if (cauHinhIn is not null)
            {
                cauHinhIn.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA5);
                cauHinhIn.PageOrientation = PageOrientation.Portrait;
                hopThoai.PrintTicket = cauHinhIn;
            }
            if (hopThoai.ShowDialog() != true) return;

            // Dựng lại trang theo vùng in thực tế để bảng nhãn/nội dung giữ đúng tỷ lệ.
            double rongIn = hopThoai.PrintableAreaWidth > 0
                ? Math.Min(RongGiayA5, hopThoai.PrintableAreaWidth) : RongGiayA5;
            double caoIn = hopThoai.PrintableAreaHeight > 0
                ? Math.Min(CaoGiayA5, hopThoai.PrintableAreaHeight) : CaoGiayA5;
            var document = TaoTaiLieu(phieuDangChon, rongIn, caoIn);
            hopThoai.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,
                "Phiếu hướng dẫn - " + phieuDangChon["MaPhieu"]);
            lblThongBao.Text = "Đã gửi tài liệu đến máy in đã chọn. Chọn Microsoft Print to PDF nếu muốn lưu PDF.";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể in phiếu: " + ex.Message, "Phiếu hướng dẫn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MoKham_Click(object sender, RoutedEventArgs e)
    {
        if (phieuDangChon is null || !btnMoKham.IsEnabled) return;
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            // MainWindow áp dụng lại quyền mở phân hệ và quyền xem hồ sơ/ca khám.
            mainWindow.OpenHoSoApp("KhamDieuTri", phieuDangChon["MaBN"].ToString(),
                medicalId: phieuDangChon["MaHS"].ToString(),
                receiptId: phieuDangChon["MaPhieu"].ToString());
        }
    }
}
