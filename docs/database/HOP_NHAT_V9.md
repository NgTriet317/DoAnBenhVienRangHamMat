# Hợp nhất SQL V9 vào database chính

File cài/đồng bộ duy nhất: `Database/QLBenhVienRangHamMat.sql`. Chạy toàn bộ bằng SSMS F5 hoặc sqlcmd, trên SQL Server2016SP1 trở lên và tài khoản đủ quyền. Ứng dụng kết nối SQL Server qua appsettings.json hoặc RHM_CONNECTION_STRING; ứng dụng không tự chạy file SQL khi F5.

## Đối chiếu nguồn

| Phần thiếu trong bản53 bảng | Thành phần đã hợp nhất từ V9 |
|---|---|
| Lịch khám | MucDoNghiemTrong, MaBSDuBi, mặc định, CHECK và FK nhân viên |
| Chính sách lương | Phụ cấp cố định, tỷ lệ bảo hiểm, hệ số tăng ca và CHECK |
| Bảng lương | Công chuẩn, phép, trễ, công thực tế, các khoản cộng/trừ, người chốt, công thức thực nhận và khóa kỳ |
| Bảng mới | NGAY_NGHI_LE, CHAM_CONG, CHI_TIET_LUONG, PHU_TA_DICH_VU, CHI_TIET_HOA_HONG_LUONG, LAM_THEM_GIO, NHAT_KY_DUYET_CHAM_CONG |
| Thủ tục | RHM_TinhLuongKy, RHM_ChotLuongNhanVien |
| View | VW_CONG_CHUAN_BANG_LUONG, VW_DICH_VU_DU_HOA_HONG; bổ sung bảng mới vào VW_THONG_KE_SO_DONG |
| Bảo vệ | Khóa phiếu/chi tiết/hoa hồng đã chốt; khóa công/lịch trong kỳ đã chốt; nhật ký chỉ thêm |
| Kiểm tra | usp_RHM_KiemTraSchema gọi cả kiểm nền, kê thuốc và kiểm V9; KiemTra.bat kiểm60 bảng |

Các quy tắc khám, kê đơn, hoàn tất và hóa đơn của bản chính được giữ lại. Không đưa trở lại TINH_TRANG_BENH.MucDo/ViTriRang đã bỏ; mức nghiêm trọng của lịch hẹn là một trường riêng. Không ghi đè hóa đơn MaCTDT bằng bản V9 cũ chưa hỗ trợ thuốc chờ xuất.

Phần dữ liệu mẫu V9 không được nhập từ file đối chiếu. Các lịch nghỉ trong file thử cũng không tự áp cho bệnh viện; nhập lịch đã duyệt vào NGAY_NGHI_LE trước khi tính kỳ tương ứng. Từ bản phục hồi ngày30/10, cài mới có snapshot dữ liệu cũ cùng danh mục; xem [nguồn dữ liệu và cách cập nhật](PHUC_HOI_VA_DU_LIEU_SQL.md).

## Bảo toàn dữ liệu khi nâng cấp

- Sao lưu database và đóng phiên ứng dụng trước khi chạy script.
- Nền53 bảng được nâng cấp lên60 trong pha V9 có giao dịch; các pha ghi phía sau chỉ chạy khi pha trước đã thành công.
- Phiếu lương cũ được đánh dấu `PhienBanTinhLuong=8`. `LuongTheoCaTruocDongBo` và `TongLuongTruocDongBo` giữ chính xác tiền trước nâng cấp; computed columns tiếp tục trả tiền đó. Không tự đặt lại ghi chú, ngày/trạng thái chốt, mật khẩu hoặc giá.
- Công/định mức cũ theo ca được biểu diễn bằng nửa công/ca để xem lại, không coi là dữ liệu đối soát V9 đã được duyệt. Người chốt cũ chưa lưu không được tự suy đoán; khóa ngoại cho phép NULL với phiếu lịch sử đã đánh dấu.
- Phiếu cũ đã chốt tiếp tục khóa. Chủ động Tính lương mới chuyển phiếu Nháp sang `PhienBanTinhLuong=9`; bản tiền gốc vẫn giữ để đối chiếu. Chốt phải tính/đối soát theo V9, đúng bản đã xem và sau hết tháng ít nhất5 ngày.
- Không cho tạo mới phiếu phiên bản8, sửa bản tiền trước đồng bộ hoặc hạ phiên bản. Nếu có trùng phiếu nhân viên/kỳ hoặc dữ liệu không khớp schema, giao dịch nâng cấp dừng để đối chiếu; không tự xóa chứng từ.
- Phiếu lịch sử chỉ bao phủ một phần tháng vẫn xuất hiện khi chọn tháng, kèm ngày bắt đầu/kết thúc thực tế và nhãn Công thức cũ. Script không tự mở rộng kỳ đó. Nếu kỳ mới chồng phiếu lịch sử chưa đủ tháng, thủ tục dừng với52333 để đối chiếu trước khi lập/tính cả tháng, tránh chi trả trùng.

File V9 gốc chỉ dùng làm nguồn đối chiếu; không chạy nguyên file đó lên database đang có vì nó là bản cài thử có dữ liệu mẫu. Không cần chạy các script rời SQL/07 hoặc BO_SUNG_DUYET_CHAM_CONG_V9 sau file chính.

## Kiểm chứng

```powershell
.\KiemTra.bat -KiemTraSql -MayChu . -Database QLBenhVienRangHamMat
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraDongBoSql.ps1 -Database RHM_Hospital_Test_20261029
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraQuyTrinhBenhVien.ps1 -Database RHM_Hospital_Test_20261029
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraQuyTrinhBenhVien.ps1 -ChiGiaoDien -Database RHM_Hospital_Test_20261029
```

Database thử phải cài từ file chính, thay tên database trong bản sao script bằng tên có tiền tố RHM_Hospital_Test_. Bộ kiểm lương từ chối ghi lên database sử dụng. Fixtures kiểm nghiệp vụ rollback sau từng trường hợp. Ca ngắn được kiểm bằng hàm xử lý từng ca của DAL trong giao dịch rollback; đây không phải thao tác bấm nút giao diện.

Đã kiểm cài mới, chạy lại, nâng cấp phiếu cũ và các bộ kiểm trên SQL Server16.0.1000.6/.NET SDK8.0.400. Chưa kiểm thực tế trên SQL Server2016SP1 hoặc máy Windows mới hoàn toàn.

Kết quả bản cuối trên database trống `RHM_Hospital_Test_FinalV9_20261029`:32/32 kiểm tra SQL/ET/DAL/chấm công/lương;47/47 kiểm tra khám/đơn thuốc/thu tiền; đăng nhập Admin và mở đủ27 phân hệ. Build Debug/Release thành công. Rà soát độc lập không còn lỗi Critical/Important sau khi sửa các vấn đề khóa kỳ, kiểm thiếu ràng buộc và hiển thị kỳ lương cũ. Bộ kiểm WPF có cảnh báo nullable tồn tại trong mã kiểm tra; các bài kiểm vẫn đạt.

Thử nâng cấp và chạy lại trên database có hai phiếu lương cũ giữ đúng tiền/ghi chú/ngày chốt và không tự điền người chốt. Thử gây lỗi trước pha kê thuốc, cho trình chạy tiếp tục các batch như SSMS: pha V9 bị chặn, không tạo bảng/phiên bản V9. Đối chiếu danh sách bảng/view/trigger/procedure giữa bản V9 gốc và bản hợp nhất không còn thiếu đối tượng theo tên.

Các kiểm chứng của đợt hợp nhất thực hiện trên database thử riêng; nhật ký tại `obj/SqlV9Merge_20261029/`. Sau đó ngày30/10, database chính đã được phục hồi và đồng bộ theo [hướng dẫn phục hồi](PHUC_HOI_VA_DU_LIEU_SQL.md).
