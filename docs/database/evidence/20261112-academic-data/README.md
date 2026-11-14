# Bằng chứng chuẩn hóa và mở rộng bộ đồ án

Người dùng xác nhận hoàn toàn là dữ liệu đồ án và cho phép tự chuẩn hóa/bổ sung. Báo cáo sử dụng: [DU_LIEU_DO_AN_20261112.md](../../DU_LIEU_DO_AN_20261112.md).

## Kết quả cuối

- Database chính: 64 bảng/3.221 dòng, marker AcademicData.Version=1.
- `audit.json`: 0 sai xác định,0 cần đối chiếu theo bộ đối soát đầy đủ; `findings.json`: không còn lỗi bổ sung về bản ký, đường dùng, nguồn mua, kho vật tư, thời gian lịch sử hoặc trùng lịch.
- `api-checks.json`: 21 đơn ký in được,18 bàn giao đọc đúng nguồn,6 hóa đơn chờ thu tiếp được; thử thu/giao/retry rollback.
- Cài mới toàn bộ SQL chính rồi chạy lặp: cả 64 bảng khớp số dòng và SHA256 với `snapshot-manifest.json` của database chính.
- Chạy lại công cụ trên bộ đã chuẩn hóa: cả 64 bảng giữ SHA256, không thêm hoặc hồi trạng thái.
- `continuation-probe.cs.txt`: chỉ nhận fixture riêng; commit một bàn giao DT015 và một thanh toán HD012. Sau đó chạy lại cả công cụ và SQL chính: 64 bảng vẫn khớp `after-user-actions-manifest.json` (3.227 dòng của fixture đã có thao tác tiếp diễn), không quay về dữ liệu ban đầu. `after-user-actions-api.json` có 19 bàn giao hợp lệ/5 hóa đơn còn chờ thu. Database chính giữ 3.221 dòng/6 ca chờ giao/6 ca chờ thu.
- Thử thiếu HD010 ở fixture riêng: công cụ báo đúng lỗi 59963 sau khi đã thêm danh mục và tạm tắt trigger để vào pha legacy. Rollback trả về đúng SHA256 cả 64 bảng so với `rollback-before-manifest.json` (910 dòng vì neo bị xóa có chủ đích); trigger bị tắt=0,bệnh nhân DL26 thêm dở=0,marker thành công giả=0.
- Build Debug/Release: 0 lỗi; còn lần lượt 118/122 cảnh báo trong mã ứng dụng có sẵn (chủ yếu nullable và member hiding), không sửa ngoài phạm vi dữ liệu. Hợp đồng schema64 và các liên kết ET/DAL/BUS/WPF đạt. `git diff --check` không có lỗi whitespace.

## Các tệp

`counts-before.json`, `counts-after.json`, `table-counts.csv` lưu biến động từng bảng. `legacy-before.json` giữ bản gốc các bảng có liên quan, bao gồm lịch sử thu/hoàn mẫu trước chuẩn hóa; `lineage-after.json` là dữ liệu sau pha chuẩn hóa/tạo mẫu, trước nhật ký tổng kết của công cụ. Số dòng cuối bao gồm nhật ký tổng kết nằm trong `counts-after.json` và snapshot manifest.

`backup-before-apply.json` lưu đường dẫn backup COPY_ONLY/CHECKSUM đã VERIFYONLY trước ghi database chính. Không chứa bản backup nhị phân. `fresh-install.log`, `repeat-install.log`, `repeat-after-user-actions.log` và `build-and-schema.log` là log thi hành. `scenarios.txt` chứa mã hồ sơ/trạng thái để mở nhanh trên giao diện.

Rà soát độc lập phát hiện giả định chạy sau 10 giờ/sáng Chủ nhật của bộ sinh ban đầu; đã sửa bằng buổi sáng làm việc đã diễn ra và kiểm với đồng hồ 09:00/Chủ nhật cho cả sáu bác sĩ. Rà soát lại không còn Critical/Important. Các nhật ký chấm công/phiên mẫu ghi sai tháng, nguồn mua thiết bị và kho vật tư sai loại cũng được kiểm riêng và sửa trước áp dụng chính.

Các database thử tạo riêng cho công việc này được dọn sau nghiệm thu; `fixture-cleanup.txt` ghi kết quả. Database khác và các backup được giữ nguyên.
