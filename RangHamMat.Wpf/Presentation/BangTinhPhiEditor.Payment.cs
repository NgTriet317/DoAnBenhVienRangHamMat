using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ET;

namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý bảng tính phí trình nhập liệu của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class BangTinhPhiEditor
{
    /// <summary>
    /// Chờ xử lý thanh toán được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private YeuCauThanhToan? pendingPayment;
    /// <summary>
    /// Kết quả. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public KetQuaThanhToan? KetQua { get; private set; }
    /// <summary>
    /// Mã yêu cầu hiển thị tải.
    /// </summary>
    public Guid MaYeuCauHienTai { get; private set; }
    /// <summary>
    /// Đang xử lý.
    /// </summary>
    public bool DangXuLy { get; private set; }
    /// <summary>
    /// Giá trị Chờ đối chiếu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool ChoDoiChieu => pendingPayment is not null && KetQua is null;
    /// <summary>
    /// Thông báo thanh toán hoàn tất cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public event EventHandler? ThanhToanHoanTat;
    /// <summary>
    /// Kiểm tra hóa đơn hiện tại rồi mở nội dung in từ dữ liệu chứng từ đã đọc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>send</c>, <c>UpdatePaymentControls</c>.</remarks>
    /// <param name="send">Giá trị gửi dùng khi thực hiện thao tác; kiểu Action&lt;BanInHoaDon&gt;.</param>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool TryInHoaDon(Action<BanInHoaDon> send,out string? error)
    {
        error=null;try
        {
            var id=KetQua?.MaHD??throw new LoiKhamException("MaHD","Chưa có kết quả thanh toán đã commit.");
            send(new BUS.BUS_THANHTOANTRONGOI().LayBanInHoaDon(id,user));return true;
        }
        catch(Exception){error=KetQua is null?"Chưa có hóa đơn đã thanh toán để in.":$"Đã thu tiền · Hóa đơn #{KetQua.MaHD}. Không in được; dùng In lại cùng chứng từ, không thanh toán lần nữa.";StatusText.Text=error;return false;}
        finally{UpdatePaymentControls();}
    }

    /// <summary>
    /// Thiết lập các trường và sự kiện của vùng thanh toán theo ngữ cảnh bảng tính hiện tại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UpdateChange</c>, <c>TryThanhToanAsync</c>, <c>OpenInvoicePrint</c>.</remarks>
    private void InitializePayment()
    {
        GivenText.TextChanged += (_, _) => UpdateChange();
        MethodBox.SelectionChanged += (_, _) => UpdateChange();
        PayButton.Click += async (_, _) => {if(await TryThanhToanAsync())OpenInvoicePrint();};
        PrintAgainButton.Click += (_,_)=>OpenInvoicePrint();
    }
    /// <summary>
    /// Đặt lại yêu cầu thanh toán và các số tiền khi thay nguồn bảng tính.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Guid.NewGuid</c>, <c>UpdatePaymentControls</c>.</remarks>
    private void ResetPayment()
    {
        pendingPayment = null; KetQua = null; MaYeuCauHienTai = Guid.NewGuid();
        GivenText.Text = "0"; ReferenceText.Text = ""; TransferChecked.IsChecked = false;
        MethodBox.SelectedIndex=0;
        UpdatePaymentControls();
    }
    /// <summary>
    /// Bật hoặc tắt thao tác thu tiền theo trạng thái bảng tính và tiến trình đang xử lý.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>user.CoQuyen</c>, <c>UpdateChange</c>.</remarks>
    private void UpdatePaymentControls()
    {
        var allowed = user is not null && user.CoQuyen(QuyenLamViec.ThuTien) && BangTinh?.DuocThanhToan == true;
        PaymentPanel.Visibility = allowed || KetQua is not null ? Visibility.Visible : Visibility.Collapsed;
        PayButton.IsEnabled = allowed && KetQua is null && !DangXuLy;
        PayButton.Content = ChoDoiChieu ? "Thử lại yêu cầu" : "Thanh toán và in hóa đơn";
        PrintAgainButton.IsEnabled=KetQua is not null&&!DangXuLy&&!ChoDoiChieu;
        PrintAgainButton.Visibility=KetQua is null?Visibility.Collapsed:Visibility.Visible;
        PaymentInputs.IsEnabled = TransferChecked.IsEnabled = !DangXuLy && !ChoDoiChieu && KetQua is null;
        PreviewButton.IsEnabled = !string.IsNullOrEmpty(visitId) && !DangXuLy && !ChoDoiChieu;
        WarehouseFilter.IsEnabled = !DangXuLy && !ChoDoiChieu;
        UpdateChange();
    }
    /// <summary>
    /// Tính và hiển thị tiền thừa theo số tiền khách đưa và số phải thu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Money</c>, <c>decimal.TryParse</c>, <c>GivenText.Text.Trim</c>.</remarks>
    private void UpdateChange()
    {
        if(ChangeText is null) return;
        if(KetQua is not null) { ChangeText.Text = "Tiền thối đã chốt: " + Money(KetQua.TienThua); return; }
        if(BangTinh is null || !decimal.TryParse(GivenText.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var given)) { ChangeText.Text="Nhập tiền VND nguyên đồng, không dùng dấu phân cách."; return; }
        var change = given - BangTinh.TongTien;
        ChangeText.Text = MethodBox.SelectedValue as string == "Chuyển khoản" ? "Chuyển khoản phải đúng số phải thu, có tham chiếu và đối chiếu thủ công." :
            change < 0 ? "Còn thiếu: " + Money(-change) : "Tiền thối dự kiến: " + Money(change);
    }
    /// <summary>
    /// Kiểm tra thông tin thu và thực hiện thanh toán bất đồng bộ; giữ mã yêu cầu để đối chiếu nếu cần gọi lại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>user.CoQuyen</c>, <c>ReferenceText.Text.Trim</c>, <c>decimal.TryParse</c>, <c>GivenText.Text.Trim</c>.</remarks>
    /// <returns>Tác vụ bất đồng bộ để nơi gọi theo dõi thời điểm kết thúc và kết quả xử lý.</returns>
    public async Task<bool> TryThanhToanAsync()
    {
        if(DangXuLy || KetQua is not null) return false;
        if(!user.CoQuyen(QuyenLamViec.ThuTien) || BangTinh?.DuocThanhToan != true || BangTinh.DauVanTay is null)
        { StatusText.Text="Cần bảng tính hợp lệ và quyền thu tiền."; return false; }
        if(pendingPayment is null)
        {
            var method=MethodBox.SelectedValue as string ?? "";var reference=ReferenceText.Text.Trim();
            if(!decimal.TryParse(GivenText.Text.Trim(),NumberStyles.None,CultureInfo.InvariantCulture,out var given) || given>9999999999999999m ||
                (method=="Tiền mặt" ? given<BangTinh.TongTien : given!=BangTinh.TongTien) || (BangTinh.TongTien==0 && given!=0))
            {StatusText.Text="Nhập số nguyên đồng: tiền mặt phải đủ, chuyển khoản phải đúng số phải thu.";return false;}
            if(method=="Chuyển khoản" && (reference.Length==0 || TransferChecked.IsChecked!=true))
            {StatusText.Text="Nhập tham chiếu và xác nhận đã đối chiếu chuyển khoản thủ công.";return false;}
            pendingPayment=new(){MaLS=BangTinh.MaLS,MaKhoCapThuoc=BangTinh.MaKhoCapThuoc,MaYeuCau=MaYeuCauHienTai,DauVanTayBangTinh=BangTinh.DauVanTay,PhuongThuc=method,SoTienKhachDua=given,MaThamChieu=reference,DaDoiChieuChuyenKhoan=TransferChecked.IsChecked==true};
        }
        var request=pendingPayment; DangXuLy=true; UpdatePaymentControls(); StatusText.Text="Đang xử lý yêu cầu thanh toán…";
        try
        {
            // Không giữ transaction trong lúc nhập tiền/đợi đối chiếu. BUS tự commit trước khi trả về đây.
            var result=await Task.Run(()=>bus.ThanhToan(request,user));
            KetQua=result;pendingPayment=null;
            TotalText.Text=$"Đã thu: {Money(result.SoTienThu)} · Khách đưa: {Money(result.SoTienKhachDua)} · Thối: {Money(result.TienThua)}";
            StatusText.Text=$"Đã thanh toán · Hóa đơn #{result.MaHD}. "+(result.CacDon.Count==0?"Lượt đã hoàn tất.":result.CacDon.Any(d=>d.TrangThaiGiao!="Đã giao")?"Thuốc đang chờ giao.":"Các đơn đã giao; lượt đã hoàn tất.");
            try { ThanhToanHoanTat?.Invoke(this,EventArgs.Empty); }
            catch { StatusText.Text+=" Chưa tải lại được danh sách; dùng Tải lại để đối chiếu hóa đơn đã lưu."; }
            return true;
        }
        catch(XungDotDuLieuKhamException ex)
        { pendingPayment=null; Clear(); StatusText.Text=ex.Message; return false; }
        catch(TuChoiQuyenNghiepVuException ex)
        { pendingPayment=null; Clear(); StatusText.Text=ex.Message; return false; }
        catch(LoiKhamException ex) when(ex.Truong!="ThanhToan")
        { pendingPayment=null;StatusText.Text=ex.Message;return false; }
        catch(Exception)
        { StatusText.Text="Chưa xác nhận được kết quả. Giữ nguyên lượt, kho và tiền; bấm Thử lại yêu cầu để đối chiếu, không lập lần thu khác.";return false; }
        finally { DangXuLy=false;UpdatePaymentControls(); }
    }
    /// <summary>
    /// Mở cửa sổ in hóa đơn với nguồn chứng từ được chỉ định.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TryInHoaDon</c>, <c>Window.GetWindow</c>.</remarks>
    private void OpenInvoicePrint()=>TryInHoaDon(source=>
    {var id=source.HoaDon.MaHD;new XemTruocHoaDon(()=>new BUS.BUS_THANHTOANTRONGOI().LayBanInHoaDon(id,user)){Owner=Window.GetWindow(this)}.Show();},out _);
}
