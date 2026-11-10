# Sprint 3 SQL/ET Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Bắt đầu P0/P1 của kế hoạch Word bằng nền dữ liệu tương thích luồng hiện tại.

**Architecture:** Migration cuối file SQL chính, giữ dữ liệu phục hồi và các trigger nghiệp vụ.
Nguồn thu thứ tư liên kết phiếu; metadata ảnh liên kết lần khám. Chưa bật khám dịch vụ ở UI.

**Tech Stack:** WPF/.NET 8, Dapper, SQL Server 2016 SP1+, PowerShell/sqlcmd.

**Spec:** `docs/sprint3/KE_HOACH_TRIEN_KHAI_20261107.md`, mục 3–6 và P0/P1 trong `KeHoachTrienKhai.docx`.

## Global Constraints

- Chỉ sửa `Database/QLBenhVienRangHamMat.sql` cho triển khai database.
- Thông thường: phí 0. Dịch vụ: 30.000đ; dữ liệu cũ không phát sinh phụ thu.
- Không đổi số tiền/chẩn đoán hoặc tắt trigger để qua kiểm thử.
- Chỉ tạo/ghi fixture trên DB tên bắt đầu `RHM_Hospital_Test_`.
- Giữ bốn project sản phẩm; project kiểm sinh trong `obj`.

## Review Focus

- Chạy lại SQL khi đã có dòng phụ thu, không tái dựng CHECK ba nguồn gây lỗi.
- Dòng phụ thu gắn sai lần khám, trùng phiếu, ghép thêm nguồn, giá NULL hoặc sai giá phải bị chặn.
- Giảm giá hoặc đổi nguồn/loại khám không được làm mất phụ thu sau chốt.
- Metadata ảnh không chấp nhận đường dẫn tuyệt đối/đi lùi, MIME sai hoặc đổi MaLS.
- Bản nâng cấp không thay các cột/dòng nghiệp vụ cũ ngoài cột mới mặc định.

### Task 1: Bằng chứng nền và kiểm thử RED

**Files:** `tools/KiemTraNenTangSprint3.ps1`, `tools/Sprint3FoundationChecks.cs.txt`.
**Interfaces:** nhận `MayChu`, `Database` cách ly; sinh console tạm tham chiếu BUS/ET.

- [x] Lưu SHA/hash/bản SQL gốc; build solution.
- [x] Tạo kiểm thực trên SQL: schema mới, mặc định cũ, nguồn phụ thu, khóa hóa đơn, metadata ảnh.
- [x] Cài SQL gốc vào DB cách ly; chạy kiểm mới và thấy thiếu schema/nguồn phụ thu.

### Task 2: Migration nền dữ liệu

**Files:** `Database/QLBenhVienRangHamMat.sql`.
**Interfaces:** `PHIEU_KHAM_BENH.LoaiKham/PhiPhuThuDichVu`,
`CHI_TIET_HOA_DON.MaPhieuPhuThu`, `ANH_XQUANG_LAN_KHAM`,
`usp_RHM_KiemTraNenTangSprint3` chỉ đọc.

- [x] Thêm cột mặc định, CHECK/FK/unique; chặn đổi loại khám/phí sau tiếp nhận.
- [x] Giữ kiểm nguồn cũ; thêm tính phí, bảo vệ giảm giá, kiểm lúc xác nhận/chốt và view.
- [x] Thêm metadata ảnh với FK/index/kiểm đường dẫn, đuôi/MIME/hash/trạng thái và bất biến liên kết.
- [x] Chạy bộ kiểm SQL, sửa đến GREEN; kiểm chạy lại toàn bộ SQL có phụ thu đã lưu.

### Task 3: ET và công cụ kiểm

**Files:** `ET/ET_PHIEUKHAMBENH.cs`, `ET/ET_CHITIETHOADON.cs`,
`ET/ET_ANHXQUANGLANKHAM.cs`, `tools/KiemTraBanGiao.ps1`.
**Interfaces:** ánh xạ đúng tên/kiểu/nullability SQL; constructor cũ còn dùng được.

- [x] Thêm thuộc tính phiếu/nguồn phụ thu và ET bảng ảnh.
- [x] Đồng bộ kiểm bàn giao với bảng mới, không đổi hợp đồng tiếp nhận chưa hỗ trợ.
- [x] Build solution; chạy kiểm ánh xạ ET/DAL và bộ kiểm nền.

### Task 4: Nâng cấp/chạy lại/hồi quy và báo cáo

**Files:** `docs/sprint3/KET_QUA_NEN_TANG_20261107.md`, `README.md`.

- [x] Cài mới từ file chính, nâng cấp DB đã cài SQL gốc, chạy lại có dữ liệu mới.
- [x] So sánh hash/số dòng dữ liệu cũ, CHECK/FK tin cậy; chạy bộ kiểm có sẵn phù hợp.
- [x] Review thay đổi, ghi rõ PASS/FAIL/chưa chạy và P2–P6 còn lại.
- [x] Giữ diff để người dùng xem; chưa commit trước nghiệm thu toàn bộ kế hoạch Word.
