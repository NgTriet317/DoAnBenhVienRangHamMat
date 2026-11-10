using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using BUS;

namespace RangHamMat.Views;

public partial class QuyPhepNhanSu : UserControl
{
    private int nam = DateTime.Today.Year;
    private readonly BUS_QUYPHEP bus = new BUS_QUYPHEP();
    private DataTable danhSach = new DataTable();
    private bool dangTai;

    public QuyPhepNhanSu()
    {
        InitializeComponent();
        Loaded += QuyPhepNhanSu_Loaded;
    }

    private void QuyPhepNhanSu_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= QuyPhepNhanSu_Loaded;
        TaiDuLieu();
    }

    private void TaiDuLieu()
    {
        if (dangTai) return;
        lblNam.Text = nam.ToString();
        if (!PhienDangNhap.IsAdmin)
        {
            gridQuyPhep.ItemsSource = null;
            gridNgayPhep.ItemsSource = null;
            lblThongBao.Text = "Chỉ quản trị viên được xem quỹ phép của toàn bộ nhân viên. " +
                "Nhân viên có thể xem quỹ của mình trong Đơn xin nghỉ phép.";
            return;
        }
        dangTai = true;
        try
        {
            string maDaChon = (gridQuyPhep.SelectedItem as DataRowView)?["MaNV"].ToString() ?? "";
            danhSach = bus.layDanhSachQuyPhep(nam);
            gridQuyPhep.ItemsSource = danhSach.DefaultView;

            decimal duoc = 0, nghi = 0, dat = 0, con = 0;
            foreach (DataRow row in danhSach.Rows)
            {
                duoc += Convert.ToDecimal(row["PhepDuocHuong"]);
                nghi += Convert.ToDecimal(row["PhepDaSuDung"]);
                dat += Convert.ToDecimal(row["PhepDaDatTruoc"]);
                con += Convert.ToDecimal(row["PhepConKhaDung"]);
            }
            txtTongDuoc.Text = duoc.ToString("0.##") + " ngày";
            txtTongDaNghi.Text = nghi.ToString("0.##") + " ngày";
            txtTongDaDat.Text = dat.ToString("0.##") + " ngày";
            txtTongCon.Text = con.ToString("0.##") + " ngày";
            lblSoNhanVien.Text = danhSach.Rows.Count + " nhân viên";

            DataTable cauHinh = bus.layCauHinh(nam);
            lblCauHinh.Text = cauHinh.Rows.Count == 0 ? "Chưa thiết lập cấu hình năm" :
                $"Định mức: {Convert.ToDecimal(cauHinh.Rows[0]["SoNgayPhepNam"]):0.##} ngày / {cauHinh.Rows[0]["SoThangTieuChuan"]} tháng";
            LocDanhSach();
            // Nạp chi tiết sau khi kết thúc giai đoạn nạp bảng.
            dangTai = false;
            gridQuyPhep.SelectedItem = null;
            foreach (DataRowView item in danhSach.DefaultView)
                if (item["MaNV"].ToString() == maDaChon) { gridQuyPhep.SelectedItem = item; break; }
            if (gridQuyPhep.SelectedItem == null && danhSach.DefaultView.Count > 0)
                gridQuyPhep.SelectedIndex = 0;
            if (gridQuyPhep.SelectedItem == null) XoaChiTiet();
            lblThongBao.Text = "Chỉ các đơn nghỉ phép năm đã duyệt mới có lịch sử ngày phép. " +
                "Quỹ năm mới không tự cộng số dư từ năm cũ; dữ liệu lịch sử vẫn được giữ.";
        }
        catch (Exception ex)
        {
            gridQuyPhep.ItemsSource = null;
            XoaChiTiet();
            lblThongBao.Text = "Không thể tải quỹ phép từ SQL: " + ex.Message;
        }
        finally { dangTai = false; }
    }

    private void LocDanhSach()
    {
        if (danhSach.Columns.Count == 0) return;
        string noiDung = Regex.Replace(txtTimKiem.Text.Trim(), @"[^\p{L}\p{M}\p{N} ]", "");
        string tim = noiDung.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]");
        danhSach.DefaultView.RowFilter = tim == "" ? "" :
            $"MaNV LIKE '%{tim}%' OR HoTen LIKE '%{tim}%'";
        lblSoNhanVien.Text = danhSach.DefaultView.Count + " / " + danhSach.Rows.Count + " nhân viên";
    }

    private void TimKiem_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        string noiDung = Regex.Replace(txtTimKiem.Text, @"[^\p{L}\p{M}\p{N} ]", "");
        if (noiDung != txtTimKiem.Text)
        {
            int viTri = txtTimKiem.SelectionStart;
            txtTimKiem.Text = noiDung;
            txtTimKiem.SelectionStart = Math.Min(viTri, noiDung.Length);
            return;
        }
        LocDanhSach();
    }

    private void ChonNhanVien_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dangTai || gridQuyPhep.SelectedItem is not DataRowView hang)
        {
            if (!dangTai) XoaChiTiet();
            return;
        }
        lblTenNhanVien.Text = hang["HoTen"].ToString() ?? "";
        lblChiTietNhanVien.Text = "Mã: " + hang["MaNV"] + "   |   Vào làm: " +
            Convert.ToDateTime(hang["NgayVaoLam"]).ToString("dd/MM/yyyy") +
            "   |   " + hang["TrangThai"];
        lblPhepNhanVien.Text = $"Được hưởng {Convert.ToDecimal(hang["PhepDuocHuong"]):0.##} · " +
            $"Đã nghỉ {hang["PhepDaSuDung"]} · " +
            $"Đã đặt {hang["PhepDaDatTruoc"]} · " +
            $"Còn {Convert.ToDecimal(hang["PhepConKhaDung"]):0.##} ngày";
        try
        {
            gridNgayPhep.ItemsSource = bus.layNgayPhepNhanVien(hang["MaNV"].ToString()!, nam).DefaultView;
        }
        catch (Exception ex)
        {
            gridNgayPhep.ItemsSource = null;
            lblThongBao.Text = "Không thể đọc chi tiết ngày phép: " + ex.Message;
        }
    }

    private void XoaChiTiet()
    {
        lblTenNhanVien.Text = "Chọn nhân viên bên trái";
        lblChiTietNhanVien.Text = "";
        lblPhepNhanVien.Text = "Chưa chọn nhân viên";
        gridNgayPhep.ItemsSource = null;
    }

    private void NamTruoc_Click(object sender, RoutedEventArgs e)
    {
        if (nam > 2000) { nam--; TaiDuLieu(); }
    }

    private void NamSau_Click(object sender, RoutedEventArgs e)
    {
        if (nam < 2099) { nam++; TaiDuLieu(); }
    }

    private void TaiLai_Click(object sender, RoutedEventArgs e) => TaiDuLieu();
}
