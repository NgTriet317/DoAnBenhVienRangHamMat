# Chốt khám, lịch sử dịch vụ và SQL thống nhất

> Mốc lịch sử trước PrescriptionFlow1. Luồng hiện tại tách xác nhận thực hiện và kê thuốc trong khám trước Tổng kết; xem [KE_THUOC_TRONG_KHAM.md](KE_THUOC_TRONG_KHAM.md).

Yêu cầu29/10/2026: Dịch vụ thực hiện chỉ xem lịch sử; Hoàn tất khám chốt điều trị và lập hóa đơn ngay; đa dạng danh mục RHM, không chỉ bệnh răng; một file SQL để cài/sync, bỏ phụ thuộc Migrations và dữ liệu demo.

Thiết kế: bác sĩ lập và duyệt kế hoạch ngay trong khám. Nút Hoàn tất yêu cầu xác nhận các dịch vụ trong kế hoạch đã làm xong; backend chốt phần lượng còn lại, ghi dịch vụ đã thực hiện theo đúng bước/giá đã duyệt, khóa lượt, tạo một hóa đơn chờ thu (dịch vụ và đơn đã ký), tất cả chung transaction. Dịch vụ khám tiếp nhận được ghi một lần nếu chưa có trong các bước. Không tự thu tiền hoặc xuất thuốc khi hoàn tất. Quầy thu dùng lại chính hóa đơn đó, xuất thuốc/thu/giao theo luồng hiện có; retry không tạo hóa đơn thứ hai. Kế hoạch có nhiều buổi chỉ hoàn tất khi đã thực hiện xong toàn bộ, theo xác nhận của bác sĩ.

Dịch vụ thực hiện chỉ nạp lượt đã xác nhận và các dịch vụ đã chốt. Không có nút thêm/sửa/xác nhận hoặc dịch vụ phát sinh; API ghi cũ bị khóa, không chỉ giấu nút. Các thao tác thực hiện không được mở sau khi hóa đơn đã phát sinh.

SQL chính cài database trống và cập nhật schema/catalog trên database hiện có được hỗ trợ. Không chạy file khác. Cài mới chỉ có admin hệ thống, vai trò/cấu trúc tổ chức/danh mục có nguồn; không hồ sơ người giả, tồn kho, thu tiền, lịch trực giả. Dữ liệu DEMO28 có dấu sở hữu được dọn theo FK và nội dung; bất kỳ tham chiếu người dùng/tài chính bất thường nào làm thao tác dừng để đối chiếu. Chứng từ lịch sử đã chốt không bị ép xóa bằng cách tắt trigger. Hồ sơ người thật chỉ nhập từ nguồn được phép sử dụng; bảng giá công khai ghi rõ nguồn/ngày.

## Các bước thực hiện

- [x] Tái hiện hoàn tất chưa có hóa đơn và màn dịch vụ còn ghi.
- [x] Chốt dịch vụ trong khám, khóa write API, lịch sử đọc.
- [x] Hóa đơn ngay khi hoàn tất, thu lại hóa đơn đó, giữ giá thuốc và chống retry.
- [x] Danh mục mở rộng RHM và SQL tự chứa; dọn demo an toàn.
- [x] Kiểm tra cài mới/sync/lặp, luồng tiền/thuốc, quyền, rollback và GUI.
- [x] Backup, áp dụng SQL chính, publish và cập nhật hướng dẫn.
- [x] Lưu bằng chứng và xóa đúng bốn database kiểm thử do lần triển khai này tạo.

## Kết quả29/10/2026

Bản phát hành vượt qua34 nhóm kiểm nghiệp vụ, bao gồm WPF chọn nhanh theo nhóm/gãy hàm, kế hoạch nhiều bước, hóa đơn ngay khi hoàn tất, rollback khi lập hóa đơn lỗi, giá thuốc đóng băng trước xuất kho, tách hai lô khi thu, retry/alias, phạm vi điều dưỡng và che kết quả lâm sàng với thu ngân. Kiểm Admin mở đủ26 phân hệ đạt trên cả database cài mới và database đang dùng. Debug/Release và kiểm schema chỉ đọc đều đạt. Cài mới/chạy lặp từ duy nhất SQL chính đã được kiểm chứng sau khi xóa thư mục Migrations. SQL từ chối đồng bộ bộ demo đã được xử lý và rollback toàn bộ thay đổi giai đoạn3.

Database hiện tại được backup COPY_ONLY/CHECKSUM và RESTORE VERIFYONLY trước đồng bộ;349 dòng DEMO28 nguyên bản được xóa. So sánh từng dòng của53 bảng chứng minh tất cả dòng ngoài bộ demo được giữ nguyên, gồm mật khẩu/giá/chứng từ. Một nhật ký chỉ tra cứu dịch vụ demo, không chứa nội dung trước/sau, được giữ nguyên; nhật ký sửa dữ liệu hoặc tham chiếu tài chính vẫn chặn việc dọn.

**Phạm vi giữ lại:** database đang dùng còn11 người bệnh (10 hồ sơ seed cũ gắn lịch sử tài chính và1 hồ sơ người dùng thêm),2 bác sĩ cũ và50 dịch vụ vì giữ8 dịch vụ cũ ngoài danh mục khởi tạo. Các hồ sơ/chứng từ cũ này chưa bị xóa trong lần cập nhật này. Bộ cài sạch có0 người bệnh,0 bác sĩ,1 Admin,0 hóa đơn,47 tình trạng,42 dịch vụ và112 liên kết; không có dữ liệu người giả hay tồn kho thử. Không tự bịa hồ sơ người thật để thay dữ liệu mẫu.

Bằng chứng, hash bản phát hành và đường dẫn backup: [evidence/HospitalWorkflow3](evidence/HospitalWorkflow3/README.md). Dùng [README hiện tại](../../README.md) và SQL chính để cài/chạy; các tài liệu mốc cũ chỉ dùng đối chiếu lịch sử.
