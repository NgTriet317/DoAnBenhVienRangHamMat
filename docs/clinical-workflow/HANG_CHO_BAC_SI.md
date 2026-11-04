# Hàng chờ khám theo bác sĩ — thiết kế và kế hoạch

Yêu cầu ngày28/10/2026: tiếp nhận phân bác sĩ xong phải xuất hiện trong danh sách làm việc của bác sĩ; ai tiếp nhận trước hiển thị trước. Bác sĩ chọn người bệnh để bắt đầu/tiếp tục khám. Giữ lịch sử ở vị trí hợp lý. Người dùng cho phép thiết kế lại giao diện khi cần.

Phương án triển khai do người thực hiện chọn trong phạm vi được giao: dùng hai tab **Hàng chờ khám** (mặc định) và **Lịch sử khám** trên màn hình Khám điều trị. Giữ không gian khám hiện có ở bên phải, không thêm project hoặc tạo giao diện bằng C#. Phương án giữ nút mở hộp chọn tiếp nhận bị loại vì vẫn giấu công việc chính; tách hàng chờ thành mục menu mới làm tăng bước chuyển màn không cần thiết.

Hàng chờ chỉ gồm phiếu Chờ khám/Đang khám còn lượt nháp hoặc chưa có lượt, xếp `ThoiGianTiepNhan, SoThuTu, MaPhieu`. Mặc định giữ cả phiếu đang chờ từ ngày trước; khoảng ngày là bộ lọc tùy chọn. Số thứ tự đang cấp theo phòng/ngày được giữ nguyên; không đánh lại số khi đổi bác sĩ. Bác sĩ chỉ thấy phiếu của mình; Admin xem tất cả hoặc lọc bác sĩ. Điều dưỡng/lễ tân tiếp nhận theo quyền hiện có.

Chọn dòng hiển thị biên nhận, bệnh nhân, bác sĩ, phòng, dịch vụ, lý do và giờ tiếp nhận; chưa tạo lần khám. Bấm Bắt đầu khám/Khám tiếp mới gọi đầu mối hiện có, giữ đúng MaPhieu/MaBS. Bấm lặp mở lại cùng MaLS. Hoàn tất khám chuyển sang lịch sử, không nằm trong hàng chờ. Lịch sử ở tab riêng cho tra cứu; lượt đã xác nhận không sửa.

Danh sách phân trang SQL25 dòng, có tổng số và tìm kiếm. Tự tải lại mỗi10 giây khi trang đang hiển thị, có nút Tải lại. Giữ lựa chọn theo mã, không thay bằng bệnh nhân khác khi phiếu biến mất. Khi có nháp chưa lưu, không thay editor/ngữ cảnh; dừng tự tải lại editor. Trước khi mở khám luôn kiểm tra lại SQL; thay đổi ở máy khác phải được phản ánh, không dựa vào cache50 bệnh nhân. Không yêu cầu migration vì dùng cột hiện có.

## Kế hoạch thực hiện

- [x] Test RED: lọc trạng thái, phiếu chờ qua ngày, đúng bác sĩ, Admin/lọc bác sĩ, phân trang, tiếp nhận qua BUS, hoàn tất ra khỏi hàng chờ; GUI mặc định hàng chờ và không tự tạo lượt khi chọn.
- [x] ET/BUS/DAL: thêm bộ lọc bác sĩ/phiếu và thông tin hiển thị, API `LayTrangHangDoi` có tổng số, giữ API `LayHangDoi` cho caller cũ; xác thực SQL và nhóm điều kiện quyền/trạng thái.
- [x] WPF: hai tab XAML, hàng chờ/preview và nút mở khám, bộ lọc/phân trang, deep link và timer; lịch sử tiếp tục dùng nguồn có quyền hiện có.
- [x] Test GREEN: tiếp nhận→bác sĩ đúng→bắt đầu/lặp→tiếp tục→hoàn tất→lịch sử, trạng thái đổi/hủy từ kết nối khác, cache ngoài50, chuyển tab khi nháp, tự làm mới; kiểm tra cửa sổ nhỏ.
- [x] Review độc lập, sửa phát hiện, build Debug/Release và kiểm tra Admin hồi quy; cập nhật PhatHanh, hướng dẫn và bằng chứng, dọn database thử.

Test code dự kiến `tools/HangChoKhamChecks.cs.txt`, runner `tools/KiemTraHangChoKham.ps1`, project tạm trong `obj` và database riêng `RHM_HangChoKham_Test_20261028`. Database mặc định chỉ chạy kiểm tra GUI/đọc; không tạo bệnh nhân khám thử ở đó.

## Kết quả và cách chạy

- **16/16** nhóm kiểm thử SQL/WPF đạt trên source và trên DLL Release trong PhatHanh. Có thử tiếp nhận qua BUS, hai bác sĩ, Admin, phiếu qua ngày, thứ tự khác số phòng, phân trang, từ khóa đặc biệt, nhấn bắt đầu lặp, hoàn tất sang lịch sử, không tạo lượt khi chọn dòng, chống đổi tab khi nháp, hủy phiếu ở kết nối khác, timer nhận phiếu mới,55 bệnh nhân ngoài cache và ba kích thước cửa sổ1280×850/1024×768/800×600.
- **12/12** hồi quy Admin đạt; GUI đọc trên database mặc định: hàng chờ bác sĩ **1/1**, đăng nhập/Admin26 mục/bàn giao **3/3**. Build Debug/Release0 lỗi, lần kiểm tra cuối Debug0/Release53 cảnh báo cũ. Publish thành công; DLL phát hành khớp SHA256 với bản Release.
- Review độc lập phát hiện1 P2: mở khám dưới bộ lọc Chờ khám rồi đổi trạng thái khiến lần tải lại sau đóng editor. Đã tái hiện RED và sửa GREEN; phiên khám đang mở được giữ độc lập với các dòng đang nằm trong trang/bộ lọc. Lưu nháp hoặc đổi trang hàng chờ không làm tự đóng phiên đó.
- Kiểm tra hình ảnh phát hiện tên người bệnh bị cuộn khuất khi tìm nút khám ở cửa sổ nhỏ. Đã bố trí tên/ngày sinh/mã phiếu và nút khám ở vùng cố định, phần bác sĩ/phòng/lý do có thể cuộn. Kiểm tra vị trí và chiều cao vùng nội dung đã đạt; ảnh trong [evidence/DoctorQueue](evidence/DoctorQueue).
- Không sửa schema hoặc tạo bệnh nhân/phiếu khám thử trên `QLBenhVienRangHamMat`; database vẫn52 bảng và cấu hình mặc định được giữ. Dữ liệu thử nằm ở hai database riêng đã dọn sau kiểm tra. Tra cứu Admin có thêm nhật ký theo thiết kế hiện có.

Kiểm tra GUI chỉ đọc bằng bản phát hành trên database mặc định:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraHangChoKham.ps1 -Database QLBenhVienRangHamMat -ChiGiaoDien -BanPhatHanh
```

Để chạy đủ16 ca: tạo database thử **mới/trống** từ SQL chính bằng cách đổi đồng thời tên database thành `RHM_HangChoKham_Test_20261028`, rồi chạy runner không có `-ChiGiaoDien`. Mỗi lần kiểm tra đầy đủ cần fixture sạch: các ca GUI chủ động commit dữ liệu giả ở database thử để mô phỏng quầy/bác sĩ trên các kết nối khác nhau. Runner từ chối ghi lên tên database khác. Giữ đúng bốn project sản phẩm; project test tạm chỉ sinh dưới `obj`.

Tự tải lại tạm ngừng khi có nháp chưa lưu. Chọn phiếu theo thứ tự hiển thị; hệ thống không tự gọi người tiếp theo hoặc tự bắt đầu khám khi chỉ chọn dòng. Quyền và trạng thái được SQL kiểm tra lại trước thao tác. Lịch làm việc/quyền điều phối tiếp nhận vẫn dùng các quy tắc sẵn có.

Giới hạn rà soát: bộ lọc văn bản/ngày có thể đổi phần hiển thị trong lúc editor còn nháp, nhưng dữ liệu/ngữ cảnh editor không bị đổi và SQL không được tải lại cho đến khi lưu/bỏ nháp; đây là cách xử lý đã có từ trước. Kiểm thử không đánh giá tải đồng thời trên nhiều máy hoặc DPI khác nhau; ba kích thước được chạy bằng cửa sổ WPF thật ở DPI của máy kiểm tra.
