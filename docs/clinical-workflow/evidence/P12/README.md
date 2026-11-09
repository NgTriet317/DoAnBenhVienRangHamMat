# Bằng chứng P12

Kết quả có tên ca và exit kiểm chứng được tóm tắt trong results.json/BAN_GIAO_P12.md. Không cộng nhiều lần chạy thành số ca mới. Các log RED được giữ có chủ đích: review-fix-red0/4 và reception-red4/1; kết quả cuối fix-final5/5. recovery-backup-restore-attempt có backup/verify/restore thành công rồi phép so sánh JSON bảng trống ban đầu báo lỗi; recovery-verified sau chuẩn hóa NULL→[] xác minh CHECKDB và hash52 bảng khớp. Không có lỗi dữ liệu cần bỏ qua.

Source harness phát triển ở Git3f77526; khi phục hồi SchemaChecks phải phục hồi SQL46 bảng từ Git144c901:Database/QLBenhVienRangHamMat.sql vào obj/ClinicalWorkflowP12/baseline-v8.sql. Source ca sửa review/tiếp nhận giữ dạng ReviewFixChecks.cs.txt cho audit; không là project thứ năm và không được app compile. Cổng chạy bản hiện tại là KiemTra.bat/tools/KiemTraBanGiao.ps1.

Sáu database fixture đã được dọn sau bất biến/CHECKDB; database-cleanup-final xác nhận DB_ID=NULL, original/main vẫn tồn tại. Không xóa journal chống lặp trong database đang dùng. File backup recovery COPY_ONLY ở thư mục Backup của SQL Server được giữ ngoài source.