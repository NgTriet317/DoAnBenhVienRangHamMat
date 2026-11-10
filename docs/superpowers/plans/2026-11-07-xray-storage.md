# P4 X-ray Storage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tải, xem, ẩn ảnh X-quang theo MaLS với kho chung, phân quyền, audit và công cụ sao lưu/phục hồi.

**Architecture:** Giữ bảng metadata P1 và bốn tầng; BUS xác minh ảnh và phối hợp tệp/SQL, DAL kiểm quyền và ghi metadata/audit. Ảnh gốc lưu khóa GUID trong thư mục UNC cấu hình. Giao diện nằm trong lần khám và phần lịch sử; không có DICOM viewer.

**Tech Stack:** .NET 8/WPF, SQL Server/Dapper, ImageSharp đã có trong dependency hiện tại; không thêm package/project sản phẩm.

**Spec:** Mục 5/9 của `KeHoachTrienKhai.docx`, `docs/sprint3/KE_HOACH_TRIEN_KHAI_20261107.md`. Người dùng đã cho phép tiếp tục kế hoạch và điều chỉnh phù hợp dự án.

**Kết quả:** P4 đã triển khai; xem `docs/sprint3/KET_QUA_ANH_XQUANG_20261107.md` và `docs/sprint3/evidence/20261107-xray`. Kiểm mới32/32, mở lại ảnh/viewer sau phục hồi2/2;15 tệp gồm ảnh ẩn phục hồi đúng. SQL chạy lại giữ dữ liệu64 bảng. Kiểm UNC giữa hai máy chờ hạ tầng, không tính là đã chạy.

## Global Constraints

- Giữ P0–P3 và thay đổi package của người dùng; làm việc tại nhánh Sprint3_tan.
- Metadata đúng MaLS, chỉ JPG/JPEG/PNG, tối đa 20 MiB và 40 triệu điểm ảnh; tên lưu GUID32, giữ nguyên byte ảnh gốc.
- Kho mặc định phải UNC, không có đường dẫn máy trạm mặc định. Chỉ chế độ kiểm/thử có cấu hình rõ mới cho thư mục cục bộ.
- Bác sĩ phụ trách/Admin sửa ảnh ở lượt Nháp còn mở; lịch sử chỉ xem. Nhân viên có CapNhatKetQuaDuocGiao chỉ thao tác ảnh gắn dịch vụ CK004 được giao, không xem phần của người khác.
- Thu ngân/dược sĩ không có XemHoSo bị từ chối. Bác sĩ từng phụ trách được xem lượt hoàn tất cùng hồ sơ; tiếp nhận có XemHoSo xem lượt đã xác nhận theo phạm vi hiện có.
- Không xóa cứng ảnh/metadata; tệp mất/sai hash không được mở và phải có audit. Không tự dọn orphan.
- Upload sở hữu giao dịch riêng: khóa kho chia sẻ → khóa lượt → xác minh ảnh → tệp tạm → đích → metadata/audit → commit. Lỗi trước commit dọn tệp mới; commit chưa rõ kết quả giữ tệp để đối soát.
- Chỉ ghi kiểm thử vào DB RHM_Hospital_Test_... và kho trong obj. Không nâng cấp DB ứng dụng, không commit/push/publish.

## Review Focus

- Thu hồi quyền/đổi bác sĩ hoặc MaLS giả không được xem/tải/ẩn, kể cả đã có metadata/thumbnail cũ.
- Tệp giả MIME, sai đuôi, hỏng, quá dung lượng/kích thước, đường dẫn đi lùi hoặc reparse point không được đọc/ghi ngoài kho.
- SQL thất bại sau khi ghi tệp không để ảnh bị liên kết sai; commit chưa rõ không xóa tệp có thể đã tham chiếu.
- Ẩn ảnh giữ metadata và tệp, lịch sử chỉ xem, sai quyền không được suy ra thông tin bệnh nhân.
- Sao lưu phải giữ SQL và mọi tệp tham chiếu cùng trạng thái; thử restore ở DB và thư mục khác, đối soát hash/ảnh ẩn.

### Task 1: Kho ảnh và hợp đồng

**Files:** `ET/ET_DULIEUANHXQUANG.cs`, `BUS/CauHinhKhoAnh.cs`, `BUS/KhoLuuAnhXquang.cs`, `tools/KiemTraAnhXquang.ps1`, `tools/XrayChecks.cs.txt`.

- [x] Viết kiểm RED bằng ảnh PNG/JPEG fixture thực và kho tạm, không dùng ảnh bệnh nhân.
- [x] Thêm cấu hình env/appsettings, xác minh filename/MIME/chữ ký/decode/kích thước/hash, chống đường dẫn và liên kết/reparse.
- [x] Thêm staging/di chuyển/đọc xác minh/dọn có kiểm soát và đối soát missing/orphan/hash; chạy kiểm tới GREEN.

### Task 2: Quyền, SQL và nhất quán tệp

**Files:** `DAL/DAL_ANHXQUANG.cs`, `BUS/BUS_ANHXQUANG.cs`, `DAL/DAL_NHATKY.cs` (nội dung audit an toàn).

- [x] Viết kiểm quyền bác sĩ khác, thu ngân, nhân viên được giao; chữ ký phiên và quyền phải đọc lại trong SQL.
- [x] Ghi metadata/audit UTC, xem/ẩn và lỗi tệp; không sửa ảnh của lượt đã hoàn tất.
- [x] Kiểm upload thành công, SQL lỗi dọn tệp, ảnh trùng retry, ẩn còn tệp, mất/sai hash và tài khoản bị khóa.

Điều chỉnh sau review: bảng `PHAN_CONG_ANH_XQUANG`/ET riêng và API/UI phân công từ dịch vụ chụp do bước Thực hiện tạo. Giữ người/giá trên chứng từ dịch vụ. Đã có ca thay người bị khóa, thu hồi quyền cũ, lịch sử2 dòng và phiên XR_ASSIGN. Schema64, XrayStorage1 trong file SQL chính.

### Task 3: Giao diện

**Files:** `Views/AnhXquangLanKham.xaml(.cs)`, `Views/XemAnhXquang.xaml(.cs)`, `Views/KhamLamSang.xaml(.cs)`, `Views/ChiTietKhamDieuTri.xaml(.cs)`.

- [x] Thêm kiểm WPF thực cho danh sách/thumbnail, upload/xem/ẩn, quyền và trạng thái lịch sử.
- [x] Gắn vùng ảnh vào lần khám và lịch sử; ảnh lớn có phóng/thu. Xử lý kho chưa cấu hình/tệp lỗi bằng thông báo, không crash hoặc làm mất nháp khám.
- [x] Render và lưu ảnh minh chứng bằng fixture.

Kiểm hình ảnh phát hiện vùng xem trước bị che ở trang kỹ thuật viên. Đã RED/GREEN, thêm cuộn trang và kiểm vùng ảnh/nút xem khi cửa sổ đang mở. Viewer phục hồi được mở thật và thay đổi slider phóng thu.

### Task 4: Vận hành, hồi quy và bàn giao

**Files:** `tools/QuanLyKhoAnhXquang.ps1`, `tools/XrayStorageOperations.cs.txt`, `docs/sprint3/KET_QUA_ANH_XQUANG_20261107.md`, `README.md`, cấu hình/ignore phù hợp.

- [x] Công cụ kiểm kho chỉ đọc; sao lưu SQL COPY_ONLY/CHECKSUM và tệp/manifest dưới khóa kho độc quyền, không ghi đè bản sao.
- [x] Restore vào DB thử mới và thư mục khác; xác minh ảnh mở được và hash khớp. Không dùng WITH REPLACE trên DB hiện hữu.
- [x] Chạy kiểm mới, build Debug/Release và hồi quy P0–P3/clinical/SQL/report; review độc lập và xử lý finding quan trọng.
- [x] Ghi rõ PASS/FAIL/chưa kiểm, đường dẫn kho, quyền NTFS/share, backup/restore/rollback; cập nhật P4, giữ P5/P6 chưa hoàn tất.
