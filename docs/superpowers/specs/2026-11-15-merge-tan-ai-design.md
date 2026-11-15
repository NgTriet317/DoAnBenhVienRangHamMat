# Hợp nhất Sprint3_tan và AI vào tan

Yêu cầu: giữ chức năng của hai nhánh, liên thông dữ liệu và đúng luồng nghiệp vụ trên nhánh `tan`.

## Cách thực hiện

Gộp Git thực sự để giữ lịch sử hai nhánh. `tan` hiện là tổ tiên chung; đưa `Sprint3_tan` vào trước rồi gộp `origin/AI`, xử lý từng xung đột theo chức năng. Không chọn nguyên một phía cho mọi xung đột vì sẽ mất chức năng phía còn lại. Không chép binary cũ làm bằng chứng cho mã nguồn mới.

## Ràng buộc

- Giữ .NET 8, WPF, ET/BUS/DAL; thêm Kiosk API từ AI.
- Giữ một SQL chính `Database/QLBenhVienRangHamMat.sql`, có schema khám/thu tiền/X-quang và schema AI, tài khoản, nhân sự.
- AI lưu bản khai; nhân viên xác minh rồi tạo phiếu chính thức. Không để AI vượt bước duyệt hoặc tự thu tiền.
- Luồng khám → dịch vụ → thuốc → chốt hóa đơn → thanh toán giữ giao dịch, phân quyền và chống thao tác trùng.
- Cấu hình kết nối và kho ảnh phải dùng nhất quán giữa WPF và backend.
- Chỉ chạy kiểm thử ghi trên database thử có tiền tố `RHM_Hospital_Test_`; không cài SQL lên database nghiệp vụ.
- Không cam kết phần chưa kiểm chứng (dịch vụ AI ngoài, UNC hai máy, dữ liệu thực).

## Tiêu chí xác minh

Hai commit nguồn là tổ tiên của `tan`; không còn conflict; build Debug/Release và publish được; các bộ kiểm tra phù hợp về SQL, khám/thu tiền, AI, nhân sự/phân quyền, giao diện, báo cáo và ảnh chạy được trên cơ sở dữ liệu cách ly. Ghi lệnh, kết quả và giới hạn vào báo cáo tích hợp.
