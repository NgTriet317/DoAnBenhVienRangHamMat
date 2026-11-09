# Cửa sổ kê thuốc và chốt hóa đơn —29/10/2026

| Kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Trước sửa |41 hồi quy đạt,2 yêu cầu mới chưa đạt | red.log |
| Bản nguồn |47 đạt,0 lỗi | source-tests.log |
| DLL bản phát hành |47 đạt,0 lỗi | published-tests.log |
| Admin trên database đang dùng |2 nhóm đạt, mở đủ26 phân hệ | current-ui.log |
| Debug/Release/publish/schema | Đạt; vẫn4 project/53 bảng | delivery.log, publish.log |
| Dọn fixture | Chỉ xóa RxDialog do tác vụ tạo | cleanup.log |

Các kiểm mới sử dụng WPF thật: mở cửa sổ, lọc nhóm/chọn thuốc, nhập lượng/ngày/liều/đường dùng/hướng dẫn trong cùng cửa sổ, bấm Xác nhận đưa nháp về Kê thuốc, mở sửa lượng rồi Chốt để ký đơn và lập hóa đơn. Kiểm riêng dữ liệu sai không mất khi xác nhận/đóng bị hủy, không hỏi hai lần, bỏ nhập không tạo đơn, bỏ cờ Không kê thuốc có thể lưu và kê lại. Tổng kết cũ bị từ chối; lỗi hóa đơn rollback cả chữ ký; thử lại giữ cùng hóa đơn; đơn nguồn và nháp thay thế không bị cộng đôi. Các hồi quy thu đủ/bàn giao/giá/tồn/phân quyền vẫn đạt.

Nháp được lưu trong SQL để còn mở sửa. Mục Đơn thuốc bên ngoài vẫn chỉ thấy bản đã ký và bàn giao theo thu đủ. Chốt lấy khóa cùng thứ tự với đầu mối kê đơn, kiểm fingerprint các nguồn đã xem, ký và lập hóa đơn trong savepoint của cùng transaction. Đơn đã ký từ phiên cũ giữ tính bất biến và thay bằng đơn có nguồn hợp lệ.

Tất cả DML kiểm thử ở database `RHM_Hospital_Test_RxDialog_20261029`, trong transaction rollback. Không chốt khám/thu/giao hoặc thay schema/dữ liệu trên database người dùng; kiểm schema và UI Admin chỉ đọc. SQL chuẩn không thay đổi, vẫn dùng PrescriptionFlow1. Một lỗi binding Run.Text giá chỉ đọc trong bố cục thử đã được phát hiện, sửa Mode=OneWay và kiểm lại47/47 trước bàn giao. Rà soát độc lập không có phát hiện cần sửa thêm.

Ảnh cửa sổ, danh sách trong khám và tổng kết lấy từ giao diện WPF chạy fixture có nhãn FIXTURE. Hash DLL/SQL/bộ kiểm ở results.json; DLL đóng gói khớp Release. Publish có các cảnh báo nullable sẵn có, không có lỗi build.

Chạy lại:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraQuyTrinhBenhVien.ps1 -TaoDatabaseKiemThu
dotnet publish RangHamMat.Wpf -c Release -o PhatHanh
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraQuyTrinhBenhVien.ps1 -BanPhatHanh
.\KiemTra.bat -KiemTraSql
```

Database fixture đã được dọn. Không cần chạy SQL bổ sung để dùng cửa sổ mới nếu database đã có PrescriptionFlow1. Xem [hướng dẫn cửa sổ](../../CUA_SO_KE_THUOC.md).
