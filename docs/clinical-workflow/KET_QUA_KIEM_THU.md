# Kiểm chứng triển khai — 25/10/2026

**Kết quả mới nhất P05:** build/schema đạt; **104 ca SQL và 23 ca WPF đạt**, hai mode đồng thời đạt. Ba lỗi suite baseline còn nguyên. Chi tiết từng đợt nằm cuối tài liệu; phần đợt nền dưới đây được giữ làm bằng chứng lịch sử.

Phạm vi đã triển khai: P01–P05, chưa phải nghiệm thu sáu chức năng/P12. Kết quả trước sửa tại [BASELINE.md](BASELINE.md); việc còn lại tại [TIEN_DO.md](TIEN_DO.md).

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

## Đợt P04: hồ sơ và tiếp nhận

Kết quả sau rà soát độc lập và một lượt sửa, ngày 25/10/2026. Không đổi schema v2 hoặc áp SQL lên database gốc.

| Kiểm tra | Kết quả P04 |
| --- | --- |
| Build solution Debug | Exit 0; 0 lỗi, 48 cảnh báo hiện có |
| SchemaChecks | Exit 0; 45 entity / 363 cột / 0 lỗi |
| ClinicalChecks | **80 PASS / 0 FAIL**; 65 ca trước + 15 ca tiếp nhận, fixture rollback |
| Tiếp nhận đồng thời | Hai kết nối thật: hai BN cùng ngày/phòng có hai số liên tiếp; cùng lịch hẹn trả cùng MaPhieu; ba BN trùng tên giữ ba mã/ba hồ sơ; ReadCommitted giữ khóa lịch đến kết thúc; cleanup đạt |
| ClinicalUiChecks | **14 PASS / 0 FAIL**; gồm lưu form thật, cache sau commit và thử 1.000 BN/10.000 lượt trong bộ nhớ |
| FinanceChecks | **54** ca transaction đạt |
| SupplierChecks / MaintenanceChecks / ReportChecks | Exit 0; báo cáo 18 chuyên đề |
| Checks | Lỗi sẵn trùng MaCa, mã -532462766 |
| PersistenceChecks | Lỗi sẵn CS1729 BUS_NHANVIEN constructor transaction |
| WpfSmoke | Lỗi sẵn CS1061 NhanSu.TryReadPosition/TryReadEmployee |

Các ca P04 bao gồm bác sĩ đúng/sai chuyên khoa, tài khoản khóa hoặc đổi chức vụ sau mở form, dịch vụ/bác sĩ bắt buộc, lịch vắng, nghỉ đã duyệt, thiếu lịch phải có quyền/lý do điều phối và nhật ký. Đã kiểm tra dùng hồ sơ cũ, lập một hồ sơ khi chưa có, retry trực tiếp, retry cùng lịch, lịch không chuyển sang người trùng tên khác, không sinh MaLS khi tiếp nhận và phiếu đến đúng hàng đợi bác sĩ. Guard form chung từ chối lập phiếu bỏ qua đầu mối mới.

Bằng chứng trước/sau:

- Backend ban đầu **0 PASS/10 FAIL** do API chưa có; triển khai thành **10/10**, mở rộng thành 15 ca P04 và bộ tổng 80/80.
- WPF ban đầu **8 PASS/2 FAIL**: thiếu chọn bác sĩ theo dịch vụ và lịch sử hẹn bị giấu theo quyền lập lịch; sau tích hợp đạt 10/10. Bổ sung tìm ngày sinh và lưu qua form thật thành 12/12.
- Rà soát phát hiện giữ khóa kiểm tra phụ thuộc isolation ngoài: ca hai kết nối với ReadCommitted đã cho đổi lịch sau kiểm tra, trước commit (FAIL). Thêm HOLDLOCK cho dịch vụ, chuyên khoa, bác sĩ/tài khoản, ca/lịch, nghỉ và phòng; ca này chuyển PASS.
- Hai phát hiện Minor được nâng lên Important theo ảnh hưởng: lịch sử chỉ đọc còn nút sửa/hủy và phép tính lần khám gần nhất bị quét lặp. Quan sát **12 PASS/2 FAIL**, 4.357 ms cho bước trang đầu. Sau khóa thao tác lịch sử và gom map theo mã một lần: **14/14**, **53 ms** ở lần cuối với 1.000 BN/10.000 lượt; có kiểm tra ngày gần nhất đúng từng BN. Không còn phát hiện quan trọng hoặc Minor hoãn lại trong P04.

Ca WPF lưu một người bệnh giả chưa có hồ sơ: chọn dịch vụ/bác sĩ có lịch, lưu thành công, thấy MaPhieu trong cache sau commit, không có MaLS và không có nút mở nhập chẩn đoán; bấm lại giữ biên nhận. Form giữ nhận diện BN/bác sĩ ở đầu và hành động ở cuối khi cuộn. Đã xem ảnh render trước/sau lưu với dữ liệu FIXTURE. Thử DPI, bàn phím và kích thước toàn ứng dụng còn thuộc P11/P12.

80 ca SQL mặc định rollback. Kiểm tra đồng thời và WPF dùng fixture commit để nhìn thấy từ nhiều kết nối, rồi dọn theo mã GUID trong finally. Đối chiếu cuối: **52 bảng; 10 BN, 10 hồ sơ, 10 phiếu tiếp nhận, 10 hóa đơn; tổng 732.500,00, thu ròng 467.500,00, tồn 4.014,000**. Không còn BN/nhân viên/tài khoản/lịch làm việc tiền tố CW. Không giữ dữ liệu hiệu năng trong cache sau kiểm tra.

Log tại `obj/ClinicalWorkflowP04/`: `results.json`, `Clinical.log`, `Concurrency.log`, `ClinicalUi-final.log`, `data-final.log` và log các suite; ảnh `tiep-nhan-truoc-luu.png` / `tiep-nhan-da-luu.png`. Log RED/đợt đầu tại `obj/ClinicalReceptionP04*.log`, `obj/ClinicalUiP04-*.log`. Ảnh hồi quy Supplier/Maintenance đã trả về Git.

P04 hoàn tất theo phạm vi tiếp nhận. P05–P12 chưa hoàn tất: chưa có đầu mối bắt đầu/lưu/hoàn tất khám mới, áp dụng/duyệt mẫu, ký đơn snapshot, thu đủ/chống lặp, bàn giao và nghiệm thu AT01–AT34 toàn luồng. Các API legacy và việc tải snapshot lớn vẫn có giới hạn đã ghi ở tiến độ; không coi phép đo projection 53 ms là nghiệm thu tải SQL toàn hệ thống.

## Đợt P05: khám và kế hoạch điều trị

Kết quả cuối ngày 25/10/2026, sau rà soát độc lập và một lượt sửa. Không đổi schema v2; các kết quả P01–P04 ở trên là lịch sử tại thời điểm tương ứng.

| Kiểm tra | Kết quả P05 |
| --- | --- |
| Build solution Debug | Exit 0, 0 lỗi; lượt build hồi quy có 48 cảnh báo hiện có, lượt cuối incremental 0 cảnh báo |
| SchemaChecks | Exit 0; 45 entity / 363 cột / 0 lỗi; 52 bảng vật lý |
| ClinicalChecks | **104 PASS / 0 FAIL**; 80 ca trước + 24 khám/kế hoạch; fixture rollback |
| Đồng thời tiếp nhận | Các ca P04 hai kết nối, cùng lịch, trùng tên và ReadCommitted tiếp tục đạt |
| Đồng thời khám | Hai kết nối cùng phiếu trả một MaLS; lưu cùng snapshot chỉ một commit, một xung đột đúng loại; chỉ một tình trạng, cleanup đạt |
| ClinicalUiChecks | **23 PASS / 0 FAIL**, control WPF thật; 14 ca P04 + 9 ca P05 |
| FinanceChecks | **54** ca transaction đạt |
| SupplierChecks / MaintenanceChecks / ReportChecks | Exit 0; báo cáo 18 chuyên đề, render/SQL đọc đối chiếu đạt |
| Checks | Vẫn lỗi MaCa trùng trong KiemTraGiaoDien, mã -532462766 |
| PersistenceChecks | Vẫn CS1729 BUS_NHANVIEN thiếu constructor transaction |
| WpfSmoke | Vẫn CS1061 NhanSu thiếu TryReadPosition/TryReadEmployee |

Đã kiểm tra bác sĩ đúng nguồn, bác sĩ khác/Admin giả quyền bị từ chối; bắt đầu idempotent; lưu nháp không chờ thu; snapshot cũ bị chặn. Lỗi dòng tình trạng thứ hai không lưu dở dòng đầu khi caller truyền transaction. Gợi ý không nhận mẫu nháp, so mức độ theo cùng collation SQL khi áp, tổng quát đứng sau; nhiều tình trạng không tự áp/ghép. Kế hoạch thủ công không cần mẫu; cùng mẫu/nguồn retry giữ một kế hoạch, bản sao giữ nội dung khi mẫu ngừng và có phiên bản mới. Duyệt không sinh dịch vụ/phí.

Hoàn tất chặn thiếu nội dung, đơn/dịch vụ/kế hoạch nháp; chấp nhận không kê thuốc có lý do và kế hoạch tương lai đã duyệt. Kế hoạch nháp rỗng hủy được, lịch sử còn nguyên. Sau hoàn tất, API không cho ghi đè nội dung, SQL chặn sửa/xóa tình trạng đúng lỗi **52111**. Admin soạn nhưng không ký mẫu; bác sĩ duyệt được ghi đúng actor/thời gian, có audit, không tin chữ ký client. Không seed mẫu chuyên môn.

Bằng chứng trước/sau:

- Backend ban đầu **0/13 → 13/13**; mở rộng phát hiện lưu dở tình trạng và lệch so mức độ C#/SQL, **15 PASS/2 FAIL → 17/17**. Các ca hoàn tất, hủy, catalog đã ngừng, kế hoạch cũ và khóa tình trạng bổ sung thành 24/24.
- WPF thiếu control **14 PASS/2 FAIL → 16/16**. Ca lưu lần hai kế hoạch thủ công từng bị từ chối vì nguồn null thành chuỗi rỗng; đã sửa. Ca hàng đợi/bộ lọc làm mất nháp **16 PASS/1 FAIL → 17/17**.
- Rà soát phát hiện hai Important: ô bảng/ngày đang nhập chưa commit chưa được nhận biết và editor nhập liệu trong hồ sơ bị thay khi đổi BN. Phát hiện danh mục mới không giữ nháp được nâng từ Minor lên Important theo ảnh hưởng mất dữ liệu; nhãn dịch vụ ngừng hoạt động trống được sửa trong cùng lượt. Tái hiện **16 PASS/7 FAIL → 23 PASS/0 FAIL**: đánh dấu từ BeginningEdit/text ngày có focus, giữ trạng thái danh mục riêng, lịch sử chỉ đọc, giữ nhãn dịch vụ nguồn. Không còn phát hiện P05 quan trọng hoặc Minor hoãn lại.

WPF kiểm tra bắt đầu từ hàng đợi bằng nút thật, mở đúng không gian khám, giữ header/dị ứng, giữ nháp qua tab/bộ lọc/đổi ngữ cảnh, lưu/duyệt/hoàn tất và khóa; thử ô đang sửa của tình trạng, kế hoạch, mẫu, text ngày chưa commit và tên danh mục. Có kiểm tra kế hoạch cũ vẫn hiển thị tên dịch vụ đã ngừng. Đã xem ảnh render `obj/ClinicalWorkflowP05/kham-bat-dau.png` với FIXTURE. Các ca ký dùng transaction ngoài rollback; ca hàng đợi/tiếp nhận và đồng thời commit fixture rồi dọn trong finally. Không chạy các suite ghi song song.

Đối chiếu cuối: **52 bảng; 10 BN, 10 hồ sơ, 10 phiếu tiếp nhận, 10 hóa đơn; tổng 732.500,00; thu ròng 467.500,00; tồn 4.014,000**. Fixture BN/nhân viên/tài khoản/lịch CW đều 0; danh mục tình trạng và mẫu đều rỗng sau kiểm tra. Database gốc không bị thay đổi, Word giữ nguyên. Ảnh hồi quy tự sinh của Supplier/Maintenance/Report đã trả về Git.

Log tại `obj/ClinicalWorkflowP05/`: `results.json`, `final-results.json`, `Clinical-final.log`, `Ui-review-green.log`, `Ui-service-red.log`, `ReceptionConcurrency.log`, `ExaminationConcurrency-final.log`, `data-final.log` và các suite baseline. Log TDD/đợt đầu còn ở `obj/ClinicalExaminationP05*.log`, `obj/ClinicalUiP05*.log`.

P05 hoàn tất trong phạm vi khám/kế hoạch. Retry áp mẫu được thử tuần tự, kèm khóa/ràng buộc SQL; chưa đo tải nhiều bác sĩ hoặc thử mọi tranh chấp cấu hình. P06–P12 vẫn còn: dịch vụ thực hiện, đơn snapshot, bảng tính, thu đủ/chống lặp, giao thuốc, guard điều hướng toàn ứng dụng/DPI và AT01–AT34. Ba suite baseline còn lỗi; chưa tuyên bố toàn dự án đạt.
