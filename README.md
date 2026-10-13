# Đồ án Bệnh viện Răng Hàm Mặt

Đọc [giải thích code bằng tiếng Việt](GIAI_THICH_CODE_TIENG_VIET.md) để tra chức năng từng file, cách giao diện gọi xử lý/lưu dữ liệu và phạm vi hỗ trợ AI. Chú thích chi tiết nằm ngay trên các lớp, hàm, thuộc tính C# và các vùng XAML liên quan.

Đọc [phần AI đã thực hiện trong giao diện](PHAN_AI_THUC_HIEN_GIAO_DIEN.md) để biết AI đã tạo, chỉnh sửa hoặc bổ sung chú thích gì trong từng file XAML/C# của giao diện.

Đọc [kiểm tra nhập liệu](KIEM_TRA_NHAP_LIEU.md) để xem các trường hợp đã chặn, thông báo lỗi, kết quả kiểm thử và phạm vi các form mẫu chưa nối lưu SQL.

Bản chú thích mở rộng đã bao gồm toàn bộ BUS/DAL. Biểu đồ, lịch tuần, chi tiết chung và bản in hóa đơn có file XAML hiện hữu; code-behind chỉ xử lý dữ liệu/sự kiện và phân trang bản in. Xem [kết quả rà toàn bộ code và XAML](KET_QUA_TOAN_BO_CODE_XAML.md).

Bản cập nhật 11/10/2026 thực hiện yêu cầu trong `RangHamMat.Wpf/KeHoach.txt`. Hồ sơ bệnh nhân, lịch khám/tiếp nhận, khám điều trị, đơn thuốc, dịch vụ thực hiện, hóa đơn và thanh toán đọc/lưu SQL Server qua ET → DAL → BUS → WPF. Đã bỏ chức năng nạp dữ liệu minh họa. Các thao tác lưu tiếp tục tồn tại khi đóng và mở lại ứng dụng.

Schema hiện tại là **v8**: `Database/DB_Nhom4_v8.docx` và `Database/QLBenhVienRangHamMat.sql`. SQL trong workspace khớp database đang dùng; các tài liệu/JSON mang tên v7 chỉ còn mô tả bản giao diện trước.

Bản sửa 13/10/2026 có đủ 39 bộ ET–DAL–BUS cùng hậu tố, khớp 303 cột SQL. Xử lý liên kết nằm trong các DAL/BUS của từng thực thể; giao dịch nhiều dòng dùng chung IDbTransaction. Giao diện tiếp nhận và chi tiết đổi thành TiepNhanKham và ChiTietHoSo. Khóa chính/khóa ngoại là chuỗi theo SQL hiện tại. Mã tự cấp theo chữ đầu tên bảng và số tối thiểu hai chữ số: BN01, HSBA01, LSKB01; tiếp tục số lớn nhất đang có và giữ mã cũ. Xem [báo cáo sửa DAL/BUS/ET](CAP_NHAT_DAL_BUS_ET.md) để tra cấu trúc và kết quả kiểm tra mới nhất.

## Chạy ứng dụng

1. Mở `BenhVienRangHamMat.sln` bằng Visual Studio 2022 có .NET desktop development và .NET 8 SDK.
2. Chọn `RangHamMat.Wpf` làm Startup Project rồi F5.
3. Kết nối mặc định: SQL Server `DESKTOP-C9VCE8A`, database `QLBenhVienRangHamMat`, Windows Authentication. Máy hiện tại đã có database này.
4. Nếu dùng máy/server khác, đặt biến môi trường `RHM_CONNECTION_STRING` trước khi mở Visual Studio hoặc chạy ứng dụng:

```powershell
$env:RHM_CONNECTION_STRING = 'Data Source=TEN_SERVER;Initial Catalog=QLBenhVienRangHamMat;Integrated Security=True;TrustServerCertificate=True;'
dotnet run --project RangHamMat.Wpf -c Release
```

Không chạy lại script khởi tạo trên database đang có dữ liệu. Database cần schema v8 và danh mục nhân viên, phòng, dịch vụ, thuốc, kho/lô thực tế. Nút **Tải lại dữ liệu** đọc lại SQL. Đăng nhập/chọn vai trò giữ cách hoạt động của source gốc; chọn vai trò Quản trị để thử toàn bộ luồng liên phân hệ. Trong Hóa đơn cần chọn rõ nhân viên thực hiện.

## Luồng sử dụng

**Bệnh nhân → Bệnh án → Tiếp nhận → Lần khám → Kế hoạch/dịch vụ/đơn thuốc → Cấp thuốc → Hóa đơn → Thanh toán → In.**

Các liên kết giữ đúng bệnh nhân và lần khám nguồn. Form bệnh nhân có BHYT, số điện thoại dạng chuỗi, ngày sinh có thể để trống. Xác nhận lần khám lưu đồng thời ngày xác nhận. Bản xác nhận được khóa; thao tác nhiều dòng lưu trong một giao dịch, lỗi thì rollback. Form đã cũ bị chặn ghi đè dữ liệu mới.

Hóa đơn chỉ lấy dịch vụ đã xác nhận và thuốc đã xuất ghi sổ của đúng lần khám. Cấp thuốc chọn một kho đủ toàn bộ đơn, theo FIFO lô còn hạn. Một nguồn thu chỉ vào một hóa đơn; không tính lại khoản đã lập. Thu tiền không vượt số còn phải thu; hoàn không vượt tiền đã thu ròng và phải có lý do. Hóa đơn dài được phân trang; **In / Xuất PDF…** chọn máy in hoặc Microsoft Print to PDF.

Đọc [hướng dẫn thao tác](HUONG_DAN_GIAO_DIEN_BENH_VIEN.md) và [kết quả thực hiện kế hoạch](THUC_HIEN_KE_HOACH.md).

## Kiểm tra

- `KiemTra.bat`: build Release, kiểm tra XAML/luồng và đối chiếu ET với SQL.
- `KiemTraDatabase.bat`: kiểm tra persistence, nghiệp vụ tài chính và WPF trên SQL thật; mọi bản ghi thử được rollback.
- `KiemTraWpf.bat`: riêng kiểm tra WPF; cần Windows và database đang chạy.

Build và các kiểm tra đã chạy trên máy hiện tại. Xem lệnh, số kiểm tra, ảnh giao diện và giới hạn kiểm chứng trong `THUC_HIEN_KE_HOACH.md`. Các phân hệ ngoài phạm vi KeHoach.txt giữ source hiện có.
