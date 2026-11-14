# Bằng chứng rà soát dữ liệu 12/11/2026

- `audit.json`: kết quả bộ đối soát hiện có, 64 bảng, 0 mục SaiXacDinh và 22 mục CanDoiChieu (11 lượt thiếu mã tình trạng được phản ánh qua hai quy tắc).
- `table-counts.csv`: số dòng của từng bảng; tổng 911.
- `workflow-findings.txt`: kết quả SQL bổ sung chỉ đọc; số mục là theo quy tắc và có thể trùng hồ sơ. CanBoSung là thiếu dữ liệu để sử dụng chức năng, không phải vi phạm khóa ngoại.
- `backup-before.txt`: backup COPY_ONLY/CHECKSUM của database chính và kết quả RESTORE VERIFYONLY hợp lệ; không phải bản backup nhị phân.
- `snapshot-manifest.json`: số dòng và SHA256 từng bảng nguồn, dùng đối chiếu cài mới/chạy lại.
- `identity-before-fix.txt`: lỗi 544 khi nạp nhật ký identity bằng exporter cũ.
- `fresh-install.txt`: cài mới thành công sau sửa trên database thử, kết thúc bằng PROJECT_SCHEMA_READY.
- `repeat-install.txt`: chạy lại toàn bộ file SQL trên cùng bản thử thành công.
- `fixture-cleanup.txt`: hai database thử đã được dọn, database chính vẫn tồn tại.

Đã chạy `SqlSnapshot.dll ... --verify` sau cài mới, sau chạy lại, và với database chính sau đối soát. Cả ba lần trả mã thoát 0 và kết quả `PASS: all 64 tables match row counts and SHA256 fingerprints.` Các lần đối chiếu so với cùng manifest, không chỉ kiểm số bảng.

Đã chạy `tools/KiemTraBanGiao.ps1 -ChiCauTruc -KiemTraSql` thành công: bốn project, các hợp đồng HospitalWorkflow3/PrescriptionFlow1/SqlV9/BaoTriSerial/Sprint3Foundation1/XrayStorage1 và ràng buộc schema64. Không chạy nghiệp vụ ghi hoặc kiểm giao diện WPF trên dữ liệu chính.

Các kiểm tra này xác minh tính toàn vẹn bản xuất và những điều kiện kỹ thuật đã nêu. Chúng không xác nhận tính xác thực y khoa/tài chính của hồ sơ cũ và không có nghĩa công việc chuẩn hóa/bổ sung dữ liệu đã hoàn tất. Xem [báo cáo và phần còn chờ](../../RA_SOAT_DU_LIEU_20261112.md).
