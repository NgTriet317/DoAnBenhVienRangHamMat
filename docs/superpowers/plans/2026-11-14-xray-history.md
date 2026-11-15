# X-ray History Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task in the current workspace requested by the user.

**Goal:** Bốn lượt khám có chỉ định X-quang mở được ảnh tham khảo, đồng bộ database, SQL và bản chạy.

**Architecture:** Giữ BUS_ANHXQUANG và kho tệp hiện có. Đóng gói tệp được cấp phép bằng WPF Content; cấu hình kho cục bộ tương đối giải theo thư mục ứng dụng. Công cụ nhập kiểm hash và quan hệ trước khi thêm metadata cùng nhật ký trong giao dịch; xuất snapshot bằng công cụ sẵn có.

**Tech Stack:** PowerShell, SQL Server 2016 SP1+, C#/.NET 8/WPF.

**Spec:** docs/superpowers/specs/2026-11-14-xray-history-design.md

## Global Constraints

- Không sửa chẩn đoán/chứng từ đã chốt, tắt trigger hoặc ghi đè ảnh đang có.
- Ghi rõ nguồn và tính chất ảnh tham khảo trên mỗi ảnh.
- Giữ ưu tiên cấu hình kho dùng chung/RHM_XRAY_ROOT.

## Review Focus

- Chuyển thư mục chạy: đường dẫn tương đối dựa vào AppContext.BaseDirectory.
- Cấu hình mạng: không tự bật quyền dùng kho cục bộ.
- Nhập lại: không nhân bản metadata/nhật ký.
- Tệp hoặc quan hệ sai: dừng trước khi commit.
- Lượt lịch sử: mở được ảnh, không tải hoặc ẩn ảnh.

### Task 1: Đóng gói ảnh và cấu hình kho có thể chuyển máy

- [x] Viết và chạy kiểm tra đường dẫn tương đối, UNC và biến môi trường; xác nhận lỗi trước sửa.
- [x] Thêm bốn ảnh có nguồn/giấy phép, manifest và liên kết Content trong WPF.
- [x] Sửa CauHinhKhoAnh.Doc để giải đường dẫn tương đối khi ChoPhepDuongDanCucBo=true; cấu hình đồ án dùng DuLieu/AnhXquang.
- [x] Kiểm tra hash, định dạng, kích thước và đọc ảnh qua KhoLuuAnhXquang.

### Task 2: Nhập dữ liệu và kiểm xuyên SQL/BUS/WPF

- [x] Viết kiểm tra bốn lượt có ảnh đúng bệnh nhân/dịch vụ và lịch sử chỉ xem.
- [x] Thêm công cụ nhập kiểm trước, giao dịch và idempotence; sao lưu rồi áp dụng database hiện tại.
- [x] Xuất snapshot SQL, cập nhật tài liệu và bản PhatHanh.
- [x] Cài database thử từ SQL chính, kiểm BUS/WPF và nhập lại không tạo bản sao; chạy các bộ kiểm liên quan.

## Kết quả và quyết định triển khai

- Làm trực tiếp trong workspace được người dùng chỉ định để thay đổi SQL, bộ ảnh và bản chạy cùng được bàn giao; không tạo nhánh hoặc commit.
- Ảnh là phim công khai có giấy phép, ghi rõ tham khảo tại thumbnail và cửa sổ xem lớn. Không tạo ảnh y khoa bằng AI hoặc giả nguồn bệnh án thật.
- Kiểm đỏ trước sửa: đường dẫn tương đối chưa được giải; dữ liệu thiếu ảnh; giao diện lịch sử chọn sai dịch vụ. Sau sửa, kiểm SQL/BUS/WPF đạt trên bốn lượt.
- Rà soát độc lập phát hiện hai lỗi quan trọng (dịch vụ trống với người đọc lịch sử hợp lệ, mất ghi chú nguồn khi xem lớn) và lỗi cấu hình đường dẫn hỏng. Đã tái hiện bằng kiểm đỏ, sửa và kiểm xanh. Bổ sung kiểm bác sĩ, tiếp nhận, kỹ thuật viên và chặn thu ngân.
- X-quang hiện có: 34/34 đạt. DICOM: 27/27 đạt. Build Debug/Release và kiểm schema SQL đạt; Release còn 122 cảnh báo có sẵn ngoài phạm vi.
- Cài mới từ SQL: 64/64 bảng khớp SHA-256/số dòng. Nhập lại thêm 0 ảnh/nhật ký. Sai hash và thiếu quan hệ bị chặn, dữ liệu/tệp cũ giữ nguyên.
- Đã publish PhatHanh, giữ ConnectionStrings cũ; SQL được chép tự động và bốn JPG khớp hash. Kho bản phát hành đối soát sạch và mở được qua BUS/WPF.
