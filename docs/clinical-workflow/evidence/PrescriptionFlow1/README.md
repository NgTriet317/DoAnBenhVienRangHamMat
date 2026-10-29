# PrescriptionFlow1 — kiểm chứng29/10/2026

Luồng: Khám/tình trạng → kế hoạch đã duyệt → xác nhận đã thực hiện → kê thuốc và ký hoặc lưu lý do không kê → tổng kết dịch vụ thực tế/đơn hiệu lực → hóa đơn. Đơn thuốc bên ngoài chỉ tra cứu/in và bàn giao sau thu đủ. Một SQL chính,53 bảng,4 project sản phẩm.

| Kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Tái hiện trước sửa |3 lỗi yêu cầu mới;34 hồi quy cũ đạt | red.log |
| Cài mới và bản nguồn |41 đạt,0 lỗi | fresh-source-tests.log |
| Bản phát hành |41 đạt,0 lỗi | published-tests.log |
| Admin ở database sạch/đang dùng |2 nhóm đạt trên mỗi database, đủ26 phân hệ | fresh-ui.log, current-ui.log |
| Build và schema | Debug/Release/publish đạt;53 bảng, FK trusted và index nhóm thuốc | delivery.log, publish.log |
| Chạy lặp SQL chính | Không nhân nhóm/thuốc hoặc đổi giá | repeat.log |
| Database đang dùng | Backup/verify, giữ tất cả dòng/cột cũ của53 bảng | local-sync.log, preservation.json, backup-path.txt |
| Dữ liệu cuối |62 thuốc/20 nhóm/0 chưa phân nhóm; bản cài sạch52 thuốc,0 bệnh nhân/đơn/hóa đơn/lô kho giả | final-state.log |
| Dọn kiểm thử | Chỉ xóa hai database Rx/RxFinal do tác vụ tạo | cleanup.log |

Các nhóm nghiệp vụ kiểm trực tiếp WPF và DAL/BUS: cấm kê/ký trước xác nhận thực hiện; kế hoạch nháp/token cũ/bác sĩ khác bị từ chối; retry thực hiện không nhân dịch vụ; soạn/ký đơn ngay trong khám không dùng mẫu/phiên bản; Tổng kết đọc đúng dịch vụ và thuốc đã ký; hóa đơn đúng nguồn và giữ giá sau khi giá danh mục thay đổi; thuốc chưa thiết lập giá chặn hóa đơn/rollback lần hoàn tất; module Đơn thuốc không có form ghi và từ chối ngữ cảnh bệnh nhân/lượt lệch; chưa thu không giao, thu đủ có đúng nguồn bàn giao, giao lại không trừ kho thêm. Các kiểm hồi quy giá/kế hoạch/tình trạng/phân quyền/thu trùng/tách lô vẫn đạt.

Fixture dùng dữ liệu có nhãn FIXTURE, chỉ trong transaction rollback ở database thử riêng; không được đưa vào SQL cài bệnh viện. Handover GUI với transaction fixture chỉ kiểm quyền/nguồn/điều kiện, mutation bàn giao được kiểm bằng DAL trên Serializable rồi rollback; ứng dụng thật gọi BUS tự commit trước trả kết quả. Không thử thao tác khám/thu/giao thật trên database người dùng.

Ảnh `embedded-prescription.png`, `visit-summary.png`, `prescription-history.png` dựng từ WPF thật. `results.json` có hash SQL/DLL và bộ kiểm. Cảnh báo nullable có sẵn trong các màn cũ còn trong publish.log; build không lỗi. Rà soát độc lập chỉ đọc không tìm thấy lỗi cần chặn bàn giao.

Danh mục mới52 sản phẩm/20 nhóm:40 giá tham khảo công khai và12 chưa thiết lập; không tự sinh liều, hướng dẫn mẫu, người duyệt, tồn kho hay chứng từ.10 thuốc cũ được điền nhóm nếu NULL và khớp nhận diện; giá/các cột cũ giữ nguyên. Có thể nhập/chỉnh giá trong quản lý thuốc, nhập kho thực tế trước cấp/giao. Nguồn và đơn vị tại [DANH_MUC_THUOC_RHM](../../DANH_MUC_THUOC_RHM.md).

Chạy lại từ thư mục gốc:

```powershell
.\KiemTra.bat -KiemTraSql
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraQuyTrinhBenhVien.ps1 -TaoDatabaseKiemThu
dotnet publish RangHamMat.Wpf -c Release -o PhatHanh
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraQuyTrinhBenhVien.ps1 -BanPhatHanh
```

`-TaoDatabaseKiemThu` chỉ đổi tên database trong SQL chính sang tên thử, không chạy fragment/Migrations. Bộ kiểm ghi từ chối database không có tiền tố kiểm thử; `-ChiGiaoDien` chỉ đăng nhập/mở màn. Database thử đã được dọn; tạo lại bằng tham số trên nếu cần. Backup SQL Server đã verify được giữ lại; chỉ xóa thư mục tạm sau lưu bằng chứng.
