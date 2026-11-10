# P5 Data Reconciliation and Demonstration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Đối soát chỉ đọc dữ liệu đang có và tạo bộ trình diễn phi định danh ba kỳ, giải thích được bệnh lý, hóa đơn, thu thực tế và kho.

**Architecture:** Giữ bốn project. ET chứa kết quả đối soát; DAL chạy SELECT trong một giao dịch nhất quán; BUS điều phối. Công cụ PowerShell xuất JSON/CSV/Markdown, công cụ tạo dữ liệu chỉ nhận database thử mới. Không thêm giao diện hay bảng sản phẩm khi bảng xuất đã đủ nhu cầu P5.

**Tech Stack:** WPF/.NET8, SQL Server, Dapper, PowerShell; không thêm package.

**Spec:** `KeHoachTrienKhai.docx` mục7–9, `docs/sprint3/KE_HOACH_TRIEN_KHAI_20261107.md`. Người dùng đã giao tự điều chỉnh và tiếp tục từng gói.

## Global Constraints

- Làm tiếp tại `Sprint3_tan`; giữ mọi thay đổi P0–P4 và thay đổi package của người dùng. Không commit/push/publish hay nâng cấp database đang dùng.
- Audit dùng SELECT, không sửa dữ liệu hay ghi audit sự kiện; xuất PK, mã liên quan, nguyên nhân; không xuất tên/ngày sinh/chẩn đoán/nội dung bệnh án.
- Phân biệt `SaiXacDinh` với `CanDoiChieu`; không suy tình trạng nhân sự/giá lịch sử từ danh mục hiện tại. Không tự hoàn tất khám, gán mã bệnh hay sửa chứng từ cũ.
- Bộ trình diễn chỉ trong DB mới `RHM_Hospital_Test_P5Demo_...`, bỏ snapshot bệnh nhân trong bản sao SQL kiểm thử. Ghi nhãn DEMO, marker database, manifest; chạy lại từ chối DB đã tồn tại, không xóa/ghi đè.
- Giá dịch vụ hiện có mới hiệu lực tháng11; tạo mã dịch vụ/thuốc giả lập riêng trong DB DEMO cho08–10/2026. Không đổi ngày hiệu lực/giá của mã đã có. Không dựng số cùng kỳ năm trước.
- Mô phỏng lịch sử bằng bản ghi mới có lịch trình xác định từ đầu, chuyển trạng thái Nháp→xác nhận/ghi sổ qua trigger thật. Dùng DAL tạo hóa đơn từ nguồn thật; ngày hóa đơn chỉ đặt lúc còn Nháp, thanh toán mới ghi đúng kỳ. Không sửa chứng từ đã chốt/tắt trigger hoặc đổi đồng hồ production.
- Artifact trong `obj/P5Evidence`; bằng chứng đưa vào docs chỉ gồm số tổng hợp và dữ liệu giả. File SQL sản phẩm duy nhất vẫn là `Database/QLBenhVienRangHamMat.sql`; P5 không cần DDL sản phẩm.

## Approaches and Rulings

- Sửa ngay bất thường trên dữ liệu cũ: chưa có nguồn đối chiếu bệnh án/giá lịch sử; chọn báo rõ để xét từng PK, không tự sửa.
- Dồn mẫu vào DB đang dùng hoặc hồi tố mã dịch vụ: làm lẫn nguồn, sai lịch sử; chọn DB trình diễn trống và mã DEMO riêng.
- Tạo tổng báo cáo độc lập: không chứng minh liên kết nguồn; chọn chuỗi bệnh nhân/hồ sơ/ca/tiếp nhận/lần khám/tình trạng/dịch vụ/thuốc/xuất/hóa đơn/thanh toán.

## Review Focus

- Audit thiếu FK ghép/NULL, số đếm bị giới hạn nhưng lại báo đầy đủ; kiểm số tổng riêng với chi tiết giới hạn.
- Thiếu cột P1/P4 trên DB đang dùng62 bảng; báo phiên bản thiếu và vẫn kiểm phần cũ đọc được.
- Kết quả soát lịch/giá hiện hành bị hiểu là sai lịch sử; giữ mức cần đối chiếu, không tự cập nhật.
- Dữ liệu giả đi nhầm DB hoặc chạy lại nhân đôi; chặn tên, khóa tạo và DB phải mới, không chứa snapshot.
- Phải thu khác thực thu khi thanh toán qua tháng; DISTINCT bệnh nhân toàn kỳ không được cộng tháng; kiểm với số mong đợi độc lập.

### Task 1: Audit chỉ đọc

**Files:** `ET/ET_DOISOATDULIEU.cs`, `DAL/DAL_DOISOATDULIEU*.cs`, `BUS/BUS_DOISOATDULIEU.cs`, `tools/DoiSoatDuLieu.ps1`, `tools/DataReconciliation.cs.txt`, `tools/KiemTraDoiSoatP5.ps1`, `tools/P5Checks.cs.txt`.

**Interfaces:** `BUS_DOISOATDULIEU(IDbTransaction? transaction=null).KiemTra(int gioiHanMoiQuyTac=200)` trả `KetQuaDoiSoatDuLieu`; mỗi quy tắc có tổng số, chi tiết PK, mức độ và cờ giới hạn. Không nhận SQL tự do.

- [x] Viết kiểm RED qua SQL thật: audit tồn tại, không ghi; thiếu mã bệnh, ngày sai, FK ghép/NULL, danh mục hiện tại, giới hạn chi tiết.
- [x] Thêm số dòng từng bảng, metadata ràng buộc/trigger, FK mồ côi, hồ sơ/thời gian, bác sĩ/ca, tình trạng, hóa đơn/thu, tồn và nguồn thuốc; kiểm schema cũ và quyền db_datareader.
- [x] Thêm xuất có kiểm tên/đường dẫn, không ghi đè; CSV chặn công thức; chạy GREEN, audit DB đang dùng chỉ đọc và lưu bảng đối soát.

### Task 2: Bộ trình diễn và báo cáo ba kỳ

**Files:** `tools/TaoDuLieuTrinhDien.ps1`, `tools/P5DemoData.cs.txt`, `tools/P5DemoEntry.cs.txt`, bộ kiểm Task1.

- [x] Viết kiểm RED cho bộ dữ liệu mới có26 lượt/18 người qua08–10,8→12→6 lượt,13 lượt dịch vụ/13 đơn thuốc; mã tình trạng không trùng trên lượt.
- [x] Tạo marker/manifest, mã DEMO và nhập kho100 đơn vị mỗi tháng; mỗi đơn2 đơn vị; xuất thực gắn đơn/lượt. Dùng DAL tạo26 hóa đơn,150000 hoặc100000; chuyển trạng thái qua trigger.
- [x] Thu khoản cuối tháng vào tháng sau để phải thu1m/1.5m/750k khác thu900k/1.5m/750k;100k thu tháng11. Không có năm trước giả.
- [x] Đối chiếu SQL độc lập với BUS báo cáo, MoM50%/-50%, toàn kỳ18 bệnh nhân,3 tình trạng; kho nhập300/xuất26/tồn274; render WPF và xuất bảng nguồn từng lượt/CSV.

### Task 3: Hồi quy và bàn giao

**Files:** `docs/sprint3/KET_QUA_DOI_SOAT_P5_20261108.md`, `docs/sprint3/evidence/20261108-p5/`, `README.md`, kế hoạch tiến độ.

- [x] Chạy kiểm P5, P0–P4 phù hợp, báo cáo, build Debug/Release và schema; review độc lập và xử lý finding.
- [x] Ghi PASS/FAIL/chưa kiểm; audit cũ không tự sửa; cách tạo/mở DEMO và số đối chiếu; P5 xong, P6 còn chờ nghiệm thu tổng thể/UNC.

## Execution Ledger

- Ruling: tiếp tục inline theo ủy quyền hiện có; không lặp lại hỏi duyệt thiết kế/phương pháp, không tạo worktree/commit.
- Pre-flight: Task2 dùng DTO audit và schema64 của Task1/P4; DB thật62 thiếu P1/P4 nên các kiểm mới phải có nhánh báo thiếu schema. Task3 chỉ chốt khi bằng chứng đủ.
- Task1 complete: reader/FK ghép/NULL/giới hạn/không tự gán mã/CSV; audit62 bảng hiện hữu chỉ đọc,0 sai xác định và25 phát hiện cần đối chiếu (hai quy tắc cho cùng11 lượt,3 mục schema).
- Task2 complete: DB DEMO chính `RHM_Hospital_Test_P5Demo_20261108_125355`; đúng18/26,8→12→6 và nguồn tiền/kho; UI/CSV có nhãn DEMO theo SQL có kiểm phiên/quyền. Không thêm DDL sản phẩm.
- Review rulings: tính số dư tiền/tồn theo lịch sử, gộp timestamp; đối soát cả nguồn thuốc chờ cấp/đã xuất, chỉ cho thuốc chờ cấp ở hóa đơn Nháp, giữ giá chốt. Các ca lỗi RED14/4 → GREEN21/0; SQL chặn ký lượt trước tiếp nhận nên kiểm trường hợp này là từ chối tại SQL.
- Tool rulings: scratch GUID riêng +mutex build, chuẩn bị output trước SQL, marker INCOMPLETE; read-only recovery nếu lỗi output sau commit. Tạo đồng thời haiDB riêng thành công; guard7/7; export giữ fingerprint64/64. Recovery sau hoàn đếm26 lượt/27dòng và tách ExpectedSeed/Actual.
- Task3 complete: hồi quy35/32/28/47/32/41 tương ứng nền tảng/đồng bộ/tiếp nhận/khám/ảnh/báo cáo đều PASS;21 ca P5,7 guard; build Debug/Release0 lỗi (Release122 cảnh báo cũ); review đã đóng finding. Bằng chứng `docs/sprint3/evidence/20261108-p5/`, kết quả `docs/sprint3/KET_QUA_DOI_SOAT_P5_20261108.md`.
