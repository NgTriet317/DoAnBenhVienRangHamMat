# Hóa đơn và đơn thuốc Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cải thiện phiếu hóa đơn, chi tiết và phiếu in đơn thuốc, kiểm chứng bằng render thực tế.

**Architecture:** Giữ InvoiceDocument XAML và paginator hiện tại; cải thiện bố cục, ngắt trang và footer. Dùng một bộ dựng FlowDocument cho đơn thuốc hiện hành, dữ liệu cũ và bản in. XAML giữ phần khung xem trước và chi tiết.

**Tech Stack:** C#, WPF .NET 8, XAML, FlowDocument, DocumentPaginator.

**Spec:** docs/superpowers/specs/2026-11-07-chung-tu-hoa-don-don-thuoc-design.md

## Global Constraints

- Không thêm package, không sửa SQL/BUS/DAL hoặc quy tắc nghiệp vụ.
- Giữ chữ và tiền nguồn, trạng thái và kiểm tra quyền khi in.
- Làm trực tiếp workspace hiện có; không commit, đổi nhánh hoặc đụng thay đổi không liên quan.

## Review Focus

- Hướng dẫn thuốc dài: đầy đủ nội dung và tiếp tục sang trang sau.
- Hóa đơn dài: đủ mọi dòng và tổng cuối, không cắt chữ.
- Đơn lịch sử thiếu snapshot: không lấy tên hiện tại thay dữ liệu lịch sử.
- Chi tiết trong vùng hẹp: đọc được hướng dẫn, thao tác bàn giao vẫn truy cập được.
- Lỗi in hoặc nguồn đổi mã: chặn in và giữ nguyên nghiệp vụ.

### Task 1: Hóa đơn

**Files:** Views/InvoiceDocument.xaml và .xaml.cs, Views/XemTruocHoaDon.xaml và .xaml.cs trong RangHamMat.Wpf.

**Interfaces:** Giữ `InvoiceDocument.CreatePaginator(Size)` và `XemTruocHoaDon.TryPrint(Action<DocumentPaginator>, Size, out string?)`.

- [x] Chỉnh đầu chứng từ, thông tin chia hai cột, bảng có số thứ tự, tổng tiền theo khối, chữ ký, khung xem trước.
- [x] Render hóa đơn ngắn/dài, kiểm mọi trang có kích thước hợp lệ, số dòng và tổng tiền đầy đủ.

### Task 2: Đơn thuốc và chi tiết

**Files:** Presentation/DonThuocDocument.cs; Views/XemTruocDonThuoc.xaml và .xaml.cs; Views/PrescriptionHistoryDetail.xaml và .xaml.cs; Views/ChiTietDonThuoc.xaml.

**Interfaces:** Giữ `DonThuocDocument.Tao(BanInDonThuoc)` và `Tao(RxCard)`; bổ sung `TaoLichSu(DongLichSuDon, DonThuocChiTiet)` để dùng dữ liệu lịch sử đã được kiểm tra quyền. Giữ các control x:Name và sự kiện bàn giao.

- [x] Thống nhất các nguồn tài liệu, cải thiện hệ chữ và khối thuốc; khung xem trước khai báo XAML.
- [x] Cải thiện chi tiết đơn thuốc hiện hành và bố cục cũ, tự giãn hướng dẫn dài.
- [x] Kiểm in đơn đúng nguồn và chặn nguồn đổi mã/trạng thái; render ngắn/dài, lịch sử thiếu snapshot và vùng hẹp.

### Task 3: Kiểm chứng

**Files:** tools/KiemTraChungTu.ps1; tools/DocumentChecks.cs.txt; evidence tại obj/DocumentChecks/evidence.

- [x] Build WPF Release và chạy công cụ kiểm chứng không cần SQL, không máy in thật.
- [x] Xem ảnh hóa đơn, đơn thuốc, cửa sổ xem trước và chi tiết; sửa vấn đề tràn/cắt nếu có.
- [x] Rà diff, kiểm các thay đổi có sẵn không bị ghi đè; báo kết quả và giới hạn in thực tế.

## Kết quả kiểm chứng

- Build solution Debug và Release với --no-restore, NuGetAudit=false: thành công.
- powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBanGiao.ps1 -ChiCauTruc: đạt.
- powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraChungTu.ps1: 13/13 đạt.
- Đã xem ảnh A4, trang cuối hóa đơn/đơn dài, bản xem trước cuộn tới cuối, chi tiết 740px và 360px, bàn giao mở/thu gọn.
- Review độc lập phát hiện kiểm đếm chữ XPS ban đầu chưa lọc vùng bị cắt. Đã tái hiện lỗi bằng kiểm mỗi marker xuất hiện đúng một lần, rồi lọc theo RenderTransform, Clip và VisualBrush.Viewbox; toàn bộ 48 dòng và mô tả dài đạt, tổng tiền/chữ ký giữ trọn khối.
- Review chỉ ra nhánh RxCard cũ có thể dùng danh mục hiện tại khi thiếu snapshot. Đã tái hiện cho cả DTO lịch sử và bản ghi đơn cũ, rồi sửa bộ dựng bản in chỉ dùng snapshot hoặc mã thuốc, không đoán tên/đơn vị.
- Không thay dữ liệu SQL, không gửi máy in thật. Bộ kiểm bố cục dùng XAML thật nhưng tháo sự kiện để render độc lập; không thay thế kiểm thử tích hợp quyền/bàn giao với SQL thật.

## Quyết định triển khai

- Giữ nhánh Sprint3_tan và workspace hiện có, không commit/push; các thay đổi khác được giữ nguyên.
- Bản in đơn thuốc dùng FlowDocument riêng để kích thước giấy của máy in không làm thay đổi bố cục đang xem.
- Bàn giao chuyển thành vùng có thể mở/thu gọn để vùng đọc thuốc còn đủ chỗ ở cửa sổ hẹp; giữ các điều kiện và sự kiện nghiệp vụ.
