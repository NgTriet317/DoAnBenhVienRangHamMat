using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ET;
using RangHamMat.Controls;

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
        KiemTraNhapLieu.SetFieldValidator(GivenText,()=>
        {
            if(BangTinh is null)return;
            try{ReadTender(GivenText.Text,MethodBox.SelectedValue as string??"",ReferenceText.Text,TransferChecked.IsChecked==true,BangTinh.TongTien);}
            catch(LoiNhapLieu ex)when(ex.Field!="SoTienKhachDua"){ }
        });
        KiemTraNhapLieu.SetFieldValidator(ReferenceText,()=>{if(MethodBox.SelectedValue as string=="Chuyển khoản"&&string.IsNullOrWhiteSpace(ReferenceText.Text))throw new LoiNhapLieu("MaThamChieu","Nhập mã tham chiếu của giao dịch chuyển khoản đã đối chiếu.");});
        KiemTraNhapLieu.SetFieldValidator(TransferChecked,()=>{if(MethodBox.SelectedValue as string=="Chuyển khoản"&&TransferChecked.IsChecked!=true)throw new LoiNhapLieu("TransferChecked","Đối chiếu giao dịch chuyển khoản rồi chọn xác nhận đã đối chiếu.");});
        GivenText.TextChanged += (_, _) => UpdateChange();
        MethodBox.SelectionChanged += (_, _) => {UpdateChange();RefreshPaymentErrors();};
        TransferChecked.Checked+=(_,_)=>RefreshPaymentErrors();
        TransferChecked.Unchecked+=(_,_)=>RefreshPaymentErrors();
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
        foreach(var field in new FrameworkElement[]{GivenText,ReferenceText,MethodBox,TransferChecked,WarehouseFilter})KiemTraNhapLieu.SetError(field,"");
        UpdatePaymentControls();
    }
    private void RefreshPaymentErrors()
    {
        foreach(var field in new FrameworkElement[]{GivenText,ReferenceText,TransferChecked})
            if(KiemTraNhapLieu.GetHasError(field))KiemTraNhapLieu.RecheckField(field);
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
            if(!KiemTraNhapLieu.PrepareSave(PaymentPanel,out var inputError)) {StatusText.Text=inputError;return false;}
            var method=MethodBox.SelectedValue as string ?? "";var reference=ReferenceText.Text.Trim();
            decimal given;
            try {given=ReadTender(GivenText.Text,method,reference,TransferChecked.IsChecked==true,BangTinh.TongTien);}
            catch(LoiNhapLieu ex) {KiemTraNhapLieu.MarkError(this,ex);return false;}
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
        { pendingPayment=null;KiemTraNhapLieu.MarkError(this,new LoiNhapLieu(ex.Truong=="DaDoiChieuChuyenKhoan"?"TransferChecked":ex.Truong,ex.Message));StatusText.Text=ex.Message;return false; }
        catch(Exception)
        { StatusText.Text="Chưa xác nhận được kết quả. Giữ nguyên lượt, kho và tiền; bấm Thử lại yêu cầu để đối chiếu, không lập lần thu khác.";return false; }
        finally { DangXuLy=false;UpdatePaymentControls(); }
    }

    /// <summary>Tiền khách đưa nguyên đồng theo luồng thu trọn gói; đối chiếu phương thức, tham chiếu và xác nhận trước khi gửi BUS.</summary>
    private static decimal ReadTender(string text,string method,string reference,bool checkedTransfer,decimal total)
    {
        if(!decimal.TryParse(text.Trim(),NumberStyles.None,CultureInfo.InvariantCulture,out var amount))
            throw new LoiNhapLieu("SoTienKhachDua","Tiền khách đưa phải là số nguyên đồng không âm. Nhập số liền, ví dụ 150000; không dùng dấu phân cách.");
        KiemTraNhapLieu.ValidateValues("THANH_TOAN",new Dictionary<string,object?> {["SoTienKhachDua"]=text,["PhuongThuc"]=method,["MaThamChieu"]=reference});
        if(method is not ("Tiền mặt" or "Chuyển khoản"))throw new LoiNhapLieu("PhuongThuc","Chọn Tiền mặt hoặc Chuyển khoản.");
        if(total==0&&amount!=0)throw new LoiNhapLieu("SoTienKhachDua","Lượt không phát sinh phải thu. Nhập tiền khách đưa bằng 0.");
        if(method=="Tiền mặt"&&amount<total)throw new LoiNhapLieu("SoTienKhachDua","Tiền khách đưa còn thiếu. Nhập ít nhất "+Money(total)+".");
        if(method=="Chuyển khoản"&&amount!=total)throw new LoiNhapLieu("SoTienKhachDua","Chuyển khoản phải đúng số phải thu. Nhập "+Money(total)+".");
        if(method=="Chuyển khoản"&&string.IsNullOrWhiteSpace(reference))throw new LoiNhapLieu("MaThamChieu","Nhập mã tham chiếu của giao dịch chuyển khoản đã đối chiếu.");
        if(method=="Chuyển khoản"&&!checkedTransfer)throw new LoiNhapLieu("TransferChecked","Đối chiếu giao dịch chuyển khoản rồi chọn xác nhận đã đối chiếu.");
        return amount;
    }

    /// <summary>Hiện điều kiện thu ngay dưới trường đang rời; không xóa lỗi ở trường khác.</summary>
    private void PaymentField_LostKeyboardFocus(object sender,System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if(sender is not FrameworkElement field||field.IsKeyboardFocusWithin||BangTinh is null||!PaymentInputs.IsEnabled)return;
        try {ReadTender(GivenText.Text,MethodBox.SelectedValue as string??"",ReferenceText.Text,TransferChecked.IsChecked==true,BangTinh.TongTien);KiemTraNhapLieu.SetError(field,"");}
        catch(LoiNhapLieu ex) {if(field.Name==ex.Field||field.Tag is string tag&&tag.EndsWith("."+ex.Field,StringComparison.Ordinal))KiemTraNhapLieu.SetError(field,ex.Message);}
    }
    /// <summary>
    /// Mở cửa sổ in hóa đơn với nguồn chứng từ được chỉ định.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TryInHoaDon</c>, <c>Window.GetWindow</c>.</remarks>
    private void OpenInvoicePrint()=>TryInHoaDon(source=>
    {var id=source.HoaDon.MaHD;new XemTruocHoaDon(()=>new BUS.BUS_THANHTOANTRONGOI().LayBanInHoaDon(id,user)){Owner=Window.GetWindow(this)}.Show();},out _);
}
