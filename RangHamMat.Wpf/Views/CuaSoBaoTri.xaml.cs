using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Biểu mẫu một thiết bị vật lý; lưu chưa xác nhận hoặc lưu và khóa nội dung trong một giao dịch.</summary>
public partial class CuaSoBaoTri : UserControl, IBieuMauLuu
{
    /// <summary>
    /// Mã thiết bị được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private string maThietBi = "";
    /// <summary>
    /// Cờ Xác nhận; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool xacNhan;

    /// <summary>
    /// Khởi tạo cửa sổ bảo trì. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    public CuaSoBaoTri()
    {
        InitializeComponent();
        dpBatDau.SelectedDate = DateTime.Today;
        Loaded += (_, _) => ChuanBi();
    }

    /// <summary>Khóa thiết bị từ hàng đợi hoặc bản ghi nguồn; không cho chuyển phiếu sang serial khác.</summary>
    public void SetThietBi(string maTB)
    {
        var record = BieuMauHoSo.GetRecord(this);
        maThietBi = record is null ? maTB : UiRecord.Text(record, "MaTB");
        txtMaTB.Text = maThietBi;
        var thietBi = DuLieuGiaoDien.Find("THIET_BI", maThietBi);
        if (thietBi is null)
        {
            txtTenThietBi.Text = "Chưa chọn thiết bị";
            txtSerial.Text = "Chưa chọn serial thiết bị";
            txtPhong.Text = "Đóng biểu mẫu và chọn thiết bị trong danh sách Thiết bị hư hại.";
            return;
        }
        var dong = new DongThietBiBaoTri(thietBi);
        txtTenThietBi.Text = dong.TenThietBi;
        txtSerial.Text = "Serial: " + dong.SoSerial;
        txtPhong.Text = dong.TenPhong + " · " + dong.TinhTrang;
    }

    /// <summary>
    /// Điền thông tin và lựa chọn đầu vào cho biểu mẫu bảo trì theo chế độ tạo mới hoặc chỉnh sửa.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>BieuMauHoSo.GetRecord</c>, <c>SetThietBi</c>, <c>UiRecord.Text</c>, <c>DuLieuGiaoDien.CanEdit</c>.</remarks>
    private void ChuanBi()
    {
        var record = BieuMauHoSo.GetRecord(this);
        SetThietBi(record is null ? txtMaTB.Text : UiRecord.Text(record, "MaTB"));
        var choSua = record is null || DuLieuGiaoDien.CanEdit("BAO_TRI_THIET_BI", record);
        txtTieuDe.Text = !choSua ? "Chi tiết phiếu bảo trì" : record is null ? "Lập phiếu bảo trì" : "Cập nhật phiếu bảo trì";
        TruongNhap.IsEnabled = choSua;
        btnLuu.Visibility = btnXacNhan.Visibility = choSua ? Visibility.Visible : Visibility.Collapsed;
        btnLuu.IsEnabled = btnXacNhan.IsEnabled = choSua && maThietBi.Length > 0;
        if (!choSua) txtHuongDan.Text = "Nội dung phiếu đã khóa. Các thao tác bắt đầu hoặc hoàn thành được thực hiện ở Lịch sử bảo trì.";
        if (maThietBi.Length == 0) FormError.Text = "Hãy đóng biểu mẫu và chọn đúng serial trong danh sách Thiết bị hư hại để lập phiếu.";
    }

    /// <summary>
    /// Xử lý thao tác lưu khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ReferenceEquals</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Luu_Click(object sender, RoutedEventArgs e) => xacNhan = ReferenceEquals(sender, btnXacNhan);

    /// <summary>
    /// Kiểm tra và ghi nhận dữ liệu hiện tại theo thao tác của thành phần.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>BieuMauHoSo.GetRecord</c>, <c>DuLieuGiaoDien.CanEdit</c>, <c>DuLieuGiaoDien.Find</c>, <c>UiRecord.Text</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool Commit(out string error)
    {
        error = "";
        var record = BieuMauHoSo.GetRecord(this);
        if (record is not null && !DuLieuGiaoDien.CanEdit("BAO_TRI_THIET_BI", record))
        {
            error = "Chỉ sửa nội dung phiếu chưa xác nhận."; return false;
        }
        var thietBi = DuLieuGiaoDien.Find("THIET_BI", maThietBi);
        if (maThietBi.Length == 0 || thietBi is null)
        {
            error = "Chọn một thiết bị hư hại theo serial từ danh sách trước khi lập phiếu."; return false;
        }
        if (UiRecord.Text(thietBi, "TinhTrang") != "Hư hại")
        {
            error = "Thiết bị đã đổi tình trạng. Đóng biểu mẫu và tải lại danh sách thiết bị hư hại."; return false;
        }
        // Trạng thái đích do nút thao tác quyết định; SQL lưu nội dung và thời điểm xác nhận cùng giao dịch.
        txtMaTB.Text = maThietBi;
        txtTrangThai.Text = xacNhan ? "Đã xác nhận" : "Chưa xác nhận";
        var success = BieuMauHoSo.TrySave(this, out _, out error);
        if (!success) txtTrangThai.Text = "Chưa xác nhận";
        return success;
    }

    /// <summary>
    /// Điều chỉnh phần hiển thị theo kích thước thực tế của nội dung.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NoiDung_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var cot = e.NewSize.Width < 620 ? 1 : 2;
        TruongThongTin.Columns = TruongKeHoach.Columns = cot;
    }
}
