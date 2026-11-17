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
| WpfSyncChecks | 26/26, bổ sung focus bộ lọc không chặn runtime |
| RuntimeCoverageChecks | 15/15, Nhật ký, dependency và retry |
| RuntimeLoginSyncChecks | 4/4 với WPF và SQL thật: login/logout, focus, watcher, phiên mới tiếp tục dùng được |
| SqlSyncChecks --full-schema | 21/21, 74 bảng và giữ dữ liệu khi cài lại tracking |
| Hồi quy khám/điều trị | 47/47 |
| Hồi quy ghi nhớ đăng nhập | 15 kiểm tệp và 7 pha SQL ở các tiến trình riêng đều pass |
| SyncChecks / AIIntakeStateChecks | Pass |

Lệnh kiểm SQL đều chạy trên fixture riêng. RuntimeLoginSyncChecks nhận server và tên database đã được cài đủ schema; chỉ chấp nhận tiền tố RHM_Hospital_Test_. Log cục bộ ở obj/sync17-*.log và obj/runtime-session-flow.log.

Trạng thái triển khai tại thời điểm bàn giao mã: chưa áp dụng SQL lên database nghiệp vụ; đã gửi yêu cầu duyệt bước cấu hình database đang dùng. Phần đã thử gồm cả chạy lại installer riêng trên database fixture. Khi triển khai thực tế, dùng script ở trên và cập nhật bản ứng dụng mới trên tất cả máy.
