# Sửa lỗi lịch sử khám và lịch hẹn trong hồ sơ — 08/10/2026

## Nguyên nhân và thay đổi

Ảnh lỗi dừng tại PropertyPathWorker.CheckReadOnly của WPF với đối tượng PatientVisitRow. Mã lần khám trong HistoryGrid được hiển thị bằng Run.Text và binding `{Binding Id}`. Run.Text mặc định bind hai chiều, trong khi PatientVisitRow.Id chỉ có getter. Khi tab lịch sử được tạo, WPF kiểm tra quyền ghi của binding và ném InvalidOperationException.

Đã đổi binding Run.Text của mã lần khám sang Mode=OneWay. Hai Run hiển thị mã bệnh nhân/số điện thoại trong danh sách cũng đổi sang OneWay vì chỉ dùng để hiển thị. Không thêm setter giả vào model và không thay đổi quy tắc lưu dữ liệu.

LichKham vẫn là một view dùng chung. Khi được SetPatient để hiển thị lịch của bệnh nhân đang mở, nút Thêm bệnh nhân bị ẩn. Khi mở mục Lịch khám độc lập, nút này vẫn hiện. Form đặt hẹn trong hồ sơ tiếp tục khóa bệnh nhân đang chọn.

## Kiểm tra

- Kiểm tra nguồn trước sửa phát hiện 3 Run có binding hai chiều mặc định; sau sửa cả 3 đã qua kiểm tra.
- Kiểm tra đầy đủ 50 XML, resources, handler và đối chiếu trường của 17 bảng với DOCX: qua kiểm tra.
- Có thêm chương trình kiểm tra WPF thực tế `RangHamMat.WpfSmoke`, dùng view và dữ liệu minh họa thật của đồ án để thử: mở lịch sử, chọn lần khám đúng bệnh nhân, lịch trong hồ sơ ẩn Thêm bệnh nhân, lịch độc lập giữ nút.
- Chương trình kiểm tra WPF và build chưa chạy được trong môi trường sửa vì thiếu .NET/Windows (`dotnet: command not found`). Không coi kiểm tra XML là kết quả chạy WPF.

## Mở lại và thử trên Windows

Giải nén bản mới vào thư mục mới, mở BenhVienRangHamMat.sln, chọn RangHamMat.Wpf làm Startup Project rồi Rebuild/F5. Nạp mẫu, chọn Bệnh nhân minh họa 01, mở Lịch sử khám, chọn lần khám. Kiểm tra tiếp tab Lịch hẹn và mục Lịch khám độc lập.

Có thể chạy KiemTraWpf.bat để chạy kiểm tra tự động cho lỗi này. File này chạy chương trình kiểm tra riêng, không yêu cầu đăng nhập, không thay dữ liệu SQL và chỉ dùng dữ liệu trong tiến trình kiểm tra.
