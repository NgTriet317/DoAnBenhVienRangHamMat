# Kết quả kiểm tra — giao diện bệnh viện 08/10/2026

## Kiểm tra đã chạy

- Parse 50 tài liệu XAML thành XML, kiểm tra tên trong name scope.
- Kiểm tra StaticResource, handler sự kiện và đường dẫn form/view của bốn mục cùng các control chi tiết dùng chung.
- Kiểm tra trường DB được dùng trên form và cột bảng; đối chiếu độc lập manifest 17 bảng với `DB_Nhom4_v7(3).docx`: tên trường khớp.
- Kiểm tra không dựng Grid/Border/TextBlock/TextBox/Button/DataGrid/StackPanel/TabItem/TabControl bằng C# trong các màn hình, form và control chi tiết thuộc phạm vi.
- Kiểm tra không còn chọn răng/số-vị trí răng/MaBHYT trong giao diện mới.
- Đối chiếu byte SQL cũ và các XAML ngoài phạm vi với ZIP gốc: giữ nguyên.
- Rà soát nguồn về khóa, phạm vi bệnh nhân, điều hướng đúng lần khám/đơn, bản xác nhận, bộ dữ liệu phiên và đăng ký/hủy sự kiện khi đóng view.
- Sau rà soát, bổ sung chặn đổi dịch vụ của bước đã có thực hiện, giữ liên kết cha khi sửa, tính trạng thái bước theo số lượng hoàn thành, tiếp tục kế hoạch của cùng bệnh nhân ở lần khám khác và giữ nháp khám chung khi lưu đơn con.

Lệnh kiểm tra nguồn `python scripts/verify_clinical_ui.py` trả về exit 0, 50 XML, 4 mục/10 bảng chính và 17 bảng manifest. Đây là kiểm tra nguồn, không chứng minh WPF biên dịch hoặc hiển thị đúng.

## Chưa chạy

Lệnh `dotnet build work/Nhom4_DoAnBenhVienRHM/BenhVienRangHamMat.sln -c Release -m:1` trả về `dotnet: command not found`, exit 127. Không có .NET SDK/Windows WPF runtime nên chưa chạy biên dịch, checker C#, kịch bản tương tác, ảnh chụp WPF, DPI hoặc máy in.

Checker C# có kiểm tra XML/trường/resources/handler và quy tắc số lượng thuốc, ngày dùng, tiến độ bước kế hoạch. Các kiểm tra C# được viết nhưng chưa chạy trong môi trường sửa. Chạy `KiemTra.bat` trên Windows và nghiệm thu các kịch bản trong `HUONG_DAN_GIAO_DIEN_BENH_VIEN.md`.

## Giới hạn còn lại

- Thay đổi giữ trong phiên, chưa lưu SQL; đăng nhập và vai trò mô phỏng.
- Danh mục/module gốc chưa tự đồng bộ sang nguồn dữ liệu của bốn màn hình cập nhật; cần nối DAL/BLL sau.
- Cảnh báo lịch chồng giờ dùng lịch của phiên, chưa kiểm tra ca làm việc/ngày nghỉ từ backend.
- Bản in cần kiểm tra thực tế với đơn dài; chưa có kiểm tra DPI hoặc thiết bị in.
- Các tài liệu/HTML cũ ngoài phạm vi là tài liệu lịch sử, không mô tả hoặc chụp giao diện mới.

## Bổ sung sau phản hồi về lỗi lịch sử

Ảnh lỗi chỉ ra binding hai chiều vào thuộc tính chỉ đọc PatientVisitRow. Kiểm tra nguồn trước sửa phát hiện Run.Text của Id, MaBN, SDT không chỉ định chiều đọc; sau sửa OneWay, kiểm tra nguồn đầy đủ qua. Lịch trong hồ sơ ẩn nút Thêm bệnh nhân thông qua phạm vi SetPatient. Đã bổ sung bộ kiểm tra WPF dùng view/dữ liệu mẫu thật nhưng chưa chạy được: dotnet run --project RangHamMat.WpfSmoke và dotnet build đều bị chặn bởi SDK không có, exit 127.

## Tinh chỉnh thẩm mỹ

Đã kiểm tra source/XAML sau tinh chỉnh: resources, handlers, routes, các binding Run một chiều, tên trường DB, template part của TextBox/ComboBox, thứ tự setter/trigger và các TargetName của template. Tính tương phản của các cặp màu chữ/nền chính và nhãn trạng thái đạt ≥4.5:1. Source C# xử lý bệnh nhân/lịch sử/lưu không đổi trong bước này. Build tiếp tục không chạy được vì thiếu dotnet (exit 127); chưa có ảnh render hoặc kiểm tra kích thước/DPI WPF sau tinh chỉnh. Ảnh người dùng gửi là bằng chứng giao diện trước tinh chỉnh, không phải ảnh nghiệm thu bản này.


## Ghép vào Git — 11/10/2026

Nguồn Git: `NgTriet317/DoAnBenhVienRangHamMat`, `master`, commit `5f195fc42e65afdab3cb3dfb5e7cc3bc929f535b`.

- Kiểm tra XML, resource, sự kiện, điều hướng, XAML tĩnh và đối chiếu trường với DOCX: qua, 56 tài liệu XML, 4 phân hệ / 10 bảng chính, 17 bảng trong manifest.
- 118 tệp cần giữ của Git giống byte gốc: BUS/DAL/ET, solution/project references, Home, SQL cũ và giao diện ngoài phạm vi.
- Giữ 25 lối mở ứng dụng; giữ luồng chi tiết nhân viên của Git và thêm luồng chi tiết lâm sàng. Không còn dấu xung đột merge; `git diff --check HEAD` qua.
- Màn hình/nguồn xử lý lâm sàng giữ giao diện mới và hai thay đổi lịch sử khám/lịch hẹn đã sửa.
- Patch kiểm tra và áp dụng lại trên bản Git sạch; nội dung kết quả được đối chiếu với bản đã ghép.
- Chưa biên dịch hoặc chạy Windows WPF vì môi trường không có .NET SDK/Windows. Chưa nghiệm thu hiển thị, DPI, kết nối nhân sự, máy in. Bốn màn hình lâm sàng vẫn dùng dữ liệu trong phiên.
