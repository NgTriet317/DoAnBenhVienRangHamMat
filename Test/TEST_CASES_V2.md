# Testcase Kiosk AI V2

| STT | Tình huống | Kết quả mong đợi |
|---|---|---|
| 01 | Bệnh nhân mới gửi đầy đủ thông tin, không trùng | Tạo BN, HS, PKB; AI = DaDuyet; mở A5 |
| 02 | Bệnh nhân cũ chọn đúng hồ sơ | Dùng lại BN và HS; tạo một PKB |
| 03 | Bệnh nhân cũ chưa chọn hồ sơ | Không phát hành |
| 04 | Mới nhưng trùng SDT với hồ sơ hiện có | Báo nghi trùng, không tạo bản ghi |
| 05 | Bệnh nhân khai đã từng khám, cố chọn tạo mới | Chặn, yêu cầu xác minh hồ sơ |
| 06 | Họ tên / ngày sinh / SDT không khớp BN được chọn | Chặn liên kết; không cập nhật BN |
| 07 | Phiên CanHoTro / CanHoTroGap=1 | Chặn tạo tự động, hướng nhân viên hỗ trợ |
| 08 | Phiên DangKhaiBao / DaHuy | Chặn |
| 09 | Phiên DaDuyet bấm duyệt lại | Chỉ trả về MaPhieu cũ, không tăng số phiếu |
| 10 | Hai máy/nhân viên duyệt đồng thời một phiên | Một phiếu được tạo |
| 11 | Hai phiên khác nhau được duyệt cùng phòng cùng lúc | Không trùng SoThuTu trong phòng/ngày |
| 12 | Không chọn phòng | Chặn |
| 13 | Phòng ngừng hoạt động | Chặn tại SQL |
| 14 | Khám dịch vụ chưa chọn bác sĩ | Chặn |
| 15 | Bác sĩ không có lịch đã xác nhận hôm nay / khác phòng | Chặn |
| 16 | Dịch vụ ngừng hoạt động / khác chuyên khoa | Chặn |
| 17 | Bệnh nhân đã có phiếu còn hiệu lực hôm nay cùng phòng | Báo cần đối chiếu |
| 18 | Stored procedure lỗi giữa chừng (FK, quyền, timeout) | Rollback cả BN/HS/PKB/AI |
| 19 | Phiếu thành công | Danh sách Phiếu hướng dẫn chọn đúng MaPhieu, in A5 |
| 20 | Khám dịch vụ hợp lệ | KQ phu thu du kien 30.000, chưa tự sinh hóa đơn |
| 21 | Tài khoản không hoạt động / không có quyền tiếp nhận | Không duyệt |
| 22 | Bản khai thiếu ngày sinh, số điện thoại | Không duyệt |
