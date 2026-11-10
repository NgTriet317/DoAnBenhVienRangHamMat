# Bằng chứng P5 ngày08/11/2026

Mọi dòng nguồn/ảnh UI/CSV/manifest ở đây thuộc database DEMO giả lập riêng;
`final-audit-current.log` chỉ chứa số tổng hợp của bản đang dùng. Bảng kê PK bản
đang dùng giữ trong `obj/P5Evidence`, không đưa vào gói này.

| Tệp | Nội dung |
|---|---|
| `final-p5.log` |21/21 audit/FK/reader/giới hạn/CSV/nhân sự/thuốc/tiền/kho/DEMO/BUS/WPF |
| `tool-guards.log` |7/7 tên DB/output/junction/không ghi đè/xuất phục hồi |
| `readonly-preserved.log` |64/64 bảng giữ nguyên fingerprint khi xuất phục hồi |
| `concurrent-A.log`, `concurrent-B.log` |Hai tiến trình tạo DEMO đồng thời, đúng hai DB riêng |
| `final-audit-current.log` |62 bảng,0 sai xác định trong kiểm hiện có;11 lượt thiếu mã và3 mục schema, tổng25 phát hiện có lặp |
| `demo-audit-summary.md` |DEMO64 bảng,0 sai xác định/0 cần đối chiếu |
| `manifest.json`, `lineage.csv` |Nguồn26 lượt,18 người; số thực tại lúc xuất tách với ExpectedSeed |
| `conditions.csv`, `monthly-finance.csv` |Dữ liệu lấy từ BUS báo cáo, đã đối chiếu SQL độc lập |
| `recovery-after-refund-manifest.json` |DB DEMO phụ có hoàn5000:26 lượt/27 dòng nguồn, thu quý3145000; ExpectedSeed giữ mốc ban đầu |
| `audit-red.log`, `demo-red.log`, `review-red.log`, `demo-label-red.log` |Ca tái hiện trước triển khai/sửa |
| Các `final-*.log` còn lại |Hồi quy35 nền tảng/32 đồng bộ/28 tiếp nhận/47 khám/32 ảnh/41 báo cáo; build Debug/Release0 lỗi, Release122 cảnh báo cũ |

Ảnh SQL/WPF thật trên dữ liệu giả, có nhãn DEMO:

- [Tổng quan26 lượt/18 người, thu quý3150000](p5-overview.png)
- [Ba tình trạng theo mã](p5-conditions.png)

Các log được lọc dòng PASS/FAIL/RESULT và marker từ log đầy đủ trong `obj`.
Review độc lập đã xác nhận đóng các lỗi nguồn thuốc, số dư lịch sử, output sau
commit, race và đếm recovery; không sửa file hay database từ phía reviewer.

Xem [hành vi, lệnh chạy, diễn giải số và giới hạn](../../KET_QUA_DOI_SOAT_P5_20261108.md).
