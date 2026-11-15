# Database Sprint3 + AI

Chạy toàn bộ `Database/QLBenhVienRangHamMat.sql`. File thay thế installer đổi tên `RHM_F5_Khong_GhiNhoPhien.sql` và các bản vá AI cài riêng. Không chạy các bản vá cũ sau khi cài bản hợp nhất.

Schema có **74 bảng**: giữ nguyên 64 bảng Sprint3, metadata/phân công X-quang và dữ liệu phục hồi; thêm 10 bảng:

| Nhóm | Bảng |
| --- | --- |
| Quỹ phép | `RHM_CAU_HINH_QUY_PHEP`, `RHM_NGAY_PHEP_NAM` |
| Bản khai và nhân viên duyệt | `AI_PHIEN_TIEP_NHAN`, `AI_TIEP_NHAN_NHAP`, `AI_TIN_NHAN`, `AI_SU_KIEN`, `AI_KET_QUA_TAO_PHIEU` |
| Tương thích service AI | `AI_SERVICE_IDENTITY`, `AI_SERVICE_INTAKE_SESSION`, `AI_YEU_CAU_TAO_PHIEU` |

`usp_RHM_KiemTraSchema` kiểm cả nền Sprint3 và schema mở rộng. Các bước kiểm nền loại trừ đúng 10 bảng mở rộng khi đối chiếu số bảng cũ; kiểm tổng cuối yêu cầu đủ 74 bảng. Các FK/CHECK phải đáng tin cậy và trigger phải bật.

## Tiếp nhận và phụ thu

Bản khai AI không tự ghi bệnh án. Nhân viên có quyền tiếp nhận, tài khoản hoạt động và phiên đăng nhập hiện hành duyệt để lập phiếu. Hai procedure tạo từ bản khai và liên kết phiếu đã có đều kiểm lại phiên `RHM.MaPhien`, tài khoản, chức vụ/quyền và menu `PhieuHuongDan` trong transaction.

Tạo phiếu kiểm lại ca, giờ bắt đầu/kết thúc kể cả ca qua ngày, phòng đang hoạt động, khoa/chuyên khoa, tài khoản bác sĩ và đơn nghỉ đã duyệt. Khám dịch vụ yêu cầu bác sĩ có mặt và dịch vụ đúng chuyên khoa. Retry cùng bản khai trả lại phiếu đã tạo.

Phiếu mới lưu `LoaiKham` và `PhiPhuThuDichVu` theo chuẩn Sprint3: thông thường 0đ, dịch vụ 30.000đ. Hóa đơn ưu tiên `MaPhieuPhuThu`. `MaPhienAI` chỉ dùng cho phiếu AI lịch sử chưa lưu phí trên phiếu. View `VW_AI_PHIEU_PHU_THU(MaPhien, MaPhieu, PhuThu)` tra cả kết quả nhân viên duyệt và service AI; khi trùng mã phiên ưu tiên kết quả nhân viên duyệt.

Mỗi dòng hóa đơn có đúng một trong năm nguồn: dịch vụ, xuất thuốc, đơn thuốc, phiếu phụ thu hoặc phiên AI lịch sử. Trigger kiểm nguồn cùng lượt, chống thu trùng qua hai loại nguồn, bảo vệ khoản phí bắt buộc khỏi giảm giá và giữ quy tắc chứng từ đã chốt. Nguồn phiên AI được kiểm qua trigger vì có thể thuộc một trong hai bảng phiên. Các quan hệ phiên → kết quả → phiếu vẫn có FK.

Service AI cũ được giữ tương thích, yêu cầu identity thiết bị được cấp riêng; tài khoản/identity thiết bị không được nạp sẵn. Nhật ký service ghi đủ UTC, event và module để không mất khi chạy lại bước chuẩn hóa audit.

## Lương và quỹ phép

- Chính sách `DonGiaCa IS NULL` giữ công thức lương tháng V9 và các dòng nguồn hoa hồng đã có.
- Chính sách có `DonGiaCa` rõ ràng dùng đơn giá ca × công thực tế × 2, tự đối soát hoa hồng từ dịch vụ xác nhận/hóa đơn đã thu đủ; tính lại không nhân đôi hoa hồng.
- Đơn cũ `LoaiNghi = Chưa xác định` giữ công phép V9 theo ngày. Đơn phép năm đã phân loại dùng sổ phép mới: một ngày trừ quỹ một lần và hưởng 0,5 công, kể cả có hai ca. Ngày có ledger không được trả thêm từ nguồn đơn cũ.
- Không tự chuyển đơn phép cũ vào sổ phép. Ngày lễ ngừng hoạt động không tham gia tính công chuẩn. Phiếu lương đã chốt và tiền lịch sử V8 được giữ nguyên.

## Quyền menu

Chỉ nâng matrix User nếu khớp toàn bộ seed cũ: 25 dòng, ba quyền cơ bản, cùng thời điểm khởi tạo và không có audit chỉnh quyền của vai trò. Khi đó thêm quyền menu nghiệp vụ đã được sử dụng ở Sprint3; quyền theo chức vụ vẫn được kiểm ở UI/DAL. `TroLyAI` mới kế thừa quyền `PhieuHuongDan`; không ghi đè quyền đã tồn tại. Các cấu hình tùy chỉnh/audit chỉnh quyền được giữ nguyên. Với thay đổi trực tiếp ngoài ứng dụng không có audit và tái tạo đúng seed cũ, không thể phân biệt cấu hình đó với seed chưa sửa.

## Kiểm chứng

`tools/KiemTraHopNhatAI.ps1` chỉ nhận database mới có tiền tố `RHM_Hospital_Test_`; tự đổi tên database trong bản SQL tạm. Không đụng database thật. Kiểm gồm cài mới, schema 74 bảng, FK/CHECK/trigger, hai luồng nhân viên tạo phiếu, retry, liên kết giao dịch, từ chối giả nhân viên, lương ca và quỹ phép, chạy lại và đối chiếu số dòng từng bảng. Các fixture nghiệp vụ được rollback.

Đã kiểm trên `RHM_Hospital_Test_MergeSql_20261115_F`: cài/chạy lại và nhân viên duyệt đạt; `SqlSyncChecks.exe` đạt **32/32**, giữ nguyên assertions lương tháng V9. Kiểm bổ sung đạt lương ca rõ ràng, hai ca nghỉ chỉ trừ một ngày quỹ, 0,5 công phép, hoa hồng không trùng và chốt lương. Bằng chứng gọn tại `evidence/20261115-merge-ai/SQL_VERIFICATION.txt`.
