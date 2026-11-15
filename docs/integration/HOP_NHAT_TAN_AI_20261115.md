# Hợp nhất Sprint3_tan và AI vào tan — 15/11/2026

## Nguồn và cách hợp nhất

- `Sprint3_tan`: `0994ecb` (đã gồm X-quang `da8f326` và cập nhật báo cáo).
- `AI`: `71f344a`.
- Đích: `tan`. Giữ lịch sử Git của cả hai nhánh, giải quyết các xung đột theo nghiệp vụ.
- Một installer: `Database/QLBenhVienRangHamMat.sql`, 74 bảng. Không tự cài/nâng cấp database nghiệp vụ khi mở ứng dụng.

## Các điểm đã đồng bộ

- Tiếp nhận AI lưu bản khai; nhân viên có phiên hợp lệ và quyền tiếp nhận mới được xác minh/lập phiếu chính thức. Loại khám, phòng/bác sĩ, ca/hiện diện và phí dùng chung quy tắc với tiếp nhận thường. Gửi/duyệt lại không tạo thêm phiếu.
- Menu dùng quyền vai trò lưu trong SQL, đồng thời kiểm quyền nghiệp vụ lâm sàng/báo cáo. Vai trò tùy chỉnh đăng nhập được. Matrix User nguyên gốc chưa từng chỉnh được bổ sung quyền tới các luồng lâm sàng vốn có; matrix đã tùy chỉnh được giữ nguyên.
- Khám → dịch vụ → thuốc → tổng kết → chốt hóa đơn → thu/giao thuốc giữ giao dịch, giá đã chốt và cảnh báo rời hồ sơ chưa lưu.
- Phụ thu trên phiếu chuẩn được ưu tiên; nguồn AI cũ chỉ dùng làm tương thích khi chưa có phí chuẩn. Không cộng đôi, không giảm giá vào khoản bắt buộc. Đối soát và bản in cùng nguồn tính tiền.
- Lương tháng V9 giữ phép và ID hoa hồng cũ. Chính sách lương ca được chọn bằng cấu hình đơn giá ca; quỹ phép mới quy đổi theo ledger, không tự đổi các kỳ lương đã chốt.
- Build/publish tự dựng Kiosk API từ mã nguồn mới, đóng gói đầy đủ cấu hình AI, trang web, SQL và ảnh tham khảo. WPF/API dùng cùng BUS/DAL/ET và cùng cấu hình kết nối.
- Giữ phiên đăng nhập, đăng xuất, nhật ký, báo cáo, kiểm nhập liệu, phân công ảnh X-quang và chuyển DICOM.

## Kiểm chứng

Mọi phép ghi kiểm thử chạy trên database mới có tiền tố `RHM_Hospital_Test_`; không chạy installer hoặc sửa dữ liệu trên database nghiệp vụ.

| Bộ kiểm | Kết quả đã xác minh |
|---|---|
| SQL cài mới/chạy lại | 74 bảng; FK/CHECK trusted, trigger bật; số dòng từng bảng không đổi |
| SQL nhân viên duyệt AI | Thông thường/dịch vụ, phí chuẩn, retry, liên kết nguyên tử và từ chối thiếu phiên: đạt |
| SQL lương tháng | 32/32 |
| SQL lương ca/quỹ phép | Tính, nghỉ phép, hoa hồng, retry và chốt: đạt |
| Điều hướng/phân quyền | 9/9 |
| Vai trò tùy chỉnh | 6/6 |
| Đăng nhập/khôi phục/đăng xuất | 14 ca kho phiên + 37 ca qua nhiều tiến trình: đạt |
| Tiếp nhận dịch vụ, gồm chọn bác sĩ AI qua ca đêm | 34/34 |
| Khám/đơn thuốc/hóa đơn/giao diện | 47/47 |
| Phụ thu và thanh toán tích hợp | 8/8 |
| DICOM | 27/27 |
| Ảnh X-quang lịch sử | Bốn hồ sơ LS006/LS017/LS023/LS035: hash, xem trước, ảnh lớn và chỉ xem đạt |
| Nhập liệu toàn bộ nhóm kiểm | Đạt sau khi sửa kiểm tra editor đã bị DataGrid tháo khỏi cây giao diện |
| Công thức báo cáo | 27 ca đạt |
| HTTP/SQL từ backend đã publish | 15 ca đạt; không tạo bệnh nhân/phiếu chính thức từ bản khai |

Nghiệm thu tổng `MergeTanAI_20261115_FinalB` đạt **32/32 bước**, **20/21 tiêu chí**; tiêu chí còn lại chờ kiểm kho UNC/phục hồi trên hai máy thật. Manifest nguồn trước/sau giống nhau. Build Debug/Release và publish đều thành công; BUS/DAL/ET giữa WPF và backend có hash giống nhau, SQL đóng gói đúng hash nguồn cuối. Backend publish đã chạy lại đủ 15 ca HTTP/SQL.

Bằng chứng chia sẻ: [tổng hợp JSON](evidence/20261115-merge-summary.json), [log nghiệm thu cuối](evidence/20261115-p6-final.txt) và [kiểm SQL](../database/evidence/20261115-merge-ai/SQL_VERIFICATION.txt). Log đầy đủ nằm trong `obj/MergeEvidence`, `obj/MergeSqlChecks` và `obj/P6Evidence`; đây là thư mục đầu ra kiểm thử được Git bỏ qua.

Rà soát độc lập không phát hiện lỗi Critical/Important trong các điểm tích hợp đã đọc. Góp ý về bác sĩ ca đêm được xử lý và có ba ca SQL hồi quy. Các lỗi lương tháng, editor dịch vụ và luồng xác nhận AI đã có kiểm thử tái hiện trước khi sửa.

## Chạy và cài đặt

1. Sao lưu database/ảnh hiện hữu trước khi nâng cấp môi trường đang sử dụng. Chạy toàn bộ SQL chính; các script cài AI cũ trong SQL đã có guard để tránh ghi đè trigger/schema hợp nhất.
2. Build solution bằng .NET8 SDK; chọn RangHamMat.Wpf làm startup. Ứng dụng kiểm tra kết nối và cho chọn server/database trước khi đăng nhập.
3. Publish: `dotnet publish RangHamMat.Wpf -c Release -o PhatHanh`. Chuyển toàn bộ thư mục phát hành; cần .NET8 Desktop Runtime và ASP.NET Core Runtime8. Các DLL/MSI cũ trong cây nguồn không phải bản phát hành hợp nhất.
4. Khóa dịch vụ AI lấy từ môi trường hiện hữu. Kiểm thử tự động không gọi Groq/Ollama và không dùng dữ liệu bệnh nhân thật.

## Phạm vi chưa xác minh

Chưa kiểm phản hồi thực của nhà cung cấp AI, kho UNC trên hai máy thật hoặc toàn bộ dữ liệu vận hành thực tế. Các kiểm tra local/loopback không thay thế nghiệm thu những môi trường đó. Build còn các cảnh báo compiler hiện hữu; không coi số cảnh báo là số lỗi build.
