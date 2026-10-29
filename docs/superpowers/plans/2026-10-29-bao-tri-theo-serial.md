# Bảo trì thiết bị theo serial Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mỗi lần bảo trì gắn đúng một thiết bị/serial; thiết bị hư hại xuất hiện trong hàng chờ, phiếu theo đúng vòng đời và cập nhật tình trạng thiết bị trong cùng giao dịch.

**Architecture:** Dùng THIET_BI.MaTB và SoSerial duy nhất hiện có, BAO_TRI_THIET_BI.MaTB làm liên kết; không tạo lại thiết bị gộp hoặc bảng mới. SQL trigger và DAL cùng bảo vệ vòng đời. Giao diện chọn serial hư từ hàng chờ, tách nút chuyển trạng thái khỏi sửa thông tin.

**Tech Stack:** WPF .NET8, BUS/DAL/Dapper, SQL Server2016SP1+, file SQL chính duy nhất.

**Spec:** Yêu cầu người dùng: Chưa xác nhận → Đã xác nhận → Đang thực hiện → Đã hoàn thành. Chỉ Chưa xác nhận được sửa hoặc Hủy; Hủy/Hoàn thành chỉ xem; Đã xác nhận không sửa/hủy nhưng được bắt đầu thực hiện. Bắt đầu đặt thiết bị Đang bảo trì, hoàn thành đặt Đang hoạt động. Chỉ sửa SQL liên quan trực tiếp đến chức năng này.

## Global Constraints

- Giữ tất cả thay đổi cục bộ trước tác vụ và bản SQL V9 đã hợp nhất.
- SQL chỉ thay đổi dữ liệu/schema của THIET_BI và BAO_TRI_THIET_BI; các thủ tục kiểm tra liên quan được bổ sung, không thay bảng khác.
- Serial duy nhất đã có, nhập kho hàng loạt đã tạo từng thiết bị; không tạo serial giả hoặc tách lại những tài sản đã có serial hợp lệ.
- Một serial chỉ có một phiếu đang mở; hủy giữ lịch sử, không xóa vật lý; mã thiết bị/serial không đổi khi đã có lịch sử.
- Bổ sung NgayXacNhan/NgayThucHien nullable, dùng giờ SQL. NgayBatDau là ngày dự kiến; NgayKetThuc do hoàn thành ghi.
- Tình trạng vật lý dùng TinhTrang; giữ TrangThai quản lý/phân phòng hiện tại. Đang bảo trì phải hiển thị đúng thay vì quy thành Hư hại.
- Chuẩn hóa Nháp→Chưa xác nhận, Hoàn thành→Đã hoàn thành; Đã xác nhận cũ có ngày kết thúc là lịch sử hoàn tất, không đưa trở lại hàng chờ. Bảo toàn nội dung/chi phí/serial/ngày đã lưu; dừng khi dữ liệu không thể đối chiếu an toàn.
- Chỉ thử ghi trên database tiền tố RHM_Hospital_Test_; không tự áp vào database sử dụng.

## Review Focus

- Hai phiếu đồng thời cùng serial; cập nhật nhiều dòng SQL không được tác động nhầm thiết bị khác.
- Nội dung phiếu sau xác nhận, trạng thái cuối và serial phải bất biến dù gọi SQL trực tiếp.
- Bắt đầu/hoàn thành lỗi phải rollback cả phiếu và tình trạng thiết bị.
- Không cho trang thiết bị tự ghi đè tình trạng Đang bảo trì hoặc ngưng tài sản còn phiếu mở.
- Nâng cấp/rerun chỉ sửa hai bảng liên quan, giữ các phần SQL trước đó và lịch sử thật.

### Task1: Kiểm hồi quy

**Files:** tools/KiemTraBaoTriSerial.ps1, tools/MaintenanceSerialChecks.cs.txt.

- [x] Viết kiểm ca serial, vòng đời, bất biến, đồng thời và giao dịch; xác minh RED trên database trước sửa.
- [x] Kiểm bulk nhập và UI nếu hạ tầng hiện có cho phép; fixtures luôn rollback.

### Task2: SQL và nghiệp vụ

**Files:** Database/QLBenhVienRangHamMat.sql, ET/ET_BAOTRITHIETBI.cs, DAL/DAL_BAOTRITHIETBI.cs, BUS/BUS_BAOTRITHIETBI.cs, DAL/DAL_THIETBI.cs.

**Interfaces:** BUS.chuyenTrangThaiBaoTri(string maBT,string target,ET_BAOTRITHIETBI banGoc) → bool; thao tác đọc và ghi chung transaction.

- [x] Thêm pha SQL bảo trì serial, chuẩn hóa dữ liệu liên quan, trigger/state machine/index/kiểm tra.
- [x] DAL giới hạn draft edit và chuyển trạng thái có kiểm tra bản gốc; SQL đồng bộ tình trạng asset.
- [x] Chặn đổi serial, ngưng/đổi tình trạng thủ công đang có phiếu mở.

### Task3: Giao diện liên kết

**Files:** Views/BaoTri.*, Views/CuaSoBaoTri.*, Views/ThietBi.*, Controls/DuLieuGiaoDien.cs và hỗ trợ nhập liệu liên quan.

**Interfaces:** DuLieuGiaoDien.ChuyenTrangThaiBaoTri(object record,string target) → object; cập nhật cache của phiếu và thiết bị sau giao dịch.

- [x] Hàng chờ serial hư, phiếu khóa serial, lưu nháp/xác nhận, các nút theo đúng trạng thái.
- [x] Serial là cột rõ ràng trong lịch sử; chi tiết và nút chỉ đọc khi đã kết thúc.
- [x] Trang thiết bị hiển thị Đang bảo trì và đồng bộ khi quay lại màn hình.

### Task4: Kiểm chứng và bàn giao

- [x] Cài mới, nâng cấp dữ liệu cũ và chạy lại SQL; chứng minh phần không liên quan không đổi.
- [x] Kiểm bảo trì serial, kiểm hồi quy SQL V9/khám/thu tiền và mở27 phân hệ; build Debug/Release.
- [x] Rà soát độc lập; ghi hướng dẫn và giới hạn.

## Execution ledger

- User đã mô tả đầy đủ vòng đời và yêu cầu thực hiện; tiếp tục trực tiếp trên nhánh tan, không tạo worktree/commit khác với yêu cầu.
- Đã sao lưu SQL hiện tại tại obj/MaintenanceSerial_20261029/sql-before.sql; các tác vụ lương trước đó được giữ nguyên.
- Skill dispatching-parallel-agents: UI và bộ kiểm có file riêng; SQL/DAL/model/adapters do tác giả chính xử lý.
- RED:5/58 đạt,53 lỗi đúng quy tắc còn thiếu; GREEN58/58 trên database mới/cũ, gồm race và bulk nhập. Bộ kiểm tạo/dọn fixture riêng nếu không có thiết bị sẵn, không để lại dữ liệu trên database sạch.
- Ruling: giữ NULL cho thời điểm lịch sử chưa được lưu; không suy đoán. Ngày kết thúc dự kiến cũ của nháp/lần đang làm được bảo toàn trong ghi chú trước khi dành cột cho thời điểm thực tế.
- Scope check: file SQL hiện tại bắt đầu đúng nguyên văn sql-before.sql; chỉ nối PHA6. Kiểm nâng cấp/rerun:58 bảng không liên quan giữ schema và fingerprint dữ liệu.
- Final verification:58 bảo trì +32 SQL V9 +47 quy trình +2 giao diện (27 phân hệ) đạt; WPF commit/cache/rollback thật đạt; Debug/Release0 lỗi (còn cảnh báo hiện có). Không ghi database sử dụng, không commit/push.
- Final review: không có Critical/Important. Minor về nhãn thời điểm lịch sử thiếu đã sửa; không còn mục bị hoãn.
