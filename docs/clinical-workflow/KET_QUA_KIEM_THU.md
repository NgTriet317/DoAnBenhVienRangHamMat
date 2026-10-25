# Kiểm chứng đợt nền đầu tiên — 25/10/2026

Phạm vi: P01 và nền backend quyền P02, chưa phải nghiệm thu sáu chức năng/P12. Kết quả trước sửa tại [BASELINE.md](BASELINE.md); việc còn lại tại [TIEN_DO.md](TIEN_DO.md).

## Vòng cuối sau thay đổi

Chạy từ gốc repo trên Windows, connection trỏ `RHM_ClinicalWorkflow_Test_20261025`. Các lệnh `dotnet run` dùng `-c Debug --no-restore -p:NuGetAudit=false`; Checks thêm `-- .`.

| Lệnh / project | Kết quả cuối | So với baseline |
| --- | --- | --- |
| `dotnet build BenhVienRangHamMat.sln -c Debug --no-restore -p:NuGetAudit=false` | Exit 0, 0 lỗi, 48 cảnh báo ở các file cũ trong lượt build tăng dần | Build vẫn đạt; không coi giảm số cảnh báo do incremental build là sửa lỗi |
| `RangHamMat.ClinicalChecks` | Exit 0, **26 PASS / 0 FAIL** trên SQL thật | Bộ kiểm tra mới |
| `RangHamMat.FinanceChecks` | Exit 0, **54** kiểm tra transaction, rollback | Giữ kết quả đạt |
| `RangHamMat.SupplierChecks` | Exit 0, SQL/thao tác WPF/validation, rollback | Giữ kết quả đạt |
| `RangHamMat.MaintenanceChecks` | Exit 0, SQL/thao tác WPF/validation/lịch sử, rollback | Giữ kết quả đạt |
| `RangHamMat.ReportChecks` | Exit 0, 18 chuyên đề / 4 nhóm (chạy mặc định) | Giữ kết quả đạt |
| `RangHamMat.Checks` | Thất bại, mã kết thúc -532462766; trùng khóa MaCa trong KiemTraGiaoDien | Cùng lỗi sẵn ở baseline |
| `RangHamMat.SchemaChecks` | Exit 1, 39 bảng / 303 cột / 1 lỗi ET không khớp bảng | Cùng lỗi sẵn ở baseline |
| `RangHamMat.PersistenceChecks` | Exit 1 khi build, CS1729 BUS_NHANVIEN constructor transaction | Cùng lỗi sẵn ở baseline |
| `RangHamMat.WpfSmoke` | Exit 1 khi build, CS1061 NhanSu.TryReadPosition/TryReadEmployee | Cùng lỗi sẵn; **chưa chạy được smoke WPF toàn luồng** |

Log vòng cuối và `results.json` tại `obj/ClinicalWorkflowVerification/` trên máy. `git diff --check` đạt. Ảnh do test Supplier/Maintenance sinh được trả lại bản Git để giữ phạm vi.

## Bằng chứng test thất bại trước khi triển khai

- Đã tạo fixture rollback và API skeleton, chạy lần đầu: 0 PASS / 22 FAIL vì chưa có nguồn quyền SQL. Sau triển khai mapping và truy vấn: 22 PASS / 0 FAIL.
- Rà soát độc lập phát hiện thiếu ràng buộc toàn bộ định danh với phiên. Thêm bốn ca: 22 PASS / 4 FAIL trên code trước sửa; thêm principal từ đăng nhập và kiểm tra ở DAL: **26 PASS / 0 FAIL**.
- Các ca từ chối quyền chỉ nhận `TuChoiQuyenNghiepVuException`; lỗi SQL/connection bọc trong InvalidOperationException không được tính là PASS.

Các tình huống đã kiểm tra gồm quyền tiếp nhận/điều dưỡng/thu ngân/dược sĩ; Admin không ký thay bác sĩ; SQL hàng đợi chỉ của bác sĩ phiên dù bỏ lọc; giới hạn trang và từ khóa tham số; MaLS/MaDT trực tiếp của bác sĩ khác; lịch sử đã ký cùng BN; hai BN trùng tên; guard ghi không cho lượt bác sĩ khác hoặc lượt đã ký; khóa tài khoản/ngừng việc/đổi chức vụ sau mở form; cờ quyền và toàn bộ định danh giả; đổi phiên/thiếu phiên; mã không tồn tại; nhật ký tra cứu Admin không chép nội dung bệnh án.

Guard ghi mới được kiểm tra trong transaction của fixture, **chưa thay các lệnh ghi UI cũ bằng API có quyền**. Các kết quả này không thay cho ca lưu UI, thanh toán đồng thời hoặc nghiệm thu P02 hoàn chỉnh.

## Đối chiếu dữ liệu sau chạy

Truy vấn chỉ đọc sau vòng cuối cho kết quả giống nhau giữa bản thử và database gốc:

| Giá trị | Bản thử | Database gốc |
| --- | ---: | ---: |
| Bệnh nhân | 10 | 10 |
| Hóa đơn | 10 | 10 |
| Tổng hóa đơn | 732.500,00 | 732.500,00 |
| Thu ròng đã xác nhận | 467.500,00 | 467.500,00 |
| Tổng lượng tồn kỹ thuật | 4.014,000 | 4.014,000 |

Không còn BN/tài khoản mang tiền tố fixture `CW` trong bản thử. Đối chiếu trên là kiểm tra tổng hợp và cleanup, không thay thế ma trận bảo toàn toàn bộ dữ liệu khi làm migration P03. P01 chỉ backup/restore và đọc database gốc; các ca ghi chạy ở bản thử.

## Giới hạn còn mở

P02 chưa tích hợp đầy đủ cache theo phiên, quyền điều hướng, các màn hình và mọi đường ghi. Chưa có P03 migration/cài mới/rollback, P04–P11 UI và quy trình mới, hay P12 nghiệm thu AT01–AT34. Bốn suite lỗi sẵn cần được xử lý/đối chiếu trước khi có thể tuyên bố nghiệm thu toàn bộ; không thay fixture cũ hoặc bỏ assertion để làm xanh kết quả.
