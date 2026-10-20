# Rà soát giao diện WPF — tám phân hệ

Đã kiểm tra bằng cửa sổ WPF thực tế, dữ liệu SQL chỉ đọc, ở 1024×768, 1366×768 và 1920×1080 (96 DPI). Ảnh nằm trong `XemTruoc/ra-soat-giao-dien`; ảnh báo cáo có dữ liệu nằm trong `XemTruoc/bao-cao-thong-ke`.

| Phân hệ | Kết quả / điều chỉnh |
| --- | --- |
| Báo cáo và thống kê | Kiểm tra lại 18 chuyên đề, ngày/tuần/tháng và bảng đối chiếu; điều chỉnh margin theo padding ô chung, giữ tiêu đề và giá trị thẳng hàng. |
| Lịch sử bảo trì | Căn phải đồng nhất tiêu đề/chi phí; giữ độ rộng tối thiểu cho mã, ngày, chi phí, trạng thái. |
| Nhà cung cấp | Kiểm tra danh sách, thông tin liên hệ, chi tiết và biểu mẫu; dùng template ô chung để giữ khoảng đệm nhất quán. |
| Bệnh nhân | Sửa padding danh sách bệnh nhân; kiểm tra hồ sơ, các tab và biểu mẫu thêm bệnh nhân. |
| Khám điều trị | Bộ lọc giữ độ rộng đọc được; gọn phần đầu chi tiết để tăng chỗ đọc nội dung; bảng cuộn ngang thay vì ép mất cột; nới cột chứa nút thao tác. |
| Dịch vụ thực hiện | Chia không gian giữa bảng và chi tiết, tránh bảng chỉ còn tiêu đề; giữ hàng nút thao tác bên dưới vùng cuộn; căn cột số/tiền. |
| Hóa đơn và thanh toán | Giữ độ rộng tối thiểu, cuộn ngang cho bảng khoản thu/giao dịch/cấp thuốc/tồn kho; căn phải số lượng/tiền; hiển thị số lượng gọn theo tiếng Việt. |
| Đơn thuốc | Giữ độ rộng cột thuốc, liều, số lượng, đơn vị, số ngày, hướng dẫn; hỗ trợ cuộn ngang. |

Kiểu dùng chung trong `HospitalStyles.xaml` sửa khoảng đệm bị áp hai lần trong ô nhập, áp padding thực sự cho ô bảng, thêm kiểu chữ bảng/cột số và gợi ý ngày `dd/MM/yyyy`. `TreatmentStyles.xaml` dùng cùng khoảng đệm cho tiêu đề và nội dung, tăng chiều cao hàng để chứa ô nhập khi chỉnh sửa. Các bố cục và handler nghiệp vụ hiện có được giữ lại.

## Kiểm tra lại

```powershell
dotnet build RangHamMat.ReportChecks -c Release --no-restore -v quiet -p:WarningLevel=0
dotnet run --project RangHamMat.ReportChecks -c Release --no-build --no-restore -- --ui-audit
dotnet run --project RangHamMat.ReportChecks -c Release --no-restore -v quiet -p:WarningLevel=0 -- --render --sql
```

`--ui-audit` chọn bản ghi có chi tiết, chuyển tab, đổi kích thước, tìm không có kết quả và render mười biến thể biểu mẫu. Kiểm tra tự động đo căn tiêu đề/giá trị, cột bị co quá nhỏ, bảng dịch vụ bị ép mất và nút thao tác xuống dòng vượt hàng. Không gọi thao tác lưu, xác nhận, thanh toán hay cấp thuốc. Bộ kiểm tra báo cáo đối chiếu dữ liệu bằng SELECT và kiểm tra lại căn cột bảng đối chiếu.

Phạm vi xác nhận là giao diện trên dữ liệu hiện có và các kích thước trên; không thay thế kiểm thử toàn bộ nghiệp vụ ghi dữ liệu hoặc mọi mức Windows scaling.
