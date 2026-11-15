# Đồng bộ dữ liệu giữa các máy qua SQL Server

Trạng thái: dự thảo chờ người dùng duyệt trước khi triển khai.

## Yêu cầu và lựa chọn đã xác nhận

- Bổ sung đồng bộ khi phần mềm đang chạy cho toàn bộ phân hệ có dữ liệu dùng chung.
- Làm trên nhánh `dong_bo_giua_cac_may`, tạo từ `tan` tại commit `dcf4837b652b595d644c8e4a423aa2b8f2877d6c`.
- Người dùng đã chọn kiểm tra thay đổi qua SQL khoảng hai giây, không cài dịch vụ mạng riêng.
- Giữ trình tự nghiệp vụ tiếp nhận, khám, kê thuốc, chốt hóa đơn, thu tiền, bàn giao thuốc, kho và nhân sự.

## Hiện trạng đã kiểm tra

- WPF gọi BUS/DAL và kết nối trực tiếp SQL Server. Kiosk API hiện chỉ nghe localhost.
- `AppWorkspace.Reload_Click` gọi `DuLieuGiaoDien.RefreshFromDatabase`; phạm vi tải phụ thuộc ngữ cảnh và quyền.
- `DuLieuGiaoDien.Changed` là sự kiện trong tiến trình. Nhiều phân hệ khác tự tải dữ liệu qua BUS.
- Hàng chờ khám tự tải mỗi 10 giây và tránh làm mới khi nội dung khám chưa lưu.
- Giám sát tài khoản/quyền đã chạy mỗi 6 giây; phải tiếp tục giữ hiệu lực.
- Một số biểu mẫu cũng nghe `Changed`. Phát lại sự kiện một cách vô điều kiện có thể ảnh hưởng dữ liệu đang nhập.
- Build nền: `dotnet build BenhVienRangHamMat.sln --no-restore -v minimal` thành công, 0 lỗi, 133 cảnh báo có sẵn; project bộ cài `.vdproj` không được dotnet/MSBuild hỗ trợ.
- `dotnet run --project Test/AIIntakeStateChecks/AIIntakeStateChecks.csproj --no-restore` thành công.

## Các phương án

1. **Đã chọn:** kiểm tra phiên bản thay đổi SQL định kỳ; mỗi máy tự tải lại dữ liệu liên quan bằng BUS/DAL hiện có. Không thêm dịch vụ hay cổng mạng. Độ trễ bao gồm chu kỳ kiểm tra và thời gian tải.
2. Server đẩy thông báo qua kết nối lâu dài: có thể giảm độ trễ nhưng cần triển khai và vận hành một dịch vụ chung, khác với Kiosk localhost hiện có. Người dùng không chọn phương án này.

Phần kỹ thuật bên dưới là đề xuất của người triển khai, chưa phải quyết định đã được người dùng duyệt.

## Phát hiện thay đổi

- Dùng SQL Server Change Tracking cho các bảng dữ liệu dùng chung; không sao chép database về máy khách.
- Cập nhật file SQL chính theo quy ước của repository: bật Change Tracking ở database và các bảng cần theo dõi, có kiểm tra điều kiện và chạy lại an toàn. Không thêm/xóa cột dữ liệu nghiệp vụ, không nạp lại snapshot vào database đã có dữ liệu.
- Cho phép snapshot isolation phục vụ truy vấn thay đổi nhất quán; giữ nguyên mức cô lập của các giao dịch ghi nghiệp vụ hiện tại.
- Thiết lập đề xuất: lưu dấu thay đổi 7 ngày, tự dọn, không theo dõi chi tiết từng cột. Giữ cấu hình hợp lệ có sẵn thay vì tự ghi đè.
- Kiểm tra bảng có khóa chính, bảng đã bật theo dõi và quyền đọc metadata/thay đổi. Thiếu điều kiện thì hiển thị tình trạng chưa cấu hình; không âm thầm báo đồng bộ thành công.
- Vòng kiểm tra chạy nền khoảng 2 giây khi đã đăng nhập, tối đa một lượt đang chạy. Dùng phiên bản commit của SQL, không dùng thời gian máy khách hay `MAX(identity)` để xác định thay đổi.
- Đọc mốc phiên bản, kiểm tra mốc tối thiểu còn hợp lệ và danh sách bảng có thay đổi trong giao dịch snapshot ngắn. Chỉ trả tên bảng và phiên bản cho bộ điều phối; dữ liệu thực tế vẫn tải qua BUS theo quyền.
- Chỉ những thay đổi đã commit mới được đưa tới giao diện. Rollback không tạo thao tác làm mới giả.
- Phân biệt mốc đã quan sát với dữ liệu chờ áp dụng: phải giữ lại các bảng chưa tải được hoặc đang bị hoãn vì nhập liệu; không bỏ thay đổi khi tăng mốc theo dõi.
- Nếu mốc quá hạn, database được thay thế hoặc cấu hình kết nối đổi, thực hiện tải lại đầy đủ các phạm vi cần thiết vào thời điểm an toàn. Không tiếp tục dùng mốc cũ cho database khác.
- Các bảng chỉ phục vụ heartbeat/phiên không kích hoạt tải lại toàn bộ dữ liệu nghiệp vụ. Nhật ký vẫn nhận làm mới riêng khi người dùng đang xem nhật ký.
- Cả thay đổi từ WPF, Kiosk và công cụ khác ghi vào các bảng được theo dõi đều được phát hiện; không cần từng nút Lưu tự phát thông báo mạng.

## Điều phối và vòng đời

- Một dịch vụ đồng bộ cho mỗi phiên phần mềm, khởi động sau đăng nhập; hủy khi đăng xuất/thoát và tạo lại khi đổi tài khoản hoặc database.
- Tác vụ nền nhận rõ định danh, quyền, cấu hình kết nối và số thế hệ phiên. Kết quả thuộc phiên/ngữ cảnh cũ phải bị bỏ trước khi chạm giao diện.
- Gom các thay đổi liên tiếp theo bảng để tránh tải lặp; nếu không có thay đổi liên quan thì không tải lại dữ liệu.
- Đọc SQL ở nền; thay nguồn dữ liệu WPF trên Dispatcher. Không đọc hoặc thay ObservableCollection từ luồng nền.
- Chỉ cập nhật màn hình đang hoạt động và nguồn dữ liệu dùng chung cần thiết; màn hình mở sau sẽ đọc trạng thái mới theo cơ chế nạp của nó.
- Có danh sách phụ thuộc bảng cho từng phân hệ. Các phân hệ tải riêng đăng ký hàm làm mới tường minh; không dùng reflection gọi các hàm private hoặc dựng lại trang sau mỗi nhịp.
- Khi dữ liệu đọc lỗi, giữ phần hiển thị cuối cùng và giữ yêu cầu làm mới để thử lại.
- Khi mất mạng, lùi thời gian thử tối đa 30 giây; khi nối lại, đối chiếu và tải bù. Không mở MessageBox lặp lại sau mỗi nhịp.

## Bảo vệ nội dung đang nhập và giao dịch

- Tự động đồng bộ chỉ đọc và cập nhật hiển thị. Không tự lưu, hủy nháp, ký đơn, chốt khám, thu tiền, xuất kho hoặc xác nhận giao thuốc.
- Màn hình danh sách an toàn được tự làm mới; cố giữ bộ lọc, trang, dòng chọn theo khóa. Nếu dòng bị xóa hoặc rời bộ lọc, không tự chuyển sang bệnh nhân/chứng từ khác.
- Nội dung khám, đơn thuốc, biểu mẫu bệnh nhân, biểu mẫu nhân sự, chứng từ nhập/xuất, lịch làm việc và các form có nhập liệu phải có quy tắc nhận biết đang sửa trước khi nối đồng bộ.
- Với nội dung chưa lưu, modal nhập liệu, DataGrid đang sửa hoặc giao dịch thanh toán/bàn giao đang xử lý: giữ nguyên nội dung và hoãn phần làm mới có thể ảnh hưởng. Có thông báo không chặn: “Có dữ liệu mới; lưu hoặc kết thúc thao tác để cập nhật”.
- Sau khi kết thúc nhập liệu, áp dụng các thay đổi đang chờ; người dùng vẫn có thể chủ động tải lại theo quy tắc xác nhận hiện có.
- Không phát `DuLieuGiaoDien.Changed` từ cập nhật tự động khi có thể làm khởi tạo lại form đang sửa. Tách thông báo đồng bộ và các đăng ký biểu mẫu khi cần.
- Bản dữ liệu dùng đối chiếu xung đột khi lưu phải giữ là bản gốc lúc bắt đầu sửa. Không thay bản gốc bằng dữ liệu mới trong lúc giữ lại nội dung nhập cũ.
- Giữ và kiểm thử cơ chế quyền, trạng thái, chống sửa đồng thời và giao dịch ở BUS/DAL. Cập nhật màn hình không thay thế việc xác minh lại tại database trước thao tác ghi.
- Không tái sử dụng hàm mang tên “tải” nếu hàm đó thực hiện ghi hoặc đồng bộ nghiệp vụ; tách phần đọc cần thiết.

## Phạm vi phủ phân hệ

| Phân hệ trong điều hướng chính | Phạm vi đồng bộ |
| --- | --- |
| Tổng quan | Kiểm tra dữ liệu SQL thực có; chỉ cập nhật thành phần có nguồn dữ liệu động, giữ nội dung tĩnh |
| Bệnh nhân | Danh sách, hồ sơ và lịch sử theo quyền; giữ form đang nhập |
| Khám điều trị | Hàng chờ, lịch sử, trạng thái lượt và các phần chi tiết an toàn; giữ nháp khám |
| Dịch vụ thực hiện | Lịch sử đã thực hiện, phân công ảnh và trạng thái liên quan |
| Đơn thuốc | Danh sách, lịch sử, trạng thái thanh toán/bàn giao; giữ nháp kê thuốc |
| Lịch khám | Lịch hẹn và nguồn lựa chọn bác sĩ/phòng; giữ form đặt lịch |
| Hóa đơn | Danh sách, trạng thái thu, chi tiết an toàn; hoãn khi đang thanh toán |
| Phiếu hướng dẫn | Danh sách/hướng dẫn đã lưu; giữ form đang sửa |
| Nhân sự | Nhân viên, chức vụ, chuyên khoa và trạng thái nghỉ; giữ form nhập |
| Chứng chỉ nhân viên | Danh sách chứng chỉ và liên kết nhân viên; giữ form nhập |
| Ca làm việc | Lịch, đăng ký, duyệt và đơn nghỉ liên quan; giữ thao tác đang nhập |
| Chấm công | Danh sách, trạng thái duyệt và số liệu liên quan; giữ phần đang chỉnh |
| Bảng lương | Danh sách, chi tiết đã lưu và trạng thái chốt; không tự tính/chốt lương |
| Chính sách lương | Danh sách/chính sách đã lưu; giữ form nhập |
| Thiết bị | Danh sách, phân bổ, trạng thái và lịch sử liên quan |
| Bảo trì | Bảo trì, thanh lý và trạng thái thiết bị; giữ form nhập |
| Kho | Tồn kho, lô và nguồn danh mục liên quan |
| Danh mục hàng hóa | Hàng hóa, thuốc, vật tư, phân loại và giá; giữ form nhập |
| Nhà cung cấp | Danh sách và dữ liệu đã lưu; giữ form nhập |
| Nhập xuất vật tư | Phiếu và chi tiết, tồn/lô và chứng từ liên quan; giữ phiếu đang soạn |
| Dịch vụ | Danh mục, giá và nguồn tham chiếu; giữ form nhập |
| Tài khoản | Trạng thái, vai trò, quyền; tiếp tục cơ chế ngắt phiên bị khóa |
| Báo cáo | Đọc lại báo cáo đang xem khi nguồn liên quan thay đổi; giữ kỳ/bộ lọc |
| Nhật ký | Làm mới nhật ký đang xem, tránh biến heartbeat thành tải lại cả dự án |
| Xin nghỉ việc | Đơn nghỉ, phê duyệt, trạng thái liên quan; giữ đơn đang nhập |
| Trợ lý AI | Nhận dữ liệu dùng chung đã lưu phục vụ nhân viên; giữ hội thoại, dữ liệu đang khai và request AI đang chạy |

Các màn hình con gồm tiếp nhận, tiếp nhận AI, ảnh X-quang, hóa đơn nhà cung cấp, nhập kho, xuất kho, chi tiết và cửa sổ nhập liệu phải được rà soát cùng phân hệ cha. Không coi danh sách điều hướng là danh sách duy nhất cần kiểm tra. Tệp ảnh vẫn dùng kho chung UNC theo hướng dẫn hiện tại; cơ chế này không sao chép tệp ảnh giữa máy.

## Giao diện và cấu hình

- Hiển thị trạng thái gọn trên khung ứng dụng: đang theo dõi, có cập nhật đang chờ, mất kết nối hoặc chưa cấu hình SQL.
- Không thêm bước bắt buộc vào luồng tiếp nhận/khám/thu tiền.
- Có cấu hình bật/tắt và khoảng kiểm tra mặc định 2 giây, kiểm tra giá trị hợp lệ.
- Nút Tải lại dữ liệu hiện tại được nối với bộ điều phối để làm mới cả phân hệ tải riêng, theo điều kiện an toàn; không gửi lệnh tải lại sang máy khác.
- Không tự chạy ALTER DATABASE/ALTER TABLE khi mở phần mềm. Hướng dẫn triển khai yêu cầu người vận hành áp dụng phần SQL đã chuẩn bị.

## Kiểm thử và nghiệm thu

1. Kiểm thử bộ điều phối: không thay đổi, nhiều cập nhật được gom, tải lỗi được thử lại, hoãn khi đang sửa, đổi phiên/database, đăng xuất và dừng dịch vụ.
2. SQL trên database thử riêng: hai kết nối/tiến trình cùng database, insert/update/delete, commit/rollback, ghi đồng thời, mất mốc theo dõi, thiếu quyền/cấu hình. Không ghi vào database nghiệp vụ để thử.
3. Kiểm tra giao diện và đăng ký làm mới cho từng phân hệ ở bảng phạm vi; ghi rõ đã kiểm tĩnh, đã chạy giao diện hay đã thử hai tiến trình.
4. Tình huống bắt buộc: tiếp nhận ở A xuất hiện ở hàng chờ B; chốt khám ở A xuất hiện hóa đơn B; thanh toán/bàn giao ở A cập nhật B; thay đổi kho/nhân sự/lịch/lương ở A cập nhật màn hình B.
5. Trong lúc B nhập khám, kê đơn, sửa nhân sự hoặc soạn phiếu kho, A thay đổi dữ liệu: nội dung B còn nguyên, không tự chuyển người bệnh, không tự phát sinh thao tác ghi và xung đột được phát hiện khi lưu theo cơ chế hiện có.
6. Mất mạng không treo giao diện, không lặp hộp thoại, nối lại nhận đủ thay đổi còn liên quan.
7. Chạy build và các kiểm tra hồi quy phù hợp. Hai tiến trình trên một máy chỉ là kiểm tích hợp; nghiệm thu hai máy vật lý/SQL từ xa phải ghi rõ nếu chưa thực hiện.
8. Xác nhận toàn bộ thay đổi nằm trên nhánh mới và `tan` vẫn trỏ tới commit ban đầu.

## Những việc người triển khai tự quyết

Tên lớp, cấu trúc adapter, ánh xạ phụ thuộc bảng cụ thể, câu truy vấn Change Tracking, giới hạn khoảng kiểm tra và cách đặt các bộ kiểm thử được chốt trong kế hoạch sau khi thiết kế được duyệt. Cần xuất ma trận phủ các màn hình, tránh tuyên bố “toàn bộ dự án” chỉ dựa vào build thành công.

## Tài liệu kỹ thuật đã đối chiếu

- [Microsoft: Work with Change Tracking](https://learn.microsoft.com/en-us/sql/relational-databases/track-changes/work-with-change-tracking-sql-server): mốc đồng bộ, snapshot, mốc hết hạn và tính nhất quán.
- [Microsoft: Enable and Disable Change Tracking](https://learn.microsoft.com/en-us/sql/relational-databases/track-changes/enable-and-disable-change-tracking-sql-server): cấu hình database/bảng và thời gian giữ dấu thay đổi.
