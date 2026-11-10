using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;

namespace RangHamMat.Views;

public partial class ChinhSachLuong : UserControl
{
    private readonly BUS_CHINHSACHLUONG busChinhSach = new();
    private DataTable danhSachChinhSach = new();
    private string maChinhSach = "";
    private bool dangTai;

    public ChinhSachLuong()
    {
        InitializeComponent();
        Loaded += (_, _) => { if (danhSachChinhSach.Columns.Count==0) hienThiDuLieu(); };
    }

    private void hienThiDuLieu(string maCanChon = "")
    {
        if (dangTai) return;
        dangTai = true;
        try
        {
            danhSachChinhSach = busChinhSach.layDanhSachChinhSach();
            var chucVu = busChinhSach.layDanhSachChucVu();
            cboChucVuChinhSach.ItemsSource = chucVu.DefaultView;
            gridChinhSach.ItemsSource = danhSachChinhSach.DefaultView;
            timChinhSach();
            if (maCanChon.Length > 0)
            {
                foreach (DataRowView row in danhSachChinhSach.DefaultView)
                    if (Convert.ToString(row["MaCSL"])==maCanChon)
                    { gridChinhSach.SelectedItem = row; break; }
            }
            if (gridChinhSach.SelectedItem == null && danhSachChinhSach.DefaultView.Count > 0)
                gridChinhSach.SelectedIndex = 0;
            if (gridChinhSach.SelectedItem == null) TaoPhienBan_Click(this, new RoutedEventArgs());
        }
        catch (Exception ex)
        {
            lblThongBao.Text = "Không tải được chính sách. Kiểm tra SQL nâng cấp: " + ex.Message;
            btnLuuNhap.IsEnabled = false;
            btnDuyet.IsEnabled = false;
        }
        finally
        {
            dangTai = false;
            if (gridChinhSach.SelectedItem is DataRowView)
                gridChinhSach_SelectionChanged(this, new SelectionChangedEventArgs(System.Windows.Controls.Primitives.Selector.SelectionChangedEvent, new System.Collections.IList[0], new System.Collections.IList[0]));
        }
    }

    private void timChinhSach()
    {
        if (danhSachChinhSach.Columns.Count==0) return;
        string s=Regex.Replace(txtTimChinhSach.Text.Trim(),@"[^\p{L}\p{N} _-]","");
        s=s.Replace("'","''").Replace("[","[[]").Replace("%","[%]");
        danhSachChinhSach.DefaultView.RowFilter = s.Length==0 ? "" :
            $"TenChinhSach LIKE '%{s}%' OR TenChucVu LIKE '%{s}%' OR MaCSL LIKE '%{s}%'";
    }

    private void TimChinhSach_TextChanged(object sender, TextChangedEventArgs e)
    { if (!dangTai) timChinhSach(); }

    private void gridChinhSach_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dangTai || gridChinhSach.SelectedItem is not DataRowView row) return;
        maChinhSach = Convert.ToString(row["MaCSL"])??"";
        txtTenChinhSach.Text = Convert.ToString(row["TenChinhSach"])??"";
        cboChucVuChinhSach.SelectedValue=Convert.ToString(row["MaChucVu"])??"";
        dpHieuLuc.SelectedDate=Convert.ToDateTime(row["NgayHieuLuc"]);
        txtLuongCoBan.Text=Convert.ToDecimal(row["LuongCoBan"]).ToString("0.##");
        txtDonGiaCa.Text=row["DonGiaCa"]==DBNull.Value?"":Convert.ToDecimal(row["DonGiaCa"]).ToString("0.##");
        txtSoCaDinhMuc.Text=Convert.ToString(row["SoCaDinhMuc"])??"26";
        txtPhuCap.Text=Convert.ToDecimal(row["PhuCapCoDinh"]).ToString("0.##");
        txtTyLeHoaHong.Text=(Convert.ToDecimal(row["TyLeHoaHong"])*100m).ToString("0.##");
        string trangThai=Convert.ToString(row["TrangThai"])??"";
        lblTieuDe.Text="Phiên bản " + row["PhienBan"] + " – " + row["TenChucVu"];
        lblTrangThai.Text=trangThai.ToUpperInvariant();
        bool sua=PhienDangNhap.IsAdmin && trangThai=="Nháp";
        BatNhap(sua);
        btnDuyet.IsEnabled=sua;
        lblThongBao.Text=trangThai=="Đã duyệt" ?
            "Chính sách đã duyệt được bảo vệ. Muốn đổi đơn giá ca hoặc hoa hồng, hãy tạo phiên bản mới." :
            "Chính sách nháp: có thể lưu và duyệt khi đã kiểm tra số liệu.";
    }

    private void BatNhap(bool bat)
    {
        txtTenChinhSach.IsReadOnly=!bat;
        txtLuongCoBan.IsReadOnly=!bat;
        txtDonGiaCa.IsReadOnly=!bat;
        txtSoCaDinhMuc.IsReadOnly=!bat;
        txtPhuCap.IsReadOnly=!bat;
        txtTyLeHoaHong.IsReadOnly=!bat;
        cboChucVuChinhSach.IsEnabled=bat && maChinhSach.Length==0;
        dpHieuLuc.IsEnabled=bat;
        btnLuuNhap.IsEnabled=bat;
    }

    private void TaoPhienBan_Click(object sender, RoutedEventArgs e)
    {
        string chucVuCu=Convert.ToString(cboChucVuChinhSach.SelectedValue)??"";
        dangTai=true;
        gridChinhSach.SelectedItem=null;
        dangTai=false;
        maChinhSach="";
        txtTenChinhSach.Text="Chính sách lương mới";
        if (chucVuCu.Length>0) cboChucVuChinhSach.SelectedValue=chucVuCu;
        var dauThang=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1).AddMonths(1);
        dpHieuLuc.SelectedDate=dauThang;
        txtDonGiaCa.Text="";
        txtLuongCoBan.Text="0";
        txtSoCaDinhMuc.Text="26";
        txtPhuCap.Text="0";
        txtTyLeHoaHong.Text="0";
        lblTieuDe.Text="Tạo phiên bản mới";
        lblTrangThai.Text="BẢN NHÁP";
        BatNhap(PhienDangNhap.IsAdmin);
        btnDuyet.IsEnabled=false; // Lưu bản nháp trước, sau đó chọn để duyệt.
        lblThongBao.Text="Nhập mức lương theo ca và tỷ lệ hoa hồng, rồi lưu bản nháp.";
    }

    private static decimal DocTien(string ten, string value)
    {
        if (decimal.TryParse(value.Trim(),NumberStyles.Number,CultureInfo.CurrentCulture,out var d) && d>=0) return d;
        if (decimal.TryParse(value.Trim(),NumberStyles.Number,CultureInfo.InvariantCulture,out d) && d>=0) return d;
        throw new InvalidOperationException(ten+" phải là số không âm.");
    }

    private void LuuNhap_Click(object sender, RoutedEventArgs e)
    {
        if(!PhienDangNhap.IsAdmin) return;
        try
        {
            string maCV=Convert.ToString(cboChucVuChinhSach.SelectedValue)??"";
            var cs=new ET_CHINHSACHLUONG
            {
                MaCSL=maChinhSach,MaChucVu=maCV,
                TenChinhSach=txtTenChinhSach.Text.Trim(),
                NgayHieuLuc=dpHieuLuc.SelectedDate??throw new InvalidOperationException("Chưa chọn ngày hiệu lực."),
                LuongCoBan=DocTien("Lương cơ bản",txtLuongCoBan.Text),
                DonGiaCa=string.IsNullOrWhiteSpace(txtDonGiaCa.Text)?null:DocTien("Đơn giá ca",txtDonGiaCa.Text),
                SoCaDinhMuc=int.TryParse(txtSoCaDinhMuc.Text.Trim(),out int n)?n:0,
                PhuCapCoDinh=DocTien("Phụ cấp",txtPhuCap.Text),
                TyLeHoaHong=DocTien("Tỷ lệ hoa hồng",txtTyLeHoaHong.Text)/100m
            };
            string ma=busChinhSach.luuChinhSach(PhienDangNhap.MaNV,cs);
            hienThiDuLieu(ma);
            lblThongBao.Text="Đã lưu chính sách nháp. Kiểm tra rồi nhấn Duyệt / áp dụng.";
        }
        catch(Exception ex){MessageBox.Show("Không thể lưu chính sách: "+ex.Message,"Chính sách lương",MessageBoxButton.OK,MessageBoxImage.Warning);}
    }

    private void Duyet_Click(object sender, RoutedEventArgs e)
    {
        if(!PhienDangNhap.IsAdmin || maChinhSach.Length==0) return;
        if(MessageBox.Show("Duyệt phiên bản chính sách này? Phiên bản đã duyệt không được sửa. Chính sách cũ sẽ kết thúc trước ngày hiệu lực mới.",
            "Duyệt chính sách",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes) return;
        try
        {
            busChinhSach.duyetChinhSach(maChinhSach,PhienDangNhap.MaNV);
            hienThiDuLieu(maChinhSach);
            lblThongBao.Text="Đã duyệt chính sách. Các kỳ lương NHÁP tiếp theo sẽ áp dụng chính sách theo ngày hiệu lực.";
        }
        catch(Exception ex){MessageBox.Show("Không duyệt được: "+ex.Message,"Chính sách lương",MessageBoxButton.OK,MessageBoxImage.Warning);}
    }
    private void LamMoi_Click(object sender, RoutedEventArgs e) => hienThiDuLieu(maChinhSach);
}
