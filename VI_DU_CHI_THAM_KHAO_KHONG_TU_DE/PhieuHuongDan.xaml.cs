using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BUS;

namespace RangHamMat.Views;

/// <summary>
/// In hướng dẫn sau khi nhân viên đã tạo PHIEU_KHAM_BENH ở nghiệp vụ tiếp nhận.
/// Không ghi đè trạng thái tiếp nhận/khám, không tạo bản ghi phiếu khám trùng.
/// </summary>
public partial class PhieuHuongDan : UserControl
{
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
    // Mở danh sách những bản khai được bệnh nhân gửi từ Kiosk AI.
    private void MoTiepNhanAI_Click(object sender, RoutedEventArgs e)
    {
        var cuaSoCha = Window.GetWindow(this);
        var cuaSo = new TiepNhanAI();
        if (cuaSoCha != null) cuaSo.Owner = cuaSoCha;
        cuaSo.ShowDialog();
        if (!string.IsNullOrWhiteSpace(cuaSo.MaPhieuDuocDuyet))
        {
            hienThiDuLieu();
            ChonPhieu(cuaSo.MaPhieuDuocDuyet);
        }
        else if (cuaSo.MoBenhNhan && cuaSoCha is RangHamMat.MainWindow mainWindow)
        {
            mainWindow.OpenApp("BenhNhan");
        }
    }

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
            : "Kiểm tra thông tin trước khi in. Muốn lưu PDF, chọn 'Microsoft Print to PDF' trong hộp thoại in.";
    }

    private static FlowDocument TaoTrangTrong()
    {
        var taiLieu = new FlowDocument { FontSize = 15, PagePadding = new Thickness(36) };
        taiLieu.Blocks.Add(new Paragraph(new Run("Chọn một phiếu tiếp nhận từ danh sách bên trái để xem hướng dẫn."))
        {
            Foreground = Brushes.Gray, TextAlignment = TextAlignment.Center
        });
        return taiLieu;
    }

    private static FlowDocument TaoTaiLieu(DataRowView phieu)
    {
        string Lay(string cot) => phieu[cot] == DBNull.Value ? "Chưa cập nhật" :
            (phieu[cot]?.ToString() ?? "Chưa cập nhật");
        string Ngay(string cot) => phieu[cot] == DBNull.Value ? "Chưa có lịch hẹn" :
            Convert.ToDateTime(phieu[cot], CultureInfo.InvariantCulture).ToString("HH:mm - dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Times New Roman"), FontSize = 15,
            Foreground = new SolidColorBrush(Color.FromRgb(20, 38, 60)),
            // Một cột duy nhất để phần xem trước tự dàn theo chiều rộng có sẵn.
            PagePadding = new Thickness(42), ColumnWidth = double.NaN
        };
        doc.Blocks.Add(new Paragraph(new Run("CƠ SỞ KHÁM RĂNG HÀM MẶT"))
        {
            FontSize = 14, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 5)
        });
        doc.Blocks.Add(new Paragraph(new Run("PHIẾU HƯỚNG DẪN KHÁM BỆNH"))
        {
            FontSize = 22, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 3, 0, 8)
        });
        doc.Blocks.Add(new Paragraph(new Run("Mã phiếu: " + Lay("MaPhieu")))
        {
            TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20)
        });

        // FlowDocumentScrollViewer không đo ổn định cột '*' trong Table:
        // cột nhãn giãn chiếm gần hết trang, cột giá trị chỉ còn vài pixel.
        // Dùng Paragraph + Inline để nội dung tự xuống dòng, xem trước và in giống nhau.
        doc.Blocks.Add(new Paragraph(new Run("THÔNG TIN TIẾP NHẬN"))
        {
            FontSize = 16, FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 4, 0, 10)
        });

        void Dong(string nhan, string giaTri)
        {
            var dong = new Paragraph
            {
                Margin = new Thickness(0, 3, 0, 9),
                TextAlignment = TextAlignment.Left,
                LineHeight = 22
            };
            dong.Inlines.Add(new Bold(new Run(nhan + ":  ")));
            dong.Inlines.Add(new Run(giaTri));
            doc.Blocks.Add(dong);
        }
        Dong("Bệnh nhân", Lay("HoTenBN"));
        Dong("Mã bệnh nhân", Lay("MaBN"));
        Dong("Số thứ tự", Lay("SoThuTu"));
        Dong("Khoa khám", Lay("TenKhoa"));
        Dong("Phòng khám", Lay("TenPhong"));
        Dong("Bác sĩ", Lay("TenBacSi"));
        Dong("Dịch vụ", Lay("TenDichVu"));
        Dong("Thời gian tiếp nhận", Ngay("ThoiGianTiepNhan"));
        Dong("Lịch hẹn", Ngay("NgayGioKham"));

        doc.Blocks.Add(new Paragraph(new Run("HƯỚNG DẪN DI CHUYỂN"))
        {
            FontWeight = FontWeights.Bold, FontSize = 16, Margin = new Thickness(0, 10, 0, 7)
        });
        doc.Blocks.Add(new Paragraph(new Run(
            $"1. Vui lòng đến {Lay("TenPhong")} thuộc {Lay("TenKhoa")} để chờ khám."))
        { Margin = new Thickness(0, 2, 0, 7) });
        doc.Blocks.Add(new Paragraph(new Run(
            $"2. Giữ phiếu và chú ý số thứ tự {Lay("SoThuTu")}; xuất trình phiếu khi được gọi."))
        { Margin = new Thickness(0, 2, 0, 7) });
        doc.Blocks.Add(new Paragraph(new Run(
            "3. Nếu phòng khám, bác sĩ hoặc thời gian có thay đổi, vui lòng liên hệ quầy tiếp nhận để được xác nhận."))
        { Margin = new Thickness(0, 2, 0, 7) });
        doc.Blocks.Add(new Paragraph(new Run(
            "4. Nếu cần hỗ trợ khẩn cấp khi chờ khám, vui lòng thông báo ngay cho nhân viên y tế."))
        { Margin = new Thickness(0, 2, 0, 12) });
        doc.Blocks.Add(new Paragraph(new Run("Nhân viên tiếp nhận: " + Lay("TenNhanVienTiepNhan")))
        { FontStyle = FontStyles.Italic, Margin = new Thickness(0, 12, 0, 0) });
        doc.Blocks.Add(new Paragraph(new Run("Phiếu dùng để hướng dẫn di chuyển, không thay thế hồ sơ bệnh án hoặc đơn thuốc."))
        { FontSize = 12, Foreground = Brushes.Gray, Margin = new Thickness(0, 18, 0, 0) });
        return doc;
    }

    private void In_Click(object sender, RoutedEventArgs e)
    {
        if (phieuDangChon is null || !btnIn.IsEnabled) return;
        try
        {
            var hopThoai = new PrintDialog();
            if (hopThoai.ShowDialog() != true) return;
            var document = TaoTaiLieu(phieuDangChon);
            document.PageWidth = hopThoai.PrintableAreaWidth;
            document.PageHeight = hopThoai.PrintableAreaHeight;
            // Giữ một cột khi in/PDF, không dàn sang nhiều cột hoặc bó hẹp giá trị.
            document.ColumnWidth = double.NaN;
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
