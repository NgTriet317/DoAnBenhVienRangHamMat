# Bằng chứng bộ chạy XR04

Ngày10/11/2026, gói cuối `20261110_120954_a7405046`:17/17 guard và12/12 bước
kiểm gói giải nén. Xem [kết quả đầy đủ](../../KET_QUA_BO_NGHIEM_THU_XR04_20261110.md).

- `package.json`, `source.json`: danh mục/hash213 tệp payload, nguồn485 mục
  và liên hệ với481 mục cũ của P6. Binary/ZIP ở `obj/XR04Handover`, không
  chép vào Git. Đường dẫn trong JSON là vị trí hiện vật trên máy kiểm.
- `integration.json`, `steps.csv`, `xr04-integration.log`:12 bước thực tế;
  ca từ chối mong đợi ghi `ExpectedRejection=true`.
- `proof-A.json` và `proof-B.json`: hai lần Probe thật trên **cùng máy**;
  `same-host-comparison.json` phải FAIL. Không phải bằng chứng hai máy.
- `store/backup/restore/verify.log` và các `*-result.json`: kiểm SQL/kho và
  phục hồi trên một máy qua UNC loopback; verify2/2,15 ảnh toàn vẹn.
- `restored-viewer.png`: ảnh giả tạo bởi fixture, mở/phóng thu từ bản phục hồi.
- `xr04-*-red.log`, `store-red.log`: lỗi đã tái hiện trước sửa, không phải kết
  quả cuối. `xr04-review-green.log`17/17 và lần integration cuối chứng minh sửa.
- `existing.log` có FAIL do cố phục hồi vào DB đã tồn tại; thao tác đã bị
  chặn như mong đợi. Không ghi đè DB/kho.

Không có SQL snapshot, `.bak`, MDF/LDF hoặc ảnh người bệnh thật trong thư mục
này. Hash từng tệp trong `SHA256SUMS.csv`, trừ chính file danh mục hash.
XR04 còn chờ máy thứ hai và phục hồi SQL từ xa; trạng thái phát hành=false.
