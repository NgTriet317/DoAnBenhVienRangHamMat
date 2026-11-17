using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BUS;

namespace RangHamMat.Views
{
    public partial class ChamCong : UserControl
    {
        private readonly BUS_CHAMCONG busChamCong = new BUS_CHAMCONG();
        // SQL Server quyết định quyền chấm; 1 giây cập nhật UI, 10 giây đồng bộ lại giờ máy chủ.
        private readonly DispatcherTimer dongHo = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        private readonly Stopwatch boDemMayChu = new Stopwatch();
        private DateTime mocGioMayChu;
        private bool coGioMayChu;
        private DataTable danhSachCong = new DataTable();
        private DataView? danhSachDuocCham;
        private string locTimKiem = "";
        private string locTrangThai = "";
        private string maLLVDangChon = "";
        private DateTime gioMayChu;
        private bool daTai;
        private bool dangTai;
        private bool dangPhanLoai;
        private bool dangChon;
        private bool dangChamCong;

        public ChamCong()
        {
            InitializeComponent();
            DangKyDongBoManHinh();
            cboThang.SelectedIndex = DateTime.Today.Month - 1;
            txtNam.Text = DateTime.Today.Year.ToString();
            dongHo.Tick += (_, _) =>
            {
                if (IsLoaded && daTai && !dangTai && !dangChamCong)
                    capNhatPhanLoai();
            };
            Loaded += (_, _) =>
            {
                dongHo.Start();
                if (!daTai) hienThiDuLieu();
            };
            Unloaded += (_, _) => dongHo.Stop();
        }

        public void hienThiDuLieu()
        {
            if (dangTai) return;
            if (string.IsNullOrWhiteSpace(PhienDangNhap.MaTK) || string.IsNullOrWhiteSpace(PhienDangNhap.MaNV))
            {
                lblThongBao.Text = "Cần đăng nhập bằng tài khoản đã liên kết nhân viên để xem chấm công.";
                return;
            }
            if (cboThang.SelectedIndex < 0 || !int.TryParse(txtNam.Text, out int nam) || nam < 2000 || nam > 2100)
            {
                lblThongBao.Text = "Vui lòng nhập kỳ chấm công hợp lệ.";
                return;
            }
            dangTai = true;
            try
            {
                // Không thay đổi phương thức BUS/DAL lấy danh sách hiện tại.
                danhSachCong = dongBoChamCong ?? busChamCong.layChamCongTheoThang(
                    cboThang.SelectedIndex + 1, nam, PhienDangNhap.MaTK);

                if (!danhSachCong.Columns.Contains("DuocCham"))
                    danhSachCong.Columns.Add("DuocCham", typeof(bool));
                if (!danhSachCong.Columns.Contains("LyDoKhongCham"))
                    danhSachCong.Columns.Add("LyDoKhongCham", typeof(string));
                if (!danhSachCong.Columns.Contains("CongDuKien"))
                    danhSachCong.Columns.Add("CongDuKien", typeof(decimal));
                if (!danhSachCong.Columns.Contains("SoPhutDiTreDuKien"))
                    danhSachCong.Columns.Add("SoPhutDiTreDuKien", typeof(int));

                const string sapXep = "NgayLam DESC, GioBatDau ASC, HoTen ASC";
                danhSachCong.DefaultView.Sort = sapXep;
                danhSachDuocCham = new DataView(danhSachCong) { Sort = sapXep };

                dangPhanLoai = true;
                try
                {
                    gridDuocCham.ItemsSource = null;
                    gridChamCong.ItemsSource = null;
                    gridDuocCham.ItemsSource = danhSachDuocCham;
                    gridChamCong.ItemsSource = danhSachCong.DefaultView;
                }
                finally { dangPhanLoai = false; }

                lblTongCa.Text = danhSachCong.Rows.Count.ToString("N0");
                lblChuaCham.Text = danhSachCong.AsEnumerable().Count(
                    r => r.Field<string>("TrangThai") == "Chưa chấm công").ToString("N0");
                lblDaCham.Text = danhSachCong.AsEnumerable().Count(
                    r => (r.Field<string>("TrangThai") ?? "").StartsWith("Đã chấm công", StringComparison.Ordinal)).ToString("N0");

                daTai = true;
                lblThongBao.Text = PhienDangNhap.IsAdmin
                    ? "BQL xem toàn bộ ca; chỉ được chấm công ca của chính mình. Ca hết giờ sẽ khóa tự động."
                    : "Chỉ hiển thị ca của bạn. Chấm trước khi hết ca; đúng giờ 0,5 công, trễ tối đa 60 phút 0,25 công, trễ hơn 60 phút 0 công.";
                capNhatPhanLoai();
            }
            catch (Exception ex)
            {
                if (RangHamMat.Services.DongBoManHinh.DangCapNhatTuDong) throw;
                daTai = false;
                danhSachDuocCham = null;
                gridDuocCham.ItemsSource = null;
                gridChamCong.ItemsSource = null;
                danhSachCong = new DataTable();
                xoaChiTiet();
                lblThongBao.Text = "Không thể tải dữ liệu chấm công: " + ex.Message;
                MessageBox.Show(lblThongBao.Text, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { dangTai = false; }
        }

        // Cùng một nguồn dữ liệu nhưng hai DataView được lọc riêng, không ghi dữ liệu phân loại xuống SQL.
        private void locDuLieu()
        {
            if (danhSachCong.Columns.Count == 0 || danhSachDuocCham == null) return;
            string dieuKien = locTimKiem;
            if (locTrangThai != "") dieuKien += (dieuKien == "" ? "" : " AND ") + locTrangThai;
            string tienTo = dieuKien == "" ? "" : dieuKien + " AND ";
            danhSachDuocCham.RowFilter = tienTo + "DuocCham = true";
            danhSachCong.DefaultView.RowFilter = tienTo + "DuocCham = false";
            lblCaDuocCham.Text = $"{danhSachDuocCham.Count:N0} ca có thể chấm công";
            lblSoDong.Text = $"{danhSachCong.DefaultView.Count:N0} ca không thể thao tác · " +
                                $"{danhSachDuocCham.Count + danhSachCong.DefaultView.Count:N0} ca đang hiển thị";
        }

        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            string noiDung = Regex.Replace(txtTimKiem.Text, @"[^\p{L}\p{M}\p{N} ]", "");
            if (txtTimKiem.Text != noiDung)
            {
                int viTri = txtTimKiem.SelectionStart;
                txtTimKiem.Text = noiDung;
                txtTimKiem.SelectionStart = Math.Min(viTri, noiDung.Length);
                return;
            }
            locTimKiem = noiDung.Trim() == "" ? "" :
                $"(MaNV LIKE '%{noiDung.Trim()}%' OR HoTen LIKE '%{noiDung.Trim()}%')";
            if (daTai && !dangTai) { locDuLieu(); chonLaiCa(); capNhatChiTiet(); }
        }

        private void cboTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string trangThai = (cboTrangThai.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            locTrangThai = trangThai == "Tất cả trạng thái" || trangThai == "" ? "" :
                trangThai == "Đã chấm công" ? "TrangThai LIKE 'Đã chấm công%'" :
                $"TrangThai = '{trangThai}'";
            if (daTai && !dangTai) { locDuLieu(); chonLaiCa(); capNhatChiTiet(); }
        }

        private void cboThang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (daTai && !dangTai) hienThiDuLieu();
        }

        private void txtNam_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (daTai && !dangTai && txtNam.Text.Length == 4 && int.TryParse(txtNam.Text, out _))
                hienThiDuLieu();
        }

        private void btnTaiLai_Click(object sender, RoutedEventArgs e) => hienThiDuLieu();

        // Tính lại theo giờ SQL Server. Khi không kết nối được, toàn bộ ca bị khóa an toàn.
        private void capNhatPhanLoai()
        {
            if (danhSachCong.Columns.Count == 0 || danhSachDuocCham == null) return;
            string? loiDongHo = null;
            if (!coGioMayChu || !boDemMayChu.IsRunning ||
                boDemMayChu.Elapsed >= TimeSpan.FromSeconds(10))
            {
                try
                {
                    mocGioMayChu = busChamCong.layThoiGianMayChu();
                    boDemMayChu.Restart();
                    coGioMayChu = true;
                }
                catch (Exception ex)
                {
                    coGioMayChu = false;
                    boDemMayChu.Reset();
                    loiDongHo = "Không xác định được giờ SQL Server: " + ex.Message;
                }
            }
            if (coGioMayChu)
                gioMayChu = mocGioMayChu.Add(boDemMayChu.Elapsed);
            else
                loiDongHo ??= "Chưa đồng bộ được giờ SQL Server. Vui lòng tải lại.";

            dangPhanLoai = true;
            try
            {
                foreach (DataRow dong in danhSachCong.Rows)
                {
                    string lyDo = loiDongHo ?? layLyDoKhongCham(dong);
                    dong["DuocCham"] = lyDo == "";
                    dong["LyDoKhongCham"] = lyDo;
                    int soPhutDiTre = 0;
                    if (coGioMayChu)
                    {
                        DateTime mocBatDau = Convert.ToDateTime(dong["NgayLam"]).Date
                            .Add((TimeSpan)dong["GioBatDau"]);
                        if (gioMayChu > mocBatDau)
                            soPhutDiTre = (int)Math.Ceiling((gioMayChu - mocBatDau).TotalMinutes);
                    }
                    dong["SoPhutDiTreDuKien"] = soPhutDiTre;
                    dong["CongDuKien"] = soPhutDiTre == 0 ? 0.5m :
                        (soPhutDiTre <= 60 ? 0.25m : 0m);
                }
            }
            finally { dangPhanLoai = false; }

            locDuLieu();
            chonLaiCa();
            capNhatChiTiet();
            if (loiDongHo != null) lblThongBao.Text = loiDongHo;
        }

        // Mọi ca đều có lý do rõ ràng nếu không được phép. Điều kiện DAL là quyết định cuối cùng.
        private string layLyDoKhongCham(DataRow hang)
        {
            if (!Convert.ToBoolean(hang["LaCaCuaToi"]))
                return "Chỉ được xem; không thể chấm công thay nhân viên khác.";
            if (Convert.ToBoolean(hang["DaChot"]))
                return "Kỳ lương đã chốt, không thể thay đổi công.";
            if (Convert.ToBoolean(hang["CoDonNghi"]))
                return "Ngày này có đơn nghỉ phép được duyệt.";
            if (hang["TrangThaiLich"].ToString() != "Đã xác nhận")
                return "Ca chưa được xác nhận phân công hoặc đã bị hủy/nghỉ.";
            if (Convert.ToBoolean(hang["DaCoGhiNhan"]))
                return "Ca đã có dữ liệu chấm công; không thể chấm lại.";

            DateTime ngay = Convert.ToDateTime(hang["NgayLam"]).Date;
            if (ngay < gioMayChu.Date) return "Đã quá ngày làm việc, không được chấm bù.";
            if (ngay > gioMayChu.Date) return "Chưa đến ngày làm việc.";

            TimeSpan batDau = (TimeSpan)hang["GioBatDau"];
            TimeSpan ketThuc = (TimeSpan)hang["GioKetThuc"];
            DateTime mocKetThuc = ngay.Add(ketThuc);
            if (ketThuc <= batDau) mocKetThuc = mocKetThuc.AddDays(1);
            if (gioMayChu >= mocKetThuc)
                return $"Ca đã hết giờ lúc {mocKetThuc:HH:mm}. Không được chấm công bù.";

            if (hang["GioKetThucCaTruoc"] is TimeSpan ketThucTruoc &&
                hang["GioBatDauCaTruoc"] is TimeSpan batDauTruoc)
            {
                DateTime mocCaTruoc = ngay.Add(ketThucTruoc);
                if (ketThucTruoc <= batDauTruoc) mocCaTruoc = mocCaTruoc.AddDays(1);
                if (gioMayChu < mocCaTruoc)
                    return $"Chưa đến lượt. Ca trước kết thúc lúc {mocCaTruoc:HH:mm}.";
            }
            return "";
        }

        // Giữ một dòng đang chọn dù nó vừa chuyển từ khung có thể chấm sang khung bị khóa.
        private void chonLaiCa()
        {
            if (danhSachDuocCham == null) return;
            DataRowView? duocCham = null;
            DataRowView? khongTheCham = null;
            if (maLLVDangChon != "")
            {
                duocCham = danhSachDuocCham.Cast<DataRowView>().FirstOrDefault(
                    x => x["MaLLV"].ToString() == maLLVDangChon);
                khongTheCham = danhSachCong.DefaultView.Cast<DataRowView>().FirstOrDefault(
                    x => x["MaLLV"].ToString() == maLLVDangChon);
            }
            if (duocCham == null && khongTheCham == null)
            {
                duocCham = danhSachDuocCham.Cast<DataRowView>().FirstOrDefault();
                if (duocCham == null)
                    khongTheCham = danhSachCong.DefaultView.Cast<DataRowView>().FirstOrDefault();
            }
            dangChon = true;
            try
            {
                gridDuocCham.SelectedItem = duocCham;
                gridChamCong.SelectedItem = duocCham == null ? khongTheCham : null;
            }
            finally { dangChon = false; }
            var ca = duocCham ?? khongTheCham;
            maLLVDangChon = ca?["MaLLV"]?.ToString() ?? "";
        }

        private void gridChamCong_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dangPhanLoai || dangChon) return;
            if (sender is not DataGrid grid || grid.SelectedItem is not DataRowView hang)
            {
                if (gridDuocCham.SelectedItem == null && gridChamCong.SelectedItem == null) xoaChiTiet();
                return;
            }
            dangChon = true;
            try
            {
                if (ReferenceEquals(grid, gridDuocCham)) gridChamCong.SelectedItem = null;
                else gridDuocCham.SelectedItem = null;
            }
            finally { dangChon = false; }
            maLLVDangChon = hang["MaLLV"].ToString() ?? "";
            capNhatChiTiet();
        }

        private void capNhatChiTiet()
        {
            if ((gridDuocCham.SelectedItem ?? gridChamCong.SelectedItem) is not DataRowView hang)
            {
                xoaChiTiet();
                return;
            }
            lblNhanVien.Text = hang["HoTen"] + " · " + hang["MaNV"];
            DateTime ngay = Convert.ToDateTime(hang["NgayLam"]);
            lblThongTinCa.Text = $"{ngay:dd/MM/yyyy} • {hang["MaCa"]} • {hang["TenChucVu"]} • {hang["TrangThai"]}";
            TimeSpan batDau = (TimeSpan)hang["GioBatDau"];
            TimeSpan ketThuc = (TimeSpan)hang["GioKetThuc"];
            lblKhungGio.Text = $"{batDau:hh\\:mm} — {ketThuc:hh\\:mm}";
            lblGhiNhan.Text = hang["GioVao"] is DateTime vao ? vao.ToString("dd/MM/yyyy HH:mm") : "Chưa chấm công";
            bool duocCham = hang["DuocCham"] is bool b && b;
            string lyDo = hang["LyDoKhongCham"].ToString() ?? "";
            if (duocCham)
            {
                int phut = Convert.ToInt32(hang["SoPhutDiTreDuKien"]);
                decimal cong = Convert.ToDecimal(hang["CongDuKien"]);
                lblCanhBao.Text = $"Chấm công lúc này: đi trễ {phut} phút, được {cong:0.##} công. " +
                    "Hệ thống tính lại chính xác theo giờ SQL Server khi xác nhận.";
            }
            else if (Convert.ToBoolean(hang["DaCoGhiNhan"]))
            {
                int phut = Convert.ToInt32(hang["SoPhutDiTre"]);
                decimal cong = Convert.ToDecimal(hang["CongThucNhan"]);
                lblCanhBao.Text = $"{lyDo} Đi trễ {phut} phút, thực nhận {cong:0.##} công.";
            }
            else lblCanhBao.Text = lyDo;
            btnChamCong.IsEnabled = duocCham && !dangChamCong;
            btnChamCong.Content = duocCham ? "Chấm công ca này" : "Không thể chấm công";
        }

        private void xoaChiTiet()
        {
            lblNhanVien.Text = "Chọn một ca làm việc";
            lblThongTinCa.Text = "—";
            lblKhungGio.Text = "—";
            lblGhiNhan.Text = "Chưa chấm công";
            lblCanhBao.Text = "";
            btnChamCong.IsEnabled = false;
            btnChamCong.Content = "Chấm công ca này";
        }

        private void btnChamCong_Click(object sender, RoutedEventArgs e)
        {
            if (gridDuocCham.SelectedItem is DataRowView hang && btnChamCong.IsEnabled)
                thucHienChamCong(hang.Row);
        }

        private void btnChamTrongDanhSach_Click(object sender, RoutedEventArgs e)
        {
            string maLLV = (sender as Button)?.CommandParameter?.ToString() ?? "";
            DataRow? ca = danhSachCong.AsEnumerable().FirstOrDefault(
                x => x["MaLLV"].ToString() == maLLV);
            if (ca != null) thucHienChamCong(ca);
        }

        private void thucHienChamCong(DataRow ca)
        {
            if (dangChamCong || ca["DuocCham"] is not bool duocCham || !duocCham) return;
            string maLLV = ca["MaLLV"].ToString() ?? "";
            if (maLLV == "") return;
            int phutDuKien = Convert.ToInt32(ca["SoPhutDiTreDuKien"]);
            decimal congDuKien = Convert.ToDecimal(ca["CongDuKien"]);
            string xacNhan = "Xác nhận chấm công ca " + ca["MaCa"] + " ngày " +
                Convert.ToDateTime(ca["NgayLam"]).ToString("dd/MM/yyyy") + "?" +
                $"\nĐi trễ dự kiến: {phutDuKien} phút; công dự kiến: {congDuKien:0.##}." +
                "\nMức công cuối cùng được xác định theo giờ SQL Server khi ghi nhận.";
            if (MessageBox.Show(xacNhan, "Chấm công", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            dangChamCong = true;
            btnChamCong.IsEnabled = false;
            try
            {
                // DAL kiểm tra lại giờ SQL Server trong cùng giao dịch trước khi ghi nhận.
                busChamCong.chamCong(maLLV, PhienDangNhap.MaTK);
                maLLVDangChon = maLLV;
                hienThiDuLieu();
                DataRow? daCham = danhSachCong.AsEnumerable().FirstOrDefault(
                    r => r["MaLLV"].ToString() == maLLV);
                string ketQua = daCham != null
                    ? $" Đi trễ {Convert.ToInt32(daCham["SoPhutDiTre"])} phút; " +
                      $"thực nhận {Convert.ToDecimal(daCham["CongThucNhan"]):0.##} công."
                    : " Công thực nhận sẽ được thể hiện sau khi tải lại dữ liệu.";
                MessageBox.Show("Đã chấm công thành công." + ketQua, "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể chấm công: " + ex.Message,
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                hienThiDuLieu();
            }
            finally
            {
                dangChamCong = false;
                capNhatChiTiet();
            }
        }
    }
}
