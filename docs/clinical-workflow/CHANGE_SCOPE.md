# Phạm vi triển khai

Nguồn yêu cầu: hai file `KeHoach_CaiTo_QuyTrinhKham_6ChucNang.docx` và `.md` tại gốc dự án. Markdown là kế hoạch đánh dấu tiến độ; bản Word giữ nguyên.

- Baseline Git: `7470bf1`, nhánh ban đầu `tan`; nhánh thực hiện `cai-to-quy-trinh-kham-6-chuc-nang`.
- WPF .NET 8, ET–BUS–DAL, SQL Server/Dapper giữ nguyên.
- P01: khảo sát, bản sao SQL, tiền kiểm, kết quả kiểm tra ban đầu.
- P02 đang triển khai: hợp đồng ngữ cảnh, quyền theo chức vụ, truy vấn bác sĩ có phạm vi và project ClinicalChecks. Chỉ đánh dấu từng đầu việc có bằng chứng.
- Điểm tích hợp thêm sau rà soát: một dòng ở `Views/DangNhap.xaml.cs` để chốt principal sau khi xác thực đã thành công, cùng helper ở `PhienDangNhap.cs`; không đổi thuật toán đăng nhập hoặc quyền module khác.
- P03–P12 chưa được nghiệm thu. Không coi các endpoint nền là sáu màn hình đã cải tổ hoàn chỉnh.
- Không sửa nhân sự, kho, lịch làm việc, lịch hẹn hoặc báo cáo để làm xanh lỗi baseline.
- Không áp migration lên `QLBenhVienRangHamMat`. Kiểm tra ghi chỉ dùng bản khôi phục `RHM_ClinicalWorkflow_Test_20261025` và fixture rollback.

Không bật quyền mở các màn hình cũ cho các vai trò mới trước khi chuyển nguồn đọc và mọi đường ghi của màn hình sang hợp đồng có quyền. Điều này tránh mở giao diện đang tải toàn bệnh viện hoặc thu từng phần trong khi P02/P09 chưa xong.
