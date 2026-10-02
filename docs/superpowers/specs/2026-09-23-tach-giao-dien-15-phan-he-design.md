# Đặc tả: Tách giao diện riêng cho 15 phân hệ

Ngày: 2026-09-23  
Trạng thái: Chờ người dùng review trước khi lập kế hoạch triển khai

## 1. Mục tiêu

Chuyển project từ mô hình một giao diện CRUD dùng chung, thay đổi nội dung theo dữ liệu, sang mô hình mỗi phân hệ có một giao diện XAML riêng.

Các trang mới chỉ mô tả giao diện và control. Không triển khai xử lý nghiệp vụ, dữ liệu động, lưu trữ hoặc sự kiện nút; riêng shell có chuyển trang sidebar tối thiểu.

## 2. Nguyên tắc kiến trúc

- `MainWindow.xaml`, `App.xaml` và `Theme.xaml` là phần khung/style dùng chung.
- Mỗi phân hệ có một `UserControl` XAML riêng và một file code-behind tối thiểu tương ứng.
- Các trang không dùng `RecordsView` hoặc `EditorWindow` để thay đổi bố cục theo cấu hình.
- Các trang nghiệp vụ/hỗ trợ không chứa `Click`, `SelectionChanged`, `TextChanged`, `MouseDoubleClick`, `Command`, `DataContext` hoặc binding dữ liệu động; `MainWindow.xaml` chỉ có `SelectionChanged` cho sidebar.
- `x:Name` được phép để Visual Studio hiển thị và thiết kế control; tên này không tự tạo chức năng.
- Các nút, ô nhập, bảng và bộ lọc chỉ là control trực quan, không thực hiện thao tác.
- File trang dùng tên tiếng Việt không dấu; tên lớp khớp với tên file để tránh lỗi `x:Class`.
- `App.xaml`, `MainWindow.xaml` và `Theme.xaml` là file hạ tầng WPF hiện có, được giữ tên kỹ thuật; quy tắc tiếng Việt không dấu áp dụng cho toàn bộ file giao diện nghiệp vụ và hỗ trợ mới.

## 3. Danh sách 15 giao diện nghiệp vụ

Mỗi dòng tạo một cặp file `.xaml` và `.xaml.cs` trong `RangHamMat.Wpf/Views`.

| Phân hệ | File | Bố cục chính |
|---|---|---|
| Bệnh nhân | `BenhNhan.xaml` | Thanh tiêu đề, tìm kiếm/lọc, bảng bệnh nhân, panel thông tin cá nhân và y tế |
| Khám điều trị | `KhamDieuTri.xaml` | Chọn bệnh nhân, thông tin lần khám, sơ đồ răng, chẩn đoán, kế hoạch điều trị |
| Đơn thuốc | `DonThuoc.xaml` | Thông tin lần khám, bảng thuốc, số lượng, số ngày, hướng dẫn sử dụng |
| Nhân sự | `NhanSu.xaml` | Bảng nhân viên, bộ lọc khoa/chức vụ, hồ sơ chuyên môn và chứng chỉ |
| Thiết bị | `ThietBi.xaml` | Danh sách thiết bị, phòng, nhà cung cấp, tình trạng, hạn bảo trì |
| Bảo trì | `BaoTri.xaml` | Lịch sử sửa chữa, thiết bị, chi phí, ngày thực hiện và ngày kế tiếp |
| Vật tư | `VatTu.xaml` | Tổng quan tồn kho, cảnh báo tồn thấp/hạn dùng, bảng vật tư |
| Nhà cung cấp | `NhaCungCap.xaml` | Danh sách nhà cung cấp, thông tin liên hệ và trạng thái |
| Ca làm việc | `CaLamViec.xaml` | Lịch ca, bác sĩ, phòng, giờ làm, trạng thái và ghi chú |
| Lịch khám | `LichKham.xaml` | Lịch ngày/tuần/tháng, bộ lọc bác sĩ/phòng/trạng thái, danh sách lịch hẹn |
| Dịch vụ | `DichVu.xaml` | Danh mục dịch vụ, nhóm dịch vụ, đơn giá và trạng thái |
| Nhập xuất vật tư | `NhapXuatVatTu.xaml` | Bộ lọc chứng từ, bảng nhập/xuất, loại giao dịch, số lượng và diễn giải |
| Hóa đơn | `HoaDon.xaml` | Thông tin bệnh nhân/lần khám, dòng dịch vụ, giảm giá, đã thu và còn phải thu |
| Bảng lương | `BangLuong.xaml` | Chọn kỳ lương, bảng nhân sự, lương cơ bản, hoa hồng, phụ cấp và khấu trừ |
| Tài khoản | `TaiKhoan.xaml` | Danh sách tài khoản, nhân sự liên kết, vai trò và trạng thái |

## 4. Các giao diện hỗ trợ

Các giao diện sau không tính vào 15 phân hệ dữ liệu nhưng vẫn dùng tên file tiếng Việt không dấu:

- `TongQuan.xaml`: tổng quan bệnh viện và các thẻ chỉ số mẫu.
- `BaoCao.xaml`: bố cục bộ lọc báo cáo, thẻ thống kê, biểu đồ/bảng mẫu.
- `PhieuHuongDan.xaml`: thông tin bệnh nhân, khoa/phòng, nội dung hướng dẫn và vùng xem trước.
- `NhatKy.xaml`: bảng nhật ký thao tác mẫu.
- `ChonRang.xaml`: cửa sổ chọn vị trí răng bằng các nút/toggle button.

Các file cũ `DashboardView.xaml`, `ReportsView.xaml`, `GuidanceView.xaml`, `AuditView.xaml`, `ScheduleView.xaml`, `RecordsView.xaml`, `EditorWindow.xaml` và `ToothWindow.xaml` không được dùng làm giao diện chính sau khi chuyển đổi. Chỉ xóa chúng sau khi các file mới đã build được và không còn tham chiếu.

## 5. Phân tách trách nhiệm

### Khung ứng dụng

`MainWindow.xaml` giữ layout tổng thể gồm sidebar, header, vùng nội dung và footer. Sidebar có một sự kiện chọn trang duy nhất để nạp UserControl vào `Workspace`; `TongQuan` được chọn mặc định khi mở cửa sổ.

### Style dùng chung

`Theme.xaml` giữ các brush và style chung như `Title`, `Subtitle`, `Card`, `Primary` và `NavigationItem`. Các trang riêng chỉ dùng lại style, không sao chép định nghĩa màu hoặc template nút vào từng file.

### Trang nghiệp vụ

Mỗi trang tự quyết định bố cục, nhãn, nhóm control và cột hiển thị. Trang không biết về `DataStore`, JSON, quyền người dùng hoặc dịch vụ bên ngoài.

### Code-behind

File `.xaml.cs` của các trang chỉ chứa khai báo `partial class` và constructor gọi `InitializeComponent()`. `MainWindow.xaml.cs` là ngoại lệ hạ tầng, chỉ chứa event chuyển sidebar và ánh xạ trang; không thêm service hoặc model dữ liệu.

## 6. Lộ trình chuyển đổi

1. Tạo 15 cặp file giao diện nghiệp vụ với tên đã thống nhất.
2. Tạo 5 giao diện hỗ trợ và đổi tên các cửa sổ hỗ trợ cần giữ lại.
3. Chuyển nội dung bố cục phù hợp từ các view hiện tại sang các file mới, đồng thời loại bỏ phần layout tổng quát phụ thuộc cấu hình.
4. Cập nhật `x:Class`, namespace và các resource reference theo tên mới.
5. Bảo đảm các view cũ không còn được tham chiếu trong project; chỉ sau đó mới xóa file cũ.
6. Cập nhật bộ kiểm tra XAML-only để kiểm tra đủ file, tên lớp, resource và việc không có event/binding động.

## 7. Kiểm thử và tiêu chí nghiệm thu

- `dotnet build BenhVienRangHamMat.sln -c Debug --no-restore -m:1` đạt 0 warning và 0 error.
- `dotnet build BenhVienRangHamMat.sln -c Release --no-restore -m:1` đạt 0 warning và 0 error.
- Bộ kiểm tra tĩnh xác nhận đủ 15 file nghiệp vụ và 5 file hỗ trợ.
- Tất cả file trang có cặp `.xaml`/`.xaml.cs` và `x:Class` khớp namespace/lớp.
- Không có event attribute, `Command`, `DataContext` hoặc binding dữ liệu động trong các trang mới.
- Mọi `StaticResource` được dùng đều được khai báo trong resource dictionary chung.
- Không còn tham chiếu hoạt động tới `RecordsView` hoặc `EditorWindow` từ các trang nghiệp vụ mới.

## 8. Ngoài phạm vi

- Điều hướng nghiệp vụ sâu hơn ngoài chuyển trang sidebar.
- Thêm/sửa/xóa, tìm kiếm, lọc, xuất CSV hoặc in PDF.
- Kết nối database, JSON, API hoặc AI.
- Kiểm tra dữ liệu, phân quyền và xác thực người dùng.
- Tạo dữ liệu mẫu động hoặc binding ViewModel.
