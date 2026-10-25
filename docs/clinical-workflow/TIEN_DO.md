# Tiến độ triển khai

Ngày 25/10/2026. Kế hoạch chính: [KeHoach_CaiTo_QuyTrinhKham_6ChucNang.md](../../KeHoach_CaiTo_QuyTrinhKham_6ChucNang.md).

| Tác vụ | Trạng thái | Bằng chứng / phần còn lại |
| --- | --- | --- |
| P01 | Đã hoàn tất khảo sát baseline | Backup/verify/restore bản thử, build và toàn bộ suite cũ đã chạy; lỗi sẵn và chứng từ cần đối chiếu tại BASELINE.md |
| P02 | Đã hoàn tất quyền và tích hợp UI | Nguồn SQL có phạm vi, guard ghi chung transaction, cache theo phiên, quyền mở/sửa riêng, điều hướng và dữ liệu quầy thu giới hạn. 35 ca SQL + 8 ca WPF đạt; đã rà soát và sửa các phát hiện |
| P03 | Hoàn tất migration và hợp đồng dữ liệu | Sáu bảng + cột tám bảng cũ; nâng cấp hai lần, cài mới, rollback trống và chặn khi có dữ liệu. SchemaChecks 45 entity/363 cột đạt; ClinicalChecks tăng lên 65 ca |
| P04 | Hoàn tất hồ sơ và tiếp nhận | Bốn tab, tìm ngày sinh, lần khám gần nhất; dịch vụ–bác sĩ theo chuyên khoa/lịch; SQL cấp số và kiểm tra quyền; hai quầy không trùng phiếu/số. Tổng 80 ca SQL, 14 ca WPF đạt |
| P05–P12 | Chưa triển khai | Khám mới, áp dụng/duyệt mẫu, ký đơn snapshot, bảng tính, thu đủ/chống lặp và bàn giao thuốc |

## Quyết định khi thực hiện

1. Làm trên nhánh riêng trong checkout đang mở theo P01. Không tạo worktree phụ. Chi phí nếu cần đổi hướng: chuyển nhánh hoặc tạo worktree ở lần tiếp theo; chỉnh sửa đang ở nhánh riêng.
2. Ghi nhận bốn lỗi suite cũ, tiếp tục phần nền độc lập thay vì sửa nhân sự/lịch ngoài phạm vi. Chi phí: chưa có cổng nghiệm thu toàn dự án xanh; phải xử lý rõ các lỗi đó trước P12.
3. Đợt nền chưa mở màn hình cho vai trò mới; sau tích hợp P02 đã mở đúng năm module theo quyền nghiệp vụ, với nguồn đọc và guard ghi tương ứng. Bộ lọc/danh sách giao diện tiếp tục được cải tổ trong P04–P11.
4. Giữ kế hoạch gốc ở thư mục đồ án và đặt bằng chứng trong `docs/clinical-workflow/`, tránh hai bản Markdown tiến độ lệch nhau. Không sửa Word.
5. Thêm đúng một lệnh khởi tạo principal sau đăng nhập thành công để ràng buộc API mới với tài khoản phiên; đây là tích hợp tối thiểu cần thiết sau rà soát. Không đổi thuật toán xác thực; cần giữ điểm tích hợp khi sửa đăng nhập về sau.
6. Hóa đơn lịch sử trong phạm vi lâm sàng chỉ đối chiếu/in lại; khóa thao tác lập/thu/cấp cũ trước khi có P09. Chi phí: chưa có thao tác thu đủ/giao thuốc mới để sử dụng cho đến khi triển khai P09/P10; không thu lại chứng từ lịch sử.
7. Sửa tối thiểu `LichKham.xaml/.cs` ở chế độ nhúng trong hồ sơ để dùng nguồn SQL đã giới hạn và mở chi tiết chỉ đọc. Chi phí nếu sai: có thể ảnh hưởng lịch độc lập; đã thêm ca WPF xác minh lịch độc lập giữ nguồn và nút cũ. Thuật toán đặt lịch không đổi.
8. Thêm `RangHamMat.ClinicalUiChecks` vì WpfSmoke đang không biên dịch do API nhân sự cũ. Chi phí: thêm một project kiểm tra; không thay thế nghiệm thu WpfSmoke/AT30 ở P12.
9. P03 bổ sung trigger tiền tố `TR_CW_` thay vì thay định nghĩa trigger/view baseline. Chi phí: phải kiểm tra tương tác giữa các trigger; vòng rollback đối chiếu hash định nghĩa cũ và FinanceChecks 54 ca đều đạt.
10. Cấu hình mới có audit tại SQL, lấy actor từ session context do DAL gắn sau khi xác minh principal/tài khoản; lưu JSON trước/sau trong nhật ký sẵn có. Chi phí: sửa cấu hình trực tiếp qua SQL phải có actor hợp lệ; không thêm nội dung điều trị/liều dùng chưa duyệt.
11. DAL/BUS cấu hình P03 chỉ lưu nháp và danh mục; duyệt chuyên môn ở P05/P07, ghi bàn giao ở P10. Chi phí: chưa có giao diện/phê duyệt cấu hình hoặc giao thuốc mới để sử dụng trong mốc này.
12. SchemaChecks nhận entity bằng `[Table]` khai báo trực tiếp, loại DTO hiển thị/kế thừa. Giữ toàn bộ kiểm tra cột/nullable/constructor/tiền derived; thêm đối chiếu 46 định nghĩa bảng cũ và đúng 52 bảng vật lý. Lỗi nhận diện entity baseline đã được xử lý; ba lỗi suite khác còn nguyên.
13. P04 thêm quyền `DieuPhoiNgoaiLich` mặc định cho CV004. Điều dưỡng tiếp nhận được khi có lịch phù hợp; thiếu lịch phải có quyền điều phối, lý do và nhật ký. Chi phí: điều dưỡng không thể tự vượt lịch thiếu; phải cấp quyền có chủ đích nếu thay cấu hình công việc.
14. Giữ một khóa ứng dụng cho thứ tự ghi tiếp nhận, thêm khóa ngày/phòng khi cấp số. Chi phí: các quầy ghi tuần tự; phù hợp quy mô đồ án, cần đo tải trước khi tối ưu song song. Kiểm tra sẵn sàng có HOLDLOCK để vẫn an toàn với transaction ReadCommitted từ nơi gọi.
15. Giữ sửa lịch ở chức năng lập lịch, lịch sử trong hồ sơ chỉ đọc. Giữ đường DAL cũ để tương thích; guard giao diện lâm sàng chặn lập phiếu mới bằng form chung. Chi phí: không dùng API cũ làm đầu mối tiếp nhận mới; chưa thay toàn bộ cơ chế phân quyền legacy ngoài phạm vi.

## Rà soát độc lập

Đã rà soát phần nền. Phát hiện quan trọng: chỉ kiểm tra MaNV/MaTK trong DTO còn cho phép thay toàn bộ định danh. Đã tái hiện bằng bốn ca FAIL và sửa bằng principal tại điểm đăng nhập; cả bốn ca chuyển PASS. Assertion từ chối quyền dùng exception riêng, không chấp nhận lỗi SQL bọc trong InvalidOperationException làm PASS.

Lượt rà soát tích hợp P02 phát hiện lỗi so sánh số dạng chuỗi khi điều dưỡng lưu, nội dung khám nháp đi kèm dòng được giao, ghi chú chuyên môn trong dữ liệu thu và lịch nhúng đọc nguồn toàn viện. Đã tái hiện từng lỗi trước khi sửa: SQL 32 đạt/3 lỗi → 35/35; WPF 6 đạt/1 lỗi → 7/7; bổ sung kiểm tra lịch độc lập thành 8/8. Các view không mở lượt nháp của người khác từ ngữ cảnh tối thiểu; lỗi quyền khi mở chi tiết được hiển thị thay vì thoát ứng dụng.

Rà soát độc lập P03 phát hiện ba lỗ hổng SQL: kế hoạch đã duyệt có thể về nháp, bước đã thực hiện đổi kế hoạch cha, và kế hoạch có sẵn gắn mẫu chưa duyệt. Đã nâng mức phát hiện thiếu phiên bản mẫu từ Minor lên Important vì mất khả năng xác định nguồn. Bốn ca tái hiện từ FAIL chuyển PASS trong cùng lượt sửa; bổ sung ca mẫu ngừng áp dụng vẫn cho cập nhật tiến độ của bản đã sao chép. Bộ SQL cuối đạt 65/65 trên cả bản nâng cấp lẫn cài mới; không còn phát hiện quan trọng chưa xử lý trong P03.

Rà soát không đánh giá P04–P12, API duyệt/giao thuốc chưa triển khai, nội dung chuyên môn không được seed, và chức năng baseline ngoài phạm vi. Kiểm chứng thực thi do bộ kiểm tra SQL/WPF đảm nhiệm; chưa tuyên bố đạt các ca đồng thời thu tiền/tiếp nhận ở tác vụ sau. File Word được giữ nguyên.

Rà soát P04 phát hiện rủi ro khi nơi gọi truyền transaction ReadCommitted; hai phát hiện được nâng từ Minor lên Important vì ảnh hưởng sử dụng: nút sửa/hủy trong lịch sử hẹn và tính lần khám gần nhất quét lặp. Đã tái hiện: đổi lịch lọt trước commit; WPF 12 đạt/2 lỗi; phép tính 4.357 ms. Sửa cả ba trong một lượt: giữ khóa nguồn đến commit, lịch sử chỉ đọc và gom lịch sử một lần bằng map khóa. WPF đạt 14/14, phép tính trang đầu với 1.000 BN/10.000 lượt đạt 53 ms trên máy thử. Đây không phải nghiệm thu toàn bộ tải SQL/phân trang P11/P12.

Không còn phát hiện quan trọng hoặc Minor hoãn lại trong P04. Nghiệm thu hai kết nối thật gồm hai số liên tiếp trong cùng ngày/phòng, hai quầy cùng lịch trả cùng phiếu, ba BN trùng tên giữ riêng mã và hồ sơ, và ReadCommitted chặn thay lịch đến khi kết thúc. Fixture đã dọn sạch.

Bước tiếp theo là P05: bắt đầu khám từ hàng đợi đúng bác sĩ, lưu khám, tình trạng, gợi ý và áp dụng mẫu đã duyệt. P05–P12, phân trang SQL quy mô lớn, giao diện tổng thể và giao dịch thu đủ chưa hoàn tất.
