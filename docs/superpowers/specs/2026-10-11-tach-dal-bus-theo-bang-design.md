# Viết lại ET/DAL/BUS theo từng bảng

Yêu cầu: mỗi ET liên quan có DAL và BUS riêng theo mẫu dự án; ET có private fields, constructor rỗng/đầy đủ và get/set; view mới dùng tên tiếng Việt không dấu; mã bảng tự cấp chữ đầu bảng + số tối thiểu hai chữ số; toàn bộ KeHoach.txt tiếp tục chạy ổn định.

Nguồn chuẩn là file Database/QLBenhVienRangHamMat.sql hiện tại, đã đổi PK/FK sang NVARCHAR(50), không IDENTITY; đã đối chiếu SQL Server hiện tại cùng kiểu. Người dùng xác nhận dựa theo file .sql. Không chuyển database, không sửa/xóa dữ liệu có sẵn.

24 bảng liên quan có ET khớp tên/kiểu/nullability/computed, DAL có SQL cụ thể và BUS dùng DAL của đúng bảng. Các bảng lâm sàng có CRUD typed; hóa đơn/thanh toán/xuất kho đi qua nghiệp vụ theo trạng thái. BUS_LAMSANG chỉ điều phối các BUS riêng để phục vụ cache và giao dịch liên bảng, không chứa SQL hoặc bypass DAL từng bảng.

DAL_GIAODICH chia sẻ connection/transaction, kiểm tra schema/phiên bản/trạng thái và cấp mã; không thay DAL các bảng. Cấp mã dùng transaction-owned sp_getapplock + khóa dữ liệu khi lấy hậu tố số cao nhất, rồi INSERT trong cùng giao dịch. Quét cả mã cũ 001 để số không lùi; mã mới D2 (BN01, BN10, BN100). Không đổi mã cũ/khóa ngoại.

Đổi ClinicalVisitDetails→ChiTietKhamDieuTri, ClinicalPrescriptionDetails→ChiTietDonThuoc, InvoicePrintPreview→XemTruocHoaDon, PrescriptionPrintPreview→XemTruocDonThuoc. Đổi lớp/x:Class/tham chiếu/tests đồng thời. AppointmentCalendar trong Controls đổi LichHenKham để thống nhất tài nguyên mới.

Kiểm tra: schema và constructor của24ET, kiểu string keys và CRUD qua BUS từng bảng, mã prefix/số/không trùng nhiều cửa sổ, giao dịch rollback, tài chính/FIFO/thu-hoàn, WPF nguồn và chứng từ. Tất cả fixture SQL rollback. Không commit/push hoặc reset source đang sửa.
