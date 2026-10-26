// AI bổ sung kiểm tra nhập liệu: chặn tiền sai/range/số lẻ, giao dịch bằng 0 và hoàn thiếu lý do ngay tại giao diện; giữ nội dung khi lỗi.
// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ phần giao diện hóa đơn/thu/hoàn tiền/cấp thuốc, liên kết bộ lọc bệnh nhân/lần khám, gọi xử lý lưu và làm mới số tiền; đã chú thích handler và chỉnh hai ô tiền/lý do bằng style trong HoaDon.xaml.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BUS;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình hóa đơn, thu/hoàn tiền và cấp thuốc: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class HoaDon : UserControl, IPatientScoped
{
    private readonly BUS_HOADON finance = new();
    private bool updating;
    private string? visitId, prescriptionId;
    private string? invoiceId;
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string? PatientId { get; private set; }
    /// <summary>
    /// Khởi tạo hóa đơn, thu/hoàn tiền và cấp thuốc từ XAML. Theo dõi cache khi Loaded và làm mới nội dung khi dữ liệu thay đổi; gỡ theo dõi khi Unloaded.
    /// </summary>
    public HoaDon() { InitializeComponent(); HoTroGiaoDien.Observe(this, Refresh); }
    /// <summary>
    /// Áp dụng mã bệnh nhân nguồn và làm mới dữ liệu trong phạm vi hồ sơ này.
    /// </summary>
    public void SetPatient(string id) => FocusContext(id);
    /// <summary>
    /// Giữ bộ mã bệnh nhân/lần khám/đơn thuốc nguồn, bỏ lựa chọn hóa đơn cũ, chuyển sang tab cấp thuốc khi có đơn và tải lại danh sách đúng phạm vi.
    /// </summary>
    public void FocusContext(string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        if(!CanChangePricingContext()) return;
        PatientId = patientId; this.visitId = visitId; this.prescriptionId = prescriptionId; invoiceId = null;
        if (!string.IsNullOrEmpty(prescriptionId)) DetailTabs.SelectedItem = DispenseTab;
        Refresh();
    }
    /// <summary>
    /// Áp dụng ngữ cảnh hồ sơ rồi chọn tab Cấp thuốc để người dùng chọn kho và ghi sổ đơn đã xác nhận.
    /// </summary>
    public void OpenDispensing(string? patientId, string? visitId = null, string? prescriptionId = null)
    { FocusContext(patientId, visitId, prescriptionId); if (!DuLieuGiaoDien.PhamViLamSang) DetailTabs.SelectedItem = DispenseTab; }
    /// <summary>
    /// Nạp lựa chọn bệnh nhân/lần khám/nhân viên/kho/đơn thuốc, đọc danh sách hóa đơn qua BUS và lọc theo bệnh nhân; giữ lựa chọn cũ, cập nhật nút và chi tiết.
    /// </summary>
    private void Refresh()
    {
        if (updating || InvoiceList is null) return;
        updating = true;
        try
        {
            var employee = HoTroGiaoDien.Selected(EmployeeFilter); var warehouse = HoTroGiaoDien.Selected(WarehouseFilter);
            PatientFilter.ItemsSource = DuLieuGiaoDien.Options("BENH_NHAN", true).ToArray(); PatientFilter.SelectedValue = PatientId ?? "";
            VisitFilter.ItemsSource = new[] { new LuaChonDanhMuc("", "Tất cả lần khám") }.Concat(DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH")
                .Where(r => string.IsNullOrEmpty(PatientId) || DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", r) == PatientId)
                .Select(r => new LuaChonDanhMuc(UiRecord.Text(r, "MaLS"), $"#{UiRecord.Text(r, "MaLS")} · {UiRecord.Date(r, "ThoiGianKham"):dd/MM/yyyy} · {UiRecord.Text(r, "TrangThai")}"))).ToArray();
            VisitFilter.SelectedValue = visitId ?? "";
            EmployeeFilter.ItemsSource = DuLieuGiaoDien.Rows("NHAN_VIEN").Where(r => UiRecord.Text(r, "TrangThai") == "Đang làm việc")
                .Select(r => new LuaChonDanhMuc(UiRecord.Text(r, "MaNV"), DuLieuGiaoDien.Label("NHAN_VIEN", r))).ToArray(); EmployeeFilter.SelectedValue = employee;
            WarehouseFilter.ItemsSource = DuLieuGiaoDien.Rows("KHO").Where(r => UiRecord.Text(r, "TrangThai") == "Đang hoạt động")
                .Select(r => new LuaChonDanhMuc(UiRecord.Text(r, "MaKho"), UiRecord.Text(r, "TenKho"))).ToArray(); WarehouseFilter.SelectedValue = warehouse;
            PrescriptionFilter.ItemsSource = DuLieuGiaoDien.Rows("DON_THUOC").Where(r => UiRecord.Text(r, "TrangThai") == "Đã xác nhận"
                && !string.IsNullOrEmpty(visitId) && UiRecord.Text(r, "MaLS") == visitId)
                .Select(r => new LuaChonDanhMuc(UiRecord.Text(r, "MaDT"), $"Đơn #{UiRecord.Text(r, "MaDT")} · {UiRecord.Date(r, "NgayKe"):dd/MM/yyyy HH:mm}")).ToArray(); PrescriptionFilter.SelectedValue = prescriptionId;
            var table = DuLieuGiaoDien.PhamViLamSang ? QuyenNghiepVu.DocDanhSachHoaDon(visitId) : finance.GetInvoices(string.IsNullOrEmpty(visitId) ? null : visitId);
            table.DefaultView.RowFilter = string.IsNullOrEmpty(PatientId) ? "" : $"MaBN='{PatientId.Replace("'", "''")}'";
            InvoiceList.ItemsSource = table.DefaultView;
            InvoiceList.SelectedItem = table.DefaultView.Cast<DataRowView>().FirstOrDefault(r => Convert.ToString(r["MaHD"]) == invoiceId);
            InvoiceCount.Text = $"{table.DefaultView.Count} hóa đơn"; CreateButton.IsEnabled = HasVisit() && HasEmployee(); ShowInvoice(); ShowStock();
            if (DuLieuGiaoDien.PhamViLamSang)
            {
                CreateButton.IsEnabled = false; EmployeeFilter.SelectedValue = PhienDangNhap.MaNV; EmployeeFilter.IsEnabled = false; DispenseTab.Visibility = Visibility.Collapsed;
                var actor = QuyenNghiepVu.LayNguCanh();
                if (actor.LaAdmin || actor.CoQuyen(ET.QuyenLamViec.ThuTien) || actor.CoQuyen(ET.QuyenLamViec.Kham))
                {
                    var first = PricingHost.Content is null;
                    var pricing = PricingHost.Content as BangTinhPhiEditor ?? new BangTinhPhiEditor(actor);
                    if(first) pricing.ThanhToanHoanTat += (_,_) => { invoiceId=pricing.KetQua?.MaHD; DuLieuGiaoDien.RefreshFromDatabase(); Refresh(); };
                    PricingHost.Content = pricing; PricingTab.Visibility = Visibility.Visible; pricing.FocusContext(visitId);
                    if (first || DetailTabs.SelectedItem == DispenseTab) DetailTabs.SelectedItem = PricingTab;
                }
                PageStatus.Text = "Xem bảng tính theo lượt; chọn hóa đơn để đối chiếu lịch sử và in lại.";
            }
        }
        catch (Exception ex) { PageStatus.Text = "Không thể tải hóa đơn: " + ex.Message; }
        finally { updating = false; }
    }
    /// <summary>
    /// Hiện đầu hóa đơn, các khoản thu, lịch sử thu/hoàn và tổng tiền; chỉ bật xác nhận/hủy cho nháp, bật thanh toán cho hóa đơn xác nhận có nhân viên thực hiện.
    /// </summary>
    private void ShowInvoice()
    {
        var row = InvoiceList.SelectedItem as DataRowView;
        InvoicePanel.Visibility = row is null ? Visibility.Collapsed : Visibility.Visible; InvoiceEmpty.Visibility = row is null ? Visibility.Visible : Visibility.Collapsed;
        if (row is null) { invoiceId = null; return; }
        invoiceId = Convert.ToString(row["MaHD"]) ?? throw new InvalidOperationException("Hóa đơn thiếu mã.");
        InvoiceTitle.Text = $"Hóa đơn #{invoiceId} · {row["TrangThai"]}";
        InvoiceInfo.Text = $"{row["HoTenBN"]} · Lần khám #{row["MaLS"]} · {Convert.ToDateTime(row["NgayLap"]):dd/MM/yyyy HH:mm} · Lập bởi {row["NguoiLap"]}";
        DetailGrid.ItemsSource = (DuLieuGiaoDien.PhamViLamSang ? QuyenNghiepVu.DocChiTietHoaDon(invoiceId) : finance.GetInvoiceDetails(invoiceId)).DefaultView;
        PaymentGrid.ItemsSource = (DuLieuGiaoDien.PhamViLamSang ? QuyenNghiepVu.DocChiTietHoaDon(invoiceId, true) : finance.GetPayments(invoiceId)).DefaultView;
        TotalsText.Text = $"Tổng {Money(row["TongTien"])}  ·  Giảm {Money(row["GiamGia"])}  ·  Phải thu {Money(row["SoTienPhaiThu"])}\nĐã thu ròng {Money(row["DaThu"])}  ·  Còn phải trả {Money(row["ConPhaiThu"])}";
        ConfirmButton.IsEnabled = CancelButton.IsEnabled = Convert.ToString(row["TrangThai"]) == "Nháp";
        PaymentForm.IsEnabled = Convert.ToString(row["TrangThai"]) == "Đã xác nhận" && HasEmployee();
        if (DuLieuGiaoDien.PhamViLamSang) { ConfirmButton.IsEnabled = CancelButton.IsEnabled = false; PaymentForm.IsEnabled = false; }
    }
    /// <summary>
    /// Hiện dòng thuốc và lô tồn của kho đang chọn, kiểm tra đơn đã được xuất ghi sổ chưa và bật/tắt nút cấp thuốc theo đơn, kho, nhân viên.
    /// </summary>
    private void ShowStock()
    {
        if (DuLieuGiaoDien.PhamViLamSang) { DispenseButton.IsEnabled = false; StockGrid.ItemsSource = null; return; }
        prescriptionId = HoTroGiaoDien.Selected(PrescriptionFilter); var rx = DuLieuGiaoDien.Find("DON_THUOC", prescriptionId);
        var lines = DuLieuGiaoDien.Children("CHI_TIET_DON_THUOC", "MaDT", prescriptionId);
        PrescriptionGrid.ItemsSource = rx is null ? null : lines.Select(r => new { Thuoc = DuLieuGiaoDien.Lookup("MaThuoc", UiRecord.Get(r, "MaThuoc")), SoLuong = UiRecord.Get(r, "SoLuong"), LieuDung = UiRecord.Text(r, "LieuDung"), HuongDan = UiRecord.Text(r, "HuongDan") }).ToArray();
        var valid = !string.IsNullOrEmpty(prescriptionId) && !string.IsNullOrEmpty(HoTroGiaoDien.Selected(WarehouseFilter)); StockGrid.ItemsSource = null;
        if (valid)
        {
            var stock = finance.GetStock(HoTroGiaoDien.Selected(WarehouseFilter), prescriptionId!); stock.Columns.Add("Thuoc", typeof(string));
            foreach (DataRow row in stock.Rows) row["Thuoc"] = DuLieuGiaoDien.Lookup("MaHH", row["MaHH"]); StockGrid.ItemsSource = stock.DefaultView;
        }
        var posted = DuLieuGiaoDien.Rows("CHI_TIET_PHIEU_XUAT").Any(x => lines.Any(c => UiRecord.Text(c, "MaCTDT") == UiRecord.Text(x, "MaCTDT")) &&
            UiRecord.Text(DuLieuGiaoDien.Find("PHIEU_XUAT", UiRecord.Get(x, "MaPX")) ?? new object(), "TrangThai") == "Đã ghi sổ");
        DispenseButton.IsEnabled = valid && HasEmployee() && !posted;
        DispenseInfo.Text = posted ? "Đơn đã cấp đủ và ghi sổ. Có thể lập hóa đơn cho các khoản chưa được thu."
            : rx is null ? "Chọn một lần khám, sau đó chọn đơn thuốc đã xác nhận và kho cấp thuốc."
            : "Tồn kho và trạng thái đơn được kiểm tra lại trong giao dịch. Nếu thiếu bất kỳ thuốc nào, toàn bộ lần cấp được hủy.";
    }
    /// <summary>
    /// Kiểm tra mã lần khám đã chọn còn tồn tại trong cache và có trạng thái Đã xác nhận trước khi cho lập hóa đơn.
    /// </summary>
    private bool HasVisit() => !string.IsNullOrEmpty(visitId) && DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", visitId) is object visit && UiRecord.Text(visit, "TrangThai") == "Đã xác nhận";
    /// <summary>
    /// Kiểm tra đã chọn mã nhân viên thực hiện trong ComboBox của trang hóa đơn.
    /// </summary>
    private bool HasEmployee() => !string.IsNullOrEmpty(HoTroGiaoDien.Selected(EmployeeFilter));
    /// <summary>
    /// Đổi giá trị tiền từ DataRow sang decimal và dùng TreatmentMoney.Format để hiển thị thống nhất theo vi-VN.
    /// </summary>
    private static string Money(object value) => TreatmentMoney.Format(Convert.ToDecimal(value));
    /// <summary>
    /// Lấy mã nhân viên đang chọn; nếu chưa chọn thì phát lỗi yêu cầu chọn nhân viên trước thao tác thu/hoàn/cấp thuốc.
    /// </summary>
    private string EmployeeId() => HasEmployee() ? HoTroGiaoDien.Selected(EmployeeFilter) : throw new InvalidOperationException("Chọn nhân viên thực hiện.");
    /// <summary>
    /// Chạy thao tác BUS, báo lỗi nếu không thực hiện được; sau thành công mới tải lại SQL và hiển thị kết quả, lỗi làm mới được thông báo riêng để tránh thao tác lặp.
    /// </summary>
    private void Execute(Action operation, string success)
    {
        if (DuLieuGiaoDien.PhamViLamSang) { PageStatus.Text = "Chứng từ lịch sử chỉ được đối chiếu và in lại."; return; }
        try { operation(); }
        catch (Exception ex) { if(ex is LoiNhapLieu inputError) KiemTraNhapLieu.MarkError(this,inputError); else PageStatus.Text = "Không thể thực hiện: " + ex.Message; return; }
        try { DuLieuGiaoDien.RefreshFromDatabase(); Refresh(); PageStatus.Text = success; }
        catch (Exception ex) { PageStatus.Text = success + " Chưa tải lại được danh sách; bấm Tải lại để cập nhật: " + ex.Message; }
    }
    /// <summary>
    /// Đọc lại cache SQL và danh sách hóa đơn, thông báo kết quả; lỗi tải lại chỉ hiện lên PageStatus.
    /// </summary>
    private void Reload_Click(object sender, RoutedEventArgs e)
    { try { DuLieuGiaoDien.RefreshFromDatabase(); Refresh(); PageStatus.Text = "Đã tải lại từ database."; } catch (Exception ex) { PageStatus.Text = "Không thể tải lại dữ liệu: " + ex.Message; } }
    /// <summary>
    /// Đổi bệnh nhân lọc, xóa ngữ cảnh lần khám/đơn/hóa đơn cũ và làm mới để không dùng dữ liệu thuộc bệnh nhân trước.
    /// </summary>
    private void Patient_Changed(object sender, SelectionChangedEventArgs e)
    { if (updating || !CanChangePricingContext()) return; PatientId = HoTroGiaoDien.Selected(PatientFilter); visitId = prescriptionId = null; invoiceId = null; Refresh(); }
    /// <summary>
    /// Đổi lần khám, bỏ lựa chọn đơn và hóa đơn cũ, suy ra đúng bệnh nhân từ lần khám rồi tải lại dữ liệu.
    /// </summary>
    private void Visit_Changed(object sender, SelectionChangedEventArgs e)
    { if (updating || !CanChangePricingContext()) return; visitId = HoTroGiaoDien.Selected(VisitFilter); prescriptionId = null; invoiceId = null; var visit = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", visitId); if (visit is not null) PatientId = DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", visit); Refresh(); }

    private bool CanChangePricingContext()
    {
        if(PricingHost.Content is not BangTinhPhiEditor { } pricing || (!pricing.DangXuLy && !pricing.ChoDoiChieu)) return true;
        updating=true;try{PatientFilter.SelectedValue=PatientId??"";VisitFilter.SelectedValue=visitId??"";}finally{updating=false;}
        PageStatus.Text="Cần đối chiếu yêu cầu đang thu trước khi đổi bệnh nhân hoặc lượt.";return false;
    }
    /// <summary>
    /// Khi đổi nhân viên thực hiện, cập nhật khả năng lập hóa đơn/thanh toán/cấp thuốc theo lựa chọn hiện tại.
    /// </summary>
    private void Employee_Changed(object sender, SelectionChangedEventArgs e)
    { if (updating) return; try { CreateButton.IsEnabled = !DuLieuGiaoDien.PhamViLamSang && HasVisit() && HasEmployee(); ShowInvoice(); ShowStock(); } catch (Exception ex) { PageStatus.Text = ex.Message; } }
    /// <summary>
    /// Đọc và hiển thị chi tiết hóa đơn vừa chọn; bỏ qua sự kiện phát sinh trong lúc đổ danh sách.
    /// </summary>
    private void Invoice_Changed(object sender, SelectionChangedEventArgs e)
    { if (updating) return; try { ShowInvoice(); } catch (Exception ex) { PageStatus.Text = "Không thể đọc chi tiết: " + ex.Message; } }
    /// <summary>
    /// Tải lại dòng đơn thuốc và tồn lô khi đổi đơn hoặc kho cấp thuốc, báo lỗi nếu BUS không đọc được tồn.
    /// </summary>
    private void Stock_Changed(object sender, SelectionChangedEventArgs e)
    { if (updating) return; try { ShowStock(); } catch (Exception ex) { PageStatus.Text = "Không thể đọc tồn kho: " + ex.Message; } }
    /// <summary>
    /// Mở CuaSoHoaDon ngay trong trang, truyền bệnh nhân/lần khám/nhân viên; khi lưu xong nhận mã hóa đơn nháp và chọn lại nó.
    /// </summary>
    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (DuLieuGiaoDien.PhamViLamSang) { PageStatus.Text = "Chứng từ lịch sử chỉ được đối chiếu và in lại."; return; }
        var editor = new CuaSoHoaDon(); editor.ConfigureContext(PatientId, visitId, HoTroGiaoDien.Selected(EmployeeFilter));
        editor.CreationCompleted += id => { invoiceId = id; CloseEditor(); Refresh(); PageStatus.Text = $"Đã lập hóa đơn nháp #{id}. Kiểm tra khoản thu rồi xác nhận để thanh toán."; };
        editor.CloseRequested += CloseEditor; EditorHost.Content = editor; EditorHost.Visibility = Visibility.Visible; InvoiceContent.Visibility = Visibility.Collapsed; DetailTabs.SelectedIndex = 0;
    }
    /// <summary>
    /// Gỡ form lập hóa đơn khỏi EditorHost và hiện lại phần chi tiết/danh sách hóa đơn.
    /// </summary>
    private void CloseEditor() { EditorHost.Content = null; EditorHost.Visibility = Visibility.Collapsed; InvoiceContent.Visibility = Visibility.Visible; }
    /// <summary>
    /// Gọi BUS xác nhận hóa đơn nháp rồi tải lại dữ liệu; hóa đơn xác nhận mới được ghi nhận thanh toán.
    /// </summary>
    private void Confirm_Click(object sender, RoutedEventArgs e) => Execute(() => finance.ConfirmInvoice(invoiceId ?? ""), "Đã xác nhận hóa đơn. Có thể thu tiền.");
    /// <summary>
    /// Gọi BUS hủy hóa đơn nháp và giải phóng các nguồn thu của nó, sau đó tải lại danh sách.
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e) => Execute(() => finance.CancelInvoice(invoiceId ?? ""), "Đã hủy hóa đơn nháp và giải phóng các khoản thu.");
    /// <summary>
    /// Phân tích số tiền đã nhập, đọc loại Thu/Hoàn, phương thức và lý do rồi gọi BUS.RecordPayment; sau lưu thành công xóa ô nhập và làm mới số dư/lịch sử.
    /// </summary>
    private void Payment_Click(object sender, RoutedEventArgs e) => Execute(() =>
    {
        if (!decimal.TryParse(AmountBox.Text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)) throw new LoiNhapLieu("SoTien","Số tiền phải là số hợp lệ, ví dụ 150000 hoặc 150000,50; không dùng phân cách hàng nghìn.");
        var kind=HoTroGiaoDien.Choice(PaymentType);
        KiemTraNhapLieu.ValidateValues("THANH_TOAN",new Dictionary<string,object?> {["SoTien"]=amount,["LoaiGiaoDich"]=kind,["PhuongThuc"]=HoTroGiaoDien.Choice(MethodBox),["LyDo"]=ReasonBox.Text});
        if(amount<=0)throw new LoiNhapLieu("SoTien","Số tiền thu/hoàn phải lớn hơn 0.");
        if(kind=="Hoàn"&&string.IsNullOrWhiteSpace(ReasonBox.Text))throw new LoiNhapLieu("LyDo","Vui lòng nhập lý do hoàn tiền.");
        // Giao diện chỉ phân tích đầu vào; BUS kiểm tra hóa đơn, số dư, giới hạn thu/hoàn và lý do hoàn trong giao dịch SQL.
        finance.RecordPayment(invoiceId ?? "", EmployeeId(), amount, kind, HoTroGiaoDien.Choice(MethodBox), ReasonBox.Text); AmountBox.Clear(); ReasonBox.Clear();
        KiemTraNhapLieu.SetError(AmountBox,""); KiemTraNhapLieu.SetError(ReasonBox,"");
    }, "Đã ghi nhận giao dịch và cập nhật số còn phải trả.");
    /// <summary>
    /// Gọi BUS cấp đủ đơn thuốc từ kho đã chọn và ghi sổ phiếu xuất; BUS kiểm tra lô/tồn trong giao dịch, màn hình làm mới sau thành công.
    /// </summary>
    private void Dispense_Click(object sender, RoutedEventArgs e) => Execute(() => finance.DispensePrescription(HoTroGiaoDien.Selected(PrescriptionFilter), HoTroGiaoDien.Selected(WarehouseFilter), EmployeeId()), "Đã cấp đủ đơn thuốc và ghi sổ phiếu xuất. Có thể lập hóa đơn.");
    /// <summary>
    /// Lấy mã bệnh nhân/lần khám từ hóa đơn đang chọn hoặc ngữ cảnh trang và truyền sang phân hệ liên quan qua DieuHuongHoSo.
    /// </summary>
    private void Navigate(string module)
    { var row = InvoiceList.SelectedItem as DataRowView; DieuHuongHoSo.Open(this, module, Convert.ToString(row?["MaBN"]) ?? PatientId ?? "", Convert.ToString(row?["MaLS"]) ?? visitId, prescriptionId); }
    /// <summary>
    /// Mở hồ sơ bệnh nhân nguồn của bản ghi đang chọn, giữ mã bệnh nhân qua điều hướng của workspace.
    /// </summary>
    private void Patient_Click(object sender, RoutedEventArgs e) => Navigate("BenhNhan");
    /// <summary>
    /// Mở lần khám nguồn của bản ghi hiện tại trong cùng hồ sơ bệnh nhân.
    /// </summary>
    private void Visit_Click(object sender, RoutedEventArgs e) => Navigate("KhamDieuTri");
    /// <summary>
    /// Mở danh sách Dịch vụ thực hiện, giữ bệnh nhân/lần khám nguồn.
    /// </summary>
    private void Services_Click(object sender, RoutedEventArgs e) => Navigate("DichVuThucHien");
    /// <summary>
    /// Mở phân hệ Đơn thuốc của bệnh nhân/lần khám nguồn đang xem.
    /// </summary>
    private void Prescription_Click(object sender, RoutedEventArgs e) => Navigate("DonThuoc");
    /// <summary>
    /// Đọc lại đầu và chi tiết hóa đơn từ BUS, mở cửa sổ xem trước/in; thông báo lỗi nếu không đọc được chứng từ.
    /// </summary>
    private void Print_Click(object sender, RoutedEventArgs e)
    { try { if (invoiceId is null) return; var header = (DuLieuGiaoDien.PhamViLamSang ? QuyenNghiepVu.DocDanhSachHoaDon() : finance.GetInvoices()).Rows.Cast<DataRow>().Single(r => Convert.ToString(r["MaHD"]) == invoiceId); var details = DuLieuGiaoDien.PhamViLamSang ? QuyenNghiepVu.DocChiTietHoaDon(invoiceId) : finance.GetInvoiceDetails(invoiceId); new XemTruocHoaDon(header, details) { Owner = Window.GetWindow(this) }.ShowDialog(); } catch (Exception ex) { PageStatus.Text = "Không thể xem trước hóa đơn: " + ex.Message; } }
}

/// <summary>
/// Converter một chiều định dạng tiền cho bảng/list hóa đơn và hiển thị dấu gạch khi nguồn tiền chưa có.
/// </summary>
public sealed class InvoiceMoneyConverter : IValueConverter
{
    /// <summary>
    /// Dùng converter binding một chiều để định dạng tiền; giá trị null/DBNull/UnsetValue hiện dấu gạch thay cho một số tiền giả.
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is null or DBNull || value == DependencyProperty.UnsetValue ? "—" : TreatmentMoney.Format(System.Convert.ToDecimal(value));
    /// <summary>
    /// Không hỗ trợ đổi chuỗi tiền hiển thị ngược thành dữ liệu; converter này chỉ dùng cho binding đọc.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
