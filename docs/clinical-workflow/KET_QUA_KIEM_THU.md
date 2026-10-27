# Kiểm chứng triển khai — cập nhật 26/10/2026

**Kết quả mới nhất P11:** build/schema đạt; **210 ca SQL (172 hồi quy/P11 + 25 thanh toán + 13 bàn giao) và 179 ca WPF (39 hồi quy + 127 P11 + 5 thanh toán + 8 bàn giao/in) đạt**. P11 gồm bảo vệ nháp/đổi phiên, ngữ cảnh SQL, phân trang và 99 tổ hợp kích thước/DPI. Ba lỗi suite baseline còn nguyên. Chi tiết từng đợt nằm cuối tài liệu; phần đợt nền dưới đây được giữ làm bằng chứng lịch sử.

Phạm vi đã triển khai: P01–P11, chưa phải nghiệm thu toàn luồng/P12. Kết quả trước sửa tại [BASELINE.md](BASELINE.md); việc còn lại tại [TIEN_DO.md](TIEN_DO.md).

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

## Đợt P06: dịch vụ thực hiện — 26/10/2026

| Kiểm tra | Kết quả cuối P06 |
| --- | --- |
| Build solution Debug / SchemaChecks | Exit 0; 45 entity / 363 cột / 0 lỗi, schema 52 bảng không đổi |
| ClinicalChecks | **124 PASS / 0 FAIL**: 104 ca trước + 20 dịch vụ, rollback fixture |
| ClinicalUiChecks | **27 PASS / 0 FAIL**: 23 ca trước + 4 dịch vụ, WPF thật |
| Đồng thời P04/P05 | Hai mode tiếp nhận/khám tiếp tục đạt |
| Đồng thời P06 | Hai kết nối xác nhận cùng dòng chỉ một ký/audit/nguồn phí; hai bác sĩ/hai lượt cùng hồ sơ tranh lượng cuối chỉ một lưu; ReadCommitted giữ dịch vụ đến rollback (1222) |
| Finance / Supplier / Maintenance / Report | 54 ca tài chính đạt; các suite khác exit 0, báo cáo 18 chuyên đề |
| Checks / Persistence / WpfSmoke | Ba lỗi cũ không đổi: MaCa trùng (-532462766), CS1729 constructor BUS_NHANVIEN, CS1061 NhanSu.TryReadPosition/TryReadEmployee |

Các ca mới kiểm tra bước đã duyệt/cùng MaHS/đúng dịch vụ, lượng đã làm và nháp đang giao, hủy trả lượng, thời gian từ lúc bắt đầu lượt đến giờ SQL, người làm đúng chức vụ/tài khoản hoạt động. Điều dưỡng chỉ sửa kết quả/thời gian dòng được giao; dòng khác không được đọc/sửa, không tự tạo hoặc ký. Snapshot cũ và khóa tài khoản sau mở đều bị chặn. Phát sinh cần lý do có audit; MaLS/MaCTKH/MaDV không đổi sau lưu đầu. Xác nhận thiếu kết quả bị chặn.

Hai buổi cùng hồ sơ dùng một bước: mỗi lượt chỉ tổng hợp phần xác nhận của lượt đó. Kế hoạch/nháp/hủy không thành khoản thu. Có ca lập chứng từ legacy đã xác nhận/thu trong fixture rollback để chứng minh dịch vụ/bước nguồn đã thu vẫn khóa; đây không phải triển khai giao dịch thu đủ P09. P06 không tự phát hành hóa đơn. SQL giữ nguyên trigger bảo vệ nguồn đã ký.

Bằng chứng RED → GREEN:

- API chưa có **0/13 → 13/13**. Mở rộng nguồn đã thu/tài khoản bị khóa thành 16 ca; dòng từ kế hoạch của điều dưỡng gửi lý do trống từng **16 đạt/1 lỗi**, sau chuẩn hóa nullable thành 17/17.
- Workspace chưa có **23 đạt/2 lỗi → 25/25**. Form chung/cache có thể ghi/xóa bỏ qua kiểm tra nguồn: **25 đạt/1 lỗi → 26/26** sau chặn đúng đường ghi/xóa đó.
- Rà soát phát hiện bốn Important: kết quả/lý do lộ cho quầy thu, đọc trực tiếp Admin thiếu audit, lý do cùng giây sắp chuỗi sai tại 999/1000, chọn lượt làm mất BN khác trong bộ chọn. Lỗi chọn dòng mới vẫn giữ dòng cũ nâng từ Minor lên Important theo ảnh hưởng mở lại/nhầm đích nhập. Tái hiện **SQL 17 đạt/3 lỗi**, **WPF 25 đạt/2 lỗi**; sửa trong cùng lượt thành **124/124 SQL, 27/27 WPF**. Không còn phát hiện quan trọng hoặc Minor hoãn lại P06.

WPF đã thử chọn bước giữ dịch vụ, lưu vẫn Nháp, ô lượng sai không dùng giá trị cũ, ký riêng rồi khóa, chọn lại dòng sau mở dòng mới, chuyển giữa hai BN, điều dưỡng chỉ sửa trường được giao và giữ nháp khi đổi nguồn. Ảnh `obj/ClinicalWorkflowP06/dich-vu-da-xac-nhan.png` đã được xem sau khi xử lý lượt Render; thông tin FIXTURE, nguồn/giá chốt/khóa hiển thị đúng. Guard đóng/đổi phân hệ toàn ứng dụng và ma trận DPI thuộc P11/P12.

Ký đồng thời cần commit, nên đã tạo database riêng `RHM_ClinicalWorkflow_Test_P06Concurrency_20261026` từ SQL cài mới v2 với guard không ghi đè. Chạy lần cuối sau sửa review đạt, rồi dọn đúng database tự tạo; `concurrency-cleanup.log` xác nhận DB_ID là NULL. Không tắt trigger/xóa dòng ký để dọn trên bản thử chính. API mode này từ chối tên bản thử chính hoặc database đã có fixture CW.

Đối chiếu cuối bản thử chính: **52 bảng; 10 BN/10 hồ sơ/10 phiếu/10 hóa đơn; tổng 732.500,00; thu ròng 467.500,00; tồn 4.014,000**. Fixture CW đều 0, danh mục/mẫu thử 0. Database gốc và Word giữ nguyên. Ảnh Supplier/Maintenance/Report tự sinh đã trả về Git.

Log ở `obj/ClinicalWorkflowP06/`: `results.json`, `Clinical-final.log`, `ClinicalUi-final.log`, `services-concurrency-final.log`, `review-sql-red.log`, `review-ui-red.log`, `data-final.log`, log build/schema và ba suite baseline. P06 chưa phải nghiệm thu toàn hệ thống; P07–P12 (đơn snapshot, phí/thu đủ, bàn giao, UI tổng thể, AT01–AT34) vẫn còn.

## Đợt P07: kê đơn nhiều dòng và snapshot — 26/10/2026

| Kiểm tra | Kết quả cuối P07 |
| --- | --- |
| Build Debug / SchemaChecks | Exit 0; 45 entity / 363 cột / 0 lỗi, 52 bảng không đổi. Rebuild có 57 cảnh báo ở mã sẵn có DAL/WPF; không thêm cảnh báo trong file P07 |
| ClinicalChecks | **146 PASS / 0 FAIL**; 124 ca trước + 22 ca P07, fixture rollback |
| ClinicalUiChecks | **35 PASS / 0 FAIL**; 27 ca trước + 8 ca P07, WPF thật |
| Đồng thời P04/P05 | Tiếp nhận và khám tiếp tục đạt |
| Đồng thời P07 | Hai kết nối ký thay cùng nguồn chỉ một chữ ký/audit và đổi hiệu lực cũ; sửa cùng snapshot một xung đột; cùng nguồn chỉ một nháp; ReadCommitted giữ thuốc đến rollback; tồn không đổi |
| Finance / Supplier / Maintenance / Report | 54 ca tài chính đạt; các suite khác exit 0, báo cáo 18 chuyên đề |
| Checks / Persistence / WpfSmoke | Ba lỗi sẵn không đổi: MaCa trùng (-532462766), CS1729 constructor BUS_NHANVIEN, CS1061 NhanSu.TryReadPosition/TryReadEmployee |

Đã kiểm tra lọc nhóm, thuốc hoạt động THUOC nối HANG_HOA, nhiều dòng, trùng thuốc và thiếu liều/đường dùng/hướng dẫn/ngày/lượng. Lỗi dòng thứ ba không thay đầu/dòng cũ, kể cả transaction ngoài bắt lỗi rồi tiếp tục. Dòng mới đặt trước dòng cũ vẫn giữ mã duy nhất. Quyền bác sĩ sở hữu được đọc lại; bác sĩ khác, điều dưỡng, Admin không ký thay. Cấu hình nháp không gợi ý; chỉ hướng dẫn duyệt đúng phiên bản và xác nhận phù hợp mới được áp.

Đơn ký giữ tên/hàm lượng/đơn vị cùng mọi chỉ định. Nháp lưu giữ thông tin tham chiếu; danh mục đổi trước ký báo xung đột, cần lưu/đối chiếu lại thay vì tự ký nguồn hàm lượng mới. Có nhiều đơn hiệu lực nếu bổ sung có lý do; kiểm lý do cả khi lưu sửa và ký. Sau hoàn tất vẫn soạn/ký thay trước xử lý: cũ hiệu lực trong lúc soạn, ngừng cùng transaction ký mới. Có hóa đơn/xuất thì chặn; Không kê thuốc có lý do vẫn hoàn tất hợp lệ và không tạo đơn rỗng. KiemTraDonChoThu chặn mọi đơn/dịch vụ nháp, kể cả thay thế, để P08/P09 dùng trong transaction của mình.

Tồn trước/sau lưu/ký/thay bằng nhau. Ca xuất ghi sổ riêng trong fixture giảm đúng 3 đơn vị rồi chặn thay đơn; mọi dữ liệu rollback. Đây không phải triển khai cấp/thu P09. Bản in mới chỉ lấy nguồn đã ký đủ snapshot, có ngày sinh; preview giữ mã đơn riêng và đọc lại hiệu lực/quyền trước in, không theo editor đang đổi nguồn. Bản cũ thiếu cột chốt giữ fallback lịch sử chỉ đọc. Không gửi lệnh in máy thật trong kiểm thử; đã kiểm tra dữ liệu và document.

Bằng chứng RED → GREEN:

- API ban đầu **0/14 → 14/14**; thêm phiên bản hướng dẫn, nhiều đơn và xuất thuốc thành 17/17. Reorder dòng mới trước mã cũ gây **17 đạt/1 lỗi → 18/18** sau giữ các mã cũ trước cấp mã mới.
- Thiếu workspace **27 đạt/2 lỗi → 29/29**. Đường in nháp/snapshot cũ/form chung gây **29 đạt/3 lỗi → 32/32**; giữ đúng exception từ chối quyền khi chặn form chung (một ca P02 từng 31 đạt/1 lỗi).
- Rà soát độc lập có 4 Important: preview theo nguồn editor hiện tại, thiếu thông tin trước ký/in, cấu hình chỉ chọn 100 thuốc đầu, lý do bổ sung bị vượt qua. Thêm phát hiện cấu hình mới mất mã thuốc khi chặn đổi. Tái hiện **SQL 18 đạt/3 lỗi**, **WPF 31 đạt/4 lỗi**, sửa một lượt thành 21 ca P07/35 WPF đạt.
- Kiểm tra nguồn thuốc sau lưu tìm thêm hàm lượng có thể tự đổi khi ký: **21 đạt/1 lỗi → 22/22 P07** sau so tên/hàm lượng/đơn vị với tham chiếu đã lưu, trước mọi ghi ký. Tổng cuối 146/146. Không còn Important hoặc Minor hoãn lại P07.

WPF thử chọn ba thuốc hai lần vẫn ba dòng, điền đủ chỉ định, ô đang edit được tính nháp, không ký thay đổi chưa lưu, lưu/ký/in document và khóa; áp hướng dẫn false/true, Admin không duyệt; tìm thuốc ngoài 100 kết quả với 101 hàng giả; giữ mã cấu hình chưa lưu và tải lại bỏ nháp; preview A vẫn A khi editor B và từ chối sau A ngừng hiệu lực. Vùng an toàn/ngày sinh/tuổi hiển thị ở đầu; chỉ định dài xuống dòng để đọc đủ. Ảnh don-thuoc-da-ky.png đã được xem; dữ liệu FIXTURE, không phải hướng dẫn điều trị thật.

Đồng thời có commit chữ ký chạy trên database mới riêng RHM_ClinicalWorkflow_Test_P07Concurrency_20261026, guard không ghi đè và không nhận bản thử chính. Lượt cuối sau sửa nguồn thuốc đạt rồi dọn đúng database tự tạo; concurrency-cleanup.log ghi DB_ID NULL. Không tắt trigger/xóa lịch sử ký để dọn trên bản thử chính.

Đối chiếu cuối: **52 bảng; 10 BN/10 hồ sơ/10 phiếu/10 hóa đơn/10 đơn thuốc/10 dòng thuốc; tổng 732.500,00; thu ròng 467.500,00; tồn 4.014,000**. Fixture CW đều 0; nhóm/hướng dẫn thuốc và mẫu thử 0. Database gốc/Word giữ nguyên, ảnh hồi quy tự sinh đã trả về Git.

Log ở obj/ClinicalWorkflowP07/: results.json, final-results.json, Clinical-final.log, ClinicalUi-final.log, concurrency-final.log, review-sql-red.log, review-ui-red.log, drug-source-red.log, data-final.log và log các suite. P08–P12 còn lại: bảng phí/phân bổ dự kiến, thu đủ/chống lặp, bàn giao, UI tổng thể và AT01–AT34; chưa tuyên bố toàn dự án đạt.

## P08 — Bảng tính phí và nguồn in (26/10/2026)

- API skeleton chạy RED 0 đạt/8 lỗi; sau triển khai đạt 8/8. Bổ sung các biên nguồn/lô/lịch sử thành 14 ca P08; bộ SQL đầy đủ đạt **160/160**. WPF skeleton RED 35 đạt/2 lỗi → 37/37; rà soát thêm RED 37 đạt/2 lỗi → **39/39**.
- Phí một dịch vụ 100.000 + hai thuốc 50.000 = 150.000; thêm đơn hiệu lực có lý do được cộng. Ký đơn thay đổi lượng đổi token, đơn cũ Ngừng áp dụng và nháp Hủy không cộng; còn nháp thay thế chặn tính phí.
- Ba lô mỗi lô 0,005 × giá 1 cho tổng 0,03 thay vì làm tròn tổng 0,015 thành 0,02. Preview khớp tổng thực từ DispensePrescription + CreateInvoice + trigger trong fixture rollback; giữ FIFO theo ngày nhập dù thứ tự hạn dùng khác. Chặn dùng trùng tồn cho hai đơn và kho thiếu không ghi dở.
- Dấu vân tay ổn định khi cùng nguồn, đổi khi đổi giá, lượng/đơn hiệu lực hoặc tồn/lô sau xuất của lượt khác. Xem phí không thay số HD/PX/TT hoặc tồn. Quyền giả/nurse/pharm/bác sĩ khác bị từ chối; DTO không có kết quả hoặc hướng dẫn dùng thuốc; Admin có audit.
- Hóa đơn lịch sử phải thu 100.000,25, thu ròng 40.000,10, còn 60.000,15 giữ nguyên; không suy là đã thu đủ, không tạo nguồn thu mới. Phiếu xuất nháp cũng chuyển đối chiếu. Bản in gộp đúng dòng đơn/giá, cộng tiền đã chốt, giữ MaCTPX và phần lẻ; phân trang ở bốn chiều cao giấy không cắt dòng truy vết.
- Hai kết nối thật: tổng/token giống nhau; caller ReadCommitted giữ giá thuốc và lô tới rollback; số chứng từ/tồn giữ nguyên. Fixture ký chỉ commit trên database P08Concurrency riêng, đã dọn database (DB_ID=NULL). Lỗi setup đầu do sqlcmd thiếu -f 65001 và ca test tham chiếu cột không tồn tại đã được sửa; không thay constraint của sản phẩm.
- Build cuối exit 0, 0 lỗi (lượt incremental 0 cảnh báo; lượt build trước có 48 cảnh báo WPF cũ). Schema 45 entity/363 cột, 0 lỗi. Finance54, Supplier, Maintenance, Report18 đạt. Tiếp nhận/khám đồng thời chạy lại đạt và dọn fixture. Các kiểm chứng P06/P07 đồng thời trước đó giữ bằng chứng ở mốc tương ứng.
- Ba lỗi baseline chạy lại còn nguyên: Checks trùng MaCa (-532462766), Persistence CS1729 BUS_NHANVIEN, WpfSmoke CS1061 NhanSu. Chưa tuyên bố toàn bộ suite hoặc AT01–AT34 đạt.
- Đối chiếu cuối: 52 bảng, 10 BN/HS/phiếu/HD/đơn/chi tiết đơn; tổng hóa đơn 732.500, thu ròng 467.500, tồn 4.014; fixture CW và danh mục/hướng dẫn điều trị thử đều 0. Không áp SQL lên database gốc. Log/ảnh tại obj/ClinicalWorkflowP08/; ảnh hồi quy sinh lại được hoàn trả theo Git.

Rà soát độc lập: một Important về truy vết bản in; đường tab legacy từ phần chưa đánh giá được nâng thành Important. Cả hai có kiểm thử tái hiện thất bại trước sửa và đã đạt trong cùng lượt sửa. Không còn Important hoặc Minor hoãn lại P08. P09 thu đủ/chống lặp, P10 bàn giao, P11/P12 điều hướng/DPI/tải/nghiệm thu và quy trình xử lý lịch sử chưa thuộc kết quả P08; chưa thử máy in thật.

## P09 — Thu đủ, thử lại và rollback (26/10/2026)

- Kết quả cuối: **160/160 ClinicalChecks trên bản thử chính + 25/25 PaymentChecks trên DB riêng = 185 ca SQL**; **39/39 ClinicalUiChecks + 5/5 PaymentUiChecks = 44 ca WPF**. Các bộ thanh toán có commit thật nên không gộp vào mode rollback mặc định.
- RED API skeleton 0 đạt/14 lỗi; sau điều phối phát hiện lệch giây NgayKe/giờ máy, sửa riêng đường xuất nội bộ thanh toán theo giờ SQL datetime2(0). Ca hóa đơn lấy tên hiện tại thay chốt RED 13 đạt/1 lỗi → đạt; dùng snapshot tên/hàm lượng với fallback null cho legacy. Lỗi mở kết nối RED 20 đạt/1 lỗi → 21/21, mapper che SQL và giữ inner exception phục vụ chẩn đoán.
- Thực tế 450.000: tiền đưa 500.000 lưu Thu 450.000/thối 50.000 ngay INSERT; đưa 400.000 không ghi bảng/tồn/audit. Bank phải đúng tiền, có tham chiếu đã trim và xác nhận đối chiếu; sai đầu vào, quyền giả/khóa tài khoản hoặc Admin/bác sĩ/dược sĩ không được thu. Tổng 0 có nguồn tạo hóa đơn xác nhận, không tạo Thu 0.
- Hai kết nối cùng UUID hoặc khác UUID cùng lượt chỉ một bộ thu/xuất. Hai lượt/kho riêng cùng chờ khóa tồn tái hiện SQL1205, RED 21 đạt/1 lỗi; khóa writer trước phạm vi SQL cũ cho kết quả 22/22. Phép thử cùng UUID trên kết nối mới sau commit mô phỏng mất phản hồi của client và không định giá lại theo giá đã đổi; không cắt mạng thực tế tại thời điểm COMMIT.
- Giá đổi/tồn đổi phải xem lại; xuất hết thuốc cuối bằng chứng từ hợp lệ không xuất dở thuốc đầu. Trigger lỗi INSERT thanh toán và lỗi audit cuối đều rollback HD/CTHD/PX/CTPX/Thu/bàn giao/audit/trạng thái phiếu, tồn bằng trước. Fixture có kho riêng để ca hết thuốc không ảnh hưởng ca khác.
- Review Important: UUID mới trả hóa đơn cũ chưa được ràng buộc, tái dùng cho lượt/đầu vào khác lọt. Hai ca RED; ghi ánh xạ UUID/hash/lượt/HD trong nhật ký cùng transaction, thử lại chỉ đọc. Ca ba lô mã Unicode hợp lệ chứng minh sai comparer SQL/Ordinal; nâng từ mục chưa đánh giá thành Important. Tổng RED 22 đạt/3 lỗi → **25/25**, sửa cùng một lượt; không còn Important hoặc Minor hoãn lại.
- UI RED 0/4 → 4/4; harness bổ sung DispatcherSynchronizationContext đúng handler WPF vì không gọi Application.Run. Kiểm tra nút khóa khi SQL đang chờ, bấm lần hai không gửi thêm; bank cần đối chiếu; lỗi chưa rõ kết quả giữ DTO/UUID, chặn đổi nguồn/tiền, thử lại không đọc đầu vào bị sửa. Bác sĩ chỉ xem. Ca tích hợp màn Hóa đơn thêm thành **5/5**, kiểm tra bộ lọc không đổi đích khi đang thu và lịch sử chọn đúng HD sau commit. Ảnh đã mở kiểm tra.
- Lượt cuối trên DB mới sạch có **21 hóa đơn v2, 20 Thu đủ, 13 Chờ giao và 4 UUID đối chiếu bổ sung**. Script độc lập xác minh mọi v2 đã commit thu ròng đúng phải thu, không Thu 0/thu lặp, đủ từng lượng thuốc/dịch vụ/phần xuất, journal không trùng và đúng chứng từ. Đã dọn DB riêng, DB_ID=NULL.
- Build cuối exit0, 0 lỗi/0 cảnh báo ở lượt incremental; lượt build đầy trước có 48 cảnh báo WPF cũ, không coi incremental là sửa cảnh báo. Schema45/363 đạt; Finance54, Supplier, Maintenance, Report18 đạt. Tiếp nhận/khám đồng thời chạy lại đạt và dọn fixture. Full160 ban đầu có ca P08 legacy so đồng hồ trước khi NgayKe đã đến; fixture chờ đúng điều kiện dưới khoảng nửa giây rồi so lô/trigger, vòng cuối160/160; không thay chính sách legacy.
- Ba lỗi baseline còn nguyên: Checks trùng MaCa (-532462766), Persistence CS1729 BUS_NHANVIEN, WpfSmoke CS1061 NhanSu. Chưa tuyên bố toàn bộ suite hoặc nghiệm thu AT01–AT34 đạt. Không in máy thật hoặc giao thuốc thực tế trong P09.
- Bản thử chính cuối giữ **52 bảng, 10 BN/HS/phiếu/HD/đơn/chi tiết đơn, tổng HD732.500, thu ròng467.500, tồn4.014**, fixture/config thử đều0. Database gốc và Word giữ nguyên; ảnh Supplier/Maintenance sinh lại được hoàn trả. Log tại obj/ClinicalWorkflowP09/.

## P10 — Bàn giao, in sau thu và in lại (26/10/2026)

- Kết quả cuối **198 ca SQL =160/160 ClinicalChecks +25/25 PaymentChecks +13/13 HandoverChecks**; **52 ca WPF =39/39 ClinicalUiChecks +5/5 PaymentUiChecks +8/8 HandoverUiChecks**. Các bộ có commit chạy trên DB riêng, không gộp vào mode rollback mặc định.
- API skeleton RED0/12 →12/12: chưa thu không giao; dược sĩ thật từ SQL, cờ giả/admin/bác sĩ/thu ngân/điều dưỡng/tài khoản khóa bị từ chối; giao lặp giữ người/giờ và một audit; hai writer chỉ một chuyển trạng thái; nhiều đơn chỉ hoàn tất phiếu sau đơn cuối. Lỗi audit cuối rollback trạng thái giao/phiếu, không đổi HD/PX/Thu/tồn.
- Hàng chờ phân trang/lọc trạng thái/mã đúng nguồn; tên/hàm lượng/hướng dẫn chốt không theo danh mục đổi. Read result thanh toán phản ánh Đã giao sau giao, không tiếp tục báo Chờ giao. Print API kiểm quyền, từ chối đọc nguồn trong transaction ngoài chưa commit, giữ cùng mã/ngày/giá/tiền và lịch sử partial100.000,25/thu40.000,10/còn60.000,15.
- Review Important vòng khóa reader/writer: writer giữ global/lượt, reader đọc nguồn rồi chờ lượt, writer bị chặn UPDATE. Tái hiện thật RED12 đạt/1 lỗi; route đọc ngắn READCOMMITTEDLOCK, khóa lượt trước nguồn và xác minh lại, GREEN13/13. Không rà soát lần hai.
- UI skeleton0/5 →5/5. Thiếu đơn vị chốt trên dòng in RED6 đạt/1 lỗi →7/7; đưa unit vào nhóm/Quantity, giữ CTPX. Bổ sung nguồn reader đổi mã không được gửi in và route module dược sĩ đúng đơn; thêm thử lỗi giao giữ mã để retry. Ghi chú ký bị bỏ khỏi document RED7 đạt/1 lỗi, bổ sung note và giữ phone/note legacy →8/8.
- Kiểm ảnh thấy header/ô ban đầu chưa đo xong; harness chờ ApplicationIdle, kiểm cột/cell thật, không reset width trong sản phẩm. Lỗi thực là hàng46 DIP cắt liều/đường/ngày: vùng đọc26, cần khoảng47 DIP. Probe đo nội dung không giới hạn RED7 đạt/1 lỗi →8/8 với auto row height cục bộ và pixel scrolling. Ảnh cuối đã mở kiểm tra. UI fixture có kho/lô riêng100 để chạy lặp không cạn tồn ca khác.
- Printer/spooler lỗi được giả lập qua callback của luồng in thật: thanh toán giữ kết quả, nút in lại vẫn dùng MaHD cũ; không tăng HD/PX/Thu hoặc giảm tồn. Cửa sổ in đọc lại nguồn mỗi lần, từ chối reader chuyển mã. Đơn hướng dẫn dài khoảng6.000 ký tự được dựng nhiều trang500×350, đủ trang và đoạn kết/ghi chú, không co một trang. Chưa kiểm máy in/driver thực.
- Lượt cuối DB P10 mới sạch có **20 hóa đơn v2,20 Thu đủ,23 bàn giao (13 Chờ giao,10 Đã giao)**. Script độc lập xác minh mọi v2 đã commit thu đủ/đúng nguồn/lượng, không xuất lặp, người/giờ giao đúng trạng thái, audit giao không lặp, phiếu khớp giao hết đơn. P09 chạy lại25/25+5/5 và bất biến thu đủ đạt. Cả hai DB đã dọn, DB_ID=NULL.
- Build cuối exit0,0 lỗi/0 cảnh báo ở lượt incremental; build trước có48 cảnh báo WPF cũ và9 DAL cũ theo lượt rebuild, không coi incremental là sửa chúng. Schema45/363 đạt; Finance54, Supplier, Maintenance, Report18 đạt. Tiếp nhận/khám đồng thời chạy lại đạt và dọn fixture. Ba lỗi baseline còn nguyên: Checks MaCa (-532462766), Persistence CS1729 BUS_NHANVIEN, WpfSmoke CS1061 NhanSu.
- Bản thử chính cuối giữ52 bảng,10 BN/HS/phiếu/HD/đơn/chi tiết đơn, tổng HD732.500, thu ròng467.500, tồn4.014; fixture CW và cấu hình thử đều0. Không thay database gốc/Word, không đổi schema hoặc thuật toán kho. Ảnh hồi quy sinh lại được hoàn trả; log tại obj/ClinicalWorkflowP10/.

Chưa nghiệm thu P11/P12: chuyển/đóng/logout toàn ứng dụng, DPI, thông lượng tải cao, thiết bị in thật và AT01–AT34 đầy đủ. Rà soát không đánh giá đúng/sai y khoa hoặc xác minh ngân hàng tự động; giữ xác nhận nghiệp vụ của người giao. Không còn Important hoặc Minor hoãn lại P10 sau lượt sửa và kiểm chứng cuối.

## P11 — Liên kết, bảo vệ nháp và DPI (27/10/2026)

- Kết quả cuối **210 ca SQL =172/172 ClinicalChecks +25/25 PaymentChecks +13/13 HandoverChecks**; **179 ca WPF =39/39 ClinicalUiChecks +127/127 ExperienceUiChecks +5/5 PaymentUiChecks +8/8 HandoverUiChecks**. SQL P11 có12 ca ngữ cảnh/quyền/tìm/phân trang; toàn bộ172 fixture rollback. Các ca commit WPF chạy đúng DB riêng, không chạy experience trên bản thử chính.
- P11 WPF gồm28 ca điều hướng/nháp/phiên/lỗi/đường sử dụng thật và99 tổ hợp11 view ×3 kích thước1024×768,1366×768,1920×1080 ×DPI100/125/150%. Có parent HoaDon và AppWorkspace shell, tên/địa chỉ/chẩn đoán/ghi chú dài, trống, lỗi SQL, trạng thái chờ và dữ liệu bàn giao đã thu. Harness gửi WM_DPICHANGED tới HWND rồi xác nhận VisualTreeHelper.GetDpi thực, kiểm giới hạn hiển thị của nút/ô/hàng; không thay thiết lập màn hình hệ điều hành. Đã xem ảnh Rx, hồ sơ, shell và hóa đơn ở1024/DPI150, sửa clipping thực trước lượt cuối.
- Một review chỉ đọc, một lượt sửa. RED→GREEN:7 ca cửa sổ con/baseline form/nguồn tiền (15 đạt/7 lỗi→22/0),2 ca lỗi/focus (22/2→24/0),4 ca nguồn nằm ngoài cache (24/4→28/0). Ca Rx dùng BeginEdit và textbox thật. Dữ liệu đường sử dụng có70 BN,110 lượt ký,151 lịch hẹn và một hóa đơn legacy0 trên DB riêng để kiểm tìm nguồn ngoài cache; không xóa chữ ký để dọn.
- Bổ sung SQL xác minh chẩn đoán nháp không xuất hiện/tìm được với điều dưỡng và nguồn mã lượt hóa đơn chỉ đọc metadata đúng quyền. Không dùng snapshot toàn bộ làm điều kiện kiểm quyền màn thu. Guard đổi phiên dùng principal cũ để xử lý nháp, hủy giữ phiên cũ, đổi thật xóa cửa sổ lâm sàng/print cũ cả khi Home; cùng định danh giữ nháp. Save chỉ lưu nháp, không ký hoặc thu tự động.
- Fixture tải1000 BN/10000 lượt, trang25 dòng, lấy trang BN1/40 và lượt400, snapshot UI50 BN/100 lượt có đủ cha, tải đích ngoài cache đạt. Query ban đầu timeout hơn30 giây; cùng predicate với OPTION(RECOMPILE) đạt497ms cho ba truy vấn ở lượt cuối. Chưa coi đây là benchmark nhiều người dùng hoặc thông lượng writer tài chính.
- Sau chạy lại UI: DB P09 riêng có25 HDv2,24 Thu đủ,13 chờ giao và4 yêu cầu bổ sung có ràng buộc. DB P10 riêng có29 HDv2,29 Thu đủ,33 bàn giao (20 Chờ giao/13 Đã giao). Script độc lập kiểm mọi hóa đơn v2 đúng nguồn/tổng/thu đủ/lượng xuất, alias duy nhất, audit giao không lặp, người/giờ và trạng thái phiếu khớp giao hết. Khác số P10 cũ do UI commit thêm fixture khi chạy lại; cả hai script đạt rồi dọn đúng hai DB, DB_ID=NULL.
- Tiếp nhận/khám hai kết nối chạy lại đạt và dọn fixture. Bản thử chính giữ52 bảng,10 BN/HS/phiếu/HD/đơn/chi tiết đơn, tổng HD732.500, thu ròng467.500, tồn4.014; CW/cấu hình thử0. SHA256 Word7BAA235C67555AA80B4FF07BDE40BEF4A35336424B56B6E5F12FEEE9F5A630AA khớp ban đầu. Không ghi database gốc.
- Build kiểm chứng trước ma trận0 lỗi/52 cảnh báo cũ; build solution ngay trước checkpoint0 lỗi/61 cảnh báo, gồm cảnh báo C# sẵn có và NU1900 do không truy cập được nguồn dữ liệu lỗ hổng NuGet trong môi trường mạng hạn chế. Không có cảnh báo trong file P11 mới. Schema45 entity/363 cột, Finance54, Supplier, Maintenance, Report18 đạt. Ba lỗi baseline giữ nguyên: Checks trùng MaCa, Persistence thiếu constructor BUS_NHANVIEN(transaction), WpfSmoke thiếu NhanSu.TryReadPosition/TryReadEmployee. Không sửa nhân sự/lịch ngoài phạm vi.

Bằng chứng tại obj/ClinicalWorkflowP11/: build-checkpoint.log, build-final.log, clinical-final.log, payments-final.log, handover-final.log, ui-final.log, experience-final.log, payments-ui-final.log, handover-ui-final.log, ui-refresh-results.json, regression-results.json, reception-concurrency-final.log, examination-concurrency-final.log, payments-invariants-final.log, handover-invariants-final.log, data-main-final.log, database-cleanup-final.log và ảnh P11-*.png. P12 còn nghiệm thu đầy đủ AT01–AT34 trên cài mới/nâng cấp, khôi phục backup, thiết bị in/màn hình thực và đóng gói. Giới hạn/chi phí có chủ đích tại mục46–52 của TIEN_DO.md.