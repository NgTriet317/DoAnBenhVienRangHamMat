using System.Globalization;
using System.Windows.Controls;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Giữ bản thiết bị đã xem để chặn thanh lý trên dữ liệu cũ.</summary>
public partial class CuaSoThanhLy : UserControl, IBieuMauLuu
{
    private readonly ET_THIETBI banGoc;

    public CuaSoThanhLy(object record)
    {
        InitializeComponent();
        banGoc = UiRecord.ChuyenDoi<ET_THIETBI>(DuLieuGiaoDien.Values(record));
        var dong = new DongThietBiBaoTri(record);
        txtThietBi.Text = dong.Nhan;
        txtPhong.Text = dong.TenPhong;
        cboNhanVien.ItemsSource = DuLieuGiaoDien.Rows("NHAN_VIEN")
            .Where(row => UiRecord.Text(row, "TrangThai") == "Đang làm việc")
            .Select(row => new { Value = UiRecord.Text(row, "MaNV"), Label = UiRecord.Text(row, "HoTen") + " · " + UiRecord.Text(row, "MaNV") }).ToArray();
        cboNhanVien.SelectedValue = PhienDangNhap.MaNV;
    }

    public bool Commit(out string error)
    {
        error = "";
        if (HoTroGiaoDien.Workspace(this) is not { } workspace || !workspace.CanOpen("BaoTri"))
        { error = "Bạn không có quyền thanh lý thiết bị."; return false; }
        if (cboNhanVien.SelectedValue is not string maNV)
        { error = "Chọn nhân viên phụ trách thanh lý."; return false; }
        if (string.IsNullOrWhiteSpace(txtLyDo.Text))
        { error = "Nhập lý do thanh lý."; return false; }
        if (!decimal.TryParse(txtGiaTri.Text.Trim(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("vi-VN"), out var giaTri)
            || giaTri < 0 || decimal.Round(giaTri, 2) != giaTri)
        { error = "Giá trị thu hồi phải là số không âm, tối đa hai chữ số thập phân; nhập số liền không có dấu phân cách hàng nghìn."; return false; }
        var saved = DuLieuGiaoDien.ThanhLyThietBi(banGoc, maNV, txtLyDo.Text, giaTri);
        SetValue(BieuMauHoSo.SavedProperty, saved);
        return true;
    }
}
