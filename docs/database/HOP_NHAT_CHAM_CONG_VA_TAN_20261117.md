# Gộp SQL chấm công F5 và hai nhánh vào tan — 17/11/2026

## Nguồn hợp nhất

- Nhánh ban đầu `dong_bo_giua_cac_may`: `4832f86`.
- Nhánh chấm công: `origin/chamcongmoinhat`, `1f7a406`.
- Nhánh đích `tan` trước khi gộp: `dcf4837`.
- SQL đối chiếu: `Database/QLBenhVienRangHamMat_GopChamCong_F5.sql`.
- SHA256 bản F5: `E3EB8E3E0715B54C91B4A6D3A54B5FD0E3F677D03A74813B9FDB2E693ED68FDD`.

File F5 được giữ nguyên. File cài đặt chính tiếp tục là `Database/QLBenhVienRangHamMat.sql`.

## F5 có gì khác database gốc?

Diff chỉ có hai vùng; không thêm bảng, cột, dữ liệu mẫu hoặc thủ tục mới:

| Vùng | F5 so với SQL gốc | Cách gộp |
| --- | --- | --- |
| `RHM_ChotLuongNhanVien` | Bỏ lỗi 52015 bắt buộc mọi ca phải có công đã xác nhận | Tiếp nhận: ca quá hạn chưa ghi nhận/xác nhận tính 0 công, không chặn chốt |
| `RHM SINGLE ACCOUNT SESSION` | Thiếu toàn bộ khối phiên đăng nhập duy nhất có trong SQL gốc | Giữ nguyên khối gốc: `PhienKeTiep`, index phiên và xử lý thu hồi phiên cũ |

Sau gộp, SQL chính bằng F5 cộng lại đúng 47 dòng quản lý phiên duy nhất. Snapshot và schema 74 bảng được giữ. Quy tắc ngày chốt sau cuối tháng 5 ngày, đơn nghỉ chờ duyệt, trùng công/phép, chứng từ chưa duyệt, hoa hồng, số tiền/công đã xem và khóa kỳ đã chốt vẫn áp dụng.

## Hành vi sau hợp nhất

**Chấm công:** nhân viên đang hoạt động xem/chấm ca của mình. Admin xem toàn bộ nhưng chỉ chấm ca thuộc tài khoản mình. Thời gian lấy từ SQL Server; chỉ chấm đúng ngày, trước giờ kết thúc ca, không ghi đè công đã có. Đúng giờ 0,5 công; trễ 1–60 phút 0,25 công; trên 60 phút 0 công. Giữ kiểm tra ca trước, nghỉ phép và kỳ lương đã khóa. Dữ liệu và nhật ký được ghi trong cùng giao dịch có kiểm phiên.

**Lương:** màn hình vẫn hiển thị số ca chưa ghi nhận/xác nhận để đối chiếu, nhưng số này không khóa nút Chốt. Ca đó không cộng công; các điều kiện đối soát khác tiếp tục khóa nút và được SQL kiểm lại.

**Lễ tân AI WPF:** ô `TroLyAI` mở giao diện mới. Tài khoản cần quyền mở phân hệ và quyền nghiệp vụ tiếp nhận; SQL xác thực lại phiên và nhân viên. Bệnh nhân nhập họ tên/ngày sinh/điện thoại/nhu cầu, kiểm tra tóm tắt và xác nhận trước khi tạo phiếu. Không có Internet thì dùng Chọn nhanh; vẫn cần SQL. Cấu hình `GROQ_API_KEY` trên máy chạy; không đưa khóa vào Git. Mặc định `UseMockRepository=false`; DEMO được gắn nhãn riêng.

Mẫu in dùng lại **Phiếu hướng dẫn A5** hiện có qua `Presentation/PhieuHuongDan.AiPrint.cs`; không phụ thuộc `SlipPrintService.cs` đã bị bỏ. Giữ mã phiếu, phòng và STT thực từ SQL khi gửi lại yêu cầu, kể cả ca bác sĩ đã đóng. Phiếu trên màn hình tự ẩn sau 45 giây; phiếu SQL vẫn còn.

Kiosk trình duyệt và endpoint cũ tiếp tục chỉ lưu bản khai; `/api/ai/intake/receipts` vẫn trả HTTP 410. Luồng mới này áp dụng cho WPF đã đăng nhập.

## Các lỗi đã xử lý khi gộp

- DAL tự chấm công mới mở kết nối trực tiếp, bỏ qua guard phiên và ngữ cảnh audit: chuyển về `ThucHien` dùng chung giao dịch. Kiểm thử đã tái hiện phiên bị thu hồi vẫn ghi được trước sửa.
- SQL cho chốt ca thiếu công nhưng UI vẫn yêu cầu `caCho==0`: bỏ điều kiện UI cũ; giữ chặn đơn nghỉ/chứng từ/hoa hồng và thời hạn chốt.
- Câu trả lời như “Có rồi”, “Vâng ạ” ghi đè tên người bệnh: chỉ nhận tên chưa có hoặc sửa tên có nhãn rõ ràng.
- Rời màn hình/reset lúc SQL đang tạo phiếu làm mất kết quả: đăng ký pending work, giữ màn hình trong lúc giao dịch đang xử lý.
- Chatbot tự thông báo mã phiếu/STT chưa có từ SQL: dùng lại bộ lọc tuyên bố tạo phiếu; thông báo thành công chỉ xuất hiện từ kết quả SQL.
- Gửi lại phiên đã tạo chọn phòng mới hoặc đòi ca bác sĩ mới: đọc lại phiếu đã lưu, kiểm nội dung xác nhận và quyền trước khi trả kết quả.
- Script cài AI và kiểm nguồn tham chiếu file máy in đã xóa: cập nhật danh sách file, dùng mẫu A5 hiện có và bổ sung helper lọc phản hồi.
- Bộ kiểm bàn giao dùng chuỗi guard snapshot cũ: cập nhật nhận diện guard có whitelist bảng mở rộng của SQL hiện tại.

Các ca lỗi có log trước/sau trong `obj/MergeTan`. File kiểm Python đã bỏ phụ thuộc `lxml`, dùng thư viện chuẩn; máy hiện tại không có Python thực để chạy file này.

## Kiểm chứng

Chỉ ghi vào các database cách ly có tiền tố `RHM_Hospital_Test_`; không cài SQL hoặc ghi fixture vào database nghiệp vụ đang dùng.

| Bộ kiểm | Kết quả |
| --- | --- |
| Build solution Debug/Release + cấu trúc + schema SQL | Thành công, 0 lỗi biên dịch |
| Chấm công/lương (`tools/KiemTraDongBoSql.ps1`) | 35 đạt, 0 lỗi |
| Quy trình khám (`tools/KiemTraQuyTrinhBenhVien.ps1 -TaoDatabaseKiemThu`) | 47 đạt, 0 lỗi |
| SQL hợp nhất, quyền tiếp nhận, lương theo ca, lịch bác sĩ | Thành công |
| Cài SQL mới/chạy lại, giữ số dòng và constraint được tin cậy | Thành công |
| Đồng bộ WPF không SQL (`Test/WpfSyncChecks`) | 28 đạt, 0 lỗi |
| Giao diện/runtime và ca hồi quy mới (`Test/RuntimeCoverageChecks`) | 20 đạt, 0 lỗi |
| Phiên WPF (`Test/SessionWpfChecks`) | 12 đạt |
| Cấu hình database (`Test/DatabaseSetupChecks`) | 11 đạt |
| Phiếu AI SQL (`Test/ReceptionistSqlChecks`) | 5 đạt, 0 lỗi |
| State đồng bộ và state bản khai AI cũ | Thành công |
| Script cài AI vào bản sao thử, không build | Thành công |
| SQL Change Tracking (`Test/SqlSyncChecks`) | 20 đạt |
| Phiên SQL (`Test/SingleSessionChecks`) | 12 nhóm kiểm đạt, gồm chấm công/audit |
| Backend kiosk (`Test/ApiIntegrationChecks`) | Thành công, giữ chặn tạo phiếu công khai |
| Đồng bộ dữ liệu thực (`Test/RuntimeSyncChecks`) | 5 đạt |
| Đồng bộ đăng nhập/bản khai AI (`Test/RuntimeLoginSyncChecks`) | 6 đạt |
| Điều hướng/tab (`Test/AllModulesSyncChecks`) | 29 đạt với WPF và SQL thật |

Bản build còn cảnh báo nullable/chữ ký sự kiện tồn tại trong dự án và MSB4078 vì `dotnet build` không build project MSI `.vdproj`. Không xác nhận đã dựng MSI mới. Gọi Groq thật, máy in vật lý và hai máy Windows vật lý chưa được nghiệm thu trong lượt này.

## Cách áp dụng

1. Dùng nhánh `tan` sau hợp nhất. Cài SQL chính trên đúng SQL Server/database sử dụng, theo hướng dẫn sao lưu/cài đặt trong README; không dùng file F5 riêng thay cho file chính.
2. Build `BenhVienRangHamMat.sln` hoặc `RangHamMat.Wpf/RangHamMat.Wpf.csproj` bằng .NET 8/Visual Studio trên Windows.
3. Chọn đúng kết nối database khi mở WPF. Cấu hình kho X-quang và API key theo môi trường nếu dùng các chức năng đó.

Để chạy lại kiểm phiếu AI, tạo database thử **mới** từ SQL chính rồi chạy:

```powershell
dotnet run --project Test/ReceptionistSqlChecks -- . RHM_Hospital_Test_<ten_moi>
```

Bộ kiểm này từ chối chạy trên database không có tiền tố kiểm thử và từ chối tái dùng fixture đã có. Việc hợp nhất được thực hiện cục bộ; chưa push GitHub.
