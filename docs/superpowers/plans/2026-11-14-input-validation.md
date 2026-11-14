# Input Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Chặn dữ liệu sai và hiện lỗi cùng gợi ý ngay dưới các trường thuộc phạm vi người dùng yêu cầu.
**Architecture:** Giữ validator metadata và mẫu lỗi hiện có; nối các form thực tế với validator, bổ sung ràng buộc liên trường và giữ kiểm tra BUS/SQL.
**Tech Stack:** WPF .NET 8, C#, PowerShell.
**Spec:** `docs/superpowers/specs/2026-11-14-input-validation-design.md`.

## Global Constraints

- Không thay đổi dữ liệu SQL, không cắt giá trị sai, không tự áp giới hạn y khoa.
- Trường tùy chọn được để trống; trường chỉ đọc không bị bắt nhập.
- Nội dung lỗi và cách sửa bằng tiếng Việt, nằm dưới chính ô nhập.
- Giữ nguyên thay đổi sẵn có trong workspace; không commit hoặc reset thay đổi người dùng.

## Review Focus

- Ngày gõ sai không được dùng lại SelectedDate cũ.
- Số có dấu phân cách hàng nghìn không được đọc thành giá trị khác ý định.
- Lỗi ở tab chưa mở phải chặn lưu và chỉ ra trường tương ứng.
- Form danh mục sinh động phải hiển thị lỗi dưới ô và kiểm tra khóa nhập tay.
- Sửa một ô không được xóa lỗi chưa sửa ở ô khác.

### Task 1: Validator chung và hồ sơ bệnh nhân

- [x] Viết/chạy bộ kiểm `tools/InputValidationChecks.cs.txt` qua `tools/KiemTraNhapLieu.ps1`, xác nhận các ca thiếu hiện thất bại.
- [x] Sửa `Controls/KiemTraNhapLieu.cs`, `Themes/KieuTruongNhapLieu.xaml`, `Controls/BieuMauHoSo.cs` để kiểm tra đầy đủ trường nhập, lựa chọn, chữ/số/ngày và gợi ý sửa; xử lý tab ẩn.
- [x] Chạy lại bộ kiểm và build.

### Task 2: Khám điều trị và kê thuốc

- [x] Kiểm tra form thực tế, bổ sung ca hồi quy độc lập cho ngày/số/điều kiện cần nhập.
- [x] Nối `ClinicalFormInput` và các editor vào lỗi tại trường; kiểm tra trước lưu, giữ các ràng buộc BUS.
- [x] Kiểm chứng các ca hợp lệ và sai, báo rõ kết quả.

### Task 3: Báo cáo và danh mục hệ thống

- [x] Viết/chạy ca hồi quy bộ lọc và form sinh động.
- [x] Nối báo lỗi dưới trường, chặn ngày/năm/ngưỡng sai trước tải báo cáo; kiểm tra danh mục trước lưu.
- [x] Chạy ca hồi quy và build.

### Task 4: Bảo trì, nhà cung cấp và thanh toán

- [x] Rà soát trường nhập và ca hồi quy cho tiền/ngày/thông tin liên hệ.
- [x] Nối lỗi vào trường, bổ sung quan hệ ngày và mức thu/hoàn theo số dư; giữ giao dịch hiện có.
- [x] Chạy kiểm chứng, tổng hợp những giới hạn chưa thể kiểm trực tiếp.

### Task 5: Kiểm chứng tích hợp

- [x] Chạy bộ kiểm nhập liệu, build solution và bộ kiểm cấu trúc; rà diff cuối cùng.
- [x] Cập nhật tài liệu phạm vi, lệnh chạy và kết quả kiểm thử thực tế.

## Kết quả thực hiện

Đã kiểm chứng 101/101 ca nhập liệu, 17/17 ca hồi quy báo cáo, kiểm cấu trúc và build Debug/Release. Chi tiết phạm vi và giới hạn: [Kiểm tra nhập liệu](../../KIEM_TRA_NHAP_LIEU_20261114.md).

Ruling: giữ workspace hiện tại và không commit vì có nhiều thay đổi sẵn có của người dùng. Mô tả tình trạng vẫn tùy chọn, kiểm tra đầu ra sau quy tắc dùng tên làm mô tả. Thêm validator tại control cho các quy tắc phụ thuộc ngữ cảnh để việc sửa chữ không xóa lỗi nghiệp vụ còn hiệu lực. Các bộ lọc tìm kiếm tự do và màn hình chỉ đọc không bị thêm quy tắc bắt nhập. Không kiểm thử ghi trên SQL thực tế trong lượt này.
