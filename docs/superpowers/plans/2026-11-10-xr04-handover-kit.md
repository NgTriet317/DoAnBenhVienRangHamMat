# XR04 Handover Kit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chuẩn bị gói công cụ XR04 chạy bằng runtime trên máy thứ hai, không cần source/SDK, và kiểm thật gói đã giải nén trên môi trường hiện có.

**Architecture:** Biên dịch lại ba entry point đã có: SharedStorageProbe, XrayStorageOperations và XrayChecks ở chế độ restore. PowerShell điều phối kiểm hash, guard database thử/UNC, lưu log mới và so hai proof. Chỉ tạo gói cục bộ trong obj; không cập nhật PhatHanh hay triển khai lên máy khác.

**Tech Stack:** .NET8/WPF, PowerShell5.1, ZIP/SHA256, Windows Integrated Security.

**Spec/design:** Tiếp nối mục9 XR04 và mục11 vận hành/bàn giao của `KeHoachTrienKhai.docx`, cùng `docs/sprint3/HUONG_DAN_TRIEN_KHAI_VA_ROLLBACK.md`. Người dùng cho phép điều chỉnh và yêu cầu tiếp tục; chọn gói binary nội bộ để giảm phụ thuộc SDK ở máy nghiệm thu. Không thêm tính năng sản phẩm hoặc thay SQL.

## Global Constraints

- Giữ nhánh/working tree hiện có; không commit/push, không thay thư mục PhatHanh cũ, không cập nhật database nghiệp vụ.
- Gói chỉ chứa binary công cụ, script, hướng dẫn và metadata; không chứa SQL snapshot, .bak, kho ảnh hoặc ảnh người bệnh.
- Ràng buộc mọi thao tác kết nối qua wrapper vào `RHM_Hospital_Test_...`; restore cần DB đích mới và kho UNC mới. Không tạo share/đổi ACL.
- Dùng các nguồn P6 đã kiểm, so hash toàn bộ481 mục cũ; nguồn mới chỉ cho phép bốn script bàn giao trong kế hoạch này. Không sửa manifest/kết quả P6 lịch sử.
- Các lần kiểm cùng máy, kể cả UNC loopback và gói giải nén, không làm XR04 PASS. Gói luôn ReadyForDeployment=false.
- Máy chạy cần Windows/.NET8 runtime; VerifyRestore cần Desktop Runtime8 và phiên desktop. Máy đóng gói cần SDK; không tuyên bố đã thử máy chưa có SDK khi máy hiện tại có SDK.

## Review Focus

- Gói thiếu/sửa tệp, đường dẫn manifest thoát thư mục, extra payload và INCOMPLETE phải bị từ chối trước chạy tool.
- Nguồn nghiệp vụ khác P6 nhưng gắn lại kết quả cũ; phải từ chối đóng gói.
- Wrapper dùng nhầm DB thật/path local/restore đích sai; phải từ chối trước SQL.
- Gói giải nén chạy qua `dotnet <dll>`, không tự build/restore hay cần đường dẫn source.
- Không ghi đè log/proof/gói cũ; lỗi child phải làm wrapper thất bại, không tạo bằng chứng xanh.

### Task 1: Guard và kiểm toàn vẹn

**Files:** `tools/XR04BanGiaoSupport.ps1`, `tools/KiemTraBoBanGiaoXR04.ps1`.

- [x] RED: thiếu helper, gói đúng/sửa/mất/thừa tệp, path traversal, INCOMPLETE, DB thật/local UNC/restore sai, nguồn cũ đổi hash.
- [x] Implement helper lập/kiểm manifest, kiểm đầu vào và đối chiếu nguồn P6; chạy GREEN.

### Task 2: Đóng gói và wrapper

**Files:** `tools/TaoBoNghiemThuXR04.ps1`, `tools/ChayNghiemThuXR04.ps1`, `docs/sprint3/BO_NGHIEM_THU_XR04.md`.

- [x] Builder nhận thư mục P6, mã gói mới; so nguồn, publish ba tool vào obj, copy script/hướng dẫn, tạo manifest/hash/ZIP. Nếu lỗi giữ INCOMPLETE.
- [x] Wrapper có KiemTraGoi/Probe/Compare/KiemTraKho/SaoLuu/KhoiPhuc/XacMinhPhucHoi; log riêng, gọi DLL/runtime, đánh giá exit/marker; giữ trạng thái chưa phát hành.
- [x] Kiểm process guard và tạo một gói thật; giải nén sang thư mục khác, kiểm hash, probe UNC, backup/restore mới, BUS/viewer; so proof cùng máy phải FAIL.

### Task 3: Bàn giao

- [x] Review độc lập đúng phạm vi công cụ mới, xử lý finding xác nhận và kiểm liên quan.
- [x] Ghi kết quả/gói/hash/lệnh, nối README/tiến độ; giữ XR04 chờ hai máy và quyền vận hành.

## Execution Ledger

- Pre-flight: wrapper dùng helper manifest/input; builder copy nguyên wrapper/helper và các nguồn C# đã qua P6. So nguồn P6 trước và sau publish để phát hiện sửa trong lúc dựng.
- Ruling: tái sử dụng ba entry point đã kiểm thay vì viết lại truy cập SQL/ảnh. Chi phí nếu thiếu phụ thuộc runtime là gói không chạy trên máy đích; kiểm từ gói đã giải nén để phát hiện.
- Ruling: tiếp tục chuẩn bị cục bộ khi chưa có SQL/UNC máy thứ hai, đã hỏi thông tin bất đồng bộ. Không tự quét LAN hay giả hostname; rủi ro nếu bỏ qua là nghiệm thu sai phạm vi.
- Task 1: complete — RED0/12→GREEN12/12; CLI RED12/14→GREEN14/14. Đã kiểm process từ chối DB thật/mã gói sai trước SQL/tạo gói.
- Review độc lập `review_xr04_kit` xác nhận sai ánh xạ KiemTraKho, marker cha chưa được kiểm và thiếu payload Hidden. KiemTraKho đã tái hiện trên ZIP giải nén: tool trả `FAIL: Unknown operation`, wrapper giữ Status=FAIL. Sửa ánh xạ sang KiemTra.
- Final: marker cha và Hidden đã có RED14/17→GREEN17/17 (`obj/P6Evidence/xr04-review-*.log`). Kiểm marker cha áp dụng khi chạy gói, chỉ builder có cờ nội bộ để kiểm payload trong lúc đang dựng.
- Final: đánh giá gap Hidden là lỗi tính đầy đủ của kiểm toàn vẹn nên xử lý cùng lượt: tệp phụ thuộc Hidden bị mất có thể không được manifest phát hiện. Không coi hash là chữ ký/nguồn xác thực.
- Gói đầu `20261110_120427_16bfedd0` giữ để tái hiện lỗi, không bàn giao làm gói cuối.
- Task 2: complete — gói cuối `20261110_120954_a7405046`,213 tệp payload,18.916.481 byte ZIP; SHA256 `C070FE8F431F58066668A131FA2C3845B3C0DAA551C6A139A7A59CC0ED97D9E5`. So481 nguồn P6 và485 nguồn hiện tại, ổn định trước/sau dựng.
- Final: fixed KiemTraKho — ZIP đầu RED Unknown operation→ZIP cuối GREEN15 metadata/0 lỗi kho. Fixed marker/Hidden — RED14/17→GREEN17/17, process từ gói giải nén từ chối marker cha và payload sửa.
- Task 2 integration:12/12 bước trong `verify-3cfa54e282b349c4a78160823e49457b`; Probe15/13/2, SQL+ảnh backup/restore vào `RHM_Hospital_Test_XR04Kit_20261110_121045_88f639`, viewer2/2, hai proof thực cùng hostname bị từ chối. Không dùng unit JSON hay loopback để chốt hai máy.
- Task 3: complete — hướng dẫn độc lập trong gói, kết quả/evidence tại `docs/sprint3/KET_QUA_BO_NGHIEM_THU_XR04_20261110.md`, README/tiến độ/hướng dẫn vận hành đã nối. Không có finding đã xác nhận chưa xử lý trong phạm vi review công cụ mới; full XR04/P6 vẫn chờ hạ tầng.
- Ruling: `dotnet publish` chỉ dựng ba công cụ nội bộ vào obj; không chép vào PhatHanh, không phát hành ứng dụng hay nâng cấp database nghiệp vụ. Hiện vật ZIP có hash để mang đi kiểm; rủi ro nếu nhầm với bản đã duyệt vận hành là triển khai khi chưa kiểm máy đích.
- Kiểm bàn giao cuối:90 link cục bộ hợp lệ,32 tệp evidence khớp hash; gói gốc và giải nén qua kiểm toàn vẹn, nguồn485 mục khớp bản đóng gói, đủ481 mục P6 cũ giữ nguyên và evidence P6 lịch sử không đổi. Bốn script parse đúng, kiểm cấu trúc bốn project đạt, ảnh viewer phục hồi đã xem bằng mắt. HEAD/nhánh không đổi.
