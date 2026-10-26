using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ET;

namespace RangHamMat.Views;

public partial class BangTinhPhiEditor
{
    private YeuCauThanhToan? pendingPayment;
    public KetQuaThanhToan? KetQua { get; private set; }
    public Guid MaYeuCauHienTai { get; private set; }
    public bool DangXuLy { get; private set; }
    public bool ChoDoiChieu => pendingPayment is not null && KetQua is null;
    public event EventHandler? ThanhToanHoanTat;

    private void InitializePayment()
    {
        GivenText.TextChanged += (_, _) => UpdateChange();
        MethodBox.SelectionChanged += (_, _) => UpdateChange();
        PayButton.Click += async (_, _) => await TryThanhToanAsync();
    }
    private void ResetPayment()
    {
        pendingPayment = null; KetQua = null; MaYeuCauHienTai = Guid.NewGuid();
        GivenText.Text = "0"; ReferenceText.Text = ""; TransferChecked.IsChecked = false;
        UpdatePaymentControls();
    }
    private void UpdatePaymentControls()
    {
        var allowed = user is not null && user.CoQuyen(QuyenLamViec.ThuTien) && BangTinh?.DuocThanhToan == true;
        PaymentPanel.Visibility = allowed || KetQua is not null ? Visibility.Visible : Visibility.Collapsed;
        PayButton.IsEnabled = allowed && KetQua is null && !DangXuLy;
        PayButton.Content = ChoDoiChieu ? "Thử lại yêu cầu" : "Xác nhận thu đủ";
        PaymentInputs.IsEnabled = TransferChecked.IsEnabled = !DangXuLy && !ChoDoiChieu && KetQua is null;
        PreviewButton.IsEnabled = !string.IsNullOrEmpty(visitId) && !DangXuLy && !ChoDoiChieu;
        WarehouseFilter.IsEnabled = !DangXuLy && !ChoDoiChieu;
        UpdateChange();
    }
    private void UpdateChange()
    {
        if(ChangeText is null) return;
        if(KetQua is not null) { ChangeText.Text = "Tiền thối đã chốt: " + Money(KetQua.TienThua); return; }
        if(BangTinh is null || !decimal.TryParse(GivenText.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var given)) { ChangeText.Text="Nhập tiền VND nguyên đồng, không dùng dấu phân cách."; return; }
        var change = given - BangTinh.TongTien;
        ChangeText.Text = MethodBox.SelectedValue as string == "Chuyển khoản" ? "Chuyển khoản phải đúng số phải thu, có tham chiếu và đối chiếu thủ công." :
            change < 0 ? "Còn thiếu: " + Money(-change) : "Tiền thối dự kiến: " + Money(change);
    }
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
            StatusText.Text=$"Đã thanh toán · Hóa đơn #{result.MaHD}. "+(result.CacDon.Count>0?"Thuốc đang chờ giao.":"Lượt đã hoàn tất.");
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
}
