# Kiểm chứng chuẩn hóa hiển thị, mã và lịch sử

Ngày 12/11/2026; SQL Server `lpc:DESKTOP-C9VCE8A`, database chính `QLBenhVienRangHamMat`.

- RED quy tắc mã: 16 lỗi trước sửa, gồm PH/T/CL/HSBA/LSKB/LO/BT và bộ đếm DV124 do lấy số trong REF_DV_ENDO123. Kiểm sau sửa: PASS, gồm DMTT và cấp mã trực tiếp trong giao dịch rollback.
- RED dữ liệu:7 lượt khám Nháp,13 phiếu Chờ khám/Đang khám,624 ca thiếu phòng và các trường chứa nhãn/mã DL26.
- Database chính trước ghi3.224 dòng, gồm3 dòng nhật ký/phiên mới so với snapshot3.221. Đã giữ những phát sinh này; sau ghi3.285 dòng/64 bảng.
- Main và fixture:21 đơn xác nhận in được,18 bàn giao đọc đúng nguồn,13 hóa đơn nháp tính phí được. Phép thử thu/giao và retry được rollback; không tạo xuất kho trùng.
- Đối soát nghiệp vụ:0 sai xác định,0 cần đối chiếu. Findings và presentation-findings rỗng. DBCC CHECKCONSTRAINTS không có vi phạm; không trigger/ràng buộc bị vô hiệu hoặc FK/CHECK không được tin cậy.
- Giữ36 lượt khám, chốt7 lượt nháp qua BUS; xóa6 phiếu chờ không có lượt khám. Phân phòng624 ca, không trùng giờ nhân viên hoặc phòng. Không còn hàng chờ khám trong snapshot.
- Chạy lại chuyển đổi trên fixture: không ghi thêm, SHA256/đếm dòng đủ64 bảng không đổi.
- Thử lỗi CHECK trên fixture khác: CK_Test_RejectNewRooms chặn lúc khôi phục ràng buộc sau đổi mã/thêm phòng. Giao dịch rollback khớp SHA64 trước ghi;0 trigger tắt,0 FK/CHECK không tin cậy,0 marker giả. Xem rollback-before-manifest.json và rollback-expected-failure.log.
- Cài mới từ toàn bộ SQL chính, rồi chạy lại: cả hai lần khớp SHA256/đếm dòng đủ64 bảng với main-manifest.json. Xem install-packaged.log/repeat-packaged.log.
- Kiểm tiếp nhận mới qua BUS tạoPKB037, bắt đầu khám tạoLS037; chạy check và chuyển đổi lại vẫn giữ nguyên SHA64. Vì kiểm vào sau17h, thêm ca trực chỉ trên fixture để API có bác sĩ/phòng đang hoạt động. Database chính không được thêm ca hoặc hàng chờ bởi phép thử này.
- Kiểm cấu trúc và schema dự án PASS; Debug/Release0 lỗi,118/122 cảnh báo cũ.
- Rà soát độc lập đã sửa: kiểm hàng chờ chỉ áp dụng cohort đóng gói; giữ lý do thanh lý thiết bị; cấp mã DMTT đồng nhất. Kiểm kho vật tư tiếp tục bao phủ PN011–PN120 sau bỏ tiền tố DL26.
- SQL chính không chứa `[ĐỒ ÁN]`, `[ĐỒ ÁN 2026]`, `DL26_`, `mô phỏng`, `giả lập`. Nguồn gốc dữ liệu giữ tại tài liệu hướng dẫn và backup.
- Ba database thử riêng đã xóa: `RHM_Hospital_Test_DataConventions_d0f514c0`, `RHM_Hospital_Test_DataConventionsRollback_cbd253b8`, `RHM_Hospital_Test_DataConventionsInstall_16f8c27d`. Database chính và database khác được giữ.

Bản kê SHA và dữ liệu bằng chứng được sao chép trực tiếp từ đầu ra công cụ; SQL snapshot hiện tại nằm trong file database chính, không nhân bản ở đây. Báo cáo này ghi các kết quả quan sát trong phiên thực thi.
