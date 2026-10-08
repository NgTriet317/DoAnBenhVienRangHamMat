# Kết quả kiểm tra phần giao diện bổ sung DB v7

## Đã kiểm tra tĩnh trong môi trường hiện tại

- 47 file XAML đọc/parse XML hợp lệ.
- Home có 25 launcher duy nhất: 19 launcher gốc + 6 launcher mới.
- Cả 25 launcher đều có route tương ứng trong `MainWindow.xaml.cs` và file view tồn tại.
- 6 phân hệ mới đều có đủ cặp file `.xaml` + `.xaml.cs`.
- Các cột dữ liệu của các bảng mới được thể hiện trong giao diện mới theo `DB_Nhom4_v7.docx`.
- Toàn bộ file trong thư mục `RangHamMat.Wpf/Views` vốn có ở project gốc được so sánh SHA-256 và **không thay đổi**; chỉ có 6 view mới được thêm vào.
- `Database/ui-table-map.json` và `Database/QLBenhVienRangHamMat.sql` cũ được giữ nguyên để tránh phá các kiểm tra/schema cũ. Phần mới dùng `Database/ui-table-map-dbv7-new.json`.

## Chưa thể kiểm tra trong môi trường hiện tại

Môi trường xử lý hiện tại không có `dotnet`/MSBuild/WPF runtime nên chưa thể thực hiện WPF XAML compile và chạy cửa sổ thật.

Trên Windows + Visual Studio 2022, hãy chạy:

```powershell
dotnet build BenhVienRangHamMat.sln -c Release -m:1
dotnet run --project RangHamMat.Checks -c Release
```

Sau đó mở ứng dụng và kiểm tra 6 tile mới ở các nhóm Khám chữa bệnh, Nhân sự, Kho và thiết bị, Tài chính.
