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

## Đợt P03: migration và hợp đồng dữ liệu

Kết quả cuối ngày 25/10/2026, sau một lượt rà soát độc lập và sửa bốn phát hiện về khóa/nguồn kế hoạch. Các phần P01/P02 ở trên là kết quả tại thời điểm tương ứng.

| Kiểm tra | Kết quả P03 |
| --- | --- |
| Build solution Debug | Exit 0; 0 lỗi, 48 cảnh báo hiện có |
| SchemaChecks | Exit 0; 45 entity / 363 cột / 0 lỗi; đúng 52 bảng vật lý, giữ 46 định nghĩa bảng cũ |
| ClinicalChecks trên bản nâng cấp | **65 PASS / 0 FAIL**; 35 ca P02 + 11 cấu hình + 19 hợp đồng/trigger mới |
| ClinicalChecks trên bản cài mới cuối | **65 PASS / 0 FAIL** |
| Migration contract | **30 PASS / 0 FAIL** trên bản nâng cấp và cài mới |
| ClinicalUiChecks | **8 PASS / 0 FAIL**, control WPF thật |
| FinanceChecks | **54** ca transaction đạt; nguồn giá/chứng từ/kho cũ giữ nguyên |
| SupplierChecks / MaintenanceChecks / ReportChecks | Exit 0; báo cáo 18 chuyên đề |
| Checks | Vẫn lỗi MaCa trùng trong KiemTraGiaoDien, mã -532462766 |
| PersistenceChecks | Vẫn CS1729 BUS_NHANVIEN thiếu constructor transaction tại PersistenceFixture.cs:49 |
| WpfSmoke | Vẫn CS1061 NhanSu thiếu TryReadPosition/TryReadEmployee tại NhapLieuChecks.cs |

SchemaChecks sửa cách phân biệt entity bảng có `[Table]` trực tiếp với DTO kế thừa/hiển thị; không bỏ kiểm tra cột, kiểu, nullable, backing field, constructor, khóa hoặc `[Write(false)]`. Bổ sung kiểm tra đầy đủ các cột/constraint baseline trong SQL mới. Vì vậy từ bốn lỗi baseline còn ba; chưa có cổng nghiệm thu toàn dự án đạt.

### Vòng đời SQL đã thực thi

- Backup bản thử P02 `RHM_Clinical_P03_Before_20261025.bak` bằng COPY_ONLY CHECKSUM và RESTORE VERIFYONLY thành công.
- Upgrade `RHM_ClinicalWorkflow_Test_20261025`: từ 46 lên 52 bảng; hash toàn bộ cột/dòng của cả 46 bảng cũ không đổi. Hash định nghĩa trigger/view baseline không đổi. Chạy upgrade lần hai không đổi schema/nhân bản đối tượng; verify đạt.
- Cài mới từ file độc lập cuối vào `RHM_ClinicalWorkflow_Test_FreshFinal_20261025`: 52 bảng, cả 65 ca SQL và 30 ca hợp đồng đạt. Chạy lại file cài mới bị chặn bởi lỗi 50001 do database đã có bảng.
- Bản restore riêng `RHM_ClinicalWorkflow_Test_Rollback_20261025`: upgrade hai lần → rollback → upgrade lại. Schema cũ, dữ liệu cũ và trigger/view khớp hash trước/sau. Có dữ liệu cột mới hoặc một dòng trong bảng mới thì rollback dừng đúng lỗi **52104**, giữ đủ schema; fixture không để lại dữ liệu.
- Rollback giữ transaction/khóa bảng xuyên suốt tiền kiểm đến DDL; không có cửa sổ ghi chen giữa kiểm tra rỗng và DROP.

### Bằng chứng RED → GREEN P03

1. Hợp đồng ban đầu 0/30 đạt; triển khai SQL/ET thành 30/30.
2. Thiếu DAL/BUS cấu hình gây 10 lỗi; sau triển khai và siết assertion từ chối đúng nguyên nhân: 11/11. Đã kiểm tra cấp mã, roundtrip nullable/decimal, actor audit, khóa tài khoản, quyền thu ngân, chữ ký giả và snapshot cũ.
3. Snapshot đơn và lần khám ban đầu bỏ sót cột mới; ca tái hiện thất bại, sau sửa projection và giữ giới hạn nội dung thu ngân thì đạt. Kiểm tra verify trigger bị tắt thất bại trước khi thêm guard, sau sửa đạt.
4. Rà soát P03 phát hiện ba lỗi Important và một lỗi thiếu phiên bản được nâng lên Important: kế hoạch đã duyệt về nháp; bước đã thực hiện chuyển kế hoạch khác BN; kế hoạch có sẵn gắn mẫu chưa duyệt; phiên bản mẫu null lọt CHECK do SQL UNKNOWN. Bộ mới **15 PASS / 4 FAIL → 19 PASS / 0 FAIL** sau sửa cả upgrade và SQL cài mới. Thêm ca tiến độ vẫn cập nhật khi mẫu nguồn đã ngừng áp dụng. Không còn phát hiện quan trọng chưa xử lý; không có Minor hoãn lại trong P03.

Đối chiếu bản nâng cấp sau toàn bộ kiểm tra: 10 BN, 10 hóa đơn, tổng 732.500,00; thu ròng 467.500,00; tồn tổng 4.014,000. Không còn BN/tài khoản fixture `CW`; không có dòng thanh toán cũ bị điền tiền đưa/thối giả. Database gốc không chạy migration; file Word giữ nguyên.

Log vòng cuối ở `obj/ClinicalWorkflowP03/`, gồm `final-results.json`, `upgrade.log`, `rollback.log`, `fresh.log`, `FreshClinical.log`, `FreshContract.log`, `review-fixes.log`, `data-final.log`; log chuẩn bị ở `obj/ClinicalP03Generator/`. Ảnh Supplier/Maintenance sinh lại đã trả về Git. Hướng dẫn chạy/các mode thực thi DDL nằm trong [HUONG_DAN_CHAY.md](HUONG_DAN_CHAY.md).

P03 chỉ hoàn tất schema và hợp đồng nền. Cấu hình lưu nháp, không seed nội dung điều trị/liều dùng; phê duyệt/áp dụng mẫu ở P05/P07. Thu đủ/chống lặp, bàn giao, các ca đồng thời và AT01–AT34 toàn luồng thuộc P04–P12, chưa được tuyên bố đạt.
