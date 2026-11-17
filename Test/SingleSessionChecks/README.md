Chạy từ thư mục gốc bằng quyền Windows có thể tạo database test trên SQL local:

```powershell
dotnet run --project Test/SingleSessionChecks/SingleSessionChecks.csproj -- lpc:.
```

Harness tạo database `RHM_Hospital_Test_SingleSession_<GUID>`, dựng schema tối thiểu tương ứng phần định danh/phiên/audit, rồi xóa database trong `finally`. Không kết nối database nghiệp vụ để ghi dữ liệu.

12 nhóm kiểm tra SQL thật bao gồm đăng nhập sau thu hồi phiên trước, thông báo chính xác, LOGOUT/LOGIN_SUCCESS cùng giao dịch, heartbeat và truy cập nghiệp vụ từ phiên đã thu hồi bị từ chối trên cả transaction có sẵn, logout cũ không đóng phiên mới, tài khoản độc lập, login đồng thời cùng/khác tài khoản, watcher chỉ đọc và phân biệt hết hạn/mất phiên/đăng xuất, hủy watcher, sai account, restore token mới nhất và race hai restore, thông báo schema cũ.

Bằng chứng RED đầu tiên: phiên baseline giữ hai phiên `Đang mở`, lỗi `Two machines kept active sessions.` Sau triển khai: 11 kiểm tra PASS. Kiểm tra khác tài khoản chạy đồng thời từng phát hiện SQL deadlock 1205; deadlock graph chỉ ra UPDATE pointer quét PK và tranh range với index tài khoản. Bản sửa dùng `IX_Rhm_Phien_Account` và READCOMMITTED cho UPDATE theo account, dưới khóa UPDLOCK của tài khoản giữ tới hết transaction.

Kiểm tra bổ sung đường ghi legacy đã RED với `Revoked legacy write was accepted: luuChinhSach`, rồi GREEN cho đủ năm API: lưu/duyệt chính sách, duyệt chấm công, tính/chốt lương. Harness dựng bảng tối thiểu và thủ tục lương đơn giản để kiểm tra đúng dispatch DAL, transaction và session, không kiểm tra công thức lương. Trigger audit được cài từ chính đoạn generator trong `Database/QLBenhVienRangHamMat.sql`: phiên hiện tại ghi được audit có đúng MaPhien cho `CHINH_SACH_LUONG`, `CHAM_CONG`, `BANG_LUONG`; SQL có context phiên bị thu hồi bị trigger từ chối ở cả ba bảng. Một thủ tục đang chạy có điểm dừng SQL chứng minh login mới phải chờ giao dịch tính lương commit; sau khi login thay thế commit, cả năm đường ghi từ principal cũ bị từ chối ngay, không cần watchdog UI. Các đường dùng `CreateConnection` còn lại trong DAL được quét lại và chỉ đọc dữ liệu.

API watcher: `BUS_NHATKY.KiemTraPhienHienTaiAsync(string connectionString, Guid session, string account, CancellationToken)` trả `ET.TrangThaiPhienHienTai`. Principal phải có đúng định danh RHM, account và claim `RHM.MaPhien` khớp. API mở kết nối đọc riêng và không gia hạn heartbeat; lỗi mạng được phát lại cho UI thử lại. `KhoiPhucPhien(Guid rememberedSession, NguCanhNghiepVu actor)` dùng principal vừa được xác thực, chưa có claim phiên, và kiểm tra `PhienKeTiep IS NULL` trong cùng khóa tài khoản trước khi tạo phiên mới.

Đường bootstrap/caller nội bộ không có claim phiên giữ tương thích hành vi cũ. Guard phiên áp dụng khi principal mang claim phiên và các API vòng đời nhận session tường minh; đây không phải thay đổi toàn bộ mô hình xác thực của ứng dụng.
