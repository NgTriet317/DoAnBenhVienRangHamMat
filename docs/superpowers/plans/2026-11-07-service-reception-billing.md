# P2/P3 Service Reception and Billing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Hoàn thiện tiếp nhận khám dịch vụ và phụ thu từ lưu phiếu đến tổng kết, thu tiền và in hóa đơn.

**Architecture:** Dùng schema P1 hiện có, giữ bốn tầng và một file SQL triển khai. Yêu cầu chỉ gửi loại khám, DAL cấp mức phí; các nhánh tài chính cùng đọc nguồn phụ thu từ phiếu. UI chỉ bật luồng dịch vụ khi toàn bộ đường thu phí đã được nối.

**Tech Stack:** WPF/.NET 8, SQL Server, Dapper, PowerShell; không thêm dependency.

**Spec:** `docs/sprint3/KE_HOACH_TRIEN_KHAI_20261107.md` và mục 3/4/9 trong `KeHoachTrienKhai.docx`. Người dùng đã cho phép tiếp tục thực hiện, điều chỉnh phù hợp dự án.

## Global Constraints

- Giữ nguyên nhánh `Sprint3_tan`, thay đổi P0/P1 và việc người dùng đã bỏ package fo-dicom.Desktop trong bốn csproj.
- Khám dịch vụ chỉ cho đến trực tiếp, phí 30000/lượt, không có tham số giá từ UI.
- Bác sĩ phải có ca xác nhận hiện tại, Có mặt, không nghỉ, có phòng hợp lệ; dịch vụ cùng MaCK và NgayApDung không ở tương lai.
- Thông thường/lịch hẹn giữ hành vi cũ, gồm điều phối theo quyền; không tự nâng loại phiếu đang có.
- Không commit/push hoặc cập nhật DB ứng dụng; fixture chỉ ghi DB `RHM_Hospital_Test_...`.

## Review Focus

- Đổi nguồn/loại/bác sĩ phải xóa lựa chọn phụ thuộc; lịch hẹn không giữ loại dịch vụ cũ.
- Ca bị hủy hoặc tài khoản khóa sau khi UI chọn phải bị DAL từ chối, không tạo hồ sơ/phiếu mồ côi.
- Retry phiếu và hóa đơn không nhân phí; phải so loại khám.
- Tổng kết, bảng phí, hash đối soát, thuốc theo lô và bản in cùng tính đúng một phụ thu.
- Hai đường hóa đơn, giảm giá tối đa, thu/hoàn và luồng không thuốc vẫn hoạt động.

### Task 1: Tiếp nhận tại ET/BUS/DAL

**Files:** `ET/ET_DULIEUQUYTRINHKHAM.cs`, `BUS/BUS_TIEPNHAN.cs`, `DAL/DAL_TIEPNHAN.cs`.
**Interfaces:** `YeuCauTiepNhan.LoaiKham`, `LayBacSiDichVu(NguCanhNghiepVu)`, `LayDichVuTheoBacSi(string,NguCanhNghiepVu)`; kết quả trả loại/phí đã lưu.

- [x] Viết bộ kiểm SQL thực (tái sử dụng fixture lâm sàng cũ), thấy RED cho luồng dịch vụ chưa có.
- [x] Thêm API danh sách ca nghiêm ngặt, dịch vụ theo bác sĩ; xác minh lại trong transaction, lưu loại/phí và audit.
- [x] Kiểm bác sĩ thiếu/hủy ca, ca qua ngày, vắng/nghỉ, phòng, tài khoản; retry so loại; thông thường không phụ thu.

### Task 2: Nguồn phụ thu và tổng tài chính

**Files:** `DAL/DAL_CHITIETHOADON.cs`, `DAL/DAL_HOADON.cs`, `DAL/DAL_THANHTOANTRONGOI*.cs`, `DAL/DAL_KHAMDIEUTRI.ThucHien.cs`, `ET/ET_DULIEUTINHPHI.cs`, `ET/ET_TONGKETKHAM.cs`, `ET/ET_DULIEUBANGIAOTHUOC.cs`.
**Interfaces:** `PhiPhuThu`, `MaPhieuPhuThu` trên bảng phí/tổng kết/bản in; tổng và fingerprint gồm phụ thu.

- [x] Viết ca RED hoàn tất khám có phụ thu, bảng phí/thu/in và nhánh hóa đơn cũ.
- [x] Bổ sung nguồn thứ tư ở hai truy vấn và INSERT chi tiết, kiểm giảm giá cùng SQL, đối soát phí ở cả trước và sau xuất thuốc.
- [x] Kiểm tổng, thu đủ/retry, thuốc theo lô, hóa đơn cũ thu một phần/hoàn; không cộng phí vào thống kê dịch vụ.

### Task 3: Giao diện tiếp nhận, tổng kết và in

**Files:** `RangHamMat.Wpf/Views/TiepNhanKham.xaml(.cs)`, `BangTinhPhiEditor.xaml(.cs)`, `XemTruocHoaDon.xaml.cs`, `Presentation/KhamLamSang.KeThuoc.cs`, helper nhóm dòng in hiện có.

- [x] Thêm kiểm WPF thực cho thứ tự/chọn/reset, dòng phí và bản in.
- [x] Thêm chọn loại chỉ khi trực tiếp; dịch vụ khóa tới khi chọn bác sĩ; phòng lấy từ ca; chặn double-click khi lưu.
- [x] Hiển thị phụ thu riêng ở tổng kết/bảng phí và giữ dòng riêng khi nhóm/in; render kiểm và lưu ảnh bằng chứng.

### Task 4: Hồi quy, review, bàn giao

- [x] Build Debug/Release; chạy kiểm mới và hồi quy nền tảng, lâm sàng, ET/DAL, báo cáo.
- [x] Review độc lập; tái hiện và sửa lỗi quan trọng bằng ca RED/GREEN.
- [x] Cập nhật tiến độ, bằng chứng và hướng dẫn. Giữ P4/P5/P6 tổng thể chưa hoàn tất.
