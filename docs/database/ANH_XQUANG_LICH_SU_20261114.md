# Ảnh X-quang trong lịch sử khám — 14/11/2026

Đã bổ sung bốn tệp panorama có thể mở xem cho bốn lượt có dịch vụ chụp đã thực hiện. Mỗi ảnh có MaLS, MaDVThucHien, khóa tệp GUID, định dạng JPEG, dung lượng và SHA-256 khớp tệp thật. Giữ nguyên tình trạng bệnh, kế hoạch, giá dịch vụ, hóa đơn và trạng thái đã chốt.

| Bệnh nhân | Lịch sử khám | Dịch vụ chụp liên kết |
|---|---|---|
| BN006 — Đặng Thị Hạnh | LS006 | DVTH006 — Chụp X-quang hàm mặt |
| BN028 — Hoàng Thiên Ân | LS017 | DVTH014 — Chụp X-quang toàn cảnh |
| BN034 — Võ Kim Ngân | LS023 | DVTH024 — Chụp X-quang toàn cảnh |
| BN046 — Hoàng Thảo Nhi | LS035 | DVTH048 — Chụp X-quang toàn cảnh |

Cả bốn lượt có tình trạng chấn thương phần mềm hàm mặt DMTT011 và dịch vụ chụp đã ghi nhận. Việc bổ sung ảnh dựa trên dịch vụ đã được chỉ định/thực hiện, không mặc định mọi chấn thương phần mềm đều phải chụp phim. Những lượt không có dịch vụ chụp không được gắn ảnh tùy tiện.

## Xem trong phần mềm

Mở **Khám và điều trị → Lịch sử khám**, tìm bệnh nhân hoặc mã lượt trong bảng trên, mở chi tiết rồi chọn **Ảnh X-quang**. Danh sách tự chọn ảnh đầu tiên; bấm **Xem lớn / phóng thu** để mở ảnh. Ô dịch vụ ở lượt chỉ xem phản ánh dịch vụ thật của ảnh đang chọn. Lịch sử đã chốt không cho tải thêm hoặc ẩn ảnh; quyền xem vẫn kiểm lại trên SQL và mỗi lần mở được ghi nhật ký.

Đăng nhập admin hoặc bác sĩ có quyền trên hồ sơ tương ứng. Tài khoản thu ngân/kho không được cấp thêm quyền xem ảnh qua thay đổi này.

## Nguồn và giới hạn dữ liệu

Đây là **ảnh X-quang tham khảo công khai** minh họa bộ hồ sơ giả lập của đồ án, **không phải phim chụp của những bệnh nhân trong bảng**. Ghi chú ảnh hiển thị thông tin này, tác giả, nguồn và giấy phép. Không dùng ảnh tham khảo để suy diễn hoặc sửa kết luận chẩn đoán của các hồ sơ. [Nguồn và giấy phép từng ảnh](../../Database/AnhXquang/README.md), [manifest dung lượng/hash/liên kết](../../Database/AnhXquang/manifest.json).

Ngày tạo metadata là ngày nhập bổ sung; NV001/TK001 là tài khoản quản trị nhập bộ ảnh. Nhật ký dùng hành động `XR_REFERENCE_IMPORT` cùng thời gian UTC và ghi chú nguồn, không giả ngày chụp hoặc người chụp. Lượt LS006 trước đây chỉ có câu kết quả dạng văn bản; nay có thêm tệp và metadata thực tế để mở xem.

## Cài mới và database hiện có

SQL chính đã chứa bốn bản ghi ảnh cho **cài mới**. Build và publish tự chép JPG vào `DuLieu/AnhXquang` cạnh exe và tài liệu nguồn vào `DuLieu/NguonAnhXquang`. Kho vận hành chỉ chứa tệp ảnh, không chứa manifest/tài liệu. Cấu hình bản đồ án dùng đường dẫn tương đối, cho phép kho cục bộ; không phụ thuộc thư mục làm việc hoặc ổ đĩa của người phát triển. Chép nguyên thư mục phát hành khi chuyển máy.

Chạy lại SQL trên database đã có bảng vẫn giữ dữ liệu hiện tại và không nạp đè snapshot. Với một bản database cũ chưa có ảnh, sao lưu rồi dùng:

```powershell
# Chỉ kiểm hash, bệnh nhân, tình trạng, dịch vụ; chưa ghi dữ liệu:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/BoSungAnhXquang.ps1 -MayChu .
# Nhập metadata/nhật ký và chép JPG vào kho của bản PhatHanh:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/BoSungAnhXquang.ps1 -MayChu . -ApDung
```

Công cụ chỉ nhận database đồ án hoặc tên thử `RHM_Hospital_Test_*`. Bất kỳ liên kết sai, hash sai hoặc khóa tệp trùng nhưng nội dung khác đều dừng, không ghi đè. Kiểm trước tất cả liên kết trong cùng giao dịch; nhập lại giữ metadata/ghi chú/trạng thái đã có, không tạo thêm nhật ký nhập. Kho khác truyền `-ThuMucKho` và cấu hình `XQuang` cạnh exe trỏ tới đúng kho đó. Với kho mạng nhiều máy, dùng UNC và `ChoPhepDuongDanCucBo=false`; `RHM_XRAY_ROOT` vẫn ưu tiên hơn file cấu hình.

Database trên máy đã được sao lưu COPY_ONLY/CHECKSUM vào `obj/XrayHistoryDataChecks/QLBenhVienRangHamMat_before_xray_20261114_192136.bak` trước khi nhập. Snapshot được xuất lại từ database này; các bảng lâm sàng và tài chính giữ nguyên fingerprint so với mốc kiểm tra 14/11 trước bổ sung. Khác biệt nằm ở ảnh và nhật ký/phiên được giữ từ database đang dùng.

## Kiểm chứng

`tools/KiemTraDuLieuAnhXquang.ps1` kiểm đường dẫn theo thư mục exe, UNC/biến môi trường, đường dẫn hỏng, hash và giải mã bốn JPG. Thêm `-Database RHM_Hospital_Test_<ten> -MayChu .` để kiểm trên database thử: BUS đọc ảnh đúng bệnh nhân/dịch vụ, WPF hiển thị ảnh xem trước và ảnh gốc, nguồn ảnh vẫn hiện khi xem lớn, dịch vụ hiển thị đúng, nút tải/ẩn bị khóa. Kiểm cả admin/bác sĩ/tiếp nhận được phép xem, phạm vi kỹ thuật viên và chặn thu ngân. Thêm `-ThuMucKho PhatHanh/DuLieu/AnhXquang` để kiểm kho được đóng gói và đối soát không còn tệp ngoài metadata. Công cụ lưu ảnh chụp giao diện tại `obj/XrayHistoryDataChecks`.

Đã kiểm cài database thử từ SQL sau cập nhật và đối chiếu **64/64 bảng** khớp số dòng/SHA-256 với snapshot xuất từ database chính. Nhập lần hai thêm **0 ảnh, 0 nhật ký nhập**; tổng vẫn bốn ảnh và bốn nhật ký nhập.

Bộ kiểm X-quang hiện có đạt **34/34**, DICOM đạt **27/27**; build Debug/Release và kiểm schema SQL đạt. Release còn các cảnh báo có sẵn của dự án. Bản phát hành đã được cập nhật DLL, SQL, ảnh và tài liệu nguồn, đồng thời giữ cấu hình kết nối SQL cũ. [Minh chứng kiểm và fingerprint snapshot](evidence/20261114-xray-history/README.md).
