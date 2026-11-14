# Rà soát dữ liệu ngày 12/11/2026

**Đã xử lý sau khi người dùng xác nhận toàn bộ là dữ liệu đồ án:** chuẩn hóa cũ và thêm 36 người bệnh liên thông; database hiện có 64 bảng/3.221 dòng. Xem [bộ dữ liệu đã áp dụng và các ca dùng thử](DU_LIEU_DO_AN_20261112.md). Phần bên dưới giữ nguyên hiện trạng trước sửa (911 dòng) để đối chiếu.

## Phạm vi và trạng thái

Đã đối chiếu database `QLBenhVienRangHamMat` trên SQL Server cấu hình trong dự án, metadata 64 bảng, các khóa ngoại/CHECK/trigger, bộ đối soát tài chính–kho–lâm sàng hiện có, và các điều kiện sử dụng trong DAL/BUS/WPF. Database có 911 dòng tại thời điểm xuất. Số liệu chi tiết từng bảng và danh sách mã cần đối chiếu nằm trong `evidence/20261112-data-review/`.

**Chưa sửa nội dung bệnh án, chỉ định, khoản thu, xuất kho, bàn giao hoặc thêm hồ sơ/nguồn nhập mới vào database chính.** Cần xác định dữ liệu hiện tại là hồ sơ mẫu đồ án hay dữ liệu vận hành để lựa chọn cách sửa đúng nguồn. Không có tài liệu gốc để khẳng định thông tin nào là sự thật khi dữ liệu đã ký có mâu thuẫn.

## Kết quả kiểm tra

Bộ đối soát hiện có đọc đủ 64 bảng, không phát hiện vi phạm khóa ngoại, ràng buộc bị tắt, tổng tiền không khớp hoặc tồn kho âm. Có 22 mục cần đối chiếu: 11 dòng tình trạng thiếu mã và cùng 11 lượt khám chưa có tình trạng gắn mã. Đây là 11 lượt, không phải 22 lượt khác nhau.

Kiểm tra bổ sung bằng `tools/KiemTraDuLieuLienThong.sql` cho thấy các điểm sau. Nhiều quy tắc cùng chỉ đến một hồ sơ; không cộng các số lượng thành tổng số hồ sơ lỗi.

| Nhóm | Số mục | Dữ liệu và ảnh hưởng |
|---|---:|---|
| Đơn đã ký thiếu bản chốt | 10 dòng | CTDT001–CTDT010 thuộc DT001–DT008 thiếu tên/hàm lượng/đơn vị chốt. `LayBanInDon` từ chối in bản ký này. |
| Đường dùng ghi hàm lượng | 4 dòng | CTDT011–CTDT014 thuộc DT011 đều có `DuongDung=500mg`; có cả thuốc viên và dung dịch súc miệng. Cần phân biệt đường dùng, hàm lượng và liều, không tự suy ra liều điều trị. |
| Chưa có bàn giao | 8 đơn | DT001–DT008 đã ký, chưa có BAN_GIAO_THUOC. DT001–DT003 đã thu ròng đủ nhưng thuộc quy trình cũ. DT004–DT008 chưa thu. |
| Hóa đơn quy trình cũ | 10 hóa đơn | HD001–HD010 không phải hóa đơn v2; v2 chỉ nhận đúng một lần Thu đủ, trong khi chứng từ cũ có nhiều lần thu/hoàn. Chỉ đổi phiên bản sẽ không hợp lệ. |
| Phiếu khám còn trạng thái cũ | 10 phiếu | PKB001–PKB010 còn `Đã khám`, chưa phản ánh Chờ thanh toán/Chờ nhận thuốc/Hoàn tất theo luồng hiện tại. |
| Xuất thuốc nhưng thiếu nguồn hóa đơn | 5 dòng | CTPX006–CTPX010 của PX004–PX008 chưa vào chi tiết hóa đơn. Các phiếu đã ghi sổ nên không được xuất thêm để bù liên kết. |
| Đã xuất thuốc nhưng chưa có thu đủ | 5 phiếu | PX004–PX008 cần đối chiếu chứng từ; tên quy tắc phản ánh tình trạng thu hiện tại, không kết luận trình tự thu trong quá khứ. |
| Dịch vụ chưa vào hóa đơn | 5 dòng | Nguồn dịch vụ xác nhận của LS006–LS010 chưa có dòng hóa đơn tương ứng. |
| Cờ không kê thuốc chưa khớp | 2 lượt | LS009/LS010 có đơn Hủy, không có đơn hiệu lực nhưng `KhongKeThuoc=0`. |
| Thuốc chưa có nguồn nhập | 52 thuốc | Có danh mục, chưa có nguồn nhập ghi sổ để cấp. Thêm dòng THUOC không tự tạo tồn. |
| Vật tư chưa có nguồn nhập | 10 vật tư | Có danh mục, chưa có chứng từ nhập để xuất sử dụng. |
| Dịch vụ thiếu nhân sự tương ứng | 39 dịch vụ | CK002/CK003/CK004/CK007 chưa có bác sĩ đang làm việc, có tài khoản hoạt động đúng chuyên khoa để chọn trong luồng tiếp nhận tương ứng. |
| Lịch làm việc hiện tại | 1 chuyên khoa | CK001 có bác sĩ nhưng chưa có lịch xác nhận/Có mặt hôm nay khi chạy kiểm tra. Kết quả phụ thuộc ngày chạy; không phải lỗi hồ sơ lịch sử. |
| Thiết bị thiếu nguồn mua trong phần mềm | 10 thiết bị | Có serial/ngày mua, chưa có chi tiết nhập mặt hàng tương ứng. Có thể là tài sản có trước hệ thống; không được tự tạo hóa đơn mua thật từ việc thiếu liên kết này. |

Một mâu thuẫn cần lưu ý: LSKB011 có chẩn đoán sâu răng, còn TTB011 ghi sẹo vùng hàm mặt sau chấn thương. Không thể kết luận bản nào phản ánh thực tế chỉ từ mã nguồn.

## Công việc đã thực hiện

1. Tạo backup `COPY_ONLY` có checksum và chạy `RESTORE VERIFYONLY ... WITH CHECKSUM` thành công. Bản sao lưu có tên duy nhất, không ghi đè backup khác. Đường dẫn trong `backup-before.txt` tại thư mục bằng chứng.
2. Sửa `SqlSnapshotExporter.cs.txt` để nhận schema 64 bảng sau khi tái hiện lỗi công cụ chỉ nhận 60/62 bảng.
3. Tái hiện và sửa lỗi cài mới `Cannot insert explicit value for identity column ... NHAT_KY_DUYET_CHAM_CONG`. `IDENTITY_INSERT ON`, INSERT và OFF hiện nằm cùng batch động.
4. Xuất snapshot 64 bảng/911 dòng, cài bằng toàn bộ file SQL trên database thử riêng. Tất cả 64 bảng sau cài mới và sau chạy lại khớp số dòng và SHA256 với nguồn, kể cả dòng nhật ký identity. Hai database thử riêng đã được dọn; database chính và backup giữ nguyên.
5. Cập nhật vùng snapshot trong `Database/QLBenhVienRangHamMat.sql` từ database đã đối chiếu. Ví dụ, DT011 trong file hiện phản ánh bản ghi bàn giao `Đã giao` đã lưu trên database. Các pha nâng cấp ngoài snapshot được giữ nguyên, trừ chú thích về mốc snapshot.
6. Bổ sung công cụ kiểm tra liên thông chỉ đọc để lưu rõ mã bản ghi và nguyên nhân cần đối chiếu.

## Phương án chuẩn hóa tiếp theo

Nếu là dữ liệu đồ án, có thể giữ mã hồ sơ/tài khoản hiện hữu, chuẩn hóa các tình huống mẫu theo một kịch bản thống nhất và lưu bảng đối chiếu trước/sau. Cần xử lý cùng lúc bản ký, dịch vụ, hóa đơn, thu, xuất và bàn giao; không chỉ sửa trạng thái để vượt điều kiện ứng dụng.

Nếu có dữ liệu vận hành thật, cần giữ lịch sử thu/hoàn, tên/hàm lượng/giá đã chốt và ngày/người thực hiện. Các mâu thuẫn cần đối chiếu với chứng từ gốc; dữ liệu giả lập chỉ thêm với dấu nhận diện rõ. Không thể chuyển nhiều lần thu/hoàn thành một lần Thu đủ và coi đó vẫn là lịch sử gốc.

Dữ liệu bổ sung dự kiến bao phủ danh mục dịch vụ/thuốc/vật tư/thiết bị, nhân sự–phòng–ca phù hợp, nguồn nhập có lô và đơn vị, tồn khả dụng, bệnh nhân–hồ sơ–lịch hẹn–tiếp nhận, tình trạng–kế hoạch–thực hiện, đơn thuốc–hóa đơn–thu–bàn giao ở các trạng thái đang sử dụng. Danh mục nhận diện có nguồn; giá mô phỏng, số lô, serial, người bệnh và chứng từ giả lập phải được ghi rõ. Không suy diễn liều hoặc tạo dấu phê duyệt y khoa thật.

Mọi sửa/bổ sung phải được thử trên database riêng, kiểm tra FK/CHECK, tổng tiền, nguồn phí, tồn theo lô và lịch sử tồn, điều kiện sử dụng qua BUS/DAL, chạy lại không sinh trùng hoặc hồi trạng thái; sau đó mới áp dụng cùng thay đổi và xuất lại file SQL. Các thay đổi WPF/DAL/ET có sẵn trước yêu cầu này được giữ nguyên.
