# Sửa đồng bộ và đăng nhập nhiều máy — 17/11/2026

## Nguyên nhân được xác minh

Kiểm tra chỉ đọc database QLBenhVienRangHamMat tại máy làm việc cho thấy 74 bảng, 0 bảng Change Tracking, snapshot_isolation_state=0. Vòng theo dõi chưa được cấu hình trên database này. Bản mã nguồn trước đã có tính năng, nhưng cần cài phần SQL trên server để sử dụng.

Mã nguồn còn hoãn cập nhật khi focus nằm trong mọi TextBox, kể cả bộ lọc nhật ký; một số dependency SQL thiếu; lỗi tải đơn lồng trong tổng kết khám có thể bị nuốt. Các điểm này được sửa và có regression.

## Hành vi phiên đăng nhập

- Lần đăng nhập mới của cùng tài khoản thu hồi các phiên đang mở trước đó trong một giao dịch SQL khóa theo tài khoản.
- Máy cũ có watchdog riêng khoảng hai giây, độc lập vòng đồng bộ và các form chưa lưu; khi phát hiện bị thay thế, bỏ quyền/token của phiên đó, đóng các cửa sổ nhập và quay về đăng nhập.
- Thông báo chính xác: **tài khoản đã được đăng nhập vào máy khác**.
- Không tự lưu nháp hoặc tự gửi lại yêu cầu thanh toán/xuất kho khi phiên bị thu hồi. Người dùng cần đăng nhập lại để tiếp tục; nháp chưa lưu cục bộ sẽ không được lưu tự động.
- Phiên cũ không heartbeat/đóng phiên mới/ghi qua những đường nghiệp vụ có phiên hợp lệ. Các đường ghi lương, chính sách và chấm công dùng chung guard giao dịch.
- Tệp ghi nhớ định dạng v2 có mã phiên đã mã hóa bằng Windows DPAPI. Token cũ đã bị thay thế không tự giành lại phiên mới khi mở phần mềm. Sau nâng cấp, tệp v1 yêu cầu nhập mật khẩu lại một lần.
- Đóng ứng dụng bình thường vẫn cho phép ghi nhớ đăng nhập nếu chưa có lần đăng nhập khác thay thế token. Việc xác minh và nhận phiên mới diễn ra cùng giao dịch, tránh hai máy cùng khôi phục thành công một token.

## Cài đặt SQL và cập nhật máy khách

Phần SQL mới ở cuối Database/QLBenhVienRangHamMat.sql gồm marker SINGLE ACCOUNT SESSION và CROSS-MACHINE CHANGE TRACKING. Cài thêm cột nullable PhienKeTiep và chỉ mục khóa theo tài khoản/phiên đang mở. Chạy lại không nạp snapshot hoặc ghi đè dữ liệu bệnh viện. Nếu dữ liệu cũ có nhiều phiên đang mở cho một tài khoản, phần cài giữ một phiên và đánh dấu các phiên trùng bị thay thế.

Xuất riêng các đoạn SQL để kiểm tra trước khi áp dụng:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/CaiDatDongBoVaPhien.ps1 -MayChu 'lpc:.' -Database QLBenhVienRangHamMat
```

Sau khi người vận hành duyệt, áp dụng đúng các đoạn này:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/CaiDatDongBoVaPhien.ps1 -MayChu 'lpc:.' -Database QLBenhVienRangHamMat -ApDung
```

Dùng tên/instance server triển khai thay cho lpc:. ở máy khác. Cần chạy bản ứng dụng mới trên toàn bộ các máy; phiên bản cũ không có watchdog thoát phiên. Ứng dụng vẫn không tự ALTER database khi mở.

## Kiểm tra

- Test/SingleSessionChecks: SQL riêng, đăng nhập đồng thời/các tài khoản khác, thu hồi, guard, khôi phục và không cướp lại token.
- Test/SessionWpfChecks: tệp v2, từ chối v1, đóng modal có nháp khi forced logout, thông báo đúng và chỉ hiện một lần.
- Test/RuntimeCoverageChecks và Test/WpfSyncChecks: bộ lọc, dependency, dirty, tab ẩn, retry và nội dung đang nhập.
- Test/RuntimeLoginSyncChecks: WPF thật với kết nối SQL khác, nhật ký nhận LOGIN/LOGOUT khi focus còn ở ô lọc, watcher đẩy phiên cũ và phiên mới vẫn dùng được.
- Bộ hồi quy đăng nhập cũ và khám/điều trị được giữ để kiểm tính tương thích.

Các bộ SQL chỉ ghi database có tiền tố RHM_Hospital_Test_. Chưa nghiệm thu trên hai máy vật lý, mạng gián đoạn dài hoặc toàn bộ tương tác trong từng phân hệ. [Ma trận phạm vi cập nhật](DONG_BO_RUNTIME_MA_TRAN.md) phân biệt các route thực, child và mã legacy không được điều hướng tới.

### Kết quả chạy trên mã sửa

| Bộ kiểm | Kết quả |
| --- | --- |
| SingleSessionChecks | 12 nhóm SQL pass, gồm 5 API ghi lương/chấm công/chính sách và giữ khóa tới commit |
| SessionWpfChecks | 12/12, token DPAPI v2 và forced logout cửa sổ có nháp |
| WpfSyncChecks | 28/28, gồm focus bộ lọc và danh sách modal giữ nội dung đối chiếu |
| RuntimeCoverageChecks | 15/15, Nhật ký, dependency và retry |
| RuntimeLoginSyncChecks | 6/6 với WPF và SQL thật: login/logout, focus, danh sách AI trong modal, xung đột bản khai, watcher và phiên mới tiếp tục dùng được |
| AllModulesSyncChecks | 29/29: 26 route chính và 3 tab con kho; đọc SQL và áp dụng adapter thật, 2 route tĩnh/cục bộ được phân biệt rõ |
| SqlSyncChecks --full-schema | 21/21, 74 bảng và giữ dữ liệu khi cài lại tracking |
| Hồi quy khám/điều trị | 47/47 |
| Hồi quy ghi nhớ đăng nhập | 15 kiểm tệp và 7 pha SQL ở các tiến trình riêng đều pass |
| SyncChecks / AIIntakeStateChecks | Pass |

Lệnh kiểm SQL đều chạy trên fixture riêng. RuntimeLoginSyncChecks nhận server và tên database đã được cài đủ schema; chỉ chấp nhận tiền tố RHM_Hospital_Test_. Log cục bộ ở obj/sync17-*.log và obj/runtime-session-flow.log.

## Triển khai thực tế ngày 17/11/2026

Đã áp dụng sau khi người dùng xác nhận “Hãy triển khai giúp tôi”, trên `DESKTOP-C9VCE8A`, database `QLBenhVienRangHamMat`. Dùng installer riêng, không chạy lại phần nạp snapshot nghiệp vụ.

- Sao lưu COPY_ONLY có CHECKSUM và `RESTORE VERIFYONLY` thành công trước khi thay đổi. Tệp trên SQL server: `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\QLBenhVienRangHamMat_BeforeSessionSync_20261117_102510_7FC51A2A.bak`.
- Cả 74/74 bảng đã bật Change Tracking; `snapshot_isolation_state=1`.
- Có cột `PhienKeTiep`, chỉ mục `UX_RHM_PhienDangMo_MaTK` và `IX_Rhm_Phien_Account`; không có tài khoản trùng phiên đang mở sau cài.
- Số bệnh nhân/hóa đơn/lượt khám/nhân viên trước và sau vẫn là 47/36/36/14. Bộ kiểm chức năng ghi dữ liệu chỉ chạy trên database `RHM_Hospital_Test_*`.
- Bản chạy xuất vào `PhatHanh`, `DongBo:Bat=true`, `ChuKyGiay=2`. Sao chép toàn bộ thư mục này tới các máy trạm và cấu hình cùng SQL server/database. Chưa truy cập hoặc cập nhật trực tiếp các máy trạm khác.
- Kiểm lại gói Release: hash WPF/BUS/DAL/ET/backend trùng bản build; công cụ mở giao diện dùng DLL trong `PhatHanh` đạt 2/2. Hồi quy sau bản sửa giao dịch đạt 47/47, polling nhân sự/cache đạt 5/5.

## Phần hoàn thiện toàn bộ phân hệ

`AllModulesSyncChecks` mở theo chính `MainWindow.AllPages`, kiểm adapter đang hiển thị rồi đưa một lượt invalidation đầy đủ qua bộ điều phối thật. Đây là kiểm điều hướng/đọc/áp dụng với SQL, không phải 29 kiểm mạng hoặc toàn bộ nút ghi nghiệp vụ. Tab nhập kho được chuyển từ form tạo mới sang xem phiếu đã có để kiểm chế độ đọc; form nhập vẫn được hoãn có chủ đích.

Kiểm này phát hiện tải cache ngoài lâm sàng mở BUS nhân sự trên kết nối thứ hai, tự chờ khóa tài khoản đang được giao dịch cache giữ. Đã dùng `BUS_NHANVIEN(transaction)` cùng giao dịch như các bảng còn lại; hành vi cập nhật trạng thái nghỉ khi tải chủ động được giữ, tải tự động vẫn chỉ đọc.

Cửa sổ tiếp nhận AI có đường cập nhật danh sách riêng ngay cả khi modal đang mở. Không thay đối tượng gốc hoặc ô đối chiếu; nếu bản khai đang chọn thay đổi/được xử lý trên máy khác thì khóa nút tạo phiếu và yêu cầu Làm mới để đối chiếu lại. Không tự tạo bệnh nhân/phiếu chính thức. Bản in và hội thoại cục bộ vẫn giữ nội dung của phiên đang xem.

Lệnh kiểm mới:

```powershell
dotnet run --project Test/AllModulesSyncChecks -- 'lpc:.' RHM_Hospital_Test_RuntimeSession_20261117
dotnet run --project Test/RuntimeLoginSyncChecks -- 'lpc:.' RHM_Hospital_Test_RuntimeSession_20261117
```

Fixture phải được cài đủ schema và dữ liệu thử (có admin001 và chứng từ kho); không trỏ các lệnh kiểm này tới database nghiệp vụ. Hai kiểm không chạy đồng thời trên cùng fixture vì đăng nhập mới sẽ thu hồi phiên trước.
