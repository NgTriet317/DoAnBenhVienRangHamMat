# Tách giao diện 15 phân hệ Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox ( - [ ] ) syntax for tracking.

**Goal:** Tạo 15 giao diện XAML nghiệp vụ riêng và 5 giao diện hỗ trợ, tất cả chỉ chứa control tĩnh với tên file tiếng Việt không dấu.

**Architecture:** Giữ App.xaml, MainWindow.xaml và Theme.xaml làm hạ tầng WPF dùng chung. Mỗi trang nghiệp vụ có một UserControl và code-behind tối thiểu riêng; không dùng RecordsView/EditorWindow, không binding dữ liệu, không xử lý sự kiện trong trang. MainWindow có chuyển trang sidebar tối thiểu.

**Tech Stack:** C# / WPF / .NET 8 / XAML; không thêm package hoặc thư viện bên thứ ba.

**Spec:** docs/superpowers/specs/2026-09-23-tach-giao-dien-15-phan-he-design.md

## Global Constraints

- Các trang mới chỉ mô tả giao diện và control; không triển khai xử lý nghiệp vụ, dữ liệu động, lưu trữ, điều hướng trong trang hoặc sự kiện nút. MainWindow được phép có event chuyển sidebar.
- File giao diện nghiệp vụ và hỗ trợ dùng tên tiếng Việt không dấu; App.xaml, MainWindow.xaml và Theme.xaml là ngoại lệ hạ tầng hiện có.
- XAML các trang nghiệp vụ/hỗ trợ không chứa Click, SelectionChanged, TextChanged, MouseDoubleClick, Command, DataContext hoặc binding dữ liệu động. MainWindow chỉ có SelectionChanged cho sidebar.
- x:Name được phép; các nút, ô nhập, bảng và bộ lọc chỉ là control trực quan.
- Mỗi trang có cặp .xaml/.xaml.cs; x:Class khớp với namespace/lớp trong code-behind.
- Không khôi phục RangHamMat.Core, JSON, API, database hoặc logic nghiệp vụ.
- Các view tổng quát cũ chỉ được xóa sau khi không còn tham chiếu và solution build thành công.
- Workspace hiện không có thư mục .git; dùng các lần build/check làm checkpoint thay cho commit.

## Review Focus

- Thiếu hoặc sai tên một trong 15 trang nghiệp vụ hay 5 trang hỗ trợ — bộ kiểm tra phải báo chính xác file còn thiếu.
- x:Class không khớp code-behind — mỗi trang phải có kiểm tra cặp file và tên lớp.
- Resource dùng trong XAML nhưng không có trong Theme.xaml — kiểm tra tất cả StaticResource trước khi build.
- Event, command hoặc binding động bị đưa trở lại vào trang tĩnh — bộ kiểm tra phải quét các thuộc tính cấm trên các file mới.
- View cũ hoặc RecordsView/EditorWindow vẫn được tham chiếu sau chuyển đổi — rg và bước build cuối phải phát hiện trước khi xóa.

### Task 1: Củng cố bộ kiểm tra XAML-only

**Files:**
- Modify: RangHamMat.Checks/Program.cs
- Modify: RangHamMat.Checks/RangHamMat.Checks.csproj only if a stale Core include remains

**Interfaces:**
- Consumes: all non-generated .xaml files under RangHamMat.Wpf
- Produces: a console check that exits non-zero with a specific failure message and exits zero with a summary

- [ ] **Step 1: Add a failing page-pair check**

Add this helper to Program.cs:

~~~csharp
static void RequirePagePair(string root, string stem, List<string> errors)
{
    var xaml = Path.Combine(root, "Views", stem + ".xaml");
    var codeBehind = Path.Combine(root, "Views", stem + ".xaml.cs");
    if (!File.Exists(xaml)) errors.Add($"Missing XAML page: {stem}.xaml");
    if (!File.Exists(codeBehind)) errors.Add($"Missing code-behind: {stem}.xaml.cs");
}
~~~

Use it first with the five support stems TongQuan, BaoCao, PhieuHuongDan, NhatKy, ChonRang. Run the checker and confirm it fails with missing-page messages rather than a compiler error.

- [ ] **Step 2: Implement the common static checks**

Add these regular expressions:

~~~csharp
var banned = new Regex(@"\b(Click|SelectionChanged|TextChanged|SelectedDateChanged|MouseDoubleClick|Loaded|Closing|Closed|KeyDown|MouseDown|Command|DataContext)\s*=|\{Binding", RegexOptions.Compiled);
var classPattern = new Regex(@"x:Class=""([^""]+)""", RegexOptions.Compiled);
var resourcePattern = new Regex(@"\{StaticResource\s+([^}]+)\}", RegexOptions.Compiled);
var keyPattern = new Regex(@"x:Key=""([^""]+)""", RegexOptions.Compiled);
~~~

For every required new XAML file, verify that x:Class ends with the file stem and banned does not match. Scan all non-generated XAML for missing StaticResource keys. Print each failure with its relative file path.

- [ ] **Step 3: Verify the checker implementation**

Run: dotnet run --project RangHamMat.Checks/RangHamMat.Checks.csproj -c Release --no-restore

Expected: FAIL with missing migration-page messages, not a compiler error. Do not proceed until the checker itself compiles.

### Task 2: Create shared/support pages

**Files:**
- Create: RangHamMat.Wpf/Views/TongQuan.xaml and TongQuan.xaml.cs
- Create: RangHamMat.Wpf/Views/BaoCao.xaml and BaoCao.xaml.cs
- Create: RangHamMat.Wpf/Views/PhieuHuongDan.xaml and PhieuHuongDan.xaml.cs
- Create: RangHamMat.Wpf/Views/NhatKy.xaml and NhatKy.xaml.cs
- Create: RangHamMat.Wpf/Views/ChonRang.xaml and ChonRang.xaml.cs
- Modify: RangHamMat.Checks/Program.cs to require these five stems

**Interfaces:**
- Consumes: Theme.xaml resources Title, Subtitle, Card, Primary, Muted, Accent, and Line
- Produces: five standalone XAML surfaces with no data or event dependencies

- [ ] **Step 1: Extend the required-file test**

Add TongQuan, BaoCao, PhieuHuongDan, NhatKy, and ChonRang to the required support list. Run the checker. Expected: FAIL only for these five missing pairs.

- [ ] **Step 2: Create the five XAML roots with these exact control inventories**

- TongQuan: UserControl; ScrollViewer, StackPanel, four metric Border cards, DataGrid, ItemsControl.
- BaoCao: UserControl; two DatePicker controls, two Button controls, four metric Border cards, ProgressBar, DataGrid.
- PhieuHuongDan: UserControl; two ComboBox controls, two multiline TextBox controls, two Button controls, FlowDocumentScrollViewer.
- NhatKy: UserControl; header StackPanel, Card Border, DataGrid with static headers ThoiGian, VaiTro, PhanHe, MaHoSo, ThaoTac.
- ChonRang: Window; two UniformGrid controls, two Button controls, one TextBlock for the selection label.

Declare every Button without Click. Give every table column a static Header and Width, never Binding.

- [ ] **Step 3: Create minimal code-behind**

Each UserControl uses this shape, changing the class name to the file stem:

~~~csharp
using System.Windows.Controls;

namespace RangHamMat.Views;

public partial class TongQuan : UserControl
{
    public TongQuan() => InitializeComponent();
}
~~~

ChonRang.xaml.cs uses Window and using System.Windows instead of UserControl.

- [ ] **Step 4: Run the group check**

Run: dotnet run --project RangHamMat.Checks/RangHamMat.Checks.csproj -c Release --no-restore

Expected: PASS for the five support pairs and no banned XAML attributes.

### Task 3: Create patient-care pages

**Files:**
- Create: RangHamMat.Wpf/Views/BenhNhan.xaml and BenhNhan.xaml.cs
- Create: RangHamMat.Wpf/Views/KhamDieuTri.xaml and KhamDieuTri.xaml.cs
- Create: RangHamMat.Wpf/Views/DonThuoc.xaml and DonThuoc.xaml.cs
- Modify: RangHamMat.Checks/Program.cs to require these three stems

**Interfaces:**
- Consumes: shared WPF styles only
- Produces: classes RangHamMat.Views.BenhNhan, RangHamMat.Views.KhamDieuTri, and RangHamMat.Views.DonThuoc

- [ ] **Step 1: Add the three stems to the required-page test and run it**

Expected: FAIL for BenhNhan, KhamDieuTri, and DonThuoc before their files exist.

- [ ] **Step 2: Create BenhNhan.xaml**

Use a title/subtitle, a Card with TextBox search, ComboBox status, DatePicker, and visual buttons; a main Card with DataGrid headers Ma, HoTen, NgaySinh, DienThoai, TrangThai; and a detail Card with labeled TextBox controls for address, reason, medical history, and allergy.

- [ ] **Step 3: Create KhamDieuTri.xaml**

Use a patient/appointment summary Card; a two-column body with a tooth-selection Card containing two UniformGrid controls; and a treatment Card containing TextBox controls for result, diagnosis, plan, treatment area, and follow-up date.

- [ ] **Step 4: Create DonThuoc.xaml**

Use patient/visit summary controls, a medication DataGrid with static headers TenThuoc, DonVi, SoLuong, SoNgay, HuongDan, and a notes Card containing a multiline TextBox.

- [ ] **Step 5: Create the three code-behind files and run the checker**

Each constructor calls InitializeComponent and contains no additional method. Run the checker. Expected: PASS for all three patient-care pairs.

### Task 4: Create workforce and schedule pages

**Files:**
- Create: RangHamMat.Wpf/Views/NhanSu.xaml and NhanSu.xaml.cs
- Create: RangHamMat.Wpf/Views/CaLamViec.xaml and CaLamViec.xaml.cs
- Create: RangHamMat.Wpf/Views/LichKham.xaml and LichKham.xaml.cs
- Modify: RangHamMat.Checks/Program.cs to require these three stems

**Interfaces:**
- Consumes: shared WPF styles only
- Produces: separate workforce, shift, and appointment layouts without ScheduleView or RecordsView dependencies

- [ ] **Step 1: Add the three stems to the required-page test and run it**

Expected: FAIL for the three missing pairs.

- [ ] **Step 2: Create NhanSu.xaml**

Use a filter Card with TextBox and two ComboBox controls, a staff DataGrid with static headers Ma, HoTen, ChucVu, Khoa, TrangThai, and a right-side detail Card for contact, qualifications, certificates, and start date.

- [ ] **Step 3: Create CaLamViec.xaml**

Use a toolbar Card with three visual navigation buttons, DatePicker, ComboBox for view mode, and ComboBox for doctor; use a calendar Grid with seven columns and static shift Cards containing time, room, attendance, and note controls.

- [ ] **Step 4: Create LichKham.xaml**

Use a toolbar Card with three visual navigation buttons, DatePicker, ComboBox controls for mode/doctor/room/status, a calendar Grid, and a second Card with a static appointment DataGrid with headers MaLich, BenhNhan, BacSi, Phong, DichVu, TrangThai.

- [ ] **Step 5: Create code-behind and run the checker**

Each constructor calls InitializeComponent only. Expected: PASS for NhanSu, CaLamViec, and LichKham.

### Task 5: Create equipment, inventory, and supplier pages

**Files:**
- Create: RangHamMat.Wpf/Views/ThietBi.xaml and ThietBi.xaml.cs
- Create: RangHamMat.Wpf/Views/BaoTri.xaml and BaoTri.xaml.cs
- Create: RangHamMat.Wpf/Views/VatTu.xaml and VatTu.xaml.cs
- Create: RangHamMat.Wpf/Views/NhaCungCap.xaml and NhaCungCap.xaml.cs
- Create: RangHamMat.Wpf/Views/NhapXuatVatTu.xaml and NhapXuatVatTu.xaml.cs
- Modify: RangHamMat.Checks/Program.cs to require these five stems

**Interfaces:**
- Consumes: shared WPF styles only
- Produces: separate layouts for equipment, maintenance, stock, suppliers, and stock movements

- [ ] **Step 1: Add the five stems to the required-page test and run it**

Expected: FAIL for the five missing pairs.

- [ ] **Step 2: Create ThietBi.xaml**

Use a filter Card with TextBox, ComboBox for group/room/status, a DataGrid with Ma, Ten, Nhom, Phong, NhaCungCap, TinhTrang, HanBaoTri, and a detail Card.

- [ ] **Step 3: Create BaoTri.xaml**

Use three static metric Cards, a filter row with DatePicker controls and ComboBox status, and a maintenance DataGrid with ThietBi, NgayThucHien, NoiDung, ChiPhi, NgayKeTiep, TrangThai.

- [ ] **Step 4: Create VatTu.xaml**

Use four static stock metric Cards, an alert Card for low-stock/expiry status, filters for group/supplier/status, and a stock DataGrid with Ma, Ten, Lo, Ton, Nguong, HanDung, TrangThai.

- [ ] **Step 5: Create NhaCungCap.xaml**

Use a search/status filter Card, a supplier DataGrid with Ma, Ten, NguoiLienHe, DienThoai, Email, TrangThai, and a contact/detail Card.

- [ ] **Step 6: Create NhapXuatVatTu.xaml**

Use a filter Card with date range, transaction type, supplier/department controls, a transaction DataGrid with MaPhieu, Ngay, Loai, VatTu, SoLuong, Khoa, TrangThai, and a notes/detail Card.

- [ ] **Step 7: Create code-behind and run the checker**

Each constructor calls InitializeComponent only. Expected: PASS for all five asset/inventory pairs.

### Task 6: Create finance and account pages

**Files:**
- Create: RangHamMat.Wpf/Views/DichVu.xaml and DichVu.xaml.cs
- Create: RangHamMat.Wpf/Views/HoaDon.xaml and HoaDon.xaml.cs
- Create: RangHamMat.Wpf/Views/BangLuong.xaml and BangLuong.xaml.cs
- Create: RangHamMat.Wpf/Views/TaiKhoan.xaml and TaiKhoan.xaml.cs
- Modify: RangHamMat.Checks/Program.cs to require these four stems

**Interfaces:**
- Consumes: shared WPF styles only
- Produces: separate finance/catalog/account layouts with no calculation logic

- [ ] **Step 1: Add the four stems to the required-page test and run it**

Expected: FAIL for the four missing pairs.

- [ ] **Step 2: Create DichVu.xaml**

Use a search/group/status filter Card, a service DataGrid with Ma, Ten, Nhom, DonGia, TrangThai, and a service detail Card with description and display-status controls.

- [ ] **Step 3: Create HoaDon.xaml**

Use a patient/visit summary Card, a service-line DataGrid with DichVu, SoLuong, DonGia, GiamGia, ThanhTien, and a payment summary Card with TongTien, DaThu, ConNo, and payment-method controls.

- [ ] **Step 4: Create BangLuong.xaml**

Use a month DatePicker, department/status ComboBox controls, four static metric Cards, and a payroll DataGrid with NhanSu, LuongCoBan, HoaHong, PhuCap, KhauTru, TongLuong, TrangThai.

- [ ] **Step 5: Create TaiKhoan.xaml**

Use a search/status/role filter Card, an account DataGrid with TenDangNhap, NhanSu, VaiTro, TrangThai, and a detail Card with role and lock-status controls.

- [ ] **Step 6: Create code-behind and run the checker**

Each constructor calls InitializeComponent only. Expected: PASS for all four finance/account pairs.

### Task 7: Update the static shell and remove generic-page references

**Files:**
- Modify: RangHamMat.Wpf/MainWindow.xaml
- Modify: RangHamMat.Wpf/MainWindow.xaml.cs
- Modify: RangHamMat.Checks/Program.cs
- Delete after reference verification: DashboardView.xaml/.cs, ReportsView.xaml/.cs, GuidanceView.xaml/.cs, AuditView.xaml/.cs, ScheduleView.xaml/.cs, RecordsView.xaml/.cs, EditorWindow.xaml/.cs, ToothWindow.xaml/.cs

**Interfaces:**
- Consumes: all 15 domain page names and 5 support page names
- Produces: a shell whose sidebar visibly lists the pages, with one minimal selection handler and no binding

- [ ] **Step 1: Add a failing shell assertion**

Make the checker require these 20 static sidebar labels in MainWindow.xaml: Bệnh nhân, Khám điều trị, Đơn thuốc, Nhân sự, Thiết bị, Bảo trì, Vật tư, Nhà cung cấp, Ca làm việc, Lịch khám, Dịch vụ, Nhập xuất vật tư, Hóa đơn, Bảng lương, Tài khoản, Tổng quan, Báo cáo, Phiếu hướng dẫn, Nhật ký, Chọn răng.

Also assert that MainWindow.xaml contains neither {Binding} nor an unrecognized selection event; require `SelectionChanged="Navigation_SelectionChanged"` and a `TongQuan` default selection in MainWindow.xaml.cs. Run the checker. Expected: FAIL because the current sidebar lacks the new event.

- [ ] **Step 2: Replace the old sidebar templates with static controls**

Keep the existing shell Grid and ContentControl, but replace the data-template ListBox content with 20 explicit ListBoxItem elements. Add only `SelectionChanged="Navigation_SelectionChanged"`; do not add Click, Command, ItemsSource, or any Binding.

- [ ] **Step 3: Simplify MainWindow.xaml.cs**

Remove NavItem and unused imports; keep the constructor, default `TongQuan` selection, and one selection handler that maps sidebar labels to the separate pages.

- [ ] **Step 4: Verify no generic-page references remain**

Run:

~~~powershell
rg -n "RecordsView|EditorWindow|DashboardView|ReportsView|GuidanceView|AuditView|ScheduleView|ToothWindow" RangHamMat.Wpf -g '*.xaml' -g '*.xaml.cs'
~~~

Expected: no matches in active source files. Only then delete the obsolete old view pairs.

- [ ] **Step 5: Run shell checks**

Run: dotnet run --project RangHamMat.Checks/RangHamMat.Checks.csproj -c Release --no-restore

Expected: PASS for all required page pairs, sidebar labels, resources, class names, and banned-attribute checks.

### Task 8: Update documentation and perform final verification

**Files:**
- Modify: README.md
- Modify: KET_QUA_KIEM_TRA.md
- Modify: HUONG_DAN_VISUAL_STUDIO_2022.html only where it claims removed data-driven/Core behavior
- Modify: KiemTra.bat only if its commands no longer match the XAML-only checker

**Interfaces:**
- Consumes: final page names and checker output
- Produces: documentation that describes the static multi-page UI accurately

- [ ] **Step 1: Add a documentation consistency check**

Run:

~~~powershell
rg -n "Core/|DataStore|RecordsView|EditorWindow|60 checks|19 mục|tự lưu dữ liệu|binding" README.md KET_QUA_KIEM_TRA.md HUONG_DAN_VISUAL_STUDIO_2022.html
~~~

Expected: matches are found because the documents still describe the old data-driven application.

- [ ] **Step 2: Update the documented architecture**

Document the 15 named pages, five support pages, shared shell/theme, and static controls with no business functionality. Replace the old 60-check claim with the actual XAML-only checker output.

- [ ] **Step 3: Run full Debug verification**

Run:

~~~powershell
dotnet build BenhVienRangHamMat.sln -c Debug --no-restore -m:1
dotnet run --project RangHamMat.Checks/RangHamMat.Checks.csproj -c Debug --no-restore
~~~

Expected: build succeeds with 0 warnings and 0 errors; checker reports all 20 page pairs and no banned XAML attributes.

- [ ] **Step 4: Run full Release verification**

Run:

~~~powershell
dotnet build BenhVienRangHamMat.sln -c Release --no-restore -m:1
dotnet run --project RangHamMat.Checks/RangHamMat.Checks.csproj -c Release --no-restore
~~~

Expected: build succeeds with 0 warnings and 0 errors; checker reports the same result as Debug.

- [ ] **Step 5: Run the final source scan**

Run:

~~~powershell
rg -n "(Click|SelectionChanged|TextChanged|SelectedDateChanged|MouseDoubleClick|Loaded|Closing|Closed|KeyDown|MouseDown|Command|DataContext)\s*=|\{Binding|RecordsView|EditorWindow" RangHamMat.Wpf\Views -g '*.xaml' -g '*.xaml.cs'
~~~

Expected: no matches in page source; the single approved sidebar event remains in MainWindow.xaml.
