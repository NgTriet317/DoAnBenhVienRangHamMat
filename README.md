# Bệnh viện Răng Hàm Mặt — Giao diện Blue Care

## Chạy trong Visual Studio 2022

1. Giải nén toàn bộ dự án vào thư mục mới.
2. Cài workload **.NET desktop development** và .NET 8 SDK.
3. Mở `BenhVienRangHamMat.sln`, đặt `RangHamMat.Wpf` làm **Startup Project**.
4. Nhấn **F5**, bấm **Vào không gian làm việc** trên form đăng nhập mẫu.
5. Home xuất hiện **toàn màn hình**, không thanh tiêu đề, không nút thu nhỏ/phóng to/đóng, không kéo thay đổi kích thước.

## Điều hướng

- Home giữ 19 app và bộ chọn vai trò mô phỏng, tông xanh dương.
- Bấm app để mở nội dung **ngay trong cửa sổ chính**. Chỉ một app được hiển thị tại một thời điểm.
- **Màn hình chính** đưa về Home. App không mở cửa sổ riêng và không có controlbox.
- **Thêm / Tạo phiếu** mở form ngay trong vùng nội dung. Các form này cũng không có cửa sổ riêng/controlbox.
- **Quay lại danh sách** hoặc **Esc** đưa về danh sách vừa mở form. Dữ liệu nhập trong form không được lưu.
- **Về màn hình chính** trong form đi thẳng về Home.
- Liên kết giữa các app chuyển nội dung trong cùng cửa sổ.
- **Thoát** ở góc dưới bên phải Home đóng chương trình. Vẫn dùng được Alt+F4 / chuyển ứng dụng của Windows; đây không phải chế độ kiosk khóa hệ điều hành.
- Khi cửa sổ được khôi phục về trạng thái thường, nó tự trở lại toàn màn hình. Có thể chuyển sang ứng dụng Windows khác bình thường.

Màn hình đăng nhập vẫn là form mẫu như trước; chế độ toàn màn hình bắt đầu sau khi đăng nhập. Vai trò chỉ mô phỏng ẩn/hiện app, chưa có xác thực hay phân quyền dữ liệu.

## Tệp chính

| Tệp | Vai trò |
|---|---|
| `RangHamMat.Wpf/MainWindow.xaml` | Cửa sổ toàn màn hình chứa Home và AppHost |
| `RangHamMat.Wpf/MainWindow.xaml.cs` | Chuyển Home/app, mở form nhúng, khóa kích thước và thoát |
| `RangHamMat.Wpf/AppWorkspace.xaml` | Khung app nhúng có nút Home |
| `RangHamMat.Wpf/AppWorkspace.xaml.cs` | Chuyển trang/form và xử lý Đóng/Esc/Home |
| `RangHamMat.Wpf/Views/CuaSo*.xaml` | 16 form nhúng, gồm 15 form theo schema và 1 trang chờ bổ sung lương |
| `RangHamMat.Wpf/Themes/Theme.xaml` | Bảng màu xanh và style chung |

`AppWindow` của bản cửa sổ riêng đã được thay bằng `AppWorkspace`. `ChonRang` tiếp tục được giữ trong source và ẩn như bản gốc.

## Kiểm tra

Chạy `KiemTra.bat` trên Windows hoặc:

```powershell
dotnet build BenhVienRangHamMat.sln -c Release -m:1
dotnet run --project RangHamMat.Checks -c Release
```

Xem `KET_QUA_KIEM_TRA.md` để biết các kiểm tra đã chạy và phần cần xác nhận trực tiếp trên Windows.

Chỉ có giao diện và điều hướng; không bổ sung database, API, lưu dữ liệu hay chức năng nghiệp vụ. Khi về Home/chuyển app, nội dung nhập chưa lưu có thể bị bỏ. Gói chỉ chứa mã nguồn và tài liệu, không kèm bản chạy cũ.

## Đối chiếu database mới

- Đã chỉnh các trường nhập và cột danh sách theo **34 bảng / 245 thuộc tính** trong SQL đã gửi.
- Các bảng phụ được chia thành tab trong phân hệ tương ứng. Xem `DOI_CHIEU_DATABASE.md` để tra đầy đủ từng cột.
- SQL gốc nằm tại `Database/QLBenhVienRangHamMat.sql`, giữ nguyên; chưa chạy script hay kết nối database.
- Khóa tự sinh chỉ đọc; khóa ngoại là ComboBox chưa nạp dữ liệu; thời điểm có phần ngày và giờ; trạng thái là văn bản theo SQL.
- Mật khẩu dùng PasswordBox và không hiển thị trên bảng danh sách. Giá trị mật khẩu trong database phải là chuỗi băm khi triển khai chức năng.
- Lương/hoa hồng và nhật ký không có bảng tương ứng trong SQL: giữ app với thông báo chờ bổ sung, không dựng trường dữ liệu giả.
- Tổng quan là các lối tắt công việc; Báo cáo là danh sách dữ liệu nguồn, chưa tính số liệu tổng hợp. Các nút lưu nghiệp vụ được vô hiệu hóa; nút mở form, chuyển app, Home và Thoát hoạt động ở mức điều hướng.

## Giao diện mới

- Home có lời chào, nền sáng, 19 app với icon, ô tìm kiếm và bộ lọc nhóm. Tìm kiếm kết hợp nhóm và vai trò đang xem.
- Danh sách và form được tách riêng. Danh sách hiện các cột chính; **Hiện đủ cột / Thu gọn cột** đổi mức chi tiết.
- Bấm **Thêm** mở form tạo mới. Nhấp đúp vào dòng dữ liệu mở màn chi tiết chỉ đọc của chính dòng đó; các mục con được lọc theo mã bản ghi chính.
- Form gom các trường thành thông tin chính/cá nhân, sức khỏe/nội dung và thông tin quản lý. ID tự sinh chỉ đọc; đủ các trường database như bảng đối chiếu.
- Ô nhập cao tối thiểu 42 DIP, bảng có dòng 50 DIP, phần đầu cột 48 DIP. Tab có trạng thái đang chọn rõ ràng.
- Danh sách co theo chiều cao cửa sổ; form và Home cuộn dọc. Form đăng nhập thu vừa vùng làm việc khi DPI lớn.

`XemTruoc/index.html` giữ bản minh họa bố cục Home của lần thiết kế trước, được đánh dấu rõ phạm vi. Các thay đổi lịch/đồ thị/chi tiết trong lần này triển khai ở WPF; chạy solution để xem đầy đủ.

## Cập nhật lịch, đồ thị và chi tiết — 29/09/2026

- **Ca làm / Lịch khám:** chọn Bảng dữ liệu hoặc Lịch theo tuần. Có tuần trước/sau, hôm nay và chọn ngày. Lịch dùng cùng nguồn dữ liệu với bảng; ngày nằm trên đầu cột, giờ chạy dọc 00:00–24:00. Tiêu đề ngày giữ nguyên khi cuộn. Ca qua đêm tách sang ngày kế; lịch khám chỉ biểu thị giờ bắt đầu vì SQL không có giờ kết thúc.
- **Báo cáo:** chọn Bảng dữ liệu hoặc Đồ thị. Một đồ thị đường cho bệnh viện và một đồ thị cột ghép Thu kho / Chi kho. Giá trị do tầng dữ liệu cung cấp, chưa có tính toán nghiệp vụ.
- **Chi tiết:** double-click một dòng thật; bấm tiêu đề cột/vùng trắng không mở form. Màn chi tiết chỉ đọc và lọc bảng con theo khóa của dòng đó. Double-click bảng con để xem tiếp; Quay lại/Esc quay đúng cấp trước.
- **Thêm mới:** vẫn là thao tác riêng. Tại màn chi tiết bản ghi chính, nút thêm mục con mở form tạo mới và điền/khóa mã liên kết cha. Không mở chi tiết bằng nút Thêm.
- **Vật tư / Thuốc:** hai tab dùng chung VAT_TU, loại `Thuốc` lấy qua LOAI_VAT_TU; lô thuốc dùng LO_VAT_TU. Không thêm bảng/cột SQL. Tab Thuốc có nút Thêm thuốc, loại Thuốc được chọn sẵn.
- **Nhật ký:** bộ lọc chỉ gồm Đăng nhập, Đăng xuất, Phân quyền, Thay đổi thông tin. Chưa tự ghi nhật ký; SQL gốc chưa có bảng nhật ký.
- **Thanh toán:** nguồn Doanh thu bệnh viện tách khỏi Thu–chi kho. Kho có tab Thu kho và Chi kho, không gộp vào doanh thu bệnh viện. Không tự suy ra thu/chi từ số lượng nhập/xuất.

Chưa kết nối database nên các bảng, lịch và đồ thị bắt đầu ở trạng thái trống. Không chèn dữ liệu bệnh nhân hoặc doanh thu giả. Double-click cần một dòng dữ liệu được nạp; hướng dẫn nối nguồn và kiểm thử nằm trong `HUONG_DAN_NGUON_DU_LIEU.md`.
