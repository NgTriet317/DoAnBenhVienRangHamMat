using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;

namespace RangHamMat.Views
{
    public partial class ChamCong : UserControl
    {
        private readonly BUS_CHAMCONG busChamCong = new BUS_CHAMCONG();
        private DataTable danhSachCong = new DataTable();
        private string locTimKiem = "";
        private string locTrangThai = "";
        private bool daTai;
        private bool dangTai;

        public ChamCong()
        {
            InitializeComponent();
            cboThang.SelectedIndex = DateTime.Today.Month - 1;
            txtNam.Text = DateTime.Today.Year.ToString();
            Loaded += (sender, e) => { if (!daTai) hienThiDuLieu(); };
        }

        public void hienThiDuLieu()
        {
            if (dangTai) return;
            if (!PhienDangNhap.IsAdmin || string.IsNullOrWhiteSpace(PhienDangNhap.MaNV))
            {
                lblThongBao.Text = "Chỉ tài khoản BQL/Admin đã gắn nhân viên mới được duyệt chấm công.";
                return;
            }
            if (cboThang.SelectedIndex < 0 || !int.TryParse(txtNam.Text, out int nam)
                || nam < 2000 || nam > 2100)
            {
                lblThongBao.Text = "Vui lòng nhập kỳ chấm công hợp lệ.";
                return;
            }
            dangTai = true;
            try
            {
                danhSachCong = busChamCong.layChamCongTheoThang(cboThang.SelectedIndex + 1, nam);
                danhSachCong.DefaultView.Sort = "NgayLam ASC, MaCa ASC, HoTen ASC";
                // Bỏ bảng cũ để DataGrid đo lại đúng chiều rộng cột khi đổi kỳ.
                gridChamCong.SelectedItem = null;
                gridChamCong.ItemsSource = null;
                gridChamCong.ItemsSource = danhSachCong.DefaultView;
                lblTongCa.Text = danhSachCong.Rows.Count.ToString("N0");
                lblChoDuyet.Text = danhSachCong.AsEnumerable().Count(r => r.Field<string>("TrangThai") == "Chờ đối soát").ToString("N0");
                lblDaDuyet.Text = danhSachCong.AsEnumerable().Count(r => r.Field<string>("TrangThai") == "Đã xác nhận").ToString("N0");
                locDuLieu();
                if (danhSachCong.DefaultView.Count > 0) gridChamCong.SelectedIndex = 0;
                else xoaChiTiet();
                daTai = true;
                lblThongBao.Text = "BQL có thể duyệt ca ngắn hơn 8 giờ, mỗi ca hoàn tất tính 0,5 công; không tính lại bảng lương đã lưu.";
            }
            catch (Exception ex)
            {
                daTai = false;
                gridChamCong.ItemsSource = null;
                danhSachCong = new DataTable();
                xoaChiTiet();
                lblThongBao.Text = "Không thể tải dữ liệu chấm công: " + ex.Message;
                MessageBox.Show(lblThongBao.Text, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { dangTai = false; }
        }

        private void locDuLieu()
        {
            if (danhSachCong.Columns.Count == 0) return;
            string dieuKien = locTimKiem;
            if (locTrangThai != "") dieuKien += (dieuKien == "" ? "" : " AND ") + locTrangThai;
            danhSachCong.DefaultView.RowFilter = dieuKien;
            lblSoDong.Text = $"{danhSachCong.DefaultView.Count:N0} ca đang hiển thị";
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
            if (daTai && !dangTai) locDuLieu();
        }

        private void cboTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string trangThai = (cboTrangThai.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            locTrangThai = trangThai == "Tất cả trạng thái" || trangThai == "" ? "" : $"TrangThai = '{trangThai}'";
            if (daTai && !dangTai) locDuLieu();
        }

        private void cboThang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (daTai && !dangTai) hienThiDuLieu();
        }

        private void txtNam_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (daTai && !dangTai && txtNam.Text.Length == 4 && int.TryParse(txtNam.Text, out _)) hienThiDuLieu();
        }

        private void btnTaiLai_Click(object sender, RoutedEventArgs e) => hienThiDuLieu();

        private void gridChamCong_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridChamCong.SelectedItem is not DataRowView hang) { xoaChiTiet(); return; }
            lblNhanVien.Text = hang["HoTen"] + "  ·  " + hang["MaNV"];
            DateTime ngay = Convert.ToDateTime(hang["NgayLam"]);
            lblThongTinCa.Text = $"{ngay:dd/MM/yyyy}  •  {hang["MaCa"]}  •  {hang["TenChucVu"]}  •  {hang["TrangThai"]}";
            lblKhungGio.Text = $"{(TimeSpan)hang["GioBatDau"]:hh\\:mm} — {(TimeSpan)hang["GioKetThuc"]:hh\\:mm}";
            txtGioVao.Text = hang["GioVao"] is DateTime vao ? vao.ToString("HH:mm") : "";
            txtGioRa.Text = hang["GioRa"] is DateTime ra ? ra.ToString("HH:mm") : "";
            txtLyDo.Text = "";
            cboKetQua.SelectedIndex = 0;
            if (hang["GioVao"] == DBNull.Value || hang["GioRa"] == DBNull.Value)
                cboKetQua.SelectedIndex = 1;
            if (hang["TrangThai"].ToString() == "Không hợp lệ") cboKetQua.SelectedIndex = 2;
            lblLichSu.Text = hang["NgayDoiSoat"] is DateTime duyet
                ? $"Đối soát gần nhất: {duyet:dd/MM/yyyy HH:mm} · {hang["MaNVDoiSoat"]}"
                : "Chưa có lần đối soát";
            bool khoa = Convert.ToBoolean(hang["DaChot"]);
            bool nghi = Convert.ToBoolean(hang["CoDonNghi"]);
            string trangThaiLich = hang["TrangThaiLich"].ToString() ?? "";
            bool huy = new[] { "Hủy", "Từ chối", "Nghỉ phép", "Nghỉ việc" }.Contains(trangThaiLich);
            bool quaSom = ngay.Date > DateTime.Today;
            lblCanhBao.Text = khoa ? "Kỳ lương đã chốt: không thể sửa công." :
                nghi ? "Ngày này có đơn nghỉ đã duyệt: không được tính trùng công." :
                huy ? "Lịch này đã bị hủy hoặc nghỉ phép: không được duyệt công." :
                quaSom ? "Không được duyệt ca làm việc trong tương lai." :
                "Ca hoàn tất được 0,5 công, không yêu cầu đủ 8 giờ; đi trễ 16–60 phút vẫn trừ 0,25 công khi tổng hợp lương.";
            bool duocDuyet = !khoa && !nghi && !huy && !quaSom;
            btnDuyetMot.IsEnabled = duocDuyet;
            txtGioVao.IsEnabled = duocDuyet && cboKetQua.SelectedIndex == 0;
            txtGioRa.IsEnabled = duocDuyet && cboKetQua.SelectedIndex == 0;
            cboKetQua.IsEnabled = duocDuyet;
            txtLyDo.IsEnabled = duocDuyet;
            btnDuyetNhieu.IsEnabled = duocDuyet && gridChamCong.SelectedItems.Count > 1;
        }

        private void cboKetQua_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtGioVao == null || btnDuyetMot == null) return;
            bool hoanTat = cboKetQua.SelectedIndex == 0 && btnDuyetMot.IsEnabled;
            txtGioVao.IsEnabled = hoanTat;
            txtGioRa.IsEnabled = hoanTat;
        }

        private void xoaChiTiet()
        {
            lblNhanVien.Text = "Chọn một ca làm việc";
            lblThongTinCa.Text = "—";
            lblKhungGio.Text = "—";
            txtGioVao.Text = txtGioRa.Text = txtLyDo.Text = "";
            lblCanhBao.Text = "";
            lblLichSu.Text = "";
            btnDuyetMot.IsEnabled = false;
            btnDuyetNhieu.IsEnabled = false;
        }

        private void btnDuyetMot_Click(object sender, RoutedEventArgs e)
        {
            if (!PhienDangNhap.IsAdmin || gridChamCong.SelectedItem is not DataRowView hang) return;
            try
            {
                string ketQua = (cboKetQua.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                DateTime ngay = Convert.ToDateTime(hang["NgayLam"]);
                DateTime? vao = null, ra = null;
                if (ketQua == "Hoàn tất")
                {
                    if (!TimeSpan.TryParseExact(txtGioVao.Text.Trim(), @"hh\:mm", null, out TimeSpan gioVao) ||
                        !TimeSpan.TryParseExact(txtGioRa.Text.Trim(), @"hh\:mm", null, out TimeSpan gioRa))
                        throw new ArgumentException("Nhập giờ theo định dạng HH:mm, ví dụ 06:00 và 14:00.");
                    vao = ngay.Date + gioVao;
                    ra = ngay.Date + gioRa;
                    if (ra < vao) ra = ra.Value.AddDays(1); // Ca qua đêm.
                }
                var ca = new ET_CHAMCONG
                {
                    MaLLV = hang["MaLLV"].ToString() ?? "",
                    TrangThaiCu = hang["TrangThai"].ToString() ?? "",
                    GioVaoCu = hang.Row.Field<DateTime?>("GioVao"),
                    GioRaCu = hang.Row.Field<DateTime?>("GioRa"),
                    GioVao = vao,
                    GioRa = ra,
                    KetQua = ketQua,
                    LyDo = txtLyDo.Text.Trim()
                };
                if (MessageBox.Show("BQL xác nhận đối soát ca " + ca.MaLLV + "?", "Duyệt chấm công",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                busChamCong.duyetChamCong(new[] { ca }, PhienDangNhap.MaNV);
                hienThiDuLieu();
                MessageBox.Show("Đã lưu kết quả duyệt ca. Dữ liệu đã được ghi nhật ký.", "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể duyệt ca: " + ex.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnDuyetNhieu_Click(object sender, RoutedEventArgs e)
        {
            if (!PhienDangNhap.IsAdmin) return;
            var ds = new List<ET_CHAMCONG>();
            try
            {
                foreach (DataRowView hang in gridChamCong.SelectedItems.OfType<DataRowView>())
                {
                    // Chỉ duyệt nhóm khi đã có giờ vào/ra; không kiểm tra đủ 8 giờ.
                    if (hang["GioVao"] is not DateTime vao || hang["GioRa"] is not DateTime ra)
                        throw new ArgumentException("Ca " + hang["MaLLV"] + " chưa đủ giờ vào/ra; hãy duyệt riêng.");
                    ds.Add(new ET_CHAMCONG
                    {
                        MaLLV = hang["MaLLV"].ToString() ?? "",
                        TrangThaiCu = hang["TrangThai"].ToString() ?? "",
                        GioVaoCu = vao,
                        GioRaCu = ra,
                        GioVao = vao,
                        GioRa = ra,
                        KetQua = "Hoàn tất",
                        LyDo = "BQL đối soát hàng loạt theo giờ vào/ra đã ghi nhận"
                    });
                }
                if (ds.Count < 2) { MessageBox.Show("Chọn từ hai ca trở lên để duyệt hàng loạt."); return; }
                if (MessageBox.Show($"Duyệt {ds.Count} ca? Nếu một ca sai, sẽ không lưu ca nào.", "Xác nhận",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                int soCa = busChamCong.duyetChamCong(ds, PhienDangNhap.MaNV);
                hienThiDuLieu();
                MessageBox.Show($"Đã đối soát {soCa} ca.", "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể duyệt hàng loạt: " + ex.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
