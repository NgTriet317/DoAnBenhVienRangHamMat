# Bằng chứng P6 — 10/11/2026

Lần chạy `20261110_114040_eae3f295`:32 bước PASS,20 tiêu chí PASS,XR04 chờ
hạ tầng. [Biên bản đầy đủ](../../NGHIEM_THU_P6_20261110.md) đối chiếu21 tiêu
chí Word và nêu giới hạn. Chưa cho phép phát hành chỉ dựa vào kết quả nội bộ.

- `acceptance.json`, `matrix.json`, `steps.csv`, `run.log`: kết quả từng bước,
  database thử, thời điểm, mã thoát và điều kiện còn thiếu.
- `source-before.json`, `source-after.json`, `baseline.json`: nguồn biên dịch/
  SQL/công cụ và baseline git; hai snapshot nguồn giống nhau.
- Log từng bước chỉ giữ PASS/FAIL/RESULT, marker và dòng build cần thiết.
  Log gốc cùng SQL/backup nằm ở `obj/P6Evidence/run-20261110_114040_eae3f295`;
  không đưa snapshot SQL/bệnh án hoặc `.bak` vào thư mục này.
- `summary-review-red/green.log` và `stale-error-red/green.log` là bằng chứng
  lỗi đã tái hiện rồi sửa. FAIL trong file `red` là kết quả có chủ đích trước
  sửa; `Msg53999` trong ca ngắt là lỗi mô phỏng cần xuất hiện. Xem kết quả
  cuối và marker bảo toàn dữ liệu sau đó.
- `smb-loopback.json` chứng minh UNC thật **trên một máy**,15 ảnh đúng hash,
  mở13 qua BUS, giữ2 ảnh ẩn. Không phải proof hai máy.
- `manifest.json`, `lineage.csv`, `conditions.csv`, `monthly-finance.csv` chỉ
  chứa bộ DEMO giả18 người/26 lượt/ba tháng và truy nguồn chứng từ.
- `ui/`:17 ảnh WPF dữ liệu giả, gồm đổi bác sĩ, lỗi hủy ca, hóa đơn/phụ thu,
  ảnh X-quang và báo cáo DEMO. Không có ảnh y khoa thật.

`SHA256SUMS.csv` kiểm hash các tệp trong thư mục này (không tự chứa hash của
chính nó). Các đường dẫn tuyệt đối trong JSON chỉ vị trí hiện vật trên máy
kiểm; bản log đã chọn và ảnh được lưu tương đối ngay trong thư mục evidence.
