using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BUS;
using ET;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Views;

public partial class TiepNhanAI : Window
{
    private readonly BUS_AITIEPNHAN bus = new();
    private readonly BUS_AI_TAOPHIEU busTao = new();
    private List<ET_AITIEPNHAN> danhSach = new();
    private ET_AITIEPNHAN? dangChon;
    private bool dangTai;
    private bool dangDoiChieu;
    private bool daDoiChieuTuDong;
    private List<ET_AI_BENHNHANTIM> ketQuaDoiChieu = new();

    public string? MaPhieuDuocDuyet { get; private set; }
    public bool MoBenhNhan { get; private set; }

    public TiepNhanAI()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (!CoQuyenDuyetBanKhai())
            {
                MessageBox.Show("Tài khoản không có quyền tiếp nhận bệnh nhân.", "Phân quyền");
                Close();
                return;
            }
            taiDanhMuc();
            hienThiDuLieu();
            txtMaTiepNhan.Focus();
        };
    }

    private bool CoQuyenDuyetBanKhai()
    {
        if (Owner is not MainWindow mainWindow || !mainWindow.CanOpen("PhieuHuongDan")) return false;
        try { return QuyenNghiepVu.LayNguCanh().CoQuyen(QuyenLamViec.TiepNhan); }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or System.Data.Common.DbException)
        {
            return false;
        }
    }

    private void taiDanhMuc()
    {
        try
        {
            dangTai = true;
            cboPhong.ItemsSource = busTao.layPhong();
            cboPhong.SelectedIndex = -1; // phòng phải do nhân viên chọn
            cboBacSi.ItemsSource = new List<ET_AI_DANHMUC> { new() { Ma = "", Ten = "Chưa phân công" } };
            cboBacSi.SelectedIndex = 0;
            cboDichVu.ItemsSource = new List<ET_AI_DANHMUC> { new() { Ma = "", Ten = "Chưa chọn dịch vụ" } };
            cboDichVu.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không tải được danh mục phòng/bác sĩ: " + ex.Message,
                "SQL Kiosk AI", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { dangTai = false; }
        capNhatLoaiKham();
    }

    private void hienThiDuLieu()
    {
        try
        {
            Guid? maCu = dangChon?.MaPhien;
            danhSach = bus.layDanhSachChoDuyet();
            gridPhien.ItemsSource = danhSach;
            gridPhien.SelectedItem = maCu.HasValue ? danhSach.Find(x => x.MaPhien == maCu.Value) : null;
            if (gridPhien.SelectedItem is null && danhSach.Count > 0) gridPhien.SelectedIndex = 0;
            lblThongBao.Text = $"Có {danhSach.Count} bản khai AI chờ xử lý / cần hỗ trợ / đã duyệt.";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không tải được bản khai AI: " + ex.Message, "SQL Kiosk AI",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LamMoi_Click(object sender, RoutedEventArgs e) => hienThiDuLieu();

    private void gridPhien_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Ngăn các sự kiện sửa ngày sinh gọi SQL giữa lúc đang nạp bản khai.
        dangDoiChieu = true;
        dangChon = gridPhien.SelectedItem as ET_AITIEPNHAN;
        var n = dangChon;
        txtMaPhien.Text = n is null ? "Chưa chọn" : n.TrangThai == "DaDuyet" && !string.IsNullOrWhiteSpace(n.MaPhieu)
            ? $"Mã phiên {n.MaRutGon} · Phiếu chính thức {n.MaPhieu} · Số thứ tự {n.SoThuTu?.ToString() ?? "đang cập nhật"}"
            : $"Mã {n.MaRutGon} · {n.TrangThai}";
        txtThongTinGoc.Text = n is null ? "" :
            $"{n.HoTen} · Ngày sinh: {n.NgaySinh:dd/MM/yyyy} · Liên hệ: {n.SoDienThoai}";
        txtTrieuChung.Text = n is null ? "" :
            $"Lý do: {n.LyDoKham}\nTriệu chứng tự khai: {n.TrieuChung}";
        lblCanhBao.Text = n?.CanHoTroGap == true || n?.TrangThai == "CanHoTro"
            ? "CẦN HỖ TRỢ: Không dùng chức năng tự động. Chuyển nhân viên y tế tiếp nhận trực tiếp."
            : "";
        txtHoTenDuyet.Text = n?.HoTen ?? "";
        dpNgaySinhDuyet.SelectedDate = n?.NgaySinh;
        txtDienThoaiDuyet.Text = n?.SoDienThoai ?? "";
        txtLyDoDuyet.Text = n?.LyDoKham ?? "";
        // Loại khám do bệnh nhân lựa chọn là bản khai, nhân viên vẫn được đối chiếu/sửa.
        cboLoaiKham.SelectedIndex = n?.LoaiKham == "DichVu" ? 1 : 0;
        txtTimBenhNhan.Text = n?.SoDienThoai ?? n?.HoTen ?? "";
        cboBenhNhanCu.ItemsSource = null;
        panelBenhNhanCu.Visibility = Visibility.Collapsed;
        daDoiChieuTuDong = false;
        ketQuaDoiChieu.Clear();
        chkDaDoiChieu.IsChecked = false;
        lblTraCuu.Text = n is null ? "Chưa chọn bản khai." : $"Đã chọn {n.MaRutGon}.";
        // Tự đối chiếu theo database, không theo câu trả lời đã từng khám của AI.
        dangDoiChieu = false;
        if (n is not null) doiChieuBenhNhanTuDong();
        else lblKetQuaDoiChieu.Text = "Chưa chọn bản khai.";
        btnTaoPhieu.Content = n?.TrangThai == "DaDuyet"
            ? "Mở phiếu A5 đã duyệt" : "Xác nhận & tạo phiếu A5";
    }

    private void txtMaTiepNhan_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { TraCuuMaAI_Click(sender, e); e.Handled = true; }
    }

    private void TraCuuMaAI_Click(object sender, RoutedEventArgs e)
    {
        string ma = txtMaTiepNhan.Text.Trim().Replace(" ", "").ToUpperInvariant();
        if (ma.Length == 0) { lblTraCuu.Text = "Hãy nhập mã AI-XXXXXXXX."; return; }
        var matches = danhSach.Where(x =>
            string.Equals(x.MaRutGon, ma, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.MaPhien.ToString("D"), ma, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count != 1)
        {
            lblTraCuu.Text = matches.Count == 0 ? "Không tìm thấy mã. Hãy làm mới hoặc kiểm tra SQL." : "Trùng mã rút gọn. Chọn dòng cụ thể.";
            return;
        }
        gridPhien.SelectedItem = matches[0];
        gridPhien.ScrollIntoView(matches[0]);
    }

    private void DuLieuDoiChieu_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!dangDoiChieu && dangChon is not null) doiChieuBenhNhanTuDong();
    }

    private void DuLieuDoiChieu_NgaySinhChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!dangDoiChieu && dangChon is not null && txtHoTenDuyet is not null &&
            txtDienThoaiDuyet is not null && cboBenhNhanCu is not null)
            doiChieuBenhNhanTuDong();
    }

    // Tra cứu mỗi khi chọn bản khai / sửa định danh. Nếu 0 kết quả, tự chọn tạo mới.
    // Nếu đúng 1 hồ sơ khớp đủ 3 trường, tự liên kết. Kết quả nghi trùng phải xử lý riêng.
    private bool doiChieuBenhNhanTuDong()
    {
        if (dangDoiChieu) return daDoiChieuTuDong;
        if (dangChon is null || dpNgaySinhDuyet.SelectedDate is not DateTime ngaySinh ||
            string.IsNullOrWhiteSpace(txtHoTenDuyet.Text) || string.IsNullOrWhiteSpace(txtDienThoaiDuyet.Text))
        {
            daDoiChieuTuDong = false;
            lblKetQuaDoiChieu.Text = "Chưa đủ họ tên, ngày sinh hoặc điện thoại để kiểm tra hồ sơ.";
            return false;
        }
        try
        {
            dangDoiChieu = true;
            var truoc = cboBenhNhanCu.SelectedItem as ET_AI_BENHNHANTIM;
            ketQuaDoiChieu = busTao.doiChieuBenhNhan(txtHoTenDuyet.Text.Trim(), ngaySinh,
                                                     txtDienThoaiDuyet.Text.Trim());
            cboBenhNhanCu.ItemsSource = ketQuaDoiChieu;
            var khopChinhXac = ketQuaDoiChieu.Where(x => x.MucDoKhop == 2).ToList();
            // Chỉ tự liên kết khi thực sự chỉ có 1 hồ sơ trong danh sách đối chiếu.
            // Cùng số điện thoại của người thân hoặc cùng tên/ngày sinh phải báo nghi trùng.
            if (ketQuaDoiChieu.Count == 0)
            {
                cboBenhNhanCu.SelectedIndex = -1;
                panelBenhNhanCu.Visibility = Visibility.Collapsed;
                lblKetQuaDoiChieu.Text = "Không tìm thấy hồ sơ phù hợp trong SQL → tự tạo bệnh nhân mới khi duyệt.";
            }
            else if (ketQuaDoiChieu.Count == 1 && khopChinhXac.Count == 1)
            {
                cboBenhNhanCu.SelectedItem = khopChinhXac[0];
                panelBenhNhanCu.Visibility = Visibility.Collapsed;
                lblKetQuaDoiChieu.Text = $"Đã tìm thấy hồ sơ khớp: {khopChinhXac[0].MaBN} → tự liên kết hồ sơ cũ.";
            }
            else
            {
                panelBenhNhanCu.Visibility = Visibility.Visible;
                var duocChon = truoc is null ? null : ketQuaDoiChieu.FirstOrDefault(x => x.MaBN == truoc.MaBN);
                cboBenhNhanCu.SelectedItem = duocChon;
                lblKetQuaDoiChieu.Text = $"Có {ketQuaDoiChieu.Count} hồ sơ nghi trùng hoặc chưa khớp đủ 3 trường. " +
                    "Không tạo mới tự động; cần chọn đúng hồ sơ sau khi xác minh, hoặc chỉnh lại thông tin.";
            }
            daDoiChieuTuDong = true;
            return true;
        }
        catch (Exception ex)
        {
            daDoiChieuTuDong = false;
            lblKetQuaDoiChieu.Text = "Không kết nối được SQL để đối chiếu: " + ex.Message;
            return false; // Không được xem lỗi SQL như 'không có hồ sơ'.
        }
        finally { dangDoiChieu = false; }
    }

    private void txtTimBenhNhan_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { TimBenhNhan_Click(sender, e); e.Handled = true; }
    }

    private void TimBenhNhan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (txtTimBenhNhan.Text.Trim().Length < 3)
            {
                MessageBox.Show("Nhập ít nhất 3 ký tự để tra cứu hồ sơ bệnh nhân."); return;
            }
            var items = busTao.timBenhNhan(txtTimBenhNhan.Text.Trim());
            // Chỉ các hồ sơ có liên quan đến định danh đang khai mới được lựa chọn.
            var choPhep = ketQuaDoiChieu.Select(x => x.MaBN).ToHashSet(StringComparer.OrdinalIgnoreCase);
            items = items.Where(x => choPhep.Contains(x.MaBN)).ToList();
            cboBenhNhanCu.ItemsSource = items;
            cboBenhNhanCu.SelectedIndex = -1;
            panelBenhNhanCu.Visibility = items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            lblThongBao.Text = $"Tìm thấy {items.Count} hồ sơ liên quan. Không thấy nghĩa là chưa có hồ sơ nghi trùng.";
        }
        catch (Exception ex) { MessageBox.Show("Không tra được bệnh nhân: " + ex.Message); }
    }

    private void cboBenhNhanCu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Không ghi đè thông tin bệnh nhân đang xác nhận khi tự chọn hồ sơ.
        // SQL sẽ kiểm tra họ tên / ngày sinh trước khi liên kết.
        if (dangDoiChieu) return;
        if (cboBenhNhanCu.SelectedItem is ET_AI_BENHNHANTIM p)
            lblKetQuaDoiChieu.Text = $"Đã chọn hồ sơ {p.MaBN}. Kiểm tra danh tính trước khi duyệt.";
    }

    private void cboLoaiKham_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboDichVu is null) return;
        NapBacSiTheoPhong();
    }

    private string loaiKham() => (cboLoaiKham.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ThongThuong";

    private void capNhatLoaiKham()
    {
        bool dichVu = loaiKham() == "DichVu";
        cboDichVu.IsEnabled = dichVu && cboBacSi.SelectedItem is ET_AI_DANHMUC bs && bs.Ma.Length > 0;
        if (!dichVu) cboDichVu.SelectedIndex = 0;
    }

    private void cboPhong_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => NapBacSiTheoPhong();

    private void NapBacSiTheoPhong()
    {
        if (dangTai || cboBacSi is null || cboDichVu is null) return;
        try
        {
            dangTai = true;
            var phong = cboPhong.SelectedItem as ET_AI_DANHMUC;
            var bacSi = phong is null ? new List<ET_AI_DANHMUC>() : busTao.layBacSi(phong.Ma, loaiKham() == "DichVu");
            bacSi.Insert(0, new ET_AI_DANHMUC { Ma = "", Ten = "Chưa phân công" });
            cboBacSi.ItemsSource = bacSi;
            cboBacSi.SelectedIndex = 0;
            cboDichVu.ItemsSource = new List<ET_AI_DANHMUC> { new() { Ma = "", Ten = "Chưa chọn dịch vụ" } };
            cboDichVu.SelectedIndex = 0;
        }
        catch (Exception ex) { MessageBox.Show("Không tải được lịch bác sĩ: " + ex.Message); }
        finally { dangTai = false; }
        capNhatLoaiKham();
    }

    private void cboBacSi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dangTai || cboDichVu is null || cboPhong is null) return;
        try
        {
            dangTai = true;
            var phong = cboPhong.SelectedItem as ET_AI_DANHMUC;
            var bs = cboBacSi.SelectedItem as ET_AI_DANHMUC;
            var ds = phong is null || bs is null || bs.Ma.Length == 0
                ? new List<ET_AI_DANHMUC>() : busTao.layDichVu(phong.Ma, bs.Ma);
            ds.Insert(0, new ET_AI_DANHMUC { Ma = "", Ten = "Chưa chọn dịch vụ" });
            cboDichVu.ItemsSource = ds;
            cboDichVu.SelectedIndex = 0;
        }
        catch (Exception ex) { MessageBox.Show("Không tải được dịch vụ: " + ex.Message); }
        finally { dangTai = false; }
        capNhatLoaiKham();
    }

    private void MoHoSo_Click(object sender, RoutedEventArgs e)
    {
        MoBenhNhan = true;
        Close();
    }

    private void TaoPhieu_Click(object sender, RoutedEventArgs e)
    {
        if (!CoQuyenDuyetBanKhai())
        {
            MessageBox.Show("Tài khoản không còn quyền tiếp nhận bệnh nhân.", "Phân quyền");
            return;
        }
        if (dangChon is null) { MessageBox.Show("Chọn bản khai hoặc nhập mã AI trước."); return; }
        if (dangChon.TrangThai == "DaDuyet")
        {
            if (string.IsNullOrWhiteSpace(dangChon.MaPhieu))
            {
                MessageBox.Show("Phiên đã duyệt nhưng chưa có mã phiếu. Hãy kiểm tra SQL."); return;
            }
            MaPhieuDuocDuyet = dangChon.MaPhieu;
            DialogResult = true;
            return;
        }
        if (dangChon.TrangThai != "ChoNhanVien" || dangChon.CanHoTroGap)
        {
            MessageBox.Show("Chỉ xử lý phiên đang chờ nhân viên và không có cảnh báo cần hỗ trợ gấp."); return;
        }
        if (chkDaDoiChieu.IsChecked != true)
        {
            MessageBox.Show("Hãy kiểm tra dữ liệu và tích 'Tôi đã xác minh' trước khi tạo phiếu."); return;
        }
        if (dpNgaySinhDuyet.SelectedDate is not DateTime ngaySinh)
        {
            MessageBox.Show("Hãy xác nhận ngày sinh của bệnh nhân."); return;
        }
        if (cboPhong.SelectedItem is not ET_AI_DANHMUC phong)
        {
            MessageBox.Show("Hãy chọn phòng khám."); return;
        }
        // Bắt buộc tra SQL lại ngay trước khi tạo, không tin dữ liệu kiểm tra cũ.
        if (!doiChieuBenhNhanTuDong())
        {
            MessageBox.Show("Không đối chiếu được hồ sơ bệnh nhân. Kiểm tra kết nối SQL và định danh."); return;
        }
        var cu = cboBenhNhanCu.SelectedItem as ET_AI_BENHNHANTIM;
        if (ketQuaDoiChieu.Count > 0 && cu is null)
        {
            MessageBox.Show("Có hồ sơ nghi trùng. Hãy xác minh và chọn đúng bệnh nhân, " +
                            "không tự tạo hồ sơ mới khi còn nghi trùng."); return;
        }
        var bs = cboBacSi.SelectedItem as ET_AI_DANHMUC;
        var dv = cboDichVu.SelectedItem as ET_AI_DANHMUC;
        var m = new ET_AI_TAOPHIEU
        {
            MaPhien = dangChon.MaPhien,
            MaNV = PhienDangNhap.MaNV,
            MaBNHienCo = cu?.MaBN,
            HoTen = txtHoTenDuyet.Text.Trim(), NgaySinh = ngaySinh,
            SDT = txtDienThoaiDuyet.Text.Trim(), LyDoKham = txtLyDoDuyet.Text.Trim(),
            LoaiKham = loaiKham(), MaPhong = phong.Ma,
            MaBS = bs is { Ma.Length: > 0 } ? bs.Ma : null,
            MaDV = loaiKham() == "DichVu" && dv is { Ma.Length: > 0 } ? dv.Ma : null
        };
        if (MessageBox.Show($"Xác nhận tạo phiếu khám cho {m.HoTen}?\n" +
                            $"Phòng: {phong.Ten}\nLoại khám: {(m.LoaiKham == "DichVu" ? "Dịch vụ" : "Thông thường")}\n" +
                            "Dữ liệu sẽ được ghi chính thức vào bệnh nhân, hồ sơ và phiếu khám.",
                "Xác nhận tạo phiếu từ AI", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes) return;
        try
        {
            btnTaoPhieu.IsEnabled = false;
            var result = busTao.taoPhieu(m);
            MaPhieuDuocDuyet = result.MaPhieu;
            MessageBox.Show($"Đã tạo phiếu {result.MaPhieu}\nMã bệnh nhân: {result.MaBN}\n" +
                $"Số thứ tự: {result.SoThuTu}\nPhiếu A5 sẽ được mở trong Phiếu hướng dẫn.",
                "Tiếp nhận thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không tạo được phiếu: " + ex.Message + "\n\n" +
                "Nếu bệnh nhân có hồ sơ nghi trùng, hãy tra cứu và liên kết hồ sơ cũ. " +
                "Nếu là khám dịch vụ, kiểm tra bác sĩ có ca được xác nhận và có mặt tại phòng ở thời điểm tiếp nhận.",
                "Kiểm tra dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            // Không xóa các trường đang nhập sau lỗi để có thể sửa và thử lại.
            lblThongBao.Text = "Tạo phiếu chưa thành công; kiểm tra nội dung lỗi và xác minh lại.";
        }
        finally { btnTaoPhieu.IsEnabled = true; }
    }
}
