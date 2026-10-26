# Phạm vi triển khai

Nguồn yêu cầu: hai file `KeHoach_CaiTo_QuyTrinhKham_6ChucNang.docx` và `.md` tại gốc dự án. Markdown là kế hoạch đánh dấu tiến độ; bản Word giữ nguyên.

- Baseline Git: `7470bf1`, nhánh ban đầu `tan`; nhánh thực hiện `cai-to-quy-trinh-kham-6-chuc-nang`.
- WPF .NET 8, ET–BUS–DAL, SQL Server/Dapper giữ nguyên.
- P01: khảo sát, bản sao SQL, tiền kiểm, kết quả kiểm tra ban đầu.
- P02 đã tích hợp: ngữ cảnh/quyền theo chức vụ, snapshot SQL có phạm vi và giới hạn nội dung theo công việc, guard ghi chung transaction, cache xóa khi đổi phiên, quyền module/nút/điều hướng và ClinicalChecks/ClinicalUiChecks.
- Điểm tích hợp thêm sau rà soát: một dòng ở `Views/DangNhap.xaml.cs` để chốt principal sau khi xác thực đã thành công, cùng helper ở `PhienDangNhap.cs`; không đổi thuật toán đăng nhập hoặc quyền module khác.
- P03: thêm sáu bảng, cột tám bảng cũ, ba script upgrade/verify/rollback cạnh preflight P01, file SQL cài mới độc lập, ET/DAL/BUS và metadata/validator tương ứng. Không thay file SQL baseline hoặc định nghĩa trigger/view cũ; trigger bổ sung dùng tiền tố CW.
- P04: thêm BUS/DAL_TIEPNHAN và hợp đồng chọn bác sĩ/tiếp nhận; quyền điều phối ngoài lịch CV004; bốn tab, tìm ngày sinh/lần khám gần nhất, form tiếp nhận mới và guard chặn lập phiếu bằng form chung. Không thêm schema hoặc sửa thuật toán lập lịch; chỉ cập nhật trạng thái lịch Chờ khám trong transaction tiếp nhận như điểm tích hợp cũ.
- P05: thêm hợp đồng và aggregate BUS/DAL khám, tình trạng, kế hoạch và mẫu; màn khám bốn tab, cấu hình soạn/duyệt mẫu; bắt đầu idempotent, snapshot, ký/khóa, bảo vệ nháp. Hồ sơ nhúng lịch sử chỉ đọc; thao tác khám ở workspace Khám điều trị. Không đổi schema, tạo dịch vụ thực hiện hoặc seed chuyên môn.
- P06: thêm hợp đồng, API lưu/xác nhận/hủy và workspace Dự kiến/Thực tế; xác minh người được giao, lượng theo bước cùng hồ sơ qua nhiều lượt, snapshot và nguồn đã thu; chặn đường form chung bỏ qua API. Nguồn thu chỉ dùng dịch vụ xác nhận, quầy thu không nhận kết quả/lý do chuyên môn. Không đổi schema, thuật toán lịch hoặc tự tạo hóa đơn.
- P07: thêm DTO/API tìm, lưu, ký, hủy nháp, thay đơn và cấu hình hướng dẫn; workspace chọn nhiều thuốc, thông tin an toàn/ngày sinh/tuổi, snapshot, nguồn preview cố định và in bản ký. Chặn form chung ghi/xóa đầu/dòng đơn, lịch sử chỉ đọc có liên kết đến khu kê đơn. Không đổi schema, tự soạn liều hoặc trừ kho khi kê/ký.
- P08–P12 chưa được nghiệm thu. Không coi các endpoint nền là sáu màn hình đã cải tổ hoàn chỉnh.
- Không sửa nhân sự, kho, lịch làm việc, lịch hẹn hoặc báo cáo để làm xanh lỗi baseline.
- Không áp migration lên `QLBenhVienRangHamMat`. Kiểm tra ghi dùng `RHM_ClinicalWorkflow_Test_20261025`, bản restore `_Rollback_20261025`, cài mới `_FreshFinal_20261025` và fixture rollback. Bản `_Fresh_20261025` là lần cài mới kiểm tra trước sửa review.

Đã bật quyền mở năm module lâm sàng theo chức vụ sau khi chuyển nguồn đọc/guard ghi của chúng. Module ngoài phạm vi giữ quyền Admin/User. Luồng ghi tài chính cũ bị khóa trong chế độ lâm sàng; hóa đơn lịch sử chỉ đối chiếu/in lại, chờ P09/P10.

Điểm tích hợp lịch hẹn có giới hạn: `Views/LichKham.xaml/.cs` dùng snapshot đã có quyền khi nhúng trong hồ sơ, ngăn việc đọc toàn viện trước khi lọc. Đường chạy độc lập giữ nguồn và hành vi cũ, được kiểm chứng riêng. Không đổi thuật toán lịch hoặc phân quyền module LichKham.
