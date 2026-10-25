# Kiểm chứng triển khai — 25/10/2026

**Kết quả mới nhất P02:** build đạt; **35 ca SQL và 8 ca WPF đạt**. Chi tiết đợt tích hợp nằm cuối tài liệu. Phần đợt nền dưới đây được giữ làm bằng chứng lịch sử.

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

## Giới hạn tại thời điểm kết thúc đợt nền

P02 chưa tích hợp đầy đủ cache theo phiên, quyền điều hướng, các màn hình và mọi đường ghi. Chưa có P03 migration/cài mới/rollback, P04–P11 UI và quy trình mới, hay P12 nghiệm thu AT01–AT34. Bốn suite lỗi sẵn cần được xử lý/đối chiếu trước khi có thể tuyên bố nghiệm thu toàn bộ; không thay fixture cũ hoặc bỏ assertion để làm xanh kết quả.

## Đợt tiếp theo: hoàn tất P02

Đã nối nguồn đọc SQL có phạm vi vào năm module lâm sàng, giới hạn nội dung từng vai trò, bảo vệ nguồn cha khi lưu/xóa/xác nhận, kiểm tra lại tài khoản và chức vụ trong cùng transaction. Đã tách quyền mở khỏi quyền sửa, xóa cache khi đổi phiên, kiểm tra lại liên kết khi điều hướng và giữ lịch sử chỉ đọc đúng phạm vi.

Các lệnh vòng cuối dùng `RHM_CONNECTION_STRING` của bản thử, `-c Debug --no-restore -p:NuGetAudit=false` (ClinicalUiChecks cũng đã chạy có build/restore khi thêm project):

| Kiểm tra | Kết quả P02 |
| --- | --- |
| Build solution | Exit 0, 0 lỗi |
| ClinicalChecks | Exit 0, **35 PASS / 0 FAIL**, fixture rollback |
| ClinicalUiChecks | Exit 0, **8 PASS / 0 FAIL**, không để lại BN/chứng từ thử |
| FinanceChecks | Exit 0, **54** kiểm tra transaction |
| SupplierChecks / MaintenanceChecks / ReportChecks | Exit 0, giữ kết quả baseline |
| Checks | Vẫn trùng MaCa, mã kết thúc -532462766 |
| SchemaChecks | Vẫn 39 bảng / 303 cột / 1 lỗi nhận diện ET/bảng |
| PersistenceChecks | Vẫn CS1729 BUS_NHANVIEN constructor |
| WpfSmoke | Vẫn CS1061 NhanSu.TryReadPosition/TryReadEmployee |

Log vòng cuối: `obj/ClinicalWorkflowP02/`. Lịch độc lập có một ca WPF riêng xác nhận đủ số lịch từ SQL và nút cũ vẫn hoạt động; chỉ nguồn lịch nhúng đổi theo phạm vi hồ sơ.

Rà soát độc lập P02 đã được xử lý trong một lượt sửa:

- Form điều dưỡng có số dạng chuỗi/parent nullable: chuẩn hóa để đối chiếu với giá trị SQL, vẫn chặn đổi lượng/giá/người làm/dịch vụ/trạng thái.
- Dòng giao điều dưỡng chỉ kèm định danh lượt; loại nội dung nháp khỏi projection, hàng đợi và lịch sử không mở các lượt nháp đó.
- Dữ liệu quầy thu giữ lượng/giá nhưng loại kết quả dịch vụ, ghi chú, hướng dẫn chuyên môn và nhóm máu/tiền sử/dị ứng không cần thu. Fixture có giá trị thật ở các trường thử để tránh ca kiểm tra đạt vì nguồn vốn null.
- Control lịch nhúng dùng nguồn có quyền trước khi lọc, thay cho reader toàn viện; lịch độc lập giữ nguyên đường chạy cũ.

Đã quan sát RED rồi GREEN cho cả bốn phát hiện: SQL từ 32 PASS/3 FAIL lên 35/35; WPF từ 6 PASS/1 FAIL lên 7/7; sau đó thêm ca lịch độc lập đạt 8/8. Gọi trực tiếp Upsert/SetValue qua cache ngoài quyền vẫn bị DAL từ chối và không đổi dữ liệu.

Phạm vi nghiệm thu ở đây là P02. Hóa đơn trong chế độ lâm sàng chỉ xem/in lại, chờ P09 cho thu đủ; migration P03, luồng tiếp nhận mới, khám, kế hoạch, kê đơn, bàn giao và nghiệm thu toàn bộ vẫn ở các tác vụ sau. Bốn suite lỗi sẵn chưa được coi là đạt.
