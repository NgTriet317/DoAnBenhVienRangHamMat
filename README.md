# Nâng cấp Lễ tân AI WPF – Tạo phiếu khám SQL thật, KHÔNG QR

**Cơ sở mã nguồn:** `NgTriet317/DoAnBenhVienRangHamMat`, nhánh `dong_bo_giua_cac_may` đã đọc ngày 16/11/2026. Target `net8.0-windows`.

## Những thay đổi

- **Bỏ QR hoàn toàn** khỏi `ReceptionistView.xaml`, ViewModel và hàm in bill. Dùng `SlipPrintService.cs`, không còn `CreateQrBitmap` hay phụ thuộc QRCoder từ module.
- **Mặc định SQL thật**, `UseMockRepository: false` ở `AI/Config/ReceptionistSettings.json`. Còn `MockHospitalRepository` khi chạy kiểm thử với `UseMockRepository:true`, hiển thị DEMO rõ ràng; không ghi SQL trong mode này.
- **AI gọi `create_guidance_slip`** để chuẩn bị yêu cầu, bệnh nhân xác nhận trên màn hình, sau đó `SqlHospitalRepository.CreateGuidanceSlipAsync` -> `BUS_AI_LETAN.TaoPhieuKham` -> `DAL_AI_LETAN.DamBaoPhienChoTaoPhieu` -> **`BUS_AI_TAOPHIEU.taoPhieu` -> `DAL_AI_TAOPHIEU.taoPhieu` -> `dbo.AI_TaoPhieuTuBanKhai`**. Đây là phiếu khám thật trong `PHIEU_KHAM_BENH`, không chỉ bản khai.
- Không bỏ qua phân quyền/định danh SQL. Phải đăng nhập nhân viên có **quyền TiepNhan** trong WPF và phiên SQL còn hoạt động. Không mạo danh nhân viên từ lời bệnh nhân.
- AI phải hỏi **ngày sinh** vì thủ tục SQL tạo phiếu yêu cầu ngày sinh; đồng thời tên, điện thoại, lý do, nhóm khám và tình trạng phim liên quan. Bệnh nhân kiểm tra và bấm **Xác nhận và tạo phiếu khám THẬT** trước khi ghi chính thức.
- Bộ điều phối lấy phòng từ `PHONG`, số bệnh nhân chờ từ `PHIEU_KHAM_BENH`, kiểm tra bác sĩ trực từ `BUS_AI_TAOPHIEU.layBacSi`, chọn phòng đang có bác sĩ với số người chờ ít nhất. SQL kiểm tra lại trước khi lưu.
- Không tự chỉ định chụp X-quang; đưa người bệnh đến phòng khám được lựa chọn và ghi chú hỏi bác sĩ về chụp phim nếu cần.
- Giữ Groq GPT-OSS-20B, hai tool schema phẳng, 6 tin nhắn gần nhất, fallback khi API lỗi, 45 giây ẩn phiếu, in bill 80 mm không QR.

## Cài đặt

1. Mở terminal PowerShell ở thư mục `ReceptionistAI_Module` (đã giải nén).
2. Đảm bảo local Git đang checkout `dong_bo_giua_cac_may`, đã commit/backup các thay đổi cá nhân trước.
3. Chạy:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\Install-ReceptionistAI.ps1 -RepositoryRoot 'D:\DuAn\DoAnBenhVienRangHamMat'
```

Script thay thế điểm mở `TroLyAI` bằng `ReceptionistView`, giữ mã quyền `TroLyAI` đã cấp. Nó **không sửa** `TiepNhanAI`, `PhieuHuongDan` hoặc `RangHamMat.Kiosk.Api`. Script copy 14 file và build. Chạy `-SkipBuild` nếu chỉ muốn áp dụng patch và tự build trong Visual Studio.

4. SQL Server phải có các bảng `AI_PHIEN_TIEP_NHAN`, `AI_TIEP_NHAN_NHAP`, `AI_SU_KIEN`, `AI_KET_QUA_TAO_PHIEU` và stored procedure `dbo.AI_TaoPhieuTuBanKhai` của nhánh trên. **Không cần chạy SQL migration mới**.
5. Cấu hình biến môi trường `GROQ_API_KEY` riêng trên máy chạy WPF. Khởi động lại Visual Studio sau khi cấu hình.
6. Đăng nhập WPF bằng tài khoản có quyền nghiệp vụ tiếp nhận, quyền truy cập `PhieuHuongDan` trong SQL và quyền mở phân hệ `TroLyAI`. Tài khoản chỉ có quyền `TroLyAI` nhưng không có quyền `TiepNhan` sẽ bị DAL từ chối khi tạo phiếu.
7. Trong WPF, vào ô **Lễ tân AI – Tạo phiếu khám** (giữ Tag=`TroLyAI`).

## Bảng quy trình thực tế

| Lượt | Xử lý |
|---|---|
| 1 | Bệnh nhân cung cấp triệu chứng/nhu cầu. C# chặn các biểu hiện cấp cứu. |
| 2 | AI hỏi phim X-quang nếu liên quan, họ tên, ngày sinh, số điện thoại; dữ liệu được đối chiếu lại với văn bản bệnh nhân và C#. |
| 3 | Groq trả function `create_guidance_slip` (6 tham số, bổ sung `date_of_birth` do SQL yêu cầu). |
| 4 | UI hiển thị tóm tắt; bệnh nhân nhấn xác nhận; C# tự gọi BUS/DAL với nhân viên đang đăng nhập. |
| 5 | DAL tạo phiên AI cùng MaPhien, sau đó gọi procedure hiện có. Procedure kiểm tra SQL: quyền tiếp nhận, ca/phòng bác sĩ, hồ sơ nghi trùng, phiếu cùng ngày, phân bổ STT. Dữ liệu X-quang đã/chưa có trong 6 tháng cũng được ghi vào lý do khám nếu liên quan. |
| 6 | Trả mã phiếu `PKB_AI_...`, số thứ tự, phòng khám thật; WPF in bill 80mm **không có QR**. |

## Giới hạn và an toàn

- Chỉ hỗ trợ **khám thông thường** tự động. Khám dịch vụ cần nhân viên chọn bác sĩ và dịch vụ hợp lệ, nên không gán dịch vụ hoặc phí phụ thu từ AI. Nhóm chỉnh nha, implant, tái khám hiện được điều phối về khoa khám tổng hợp `KHOA001` do SQL nhánh hiện chưa có khoa độc lập; hãy hiệu chỉnh ánh xạ khi tổ chức thay đổi.
- Phân quyền SQL **không được bỏ** chỉ vì bệnh nhân sử dụng kiosk; trên máy sảnh không nên để lộ phiên đăng nhập nhân viên. Để cung cấp tự phục vụ độc lập, cần backend ủy quyền được kiểm soát và phê duyệt nghiệp vụ riêng.
- Thủ tục `AI_TaoPhieuTuBanKhai` đã có chống tạo phiếu trùng cùng `MaPhien` và đối chiếu hồ sơ, sử dụng lại nguyên trạng. Nếu hồ sơ nghi trùng, không có bác sĩ trực, không có quyền hoặc SQL lỗi, không tạo phiếu; gặp nhân viên.
- Giới hạn thời gian 45 giây chỉ xóa dữ liệu **trên màn hình**, không xóa phiếu đã lưu SQL; cần cân nhắc quyền riêng tư bệnh nhân và việc gửi thông tin y tế lên Groq.
- Backend kiosk trình duyệt (`RangHamMat.Kiosk.Api`) vẫn trả HTTP 410 cho `/api/ai/intake/receipts`. Bản này chỉ thay **WPF**; không âm thầm mở quyền ghi cho endpoint công khai không xác thực.
- Không thể xác nhận biên dịch / kết nối SQL / in bill / gọi Groq thực tế trong môi trường Linux hiện tại. Script cung cấp restore+build và bài kiểm thử dưới đây.

## Kiểm thử cần làm tại Windows + SQL test

1. Đăng nhập nhân viên có quyền `TiepNhan`, kiểm tra module mới hiển thị qua ô AI cũ.
2. Với ca bác sĩ đang làm, bệnh nhân mới đủ thông tin và xác nhận: `PHIEU_KHAM_BENH` tăng đúng **1**, `AI_PHIEN_TIEP_NHAN.TrangThai` thành `DaDuyet`, có đúng `SoThuTu`.
3. Nhấn tạo hai lần/cùng MaPhien: chỉ một phiếu, STT không tăng lần hai.
4. Bệnh nhân cũ trùng đủ ba trường: liên kết hồ sơ cũ. Hồ sơ nghi trùng/đang có phiếu ở cùng phòng hôm nay: báo lỗi từ SQL, không cấp thêm phiếu.
5. Không có bác sĩ trực/không có quyền nghiệp vụ/ngày sinh thiếu: không được tạo phiếu.
6. API 429/500/timeout: fallback chọn nhanh; **vẫn lưu SQL thật** sau khi xác nhận (nếu SQL hoạt động).
7. Không có máy in hoặc mặc định máy A4: phiếu vẫn hiển thị, không crash. Máy in bill đúng 80mm: in không QR.
8. Phiếu đã tạo được xóa khỏi màn hình sau 45 giây hoặc bấm hoàn tất; bản ghi SQL vẫn còn.
9. Chạy thử trên database test trước khi áp dụng dữ liệu thật, đặc biệt các trường hợp nhân viên đang đăng nhập và hoạt động ca bác sĩ thực tế.
