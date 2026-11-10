# Bằng chứng đồng bộ SQL

Xem [kết quả và cách áp dụng](../../DONG_BO_DATABASE_20261110.md).

- `sql-change.json`: hash file trước/sau, snapshot không đổi, chưa nâng cấp
  DB đang vận hành. Không đưa bản SQL/snapshot hay `.bak` vào evidence.
- Các `*-red.log`: ca hồi quy trước sửa, FAIL có chủ đích; kết quả nền tảng
  cuối ở `sql-project-green-final.log` là44/44.
- `audit-before-fix.json` và `audit-after-fix.json`: cùng tình huống index
  audit không tạo được do hai sự kiện UTC giả trùng, READY từ True→False,
  dữ liệu UTC được giữ nguyên. PASS ở log đỏ nghĩa là tái hiện đúng lỗi.
- `phase-guards.json`, `sql-project-guards-final.log`: lỗi mô phỏng P1/P4
  khi client tiếp tục GO, không in READY, không dùng cờ thành công cũ; chạy
  lại đầy đủ sửa được DB thử.
- `verify-audit-failure.ps1`, `verify-phase-interruption.ps1`: script kiểm
  bổ sung, chỉ nhận DB thử. Chạy từ gốc repository, truyền một DB thử64 bảng
  dành riêng, đã cài SQL chính. Mặc định script audit tái hiện bản cũ;
  dùng `-ExpectBlocked` với bản SQL mới để xác minh guard đã sửa. Script
  chèn/dọn đúng fixture audit và chạy lại SQL trong DB thử sau kiểm.
- `acceptance.json`, `matrix.json`, `source-before/after.json`, `steps.csv`
  và `run.log`: lần cuối `20261110_124022_78d39f69`,32/32 bước,485 mục nguồn
  ổn định,20 tiêu chí PASS và XR04 chờ hạ tầng. Log đầy đủ/kho ảnh/backup
  vẫn nằm trong obj. `ReadyForDeployment` không dùng để khẳng định đã kiểm
  máy thứ hai.

Danh mục hash cuối ở `SHA256SUMS.csv`, trừ chính file danh mục.
