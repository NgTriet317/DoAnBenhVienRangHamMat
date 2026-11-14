using System.Globalization;
using System.Windows.Controls;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Giữ bản thiết bị đã xem để chặn thanh lý trên dữ liệu cũ.</summary>
public partial class CuaSoThanhLy : UserControl, IBieuMauLuu
{
    /// <summary>
    /// Bản thiết bị tại thời điểm mở biểu mẫu để nghiệp vụ phát hiện dữ liệu đã bị đổi trước khi thanh lý.
    /// </summary>
    private readonly ET_THIETBI banGoc;

    /// <summary>
    /// Giữ bản gốc của thiết bị, hiển thị thông tin đối chiếu và nạp nhân viên đang làm việc để phụ trách thanh lý.
    /// </summary>
    /// <param name="record">Thiết bị nguồn được chọn trong màn hình bảo trì.</param>
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

    /// <summary>
    /// Kiểm tra quyền, nhân viên, lý do và giá trị thu hồi trước khi thanh lý trên bản thiết bị đã xem.
    /// </summary>
    /// <param name="error">Nhận lỗi nhập liệu hoặc thông báo thiếu quyền; chuỗi rỗng nếu kiểm tra thành công.</param>
    /// <returns>True khi đã lưu thanh lý và gắn bản ghi kết quả cho biểu mẫu; lỗi nghiệp vụ có thể được phát ra bằng ngoại lệ.</returns>
    public bool Commit(out string error)
    {
        error = "";
        if (HoTroGiaoDien.Workspace(this) is not { } workspace || !workspace.CanOpen("BaoTri"))
        { error = "Bạn không có quyền thanh lý thiết bị."; return false; }
        if(!KiemTraNhapLieu.PrepareSave(this,out error))return false;
        var maNV=cboNhanVien.SelectedValue as string??"";
        decimal giaTri;
        try {giaTri=ReadLiquidation(txtGiaTri.Text,maNV,txtLyDo.Text);}
        catch(LoiNhapLieu ex) {error=ex.Message;KiemTraNhapLieu.MarkError(this,ex);return false;}
        var saved = DuLieuGiaoDien.ThanhLyThietBi(banGoc, maNV, txtLyDo.Text, giaTri);
        SetValue(BieuMauHoSo.SavedProperty, saved);
        return true;
    }

    /// <summary>Thanh lý bắt buộc nhân viên, lý do và giá trị dù các cột này nullable trước khi thanh lý.</summary>
    private static decimal ReadLiquidation(string text,string employee,string reason)
    {
        if(string.IsNullOrWhiteSpace(employee))throw new LoiNhapLieu("MaNVThanhLy","Chọn nhân viên đang làm việc phụ trách thanh lý.");
        if(string.IsNullOrWhiteSpace(reason))throw new LoiNhapLieu("LyDoThanhLy","Nhập lý do thanh lý để lưu hồ sơ đối chiếu thiết bị.");
        if(string.IsNullOrWhiteSpace(text))throw new LoiNhapLieu("GiaTriThanhLy","Nhập giá trị thu hồi; nhập 0 nếu không thu hồi được giá trị.");
        KiemTraNhapLieu.ValidateValues("THIET_BI",new Dictionary<string,object?> {["MaNVThanhLy"]=employee,["LyDoThanhLy"]=reason,["GiaTriThanhLy"]=text});
        return decimal.Parse(text.Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture);
    }

    /// <summary>Áp dụng điều kiện bắt buộc của thanh lý ngay khi rời ô nhập.</summary>
    private void Liquidation_LostKeyboardFocus(object sender,System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if(sender is not System.Windows.FrameworkElement field||field.IsKeyboardFocusWithin)return;
        try {ReadLiquidation(txtGiaTri.Text,cboNhanVien.SelectedValue as string??"",txtLyDo.Text);KiemTraNhapLieu.SetError(field,"");}
        catch(LoiNhapLieu ex) {if(field.Tag is string tag&&tag.EndsWith("."+ex.Field,StringComparison.Ordinal))KiemTraNhapLieu.SetError(field,ex.Message);}
    }
}
