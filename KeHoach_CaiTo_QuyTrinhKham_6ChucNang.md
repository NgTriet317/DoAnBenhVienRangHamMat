# Quy trình khám và thanh toán ngoại trú Implementation Plan

> **Tiến độ 26/10/2026:** P01–P08 đã hoàn tất phần nền, migration, hồ sơ, tiếp nhận, khám, kế hoạch, dịch vụ, kê đơn snapshot và bảng tính phí. Đã kiểm chứng 160 ca SQL, 39 ca WPF và xem phí đồng thời; dữ liệu và tồn cũ giữ nguyên. P09–P12 còn lại. Xem [tiến độ và quyết định](docs/clinical-workflow/TIEN_DO.md), [baseline](docs/clinical-workflow/BASELINE.md) và [kết quả kiểm thử](docs/clinical-workflow/KET_QUA_KIEM_THU.md).

> **For agentic workers:** Triển khai từng tác vụ và đánh dấu `- [ ]` khi hoàn thành. Nếu môi trường có Superpowers, dùng `superpowers:executing-plans`; không phụ thuộc vào việc cài thêm kỹ năng để đọc hoặc thực hiện tài liệu này. Người dùng yêu cầu giao kế hoạch cho Codex terminal trong Visual Studio 2022, chưa yêu cầu triển khai trong phiên lập kế hoạch.

**Goal:** Cải tổ giao diện, nghiệp vụ và dữ liệu của hồ sơ bệnh nhân, khám điều trị, đơn thuốc, dịch vụ thực hiện, hóa đơn và thanh toán thành một quy trình ngoại trú thống nhất.

**Architecture:** Giữ WPF và ba lớp ET–BUS–DAL hiện có. Bổ sung các đầu mối nghiệp vụ có kiểm tra quyền, truy vấn SQL theo phạm vi người dùng và một giao dịch thanh toán trọn gói. Chỉ cập nhật cache giao diện sau khi SQL commit thành công.

**Tech Stack:** C#; .NET 8; WPF XAML; SQL Server; Dapper và Dapper.Contrib đang có trong dự án; Visual Studio 2022 trên Windows. Không chuyển sang web, không đổi framework, không thêm thư viện UI lớn.

**Spec:** Phần 1 đến phần 10 của chính tài liệu này là đặc tả thiết kế; phần 11 đến phần 14 là kế hoạch triển khai và nghiệm thu. Hai phần phải được đọc cùng nhau.

**Ngày lập:** 24 tháng 10 năm 2026. **Nguồn khảo sát:** `Nhom4_QuanLyBenhVienRHM(1).zip`. Tất cả đường dẫn bên dưới tính từ thư mục chứa `BenhVienRangHamMat.sln`.

## Global Constraints

- Chỉ sửa sáu miền chức năng được yêu cầu. Tiếp nhận là luồng con của hồ sơ bệnh nhân; hóa đơn và thanh toán tiếp tục dùng chung mục `HoaDon` hiện có.
- Giữ mã khóa `NVARCHAR(50)`, tiền `DECIMAL(18,2)`, lượng `DECIMAL(18,3)` và quy ước giờ địa phương bệnh viện trong `DATETIME2(0)`.
- Bác sĩ chỉ nhận và sửa lượt được phân cho chính `PhienDangNhap.MaNV`; kiểm tra lại ở BUS/DAL khi đọc chi tiết và khi ghi.
- Gợi ý điều trị và hướng dẫn thuốc phải được bác sĩ kiểm tra. Hệ thống không tự chẩn đoán hoặc tự xác nhận chỉ định.
- Luồng mới thu đủ một lần, không trả góp, không ghi nợ, không thu thiếu. Tiền khách đưa và tiền thối là hai giá trị riêng với số tiền hạch toán thu.
- Giữ nguyên dữ liệu cũ. Không chạy lại script cài mới lên database đang dùng, không tự xóa/chuyển trạng thái/chốt nợ các chứng từ lịch sử.
- Không thay đổi thuật toán nhập xuất kho, lịch làm việc, lịch hẹn, nhân sự, lương, nhà cung cấp, thiết bị, danh mục hàng hóa hoặc báo cáo ngoài phạm vi tích hợp tối thiểu đã chỉ rõ.
- Không khai báo hoàn thành chỉ dựa vào build; phải qua kiểm thử SQL, phân quyền, đồng thời và thao tác WPF trên Windows.

## Review Focus

1. Người dùng mở trực tiếp mã lần khám của bác sĩ khác hoặc bỏ bộ lọc trên UI: BUS/DAL vẫn từ chối. Chủ sở hữu ca thử: P02 và P05.
2. Hai quầy cùng thanh toán hoặc người dùng bấm lại sau mất kết nối: chỉ một bộ hóa đơn, thu tiền và xuất thuốc được commit. Chủ sở hữu: P09.
3. Tồn thuốc hoặc giá đổi sau khi xem bảng tính: giao dịch kiểm tra lại toàn bộ, không lấy số tiền cũ từ UI. Chủ sở hữu: P08 và P09.
4. Một bệnh nhân có lịch sử bác sĩ khác, nhiều buổi điều trị hoặc không có chỉ định thuốc: xem đúng lịch sử, chỉ thu dịch vụ của lượt hiện tại, vẫn hoàn tất hợp lệ khi không kê thuốc. Chủ sở hữu: P04 đến P07.
5. Database cũ có phiếu đã cấp thuốc, hóa đơn nháp hoặc thu một phần: nâng cấp không trừ kho lần nữa hoặc làm mất lịch sử. Chủ sở hữu: P01, P03 và P12.

## 1 Mục tiêu và quyết định thiết kế

Bản kế hoạch tổ chức lại sáu chức năng thành luồng: tiếp nhận đúng bệnh nhân và dịch vụ khám, phân đúng bác sĩ, khám và ghi nhận tình trạng, bác sĩ duyệt kế hoạch, xác nhận dịch vụ thực tế và đơn thuốc, thu đủ tiền, in chứng từ rồi giao thuốc. Mỗi lần khám có mã nguồn xuyên suốt để tránh chọn nhầm bệnh nhân khi chuyển màn hình.

Đây là mô hình khám ngoại trú tự chi trả phục vụ đồ án răng hàm mặt. Trường mã BHYT hiện có vẫn được giữ để lưu thông tin, nhưng không bổ sung tính quyền lợi BHYT, nội trú, cấp cứu, tạm ứng, công nợ hoặc hóa đơn điện tử thuế. Quy tắc thu đủ là yêu cầu của luồng phần mềm này; không mô tả mọi trường hợp tài chính ngoài đời bằng quy tắc đó.

### 1.1 Phương án được đề xuất

| Phương án | Phạm vi và đánh đổi | Kết luận |
| --- | --- | --- |
| Chỉ thay giao diện | Nhanh nhưng vẫn còn quyền Admin/User, thu từng phần và kê thuốc thiếu mẫu hướng dẫn | Không đủ yêu cầu |
| Cải tổ trên nền hiện tại | Giữ WPF, ET–BUS–DAL và quan hệ hiện có; thêm cấu hình gợi ý, quyền theo chức vụ và thanh toán trọn gói | Chọn làm phương án triển khai |
| Viết lại toàn bộ nền tảng | Đổi kiến trúc, kho, phân quyền và các màn hình chung; tăng rủi ro ảnh hưởng các phân hệ khác | Vượt phạm vi |

Phương án thứ hai là đề xuất của người lập kế hoạch dựa trên mã nguồn và quyền điều chỉnh mà người dùng đã giao. Việc lập tài liệu này không đồng nghĩa đã thay đổi ứng dụng hoặc database.

### 1.2 Các điều chỉnh nghiệp vụ cần giữ

- Dịch vụ tiếp nhận là nhu cầu khám ban đầu, chưa phải khoản thu. Chỉ dịch vụ thực tế được bác sĩ xác nhận mới được tính tiền; phí khám ban đầu cũng phải có một dòng dịch vụ thực hiện, không tự thu trùng từ phiếu tiếp nhận.
- Kế hoạch điều trị được tự gợi ý từ danh mục tình trạng và mẫu đã duyệt. Bác sĩ xem, sửa, chọn áp dụng; không tự biến mô tả triệu chứng thành chẩn đoán hoặc điều trị cuối cùng.
- Liều và hướng dẫn là nội dung gợi ý đã duyệt. Bác sĩ chủ yếu chỉnh lượng và ngày nhưng vẫn được sửa liều, đường dùng, hướng dẫn nếu cần; không khóa cứng các thông tin chuyên môn.
- Không bắt buộc mỗi lượt phải có thuốc. Khi không có chỉ định, bác sĩ chọn “Không kê thuốc” và ghi lý do; không tạo đơn rỗng hoặc đơn hủy giả để đủ bước.
- Thu tiền trước khi giao thuốc vật lý. Để tương thích database hiện tại, chứng từ xuất thuốc và hóa đơn được tạo trong cùng giao dịch thu đủ; dược sĩ xác nhận giao thuốc sau khi giao dịch đó đã commit.

## 2 Hiện trạng đã kiểm tra trong mã nguồn

Các nhận xét sau dựa trên nội dung file, không phải kết quả chạy ứng dụng hay kết nối database đang sử dụng. P01 phải xác nhận lại trên Windows và bản sao SQL trước khi sửa.

| Thành phần | Hiện trạng xác định được | Hướng xử lý |
| --- | --- | --- |
| Nền tảng | `RangHamMat.Wpf.csproj` dùng `net8.0-windows`, `UseWPF`; có ET, BUS và DAL riêng | Giữ nguyên nền tảng |
| Hồ sơ | `HO_SO_BENH_AN.MaBN` có UNIQUE; mỗi bệnh nhân một hồ sơ tổng, nhiều phiếu khám | Giữ; mỗi lần quay lại tạo phiếu/lần khám mới |
| Tiếp nhận | `PHIEU_KHAM_BENH` đã có `MaDV`, `MaBS`, `MaPhong`, `MaNVTiepNhan`; dịch vụ và bác sĩ đang là lựa chọn chung | Làm bộ chọn dịch vụ → bác sĩ → phòng và kiểm tra khi lưu |
| Phân quyền | `MainWindow.xaml.cs` chỉ có Admin/User; User hiện chỉ mở tổng quan, ca làm và xin nghỉ | Bổ sung quyền nghiệp vụ cho đúng các mục đang cải tổ |
| Bác sĩ phụ trách | `KhamDieuTri.Refresh()` lọc theo bệnh nhân, từ khóa, ngày và trạng thái; không thấy điều kiện `MaBS = phiên đăng nhập` trong truy vấn danh sách này | Thay nguồn đọc cho màn hình bằng truy vấn có phạm vi bác sĩ |
| Danh mục chuyên môn | `DICH_VU.MaCK` và `NHAN_VIEN.MaCK` đã có | Dùng để tìm bác sĩ phù hợp; đọc lịch làm việc nếu có dữ liệu phù hợp |
| Đơn thuốc | Có lưu đơn nhiều dòng và giao dịch lưu; `CHI_TIET_DON_THUOC` có liều, lượng, ngày và hướng dẫn | Giữ giao dịch; thêm bộ chọn theo nhóm và mẫu hướng dẫn |
| Loại thuốc | `LOAI_HANG_HOA` đang phân nhóm theo dạng viên, dung dịch, dùng ngoài cùng các loại vật tư/thiết bị | Giữ phân loại kho; bổ sung nhóm hỗ trợ kê đơn riêng |
| Tính tiền thuốc | `CHI_TIET_HOA_DON` trỏ `MaCTPX`; trigger chỉ nhận thuốc trên phiếu xuất đã ghi sổ | Giữ liên kết nguồn; điều phối xuất thuốc và thu trong cùng transaction |
| Thu tiền | `RecordPayment` cho phép số thu nhỏ hơn số còn phải trả; UI có Thu/Hoàn; chưa có cột tiền khách đưa và thối lại | Tạo luồng thu đủ mới; giữ lịch sử tài chính cũ |
| Bảo vệ dữ liệu | Có trigger tính tiền, khóa nguồn; UNIQUE theo dịch vụ/chi tiết xuất đã vào hóa đơn | Tái sử dụng, chỉ bổ sung kiểm tra còn thiếu |
| Kiểm tra hiện có | Có `Checks`, `SchemaChecks`, `PersistenceChecks`, `FinanceChecks`, `WpfSmoke` và bộ kiểm tra các miền khác | Chạy baseline; thêm kiểm tra lâm sàng và thanh toán mới |

### 2.1 Các tệp cần đọc trước khi viết code

`RangHamMat.Wpf/MainWindow.xaml.cs`, `AppWorkspace.xaml.cs`, `PhienDangNhap.cs`; các view được liệt kê ở phần 10; `Controls/DuLieuGiaoDien.cs`, `BieuMauHoSo.cs`, `TreatmentModels.cs`, `PrescriptionModels.cs`, `DieuHuongHoSo.cs`; các lớp BUS/DAL tương ứng; `DAL/dbconnect.cs`; `Database/QLBenhVienRangHamMat.sql` và các file `Database/UI_DBv7*.json`, `Database/ui-table-map*.json`.

## 3 Bảng luồng hoạt động đích

| Bước | Người thực hiện | Thao tác chính | Kết quả và điều kiện chuyển bước |
| --- | --- | --- | --- |
| 1 | Tiếp nhận hoặc điều dưỡng | Tìm theo mã BN, tên, ngày sinh và điện thoại; đối chiếu trước khi tạo mới | Chọn đúng `MaBN`; cảnh báo trùng, không tự gộp người trùng tên |
| 2 | Tiếp nhận hoặc điều dưỡng | Lập hoặc mở hồ sơ tổng, kiểm tra tiền sử và dị ứng | Có `MaHS`; không ghi đè lịch sử các lượt trước |
| 3 | Tiếp nhận hoặc điều dưỡng | Chọn dịch vụ khám, bác sĩ phù hợp, phòng, lý do; gắn lịch hẹn nếu có | Một `MaPhieu` chờ khám; người tiếp nhận lấy từ phiên; số thứ tự do SQL cấp |
| 4 | Bác sĩ được phân | Mở hàng đợi của mình, bấm Bắt đầu khám | Tạo duy nhất một `MaLS` cho `MaPhieu`, ghi bác sĩ và thời điểm; phiếu sang Đang khám |
| 5 | Bác sĩ được phân | Nhập triệu chứng, kết luận, các tình trạng và vị trí răng nếu cần | Lưu nháp được; không mất nội dung khi đổi tab |
| 6 | Bác sĩ được phân | Xem gợi ý kế hoạch, sửa/chọn bước và duyệt | Có kế hoạch nháp hoặc đã duyệt; chưa tạo khoản thu chỉ vì có bước dự kiến |
| 7 | Bác sĩ và người thực hiện phù hợp | Ghi kết quả dịch vụ thực tế, số lượng, thời gian; bác sĩ xác nhận | `DICH_VU_THUC_HIEN` đủ điều kiện thu; bước chưa làm giữ cho buổi sau |
| 8 | Bác sĩ được phân | Kê một hoặc nhiều thuốc; duyệt liều, hướng dẫn, lượng và số ngày | Đơn xác nhận, hoặc ghi Không kê thuốc với lý do |
| 9 | Bác sĩ được phân | Kiểm tra tổng kết, hoàn tất lượt khám và chuyển quầy | Lần khám Đã xác nhận; phiếu Chờ thanh toán; dữ liệu chuyên môn được khóa |
| 10 | Tiếp nhận có quyền thu hoặc thu ngân | Xem bảng tính dịch vụ thực tế và thuốc; nhập tiền khách đưa hoặc xác nhận chuyển khoản | Bảng tính được kiểm tra lại; thiếu tiền không ghi giao dịch thu |
| 11 | Thu ngân qua hệ thống | Bấm Thanh toán và in hóa đơn | Một transaction tạo/xác nhận hóa đơn, xuất thuốc kỹ thuật, thu đủ và phiếu chờ giao; thành công mới báo Đã thanh toán |
| 12 | Dược sĩ | Đối chiếu đơn và thanh toán, giao thuốc và hướng dẫn | Ghi người giao, thời gian; không trừ kho lần hai. Hoàn tất khi không cần giao thuốc hoặc mọi đơn đã giao |

Luồng lịch hẹn: đọc lịch đã có, đối chiếu bệnh nhân, dịch vụ, bác sĩ và phòng; giữ các khóa nguồn. Không sửa màn hình hay thuật toán lập lịch. Nếu lịch cần thay đổi bác sĩ/phòng, xử lý bằng chức năng lịch hẹn hiện có trước khi tiếp nhận, tránh vi phạm `TR_PHIEU_LienKet`.

### 3.1 Quy tắc chọn bác sĩ và hàng đợi

- Bác sĩ phải đang làm việc, có tài khoản hoạt động, chức vụ bác sĩ và chuyên khoa phù hợp `DICH_VU.MaCK`. Không lấy mọi nhân viên làm danh sách bác sĩ.
- Đọc lịch làm việc hiện có để hiển thị ca/phòng, số bệnh nhân đang chờ. Có lịch xác định không làm việc hoặc đang nghỉ đã duyệt thì không phân lượt đó.
- Nếu chưa có dữ liệu lịch trong thời điểm tiếp nhận, ghi rõ “Chưa có lịch làm việc để đối chiếu”; chỉ người tiếp nhận có quyền điều phối mới được phân với lý do và nhật ký. Không coi thiếu lịch là bác sĩ tự động sẵn sàng.
- Chọn dịch vụ mới phải xóa bác sĩ/phòng cũ khi không còn phù hợp. Không có bác sĩ phù hợp thì chặn lưu và thông báo cụ thể, không tự chọn chuyên khoa khác.
- Một phiếu dùng một dịch vụ khám ban đầu và một bác sĩ chính. Dịch vụ điều trị bổ sung được bác sĩ chỉ định trong lượt khám; không mở rộng thành luồng hội chẩn hoặc nhiều bác sĩ cùng sửa một lượt.
- Hàng đợi mặc định hôm nay, lọc theo bác sĩ đăng nhập và trạng thái. Bác sĩ có thể xem các lượt cũ của mình; không được dùng bộ lọc để đổi sang danh sách bác sĩ khác.
- Cấp số thứ tự trong transaction, khóa phạm vi ngày và phòng bằng `sp_getapplock` hoặc cơ chế khóa SQL tương đương; không dùng `Max()+1` trong cache làm nguồn chính. Số không phải khóa nghiệp vụ thay cho `MaPhieu`.

## 4 Trạng thái và quyền thao tác

### 4.1 Trạng thái thống nhất

| Đối tượng | Giá trị lưu trong SQL | Quy tắc |
| --- | --- | --- |
| Phiếu tiếp nhận mới | Chờ khám → Đang khám → Chờ thanh toán → Chờ nhận thuốc → Hoàn tất; không có thuốc bỏ qua Chờ nhận thuốc; Hủy trước khi khám | Chờ nhận thuốc nghĩa là đã thanh toán và chưa giao xong; phiếu chỉ Hoàn tất khi mọi đơn đã giao |
| Lần khám | Nháp, Đã xác nhận | Giữ giá trị và CHECK hiện có; hiển thị Nháp là Đang khám, Đã xác nhận là Đã hoàn tất chuyên môn |
| Kế hoạch | Nháp, Đã xác nhận, Đang thực hiện, Đã hoàn thành, Hủy | Giữ các giá trị đang dùng nếu hợp lệ; nhãn UI có thể là Chờ bác sĩ duyệt thay cho Nháp |
| Dịch vụ thực hiện | Nháp, Đã xác nhận, Hủy, Ngừng áp dụng | Giữ CHECK hiện có; UI Đã xác nhận nghĩa là đã thực hiện và được xác nhận |
| Đơn thuốc | Nháp, Đã xác nhận, Hủy, Ngừng áp dụng | Giữ CHECK hiện có; không tự tạo đơn khi không kê thuốc |
| Hóa đơn | Nháp, Đã xác nhận, Hủy | Không thêm Đã thanh toán vào CHECK cũ; tình trạng thanh toán là giá trị tính từ giao dịch |
| Thanh toán mới | Thu, Đã xác nhận | Thu đủ một lần trong giao dịch trọn gói; Hoàn cũ chỉ đọc trong màn hình mới |
| Bàn giao thuốc mới | Chờ giao, Đã giao | Chỉ chuyển Đã giao khi hóa đơn đã thu đủ; không sinh xuất kho ở thao tác giao |

Không đổi hàng loạt trạng thái lịch sử như `Đã khám`. Lớp trình bày phải ánh xạ chúng thành nhãn lịch sử; các nút quy trình mới không tự hoạt động trên bản ghi không đáp ứng điều kiện.

### 4.2 Ma trận quyền trong phạm vi này

| Hành động | Tiếp nhận hoặc điều dưỡng | Bác sĩ | Thu ngân | Dược sĩ | Admin không có chức vụ bác sĩ |
| --- | --- | --- | --- | --- | --- |
| Tìm và xem hồ sơ | Có theo phạm vi công việc | BN được phân hoặc đã từng phụ trách | Thông tin cần thu | Thông tin cần cấp thuốc | Tra cứu có ghi nhật ký |
| Tạo BN, lập hồ sơ, tiếp nhận | Có | Xem, không tự phân bệnh nhân | Xem | Không | Chỉ khi có quyền tiếp nhận |
| Xem lịch sử khám đã xác nhận | Có trong hồ sơ | Đầy đủ lịch sử BN thuộc phạm vi | Tóm tắt phục vụ thu | Chẩn đoán và đơn cần thiết | Tra cứu có ghi nhật ký |
| Chẩn đoán, duyệt kế hoạch, ký đơn | Không | Chỉ lượt được phân cho mình | Không | Không | Không |
| Ghi hoặc xác nhận dịch vụ | Điều dưỡng sửa nháp dòng được giao cho mình | Xác nhận lượt của mình | Xem để thu | Không | Không |
| Thu tiền và in hóa đơn | Có nếu được cấp quyền thu | Chỉ xem chi phí liên quan | Có | Xem trạng thái đã thu | Có khi được phân quyền thu |
| Bàn giao thuốc | Không mặc định | Không mặc định | Không mặc định | Có | Không mặc định |
| Soạn cấu hình hỗ trợ | Không | Mẫu điều trị | Không | Nhóm và hướng dẫn thuốc | Quản lý cấu hình |
| Duyệt nội dung chuyên môn | Không | Mẫu điều trị và hướng dẫn đã kiểm tra | Không | Hướng dẫn thuốc theo quyền chuyên môn | Không tự ký thay bác sĩ |

Mặc định tài khoản chức vụ tiếp nhận `CV004` kiêm thu trong mô hình đồ án theo yêu cầu quầy tiếp nhận; `CV003` điều dưỡng cần quyền thu được bật trong cấu hình nghiệp vụ nếu thực sự làm quầy. `CV007` là thu ngân, `CV005` dược sĩ, `CV002` bác sĩ theo dữ liệu đi kèm. Dùng ánh xạ cấu hình theo `MaChucVu`, kiểm tra lại với database thực tế; không suy quyền từ tên đăng nhập hoặc chữ “bác sĩ” trong họ tên.

Giữ hai vai trò hệ thống Admin/User trong database. `MainWindow.CanOpen` chỉ bổ sung nhánh cho `BenhNhan`, `KhamDieuTri`, `DonThuoc`, `DichVuThucHien`, `HoaDon` và form tiếp nhận liên quan. Rà cả `OpenApp`, `OpenModuleWindow`, `FilterApps` và bộ lọc ô chức năng; những nơi đang đọc trực tiếp `RolePermissions` phải dùng nhánh quyền mới cho đúng các module này. Với các module còn lại, giữ nguyên kết quả phân quyền cũ. Quyền mở màn hình phải tách khỏi quyền sửa, xác nhận và thu tiền.

### 4.3 Phạm vi đọc và bảo vệ khi ghi

Bác sĩ thấy bệnh nhân có ít nhất một phiếu từng được phân cho mình; đối với bệnh nhân đó được đọc lịch sử đã xác nhận của các bác sĩ trước để phục vụ điều trị. Chỉ lượt hiện tại có `PHIEU_KHAM_BENH.MaBS = MaNV` và `LICH_SU_KHAM_BENH.MaBS = MaNV` mới được sửa. Chuyển bác sĩ chỉ áp dụng trước khi bắt đầu khám; đã có lần khám thì không thay người phụ trách ngầm.

Phân công điều dưỡng dùng ngay `DICH_VU_THUC_HIEN.MaNVThucHien`: bác sĩ tạo dòng nháp và chỉ định người làm. Điều dưỡng chỉ sửa kết quả/thời gian trên dòng có mã người làm bằng phiên đăng nhập; không tự tạo dòng, đổi dịch vụ, lượng, giá, người làm hoặc xác nhận. Chỉ đọc danh sách dòng được giao trong mục Dịch vụ thực hiện; quyền tiếp nhận/hồ sơ vẫn theo ma trận riêng.

Mọi API đọc chi tiết bằng mã phải kiểm tra quyền, kể cả khi người dùng mở từ hồ sơ, đơn thuốc, dịch vụ hoặc hóa đơn. Các lần khám nháp của bác sĩ khác không được lộ qua lịch sử. Mọi lưu, xác nhận, hủy hoặc thay đơn phải kiểm tra lại người thực hiện, trạng thái và liên kết trong cùng transaction.

Các màn hình này không dùng `DuLieuGiaoDien.Rows(...)` toàn bệnh viện làm hàng rào phân quyền. Đọc danh sách và chi tiết có điều kiện SQL ngay tại DAL; nếu cần cache thì khóa cache phải bao gồm người dùng và phạm vi. Đây là kiểm soát nghiệp vụ ứng dụng desktop, không thay thế kiểm soát tài khoản truy cập SQL khi triển khai thật.

## 5 Kế hoạch giao diện cho sáu chức năng

### 5.1 Ngôn ngữ thiết kế chung

| Thành phần | Quy định đề xuất | Tiêu chí kiểm tra |
| --- | --- | --- |
| Bố cục | WPF nền sáng; vùng nhận diện BN cố định; danh sách bên trái và vùng làm việc bên phải; thanh hành động dưới cùng | Mỗi thao tác luôn nhìn thấy tên, mã BN, mã lượt và bác sĩ |
| Màu | Nền `#F3F6FA`; thẻ trắng; chữ `#172B4D`; chính `#1D4ED8`; hoàn tất `#15803D`; chờ `#B45309`; lỗi `#B91C1C` | Màu luôn đi cùng chữ hoặc biểu tượng, không dùng màu đơn lẻ để báo trạng thái |
| Chữ và khoảng cách | Segoe UI; nội dung 14 px; nhãn 13–14 px; tiêu đề 22–24 px; khoảng cách 8/12/16/24 px | Không ép nhỏ chữ khi thiếu chỗ; nhãn tiếng Việt có dấu không bị cắt |
| Điều khiển | Nút tối thiểu cao 36 px; hành động chính duy nhất mỗi bước; tìm kiếm có nút xóa; ComboBox có ô tìm | Tab/Enter hoạt động hợp lý, focus lỗi đầu tiên |
| Danh sách | Cột định danh cố định; chọn một dòng; phân trang hoặc tải theo phạm vi; bật virtualization khi phù hợp | 1.000 bệnh nhân và 10.000 lượt mô phỏng vẫn không tải toàn bộ vào UI một lần |
| Cửa sổ | Kiểm tra 1366×768, 1920×1080; cửa sổ hẹp 1024×768 và DPI 125–150 phần trăm | Chuyển vùng phụ thành tab/xếp dọc khi hẹp; có cuộn rõ ràng, không che nút |
| Lỗi và phản hồi | Lỗi dưới trường nhập; thông báo lưu ở vùng trạng thái; hộp xác nhận chỉ với hủy, ký hoặc thu tiền | Không bật nhiều MessageBox cho một lần nhập lỗi |
| Phạm vi style | Tạo `Themes/ClinicalWorkflowStyles.xaml`, merge cục bộ trong các view liên quan | Không đổi màu/kích thước style toàn cục khiến phân hệ khác đổi theo |

### 5.2 Hồ sơ bệnh nhân và tiếp nhận

Phần danh sách có tìm theo mã, tên, điện thoại, ngày sinh; kết quả gồm mã BN, họ tên, giới tính, ngày sinh và lần khám gần nhất. Hồ sơ chi tiết có bốn tab: Thông tin bệnh nhân, Hồ sơ bệnh án, Lịch sử lịch hẹn, Lịch sử khám. Tiền sử và dị ứng nằm ở khu vực dễ thấy trong cả hồ sơ và màn hình khám.

Tiếp nhận là một form trong hồ sơ với ba nhóm theo thứ tự: nhận diện BN và hồ sơ; dịch vụ khám và bác sĩ/phòng; lý do và xác nhận tiếp nhận. Nhân viên tiếp nhận, giờ và số thứ tự do hệ thống điền, người dùng không tự chọn tên đồng nghiệp. Sau lưu hiển thị “Đã chuyển vào hàng đợi của bác sĩ …”, không mở form nhập chẩn đoán cho nhân viên tiếp nhận.

Lịch sử hẹn đọc từ `LICH_KHAM` theo `MaBN`, chỉ xem và mở chi tiết có quyền; lịch sử khám theo `MaHS → MaPhieu → MaLS`, mới nhất trước. Không dựng lịch sử giả từ ghi chú hoặc trộn hồ sơ cùng tên.

### 5.3 Khám điều trị

Không gian làm việc chia ba vùng: hàng đợi của bác sĩ; thông tin BN và nội dung chuyên môn; lịch sử/tóm tắt liên quan. Trên cửa sổ hẹp, vùng lịch sử chuyển thành tab. Các bước hiện rõ: Khám và chẩn đoán, Tình trạng bệnh, Kế hoạch, Dịch vụ thực tế, Đơn thuốc, Tổng kết.

Trạng thái nhập nháp được giữ khi chuyển tab. Có Lưu nháp và Hoàn tất chuyên môn riêng; trước khi hoàn tất phải kiểm tra chẩn đoán, tình trạng, xử trí, dịch vụ còn nháp và đơn còn nháp. Các bước kế hoạch tương lai được phép chưa thực hiện; không chặn kết thúc buổi chỉ vì liệu trình còn buổi sau.

### 5.4 Đơn thuốc

Bên trái là bộ chọn nhóm thuốc, từ khóa, dạng/hàm lượng và tình trạng có sẵn; bên phải là bảng thuốc đã chọn. Có thể chọn nhiều thuốc rồi Thêm vào đơn. Mỗi dòng thể hiện tên và hàm lượng, đơn vị cấp phát, liều, đường dùng, hướng dẫn, số ngày, số lượng và giá tham khảo. Tồn hiển thị là tham khảo, chưa giữ hàng khi kê.

Mẫu hướng dẫn chỉ tự điền khi đã duyệt và bác sĩ xác nhận điều kiện áp dụng phù hợp người bệnh; nếu thiếu hoặc không phù hợp thì để trống phần cần nhập. Số ngày và số lượng là hai dữ liệu riêng. Không suy số lượng viên từ một chuỗi liều tự do; chỉ gợi ý kiểm tra lại, không âm thầm tính hoặc sửa lượng. Dữ liệu đang kê không thay đổi khi cấu hình danh mục được sửa sau đó.

Trước khi ký hiển thị dị ứng, tiền sử, tuổi tính từ ngày sinh và toàn bộ nội dung đơn. Thông tin dị ứng tự do chỉ là nhắc kiểm tra, không khẳng định có bộ phát hiện tương tác/chống chỉ định hoàn chỉnh. Bản in có mã đơn, BN, ngày sinh, ngày kê, bác sĩ, chẩn đoán, thuốc/hàm lượng, đường dùng, liều, hướng dẫn, số ngày và lượng.

### 5.5 Dịch vụ thực hiện

Danh sách tách hai tab Dự kiến từ kế hoạch và Đã thực hiện. Dòng dự kiến hiển thị lần/buổi, lượng dự kiến và phần còn lại; dòng thực tế có dịch vụ, người làm, thời gian, lượng, đơn giá chốt, kết quả, trạng thái xác nhận và đã tính tiền hay chưa.

Bác sĩ có thể chọn một bước kế hoạch để tạo thực hiện hoặc ghi dịch vụ phát sinh có lý do. Thêm dịch vụ không tự coi là đã làm. Chỉ xác nhận sau khi nhập kết quả/thông tin thực hiện cần thiết. Chặn vượt lượng bước dự kiến trừ khi bác sĩ sửa kế hoạch trước khi thực hiện; không âm thầm tăng lượng hoặc thu trước buổi sau.

### 5.6 Hóa đơn

Danh sách bên trái chia Chờ thu, Đã thanh toán và Lịch sử cũ. Bên phải là bảng kê theo lượt gồm nhóm dịch vụ và nhóm thuốc; phân biệt Bảng tính chờ thanh toán với Hóa đơn đã phát hành. Bảng tính mới có thể được dựng từ nguồn và chưa tạo `HOA_DON` trong SQL.

Hóa đơn đã phát hành chỉ đọc và in lại. Tên khoản thu, đơn vị, lượng và giá hiển thị theo bản chốt, không thay đổi theo danh mục hiện hành. Nếu một thuốc được phân bổ nhiều lô, có thể gộp dòng trên bản in theo thuốc và giá nhưng vẫn giữ đủ liên kết từng `MaCTPX` trong dữ liệu; không mất truy vết lô.

### 5.7 Thanh toán

Trong mục Hóa đơn, khối thanh toán đặt tổng phải thu nổi bật, kho cấp thuốc nếu có đơn, phương thức, tiền khách đưa, tiền thối và nút Thanh toán và in hóa đơn. Một lượt dùng một kho thuốc đang hoạt động do quầy chọn hoặc từ cấu hình mặc định; nếu kho không đủ toàn bộ đơn thì báo để xử lý, không tự chia sang kho khác. Luồng mới không hiện nhập số thu tùy ý, tùy chọn Thu một phần hoặc Công nợ. Người thu là phiên đăng nhập và hiển thị chỉ đọc.

Tiền mặt: tổng 450.000 đồng, khách đưa 500.000 đồng thì ghi thu 450.000, khách đưa 500.000, thối 50.000. Nếu đưa 400.000 thì không cho hoàn thành và không phát sinh thu 400.000. Tiền thối không phải một giao dịch Hoàn. Chuyển khoản: chỉ xác nhận sau khi nhân viên đối chiếu đã nhận đủ; ghi mã tham chiếu, số nhận bằng tổng, thối bằng 0. Không tuyên bố tự kiểm tra ngân hàng khi chưa có tích hợp.

Nếu in lỗi sau thanh toán, hiển thị Đã thu tiền và cho In lại cùng mã hóa đơn; không thực hiện thanh toán lần nữa. Hóa đơn nội bộ của đồ án không được gắn nhãn hóa đơn điện tử thuế đã tích hợp.

## 6 Quy tắc khám và gợi ý kế hoạch

1. Bác sĩ nhập triệu chứng và chẩn đoán; thêm một hoặc nhiều tình trạng từ danh mục, mức độ và vị trí liên quan. Với chẩn đoán chưa có danh mục, vẫn lưu mô tả thủ công và lập kế hoạch thủ công.
2. Khi lưu tình trạng hoặc bấm Gợi ý kế hoạch, hệ thống tìm mẫu đã duyệt khớp mã tình trạng và mức độ; mẫu không chỉ định mức độ là lựa chọn tổng quát được hiển thị sau mẫu khớp cụ thể. Không dựa vào phép tìm chuỗi tên bệnh để tự quyết định điều trị.
3. Hiển thị từng mẫu kèm nguồn tình trạng, phiên bản, người duyệt, ngày duyệt và các bước. Có nhiều tình trạng thì trình bày các gợi ý riêng; bác sĩ quyết định chọn/gộp, không tự cộng toàn bộ dịch vụ gây trùng phí.
4. Bác sĩ chọn Áp dụng để sao chép vào một kế hoạch nháp, sửa nội dung và số buổi. Ghi mã tình trạng nguồn và mẫu nguồn; mỗi tình trạng đã áp dụng không bị áp dụng lại do bấm nút hai lần.
5. Bác sĩ xác nhận kế hoạch mới được thực hiện theo kế hoạch. Mẫu được cập nhật sau này không sửa các kế hoạch đã sao chép.
6. Một kế hoạch có thể nhiều buổi. Giữ `KE_HOACH_DIEU_TRI.MaLS` là lượt lập kế hoạch; lần tái khám khác cùng `MaHS` được liên kết bước chưa làm qua `MaCTKH`, phù hợp trigger hiện có. Không ép tất cả các buổi vào một lần khám.
7. Đóng lượt khám khóa triệu chứng, chẩn đoán, tình trạng và các nội dung đã ký. Điều chỉnh sau ký dùng bản ghi bổ sung có người/thời gian/lý do trong nhật ký, không mở lại để sửa đè; đợt thực hiện tiếp theo tạo lượt mới.

Việc soạn và duyệt mẫu nằm trong tab Cấu hình hỗ trợ của Khám điều trị. Nội dung demo phải ghi rõ mô phỏng và không tự đặt thành phác đồ dùng thật. Không cung cấp sẵn một bộ chẩn đoán/điều trị giả như đã được chuyên môn xác nhận.

## 7 Quy tắc kê đơn và cấp thuốc

- Chỉ bác sĩ của lượt khám được tạo, sửa và ký đơn. `MaBS` lấy từ người dùng và đối chiếu lượt, không chọn một bác sĩ bất kỳ trong ComboBox.
- Lưu đầu đơn và mọi dòng thuốc trong một transaction. Lỗi một dòng thì không lưu dở đầu đơn hoặc mất các dòng cũ. Mặc định form đơn yêu cầu đầy đủ thuốc, liều, hướng dẫn, ngày và lượng ngay khi Lưu nháp để giữ mức kiểm tra của form hiện tại; Nháp nghĩa là chưa ký, không phải lưu cấu trúc đơn thiếu dữ liệu.
- Bộ lọc lấy `THUOC` nối `HANG_HOA`; chỉ thuốc đang được sử dụng. Nhóm kê đơn là nhóm hỗ trợ chọn thuốc riêng, không sửa `LOAI_HANG_HOA` đang phục vụ kho.
- Thêm lại cùng `MaThuoc` trong một đơn thì focus dòng đã có và báo đã chọn, không tạo bản sao âm thầm. Khi cần hai chế độ dùng khác nhau, bác sĩ ghi rõ trong hướng dẫn của dòng hoặc tạo đơn bổ sung có lý do, không tự cộng số lượng không rõ ý nghĩa.
- Lưu nháp và ký đều bắt buộc có thuốc, liều, hướng dẫn, số ngày nguyên dương, lượng dương và đơn vị rõ ràng. UI báo các mục thiếu trước khi lưu; ký còn kiểm tra quyền, trạng thái và thông tin chốt.
- Đơn ký giữ nguyên chỉ định và hướng dẫn tại thời điểm ký. Muốn thay đơn trước thanh toán/cấp thuốc thì tạo đơn nháp thay thế có `MaDTThayThe` và lý do; đơn cũ vẫn có hiệu lực trong lúc soạn. Khi ký đơn mới, xác nhận mới và chuyển đơn cũ Ngừng áp dụng trong cùng transaction. Chặn thay khi đơn đã có xuất thuốc ghi sổ hoặc đã được tính tiền.
- Lượt đã hoàn tất chuyên môn nhưng còn chờ thanh toán vẫn cho chính bác sĩ phụ trách tạo và ký đơn thay thế khi chưa có bất kỳ hóa đơn, thu hoặc xuất thuốc của lượt. Chỉ thay chỉ định bằng đơn mới, không mở khóa triệu chứng/chẩn đoán/tình trạng đã ký. `LapBangTinh` và `ThanhToan` phải từ chối khi còn bất kỳ đơn hoặc dịch vụ nháp cần xử lý, kể cả đơn thay thế đang soạn.
- Có thể có nhiều đơn xác nhận còn hiệu lực của cùng lượt; bảng tính gom tất cả đơn còn hiệu lực, loại đơn Hủy/Ngừng áp dụng và không tính thuốc đã thu. Không chọn một đơn rồi bỏ sót đơn khác.
- Kê đơn không trừ kho. Khi thiếu thuốc, không đổi sang thuốc khác hoặc giảm lượng tự động; báo bác sĩ xử lý đơn và làm mới bảng tính trước thu.
- Dược sĩ có danh sách Đã thanh toán chờ giao trong mục Đơn thuốc. Khi giao, kiểm tra đúng người và đúng đơn, ghi nhận một lần; thao tác này chỉ cập nhật bàn giao, không gọi `DispensePrescription` lần hai.

## 8 Thiết kế thanh toán trọn gói

### 8.1 Các bất biến

| Quy tắc | Định nghĩa triển khai |
| --- | --- |
| Tổng khoản thu | Tổng dịch vụ `Đã xác nhận` của lượt cộng thuốc theo các đơn đang hiệu lực; loại nguồn đã thuộc hóa đơn khác |
| Giá chốt | Giá dịch vụ tại dòng thực hiện; giá thuốc lấy lại từ `THUOC.DonGiaBan` tại lúc thanh toán và chốt vào chi tiết hóa đơn; bảng tính mô phỏng đúng phân bổ lô hiện có rồi cộng ROUND(lượng từng lô × giá, 2), giống trigger |
| Giảm giá | Luồng mới mặc định 0 và không mở tính năng giảm giá mới; dữ liệu giảm giá lịch sử được giữ nguyên |
| Thu đủ | Với tổng dương, đúng một giao dịch Thu đã xác nhận có `SoTien = SoTienPhaiThu`; không nhận thu một phần |
| Khách đưa | Tiền mặt phải lớn hơn hoặc bằng phải thu; chuyển khoản phải bằng phải thu và có đối chiếu thủ công |
| Tiền thối | `TienThua = SoTienKhachDua − SoTien`; số thối không âm và không làm giảm doanh thu ghi nhận |
| Một lần xử lý | Cùng `MaYeuCau` trả lại cùng kết quả; lượt đã được thanh toán theo luồng mới không tạo bộ thứ hai dù dùng mã yêu cầu khác |
| Không có thuốc | Vẫn thanh toán dịch vụ; bỏ qua xuất và bàn giao thuốc |
| Tổng bằng 0 | Nếu thật sự có nguồn hợp lệ tổng 0, xác nhận hóa đơn 0 và đánh dấu đã thanh toán theo công thức hiện có; không tạo `THANH_TOAN.SoTien = 0` vì CHECK cũ cấm |
| Đã thanh toán | Chỉ hiển thị sau commit; khi có thuốc còn chờ giao thì không đồng nhất Đã thanh toán với Đã giao thuốc |

Phạm vi mới dùng VND nguyên đồng ở UI; từ chối nguồn mới có số lẻ đồng và yêu cầu sửa giá phù hợp trước thu, không tự làm tròn lệch tổng trigger. Giữ `DECIMAL(18,2)` để tương thích dữ liệu cũ có phần lẻ. Khi chỉ xem/in chứng từ cũ, hiển thị đầy đủ phần lẻ nếu có.

### 8.2 Trình tự giao dịch bắt buộc

1. UI tạo một `Guid MaYeuCau` cho lần xác nhận, giữ nguyên khi thử lại cùng yêu cầu. Mã này ghi dạng Guid chuẩn D trong SQL. Nút thu bị khóa khi đang xử lý; không giữ SQL transaction trong lúc chờ người dùng nhập tiền hoặc chờ ngân hàng.
2. `BUS_THANHTOANTRONGOI.ThanhToan` mở một connection và `Serializable` transaction; mọi BUS/DAL con nhận đúng `IDbTransaction` đó. Không mở kết nối phụ để đọc dữ liệu vừa ghi.
3. Khóa theo `MaYeuCau` rồi `MaLS` bằng application lock có chủ sở hữu transaction. Nếu đã có hóa đơn cùng mã yêu cầu và đúng cùng bộ dữ liệu yêu cầu thì trả kết quả đã lưu; mã yêu cầu bị dùng cho lượt/đầu vào khác thì từ chối. Nếu lượt đã thanh toán bằng mã khác, trả hóa đơn hiện có và không thu thêm.
4. Kiểm tra lại người thu, lần khám đã xác nhận, phiếu chờ thu, các đơn hiệu lực và nguồn chưa tính tiền. Luồng mới không nhận lượt đã có hóa đơn/thu/xuất kiểu cũ chưa xử lý; chuyển sang hàng Lịch sử cũ có chỉ dẫn đối chiếu, không tự thu lại.
5. Tính lại bảng phí, so với dấu vân tay bảng tính đã hiển thị. Dấu này gồm mã kho cấp thuốc, nguồn dịch vụ, đơn/chi tiết thuốc hiệu lực, lượng, giá, phân bổ lô và tồn khả dụng ảnh hưởng phân bổ, trạng thái và phiên bản dữ liệu liên quan; không gồm thời gian render UI. Nếu nguồn/giá đổi, rollback và yêu cầu người thu xem lại số mới.
6. Kiểm tra tiền khách đưa đủ theo phương thức. Khóa các nguồn tồn thuốc theo thứ tự ổn định; gọi luồng cấp đủ đơn đang có trong cùng transaction. Giữ thuật toán phân lô hiện tại để không đổi nghiệp vụ kho ngoài yêu cầu; chỉ chọn lô hợp lệ theo chính sách hiện có. Thiếu một thuốc thì rollback toàn bộ.
7. Gọi luồng tạo hóa đơn hiện có sau khi phiếu xuất đã ghi sổ trong transaction: nguồn thuốc lúc này đã có `MaCTPX`, phù hợp `TR_CTHD_TinhTien`. Truyền tất cả nguồn dịch vụ và thuốc của lượt; giữ UNIQUE chống thu nguồn hai lần.
8. Gán `MaYeuCau`, `DauVanTayYeuCau` và `PhienBanQuyTrinh = 2` khi hóa đơn còn nháp, rồi xác nhận hóa đơn. Kiểm tra tổng từ SQL bằng tổng đã đối chiếu. Không ghi đè cột computed hoặc tự truyền tổng tiền từ UI. Vì trigger tính và làm tròn theo từng `MaCTPX`, tổng preview phải mô phỏng cùng cách phân lô và cộng từng phần, không lấy tổng số lượng thuốc nhân giá rồi làm tròn một lần. Nếu tổng sau chia lô có phần lẻ đồng, từ chối theo quy tắc VND mới và yêu cầu xử lý rõ, không tự bù chênh lệch.
9. Với tổng dương, ghi một giao dịch Thu đủ, đồng thời lưu `SoTienKhachDua`, `TienThua` và `MaThamChieu`. Không ghi xong rồi UPDATE tiền thối lên giao dịch đã xác nhận vì trigger cũ khóa sửa.
10. Tạo `BAN_GIAO_THUOC` Chờ giao cho mỗi đơn đã xuất; cập nhật phiếu Hoàn tất nếu không có thuốc, còn có thuốc thì chuyển Chờ nhận thuốc. Ghi nhật ký với mã chứng từ và người thực hiện.
11. Đối chiếu lần cuối: thu ròng bằng phải thu, đủ nguồn được tính tiền, tất cả xuất thuốc thuộc cùng lượt và đúng lượng; commit. Bất cứ lỗi nào rollback cả hóa đơn, thu, xuất và bàn giao. Nhả khóa rồi mới cập nhật UI, in hóa đơn.

Trong khoảng giữa bước xác nhận hóa đơn và ghi thu, hóa đơn có thể chưa có thu ở bên trong transaction chưa commit; không được công bố trạng thái trung gian đó ra giao diện. SQL Server không có ràng buộc kiểm tra trì hoãn đến commit, vì vậy không thêm CHECK yêu cầu có thanh toán ngay ở từng câu INSERT hóa đơn. Bất biến thu đủ được bảo vệ bằng đầu mối giao dịch mới, chặn lối ghi cũ trong UI/BUS của quy trình mới và kiểm tra cuối giao dịch.

### 8.3 Tương thích với dữ liệu và API cũ

Giữ các API tài chính cũ cho việc đọc và các kiểm tra hồi quy cần thiết; các màn hình mới chỉ gọi đầu mối trọn gói để ghi. Các thao tác ghi cũ phải từ chối hóa đơn `PhienBanQuyTrinh = 2` nếu gọi từ ngoài giao dịch điều phối hợp lệ, tránh ghi thêm Thu/Hoàn hoặc sửa nguồn trên hóa đơn mới. Không tự tạo chức năng hoàn tiền mới; lịch sử hoàn cũ vẫn hiển thị đúng.

Hóa đơn cũ có thu thiếu, phiếu xuất cũ hoặc nháp dở phải được liệt kê trong báo cáo đối chiếu trước chuyển đổi. Người triển khai không tự giả lập thu đủ, xóa bản cũ hoặc xuất thuốc lần nữa để hợp thức hóa. Nếu cần giải quyết tài chính lịch sử, đó là yêu cầu riêng phải có quyết định cụ thể của chủ dữ liệu.

## 9 Kế hoạch database

### 9.1 Những phần giữ nguyên

Giữ 46 bảng gốc trong bản gửi, quan hệ bệnh nhân–hồ sơ–phiếu–lần khám, một hồ sơ tổng mỗi BN, UNIQUE một lần khám mỗi phiếu, chuyên khoa dịch vụ/bác sĩ, đơn và chi tiết đơn, dịch vụ thực hiện, cấu trúc nguồn hóa đơn, các bảng kho và `VW_TON_KHO`. Giữ `UX_CTHD_DichVu`, `UX_CTHD_XuatThuoc` và các cột tiền tính toán.

Không sửa thuật toán lịch hẹn/ca làm, không đổi loại hàng hóa, không thêm cột tồn kho thủ công. Kế hoạch cần bổ sung dữ liệu vì bản hiện tại chưa có mẫu kế hoạch, nhóm kê đơn, mẫu hướng dẫn, tiền khách đưa/thối và trạng thái bàn giao độc lập.

### 9.2 Sáu bảng mới

Tất cả khóa mã và FK mã dùng `NVARCHAR(50)`. Mọi bảng cấu hình có người sửa/ngày sửa hoặc lịch sử qua nhật ký; bản đã được dùng không xóa cứng. Khi phiên bản nội dung đã duyệt đổi, tạo phiên bản mới hoặc lưu snapshot để không sửa nội dung lâm sàng đã áp dụng.

| Bảng mới | Cột nghiệp vụ tối thiểu | Ràng buộc chính |
| --- | --- | --- |
| `DM_TINH_TRANG_BENH` | `MaDMTT` PK; `TenTinhTrang NVARCHAR(255)`; `MoTa NVARCHAR(MAX) NULL`; `TrangThai NVARCHAR(50)` | Tên không trống; trạng thái Đang áp dụng/Ngừng áp dụng; không dùng dữ liệu mô phỏng như chẩn đoán đã duyệt |
| `MAU_KE_HOACH_DIEU_TRI` | `MaMauKH` PK; `MaDMTT` FK; `MucDo NVARCHAR(255) NULL`; `TenMau NVARCHAR(255)`; `PhienBan INT`; `TrangThai`; `MaBSDuyet NULL`; `NgayDuyet DATETIME2(0) NULL` | Phiên bản dương; Nháp/Đã duyệt/Ngừng áp dụng; đã duyệt phải có bác sĩ và ngày duyệt |
| `CHI_TIET_MAU_KE_HOACH` | `MaCTMau` PK; `MaMauKH` FK; `MaDV` FK; `ThuTu INT`; `LanThu INT`; `SoLuongDuKien DECIMAL(18,3)`; `NoiDung NVARCHAR(MAX)` | Thứ tự, lần, lượng dương; UNIQUE mẫu và thứ tự; không lưu giá như bảng giá cố định, lấy giá hiện hành khi tạo kế hoạch nháp |
| `NHOM_THUOC_KE_DON` | `MaNhomThuoc` PK; `TenNhom NVARCHAR(255)`; `MoTa NVARCHAR(MAX) NULL`; `TrangThai` | Nhóm dùng cho tìm kê đơn; không thay `LOAI_HANG_HOA`; không tự suy nhóm điều trị bằng tên thuốc |
| `CAU_HINH_THUOC_KE_DON` | `MaThuoc` PK và FK; `MaNhomThuoc NULL` FK; `DoiTuongApDung NVARCHAR(255) NULL`; `DuongDung NVARCHAR(100) NULL`; `LieuDungGoiY NVARCHAR(255) NULL`; `HuongDanGoiY NVARCHAR(MAX) NULL`; `PhienBan INT`; `TrangThai`; `MaNguoiDuyet NULL`; `NgayDuyet NULL` | Một cấu hình hiện hành mỗi thuốc; thiếu điều kiện phù hợp thì không tự điền liều; trước khi đổi cấu hình phải lưu lịch sử vào nhật ký; nội dung đã kê được chụp lại ở chi tiết đơn |
| `BAN_GIAO_THUOC` | `MaDT` PK/FK; `MaHD` FK; `MaPX` FK; `TrangThai`; `MaNVGiao NULL` FK; `ThoiGianGiao DATETIME2(0) NULL`; `GhiChu NVARCHAR(MAX) NULL` | Chờ giao chưa có người/giờ giao; Đã giao phải có; hóa đơn, xuất và đơn cùng lượt; UPDATE có điều kiện trạng thái để tránh giao hai lần |

### 9.3 Cột bổ sung vào bảng hiện có

| Bảng | Cột đề xuất | Lý do và cách giữ dữ liệu cũ |
| --- | --- | --- |
| `TINH_TRANG_BENH` | `MaDMTT NVARCHAR(50) NULL` FK; `ViTriRang NVARCHAR(100) NULL` | Gợi ý dựa vào mã; mô tả cũ giữ nguyên, không tự gán mã theo chuỗi |
| `KE_HOACH_DIEU_TRI` | `MaTTBNguon NVARCHAR(50) NULL` FK tới `TINH_TRANG_BENH`; `MaMauKH NVARCHAR(50) NULL` FK; `PhienBanMau INT NULL` | Biết kế hoạch sinh từ tình trạng/mẫu nào; kế hoạch thủ công để null |
| `CHI_TIET_KE_HOACH` | `NoiDungBuoc NVARCHAR(MAX) NULL` | Sao chép nội dung mẫu; không phụ thuộc mẫu đổi sau này |
| `LICH_SU_KHAM_BENH` | `KhongKeThuoc INT NOT NULL DEFAULT 0`; `LyDoKhongKeThuoc NVARCHAR(500) NULL` | 0 là chưa đánh dấu; 1 phải có lý do và không có đơn hiệu lực; dùng INT 0/1 để phù hợp mô hình ET hiện có |
| `DON_THUOC` | `MaDTThayThe NVARCHAR(50) NULL` FK tự tham chiếu | Đơn mới trỏ đơn cũ; cùng lượt, không tự thay đơn đã xuất/thu |
| `CHI_TIET_DON_THUOC` | `DuongDung NVARCHAR(100) NULL`; `TenThuocChot NVARCHAR(255) NULL`; `HamLuongChot NVARCHAR(255) NULL`; `DonViTinhChot NVARCHAR(255) NULL`; `PhienBanHuongDan INT NULL` | Chụp nội dung tại ký; lịch sử null thì dùng nhãn hiện tại kèm nhận diện dữ liệu cũ; liều/hướng dẫn đã có không tạo cột trùng |
| `HOA_DON` | `PhienBanQuyTrinh INT NOT NULL DEFAULT 1`; `MaYeuCau NVARCHAR(50) NULL`; `DauVanTayYeuCau NVARCHAR(64) NULL` | Cũ là 1, mới chủ động gán 2; lưu Guid dạng chuỗi theo quy ước mã; UNIQUE `MaYeuCau` khi khác null |
| `THANH_TOAN` | `SoTienKhachDua DECIMAL(18,2) NULL`; `TienThua DECIMAL(18,2) NULL`; `MaThamChieu NVARCHAR(100) NULL` | Dữ liệu cũ không suy đoán tiền khách đưa/thối; luồng mới ghi ngay lúc INSERT Thu |

Chưa thêm `rowversion` đại trà để tránh đổi hợp đồng ET/metadata và các màn hình ngoài phạm vi. Sửa đồng thời được phát hiện bằng snapshot của bản ghi và tập con, kiểm tra lại dưới khóa SQL trước khi ghi; các thao tác xác nhận dùng UPDATE có điều kiện trạng thái và kiểm tra số dòng ảnh hưởng. Token bảng tính tài chính là dấu vân tay nguồn được tính lại trong giao dịch.

### 9.4 Ràng buộc và đối tượng SQL phải rà soát

- Thêm index `(MaBS, TrangThai, ThoiGianTiepNhan)` cho phiếu và `(MaBS, ThoiGianKham)` cho lần khám nếu chưa có chỉ mục tương đương; thêm FK/index cho các cột mới, UNIQUE có lọc cho `HOA_DON.MaYeuCau` khác null.
- Không áp UNIQUE một hóa đơn cho mỗi lượt trên toàn bộ dữ liệu cũ. Với luồng mới, khóa `MaLS`, kiểm tra tồn tại hóa đơn phiên bản 2 rồi mới tạo; có thể thêm UNIQUE có lọc `(MaLS)` với `PhienBanQuyTrinh = 2` khi mọi bản mới chỉ có đúng một hóa đơn và không có yêu cầu hủy rồi lập lại.
- Bổ sung kiểm tra tiền khách đưa/thối chỉ khi cả hai có giá trị; yêu cầu luồng mới ghi đủ. Với Thu tiền mặt mới: đưa ≥ thu, thối = đưa − thu. Với chuyển khoản mới: đưa = thu, thối = 0 và có tham chiếu. Không ép các giao dịch Hoàn hoặc bản cũ theo công thức này.
- Rà `TR_TT_KiemTraSoDu` để chặn số thu không đủ trên hóa đơn phiên bản 2; giữ hành vi đọc và dữ liệu cũ. Không bỏ trigger chống vượt thu/hoàn hoặc khóa giao dịch đã xác nhận.
- Giữ `TR_CTHD_TinhTien`, `TR_HD_XacNhan` và nguồn `MaCTPX`. Nếu bổ sung cột chốt hiển thị, không làm mất việc tính tiền theo nguồn và không cho sửa hóa đơn đã xác nhận.
- Tăng bảo vệ `TR_LS_LienKet` và thêm bảo vệ tình trạng bệnh: lần khám đã xác nhận không cho sửa/xóa nội dung lâm sàng và tình trạng bên dưới. Kiểm tra tác động lên thao tác bổ sung hợp lệ trước khi thay trigger.
- Rà `TR_DT_XacNhan` và `TR_CTDT_KhoaDon`: khóa thêm snapshot/đường dùng sau ký, nhưng vẫn cho chuyển đơn chưa cấp sang Ngừng áp dụng theo nghiệp vụ thay thế có kiểm tra.
- Bảo vệ kế hoạch và chi tiết sau duyệt: nội dung/định lượng đã được thực hiện không sửa đè; cập nhật tiến độ thực hiện không bị chặn nhầm. Kiểm tra `MaTTBNguon`, `MaMauKH` đúng tình trạng và hồ sơ nguồn.
- Giữ tên và các cột đang có của `VW_HOA_DON_THANH_TOAN`, `VW_CHI_TIET_HOA_DON`, `VW_TIEN_THUOC_THEO_DON`, `VW_LICH_SU_KHAM_BENH_NHAN`; cần cột mới thì tạo view/DTO riêng để tránh làm hỏng báo cáo khác.

### 9.5 Migration và dữ liệu khởi tạo

Tạo `Database/Migrations/20261024_01_Preflight_ClinicalWorkflow.sql`, `20261024_02_ClinicalWorkflow.sql`, `20261024_03_Verify_ClinicalWorkflow.sql` và `20261024_04_Rollback_ClinicalWorkflow.sql`. Đây là các tệp sẽ được triển khai theo kế hoạch, không phải file SQL đã chạy trong phiên lập kế hoạch.

Preflight kiểm tra cấu trúc đang có, trigger/view, quyền tài khoản, mã chức vụ, dữ liệu lệch FK, phiếu nhiều lượt, đơn đã cấp, hóa đơn cũ chưa thanh toán và nguồn tính trùng. Xuất danh sách cần đối chiếu với mã, không in toàn bộ bệnh án hoặc chuỗi kết nối vào log.

Migration có version guard, `SET XACT_ABORT ON` và transaction cho DDL phù hợp; kiểm tra tồn tại và đúng kiểu trước ADD/ALTER để chạy lại không nhân đôi. Không DROP database, không TRUNCATE bảng và không sửa lịch sử để làm đẹp dữ liệu. Backup bản sao trước nâng cấp và thử restore.

Tạo thêm `Database/QLBenhVienRangHamMat_ClinicalWorkflow.sql` làm bản cài mới đầy đủ từ baseline cộng thay đổi, thay vì chỉ cập nhật ET rồi để `SchemaChecks` đọc schema cũ. Giữ script baseline gốc để đối chiếu; chỉnh đường dẫn đầu vào của bộ kiểm tra schema sang bản hợp đồng mới. Không xóa/bỏ qua kiểm tra các bảng ngoài phạm vi.

Seed chỉ thêm nhóm và cấu hình nghiệp vụ cần thiết, không phân nhóm dược lý hoặc điền liều bằng suy đoán. Mẫu điều trị/hướng dẫn chưa được chuyên môn kiểm tra phải ở Nháp. Fixture kiểm thử được phép có mẫu giả được đánh dấu rõ, nằm trong database thử nghiệm và rollback sau kiểm tra.

Rollback: khi chưa có dữ liệu quy trình mới, có thể gỡ đúng các đối tượng mới và phục hồi định nghĩa trigger từ baseline. Khi đã phát sinh chứng từ mới, ưu tiên sửa tiến và khôi phục từ backup có đối chiếu, không chạy script DROP làm mất đơn, bàn giao hoặc thu tiền. Có điểm dừng rõ trong script rollback nếu phát hiện dữ liệu mới.

## 10 Phạm vi file và hợp đồng lớp

### 10.1 File giao diện được phép thay đổi

| Miền | File hiện có cần sửa trong `RangHamMat.Wpf` | File thêm đề xuất |
| --- | --- | --- |
| Hồ sơ và tiếp nhận | `Views/BenhNhan.xaml` và `.xaml.cs`; `HoSoBenhNhan`, `CuaSoBenhNhan`, `ChiTietHoSo`, `TiepNhanKham` cùng hai loại file | `Controls/ClinicalWorkflow/HoSoViewModel.cs`, `TiepNhanViewModel.cs` |
| Khám điều trị | `Views/KhamDieuTri`, `ChiTietKhamDieuTri`, `CuaSoKhamDieuTri`; `Controls/TreatmentModels.cs` | `Controls/ClinicalWorkflow/KhamViewModel.cs`, `GoiYKeHoachViewModel.cs` |
| Đơn thuốc | `Views/DonThuoc`, `CuaSoDonThuoc`, `ChiTietDonThuoc`, `XemTruocDonThuoc`; `Controls/PrescriptionModels.cs` | `Controls/ClinicalWorkflow/KeDonViewModel.cs`, `BanGiaoThuocViewModel.cs` |
| Dịch vụ thực hiện | `Views/DichVuThucHien` và `.xaml.cs`; các phần liên quan của `TreatmentModels.cs` | `Controls/ClinicalWorkflow/DichVuThucHienViewModel.cs` |
| Hóa đơn và thu tiền | `Views/HoaDon`, `CuaSoHoaDon`, `XemTruocHoaDon`, `InvoiceDocument` cùng `.xaml/.xaml.cs` | `Controls/ClinicalWorkflow/ThanhToanViewModel.cs` |
| Dùng chung có giới hạn | `MainWindow.xaml.cs`, `AppWorkspace.xaml.cs`, `Controls/DieuHuongHoSo.cs`, `DuLieuGiaoDien.cs`, `BieuMauHoSo.cs`, `QuyTacNhapLieu.xml`, các map/schema JSON | `Controls/ClinicalWorkflow/QuyenNghiepVu.cs`; `Themes/ClinicalWorkflowStyles.xaml` |

Các tên rút gọn trong bảng đều là file dưới `Views/` với cả `.xaml` và `.xaml.cs`, trừ khi ghi rõ. Chỉ thay phần điều hướng, lưu và validation của bảng/module liên quan trong file dùng chung. Không đổi toàn bộ cơ chế form động hoặc cách tải của các phân hệ khác.

### 10.2 BUS DAL và ET

- Thêm `ET/ClinicalWorkflowContracts.cs` chứa DTO nghiệp vụ; các DTO không đặt tiền tố `ET_` vì không phải bảng SQL. Mỗi thực thể bảng mới tiếp tục có ET tương ứng theo quy ước dự án.
- Thêm các cặp `BUS/BUS_QUYENNGHIEPVU.cs` và `DAL/DAL_QUYENNGHIEPVU.cs`; `BUS_TIEPNHAN.cs` và `DAL_TIEPNHAN.cs`; `BUS_KHAMDIEUTRI.cs` và `DAL_KHAMDIEUTRI.cs`; `BUS_THANHTOANTRONGOI.cs` và `DAL_THANHTOANTRONGOI.cs`.
- Mở rộng `BUS_DONTHUOC`/`DAL_DONTHUOC`, `BUS_CHITIETDONTHUOC`/`DAL_CHITIETDONTHUOC`, `BUS_DICHVUTHUCHIEN`/`DAL_DICHVUTHUCHIEN` cho nghiệp vụ có quyền. Cập nhật ET/BUS/DAL của đúng các bảng thêm cột ở phần 9.
- Sáu bảng mới có tên lớp theo quy ước bỏ dấu gạch dưới: ví dụ `ET_DMTINHTRANGBENH`, `DAL_DMTINHTRANGBENH`, `BUS_DMTINHTRANGBENH`; tương tự `MAUKEHOACHDIEUTRI`, `CHITIETMAUKEHOACH`, `NHOMTHUOCKEDON`, `CAUHINHTHUOCKEDON`, `BANGIAOTHUOC`. Mỗi DAL phụ trách bảng của mình, các đầu mối nghiệp vụ điều phối giao dịch nhiều bảng.
- Nếu cần dùng chung cách phân bổ lô cho preview và ghi sổ, thêm `DAL/PhanBoLoThuoc.cs` và tách đúng phần tính thuần từ `DAL_PHIEUXUAT.cs`; giữ nguyên thứ tự chọn lô, kiểm tra hạn/tồn và hành vi ghi sổ. Đây là điểm tích hợp tối thiểu, không phải đổi thuật toán kho.

- Tái sử dụng `BUS_HOADON`, `BUS_CHITIETHOADON`, `BUS_THANHTOAN`, `BUS_PHIEUXUAT` và các DAL tương ứng trong transaction được truyền vào. Không đổi `dbconnect` toàn cục nếu API giao dịch hiện có đã đủ.
- Ghi sự kiện vào `NHAT_KY_THAO_TAC` bằng helper DAL có nhận transaction; không thay giao diện Nhật ký. Log gồm người, mã nguồn, hành động, kết quả và thay đổi cần thiết; không log mật khẩu/chứng thực hoặc toàn bộ nội dung y tế không cần thiết.

### 10.3 Giao diện nghiệp vụ chuẩn cho các tác vụ

Các kiểu nằm trong `ET/ClinicalWorkflowContracts.cs` trừ entity bảng. Dùng `IReadOnlyList<T>` cho danh sách, `decimal` cho tiền/lượng và `Guid` trong C# cho mã yêu cầu.

```csharp
NguCanhNghiepVu BUS_QUYENNGHIEPVU.LayNguCanh(string maNV);
IReadOnlyList<BacSiPhuHop> BUS_TIEPNHAN.LayBacSi(string maDV, DateTime thoiDiem, NguCanhNghiepVu nguoiDung);
KetQuaTiepNhan BUS_TIEPNHAN.TiepNhan(YeuCauTiepNhan yeuCau, NguCanhNghiepVu nguoiDung);
IReadOnlyList<HangDoiKhamItem> BUS_KHAMDIEUTRI.LayHangDoi(BoLocHangDoi boLoc, NguCanhNghiepVu nguoiDung);
string BUS_KHAMDIEUTRI.BatDauKham(string maPhieu, NguCanhNghiepVu nguoiDung);
LanKhamChiTiet BUS_KHAMDIEUTRI.LayChiTiet(string maLS, NguCanhNghiepVu nguoiDung);
void BUS_KHAMDIEUTRI.LuuKham(YeuCauLuuKham yeuCau, NguCanhNghiepVu nguoiDung);
IReadOnlyList<GoiYKeHoach> BUS_KHAMDIEUTRI.GoiYKeHoach(string maLS, NguCanhNghiepVu nguoiDung);
string BUS_KHAMDIEUTRI.ApDungMau(YeuCauApDungMau yeuCau, NguCanhNghiepVu nguoiDung);
string BUS_KHAMDIEUTRI.LuuKeHoach(YeuCauLuuKeHoach yeuCau, NguCanhNghiepVu nguoiDung);
void BUS_KHAMDIEUTRI.XacNhanKeHoach(string maKHDT, string dauVanTay, NguCanhNghiepVu nguoiDung);
void BUS_KHAMDIEUTRI.HoanTatKham(string maLS, string dauVanTay, NguCanhNghiepVu nguoiDung);
IReadOnlyList<ThuocKeDonItem> BUS_DONTHUOC.TimThuoc(BoLocThuoc boLoc, NguCanhNghiepVu nguoiDung);
string BUS_DONTHUOC.LuuDon(YeuCauLuuDonThuoc yeuCau, NguCanhNghiepVu nguoiDung);
void BUS_DONTHUOC.XacNhanDon(string maDT, NguCanhNghiepVu nguoiDung);
string BUS_DICHVUTHUCHIEN.LuuThucHien(YeuCauThucHienDichVu yeuCau, NguCanhNghiepVu nguoiDung);
void BUS_DICHVUTHUCHIEN.XacNhanThucHien(string maDVThucHien, NguCanhNghiepVu nguoiDung);
BangTinhThuPhi BUS_THANHTOANTRONGOI.LapBangTinh(string maLS, string? maKhoCapThuoc, NguCanhNghiepVu nguoiDung);
KetQuaThanhToan BUS_THANHTOANTRONGOI.ThanhToan(YeuCauThanhToan yeuCau, NguCanhNghiepVu nguoiDung);
void BUS_BANGIAOTHUOC.XacNhanGiao(string maDT, NguCanhNghiepVu nguoiDung);
```

`DauVanTayYeuCau` là SHA256 dạng hex 64 ký tự của MaLS, MaKhoCapThuoc, token bảng tính, phương thức, số tiền khách đưa và tham chiếu đã chuẩn hóa theo thứ tự cố định, số theo invariant culture. Dùng để đối chiếu thử lại cùng mã; khác với token bảng tính chỉ phản ánh nguồn phí. Không tính lại dấu yêu cầu từ nguồn đã thay đổi sau commit khi trả kết quả giao dịch cũ.

Không tin `NguCanhNghiepVu` được UI tự dựng: lấy từ người đã đăng nhập, truy vấn lại tài khoản/chức vụ/quyền ở đầu thao tác ghi và kiểm tra phạm vi trong SQL. `PhienDangNhap.MaNV` đã có nên chưa cần đổi cơ chế đăng nhập; quyền thực tế đọc theo liên kết `TAI_KHOAN → NHAN_VIEN → CHUC_VU`.

### 10.4 DTO quan trọng và giới hạn đầu vào

| Kiểu | Dữ liệu phải có | Quy tắc |
| --- | --- | --- |
| `YeuCauTiepNhan` | MaBN, MaHS, MaLich tùy chọn, MaDV, MaBS, MaPhong, LyDoKham, LyDoNgoaiLich tùy chọn | Không nhận người tiếp nhận hoặc số thứ tự tự khai từ UI |
| `BoLocHangDoi` | TuNgay, DenNgay, TrangThai, TuKhoa, Trang, KichThuocTrang | Không có trường đổi bác sĩ trong vai trò bác sĩ; trang tối đa 100 dòng |
| `YeuCauLuuKham` | MaLS, TrieuChung, ChanDoan, KeHoachXuLy, KhongKeThuoc, LyDoKhongKeThuoc, danh sách TinhTrangInput, DauVanTay | Dữ liệu nháp; TinhTrangInput gồm MaTTB tùy chọn, MaDMTT tùy chọn, LoaiTinhTrang, MoTaTinhTrang, MucDo, ViTriRang; kiểm tra tập con không đổi trước lưu |
| `YeuCauApDungMau` | MaLS, MaTTBNguon, MaMauKH, PhienBanMau | Mẫu đã duyệt, tình trạng thuộc lượt; gọi lại không sinh kế hoạch thứ hai cho cùng nguồn/mẫu |
| `YeuCauLuuKeHoach` | MaKHDT tùy chọn, MaLS, MaTTBNguon tùy chọn, NgayBatDau, NgayDuKienKetThuc, các bước MaDV/LanThu/SoLuongDuKien/DonGiaDuKien/NoiDungBuoc, DauVanTay | Sửa bản nháp; giá dự kiến không phải giá thu; không ghi đè bước đã thực hiện |
| `YeuCauLuuDonThuoc` | MaDT tùy chọn, MaLS, danh sách DongThuocInput, MaDTThayThe tùy chọn, LyDoThayThe, DauVanTay | Mỗi dòng có MaThuoc, LieuDung, DuongDung, HuongDan, SoNgayDung, SoLuong; không lấy MaBS tùy chọn |
| `BangTinhThuPhi` | MaLS, MaKhoCapThuoc tùy chọn, các dòng nguồn, TongDichVu, TongThuoc, SoTienPhaiThu, DauVanTay | Chỉ đọc; chưa ghi hóa đơn, chưa xuất kho, chưa giữ tồn |
| `YeuCauThanhToan` | MaLS, MaKhoCapThuoc tùy chọn, MaYeuCau, DauVanTayBangTinh, PhuongThuc, SoTienKhachDua, MaThamChieu tùy chọn | Không nhận TongTien/SoTienThu/TienThua do UI quyết định; tầng nghiệp vụ tính lại |
| `KetQuaThanhToan` | MaHD, MaTT tùy chọn, các MaPX/MaDT, SoTienThu, SoTienKhachDua, TienThua, DaTonTai | Chỉ trả sau commit; MaTT null cho hóa đơn hợp lệ tổng 0 |

Mã/chuỗi dùng đúng độ dài SQL; kiểm tra số tràn trước lưu; không dùng `double` cho tiền. Bắt lỗi nguồn đã đổi thành thông báo yêu cầu tải lại, không hiển thị lỗi SQL thô cho nhân viên.

## 11 Bảng kế hoạch triển khai

Không bắt đầu bằng thay toàn bộ XAML. Khóa hợp đồng dữ liệu và quyền trước, rồi hoàn thiện các màn hình theo luồng. Mỗi tác vụ phải có thay đổi nhỏ có thể kiểm thử và điểm lưu trong Git nếu thư mục thực tế có Git.

| Mã | Công việc | Phụ thuộc | Đầu ra bắt buộc | Điều kiện hoàn thành |
| --- | --- | --- | --- | --- |
| P01 | Khảo sát bản thực tế và lập baseline | Không | Danh sách file/phạm vi, kết quả build và kiểm tra cũ, backup SQL thử nghiệm | Phân biệt lỗi có sẵn với lỗi do thay đổi |
| P02 | Quyền nghiệp vụ và nguồn đọc theo bác sĩ | P01 | Quyền theo chức vụ, API đọc có phạm vi, điều hướng bị kiểm soát | Bác sĩ A không mở/sửa lượt B; module khác giữ quyền cũ |
| P03 | Migration và hợp đồng ET/SQL | P01, thống nhất hợp đồng P02 | Sáu bảng, cột cần thiết, schema cài mới, script nâng/hạ cấp | Chạy nâng cấp hai lần an toàn; không mất dữ liệu cũ |
| P04 | Hồ sơ và tiếp nhận mới | P02, P03 | Bốn tab hồ sơ, chọn dịch vụ–bác sĩ, phiếu hàng đợi | Tiếp nhận đi đúng bác sĩ, chống số thứ tự/lượt trùng |
| P05 | Khám, tình trạng và kế hoạch gợi ý | P04 | Workspace bác sĩ, mẫu đã duyệt, duyệt kế hoạch | Không tự duyệt, không áp mẫu trùng, giữ lịch sử |
| P06 | Dịch vụ thực tế và buổi điều trị | P05 | Phân biệt dự kiến/thực tế, xác nhận kết quả | Chỉ dịch vụ đã thực hiện được tính tiền |
| P07 | Kê đơn theo nhóm và mẫu hướng dẫn | P05, P03 | Bộ chọn nhiều thuốc, snapshot đơn, thay đơn | Không trừ kho khi kê; ký đúng quyền và đủ nội dung |
| P08 | Bảng tính phí và hóa đơn | P06, P07 | Bảng tính theo lượt, giá và nguồn chốt, dữ liệu in | Không sót/trùng dịch vụ hoặc đơn hiệu lực |
| P09 | Thu đủ trong một transaction | P08 | Đầu mối thanh toán, chống lặp, tiền thối, rollback | Không tồn tại bộ mới đã commit nhưng thu thiếu |
| P10 | Bàn giao thuốc và in lại | P09 | Danh sách chờ giao, xác nhận giao, bản in | Không giao trước thu, không xuất kho hai lần |
| P11 | Hoàn thiện giao diện và liên kết sáu chức năng | P04–P10 | Style cục bộ, thông báo lỗi, điều hướng có ngữ cảnh | Dùng được ở kích thước/DPI quy định |
| P12 | Nghiệm thu toàn luồng và đóng gói | P01–P11 | Kết quả kiểm thử, hướng dẫn SQL/chạy app, danh sách file | Đạt ma trận ở phần 13; phân hệ khác không hồi quy |

### P01 Khảo sát và tạo điểm quay lại

**Files:** Đọc solution, project, SQL và file ở phần 2.1; tạo `docs/clinical-workflow/BASELINE.md` và `CHANGE_SCOPE.md` trong bản triển khai. Không sửa source trong bước khảo sát.

**Interfaces:** Đầu vào là bản dự án người dùng đang mở và bản sao database; đầu ra là phiên bản baseline, mapping chức vụ, các chứng từ cũ cần đối chiếu và lệnh kiểm tra có thể chạy lại.

- [x] Xác nhận không dùng nhầm bản zip khác; ghi tên solution, target framework và phiên bản schema hiện có.
- [x] Nếu có Git, tạo nhánh làm việc riêng và ghi trạng thái thay đổi ban đầu; giữ các chỉnh sửa sẵn có của người dùng.
- [x] Backup database, restore sang tên thử nghiệm riêng; chỉ đặt `RHM_CONNECTION_STRING` cho bản thử, không ghi thông tin bí mật vào repository.
- [x] Chạy build và các bộ kiểm tra hiện có ở phần 12; lưu lỗi baseline và các giá trị tài chính/tồn dùng đối chiếu.
- [x] Chạy truy vấn tiền kiểm và lập danh sách nguồn cũ đã cấp/đã thu/dở dang; không sửa dữ liệu để qua kiểm tra. Lưu điểm baseline.

### P02 Quyền nghiệp vụ và giới hạn bác sĩ

**Files:** Tạo `ET/ClinicalWorkflowContracts.cs`, cặp `BUS_QUYENNGHIEPVU`/`DAL_QUYENNGHIEPVU`, `Controls/ClinicalWorkflow/QuyenNghiepVu.cs`; sửa nhánh quyền trong `MainWindow.xaml.cs`, `AppWorkspace.xaml.cs`, `DieuHuongHoSo.cs`; tạo project kiểm tra `RangHamMat.ClinicalChecks` theo kiểu console hiện có.

**Interfaces:** Cung cấp `LayNguCanh`; các API nghiệp vụ sau nhận `NguCanhNghiepVu` và tự xác minh lại. `CanOpen` giữ chữ ký, thêm kiểm tra hành động riêng thay vì dùng quyền mở để cho phép sửa.

- [x] Viết `QuyenVaTiepNhanChecks.cs`: bác sĩ A không đọc chi tiết/sửa mã của B; tài khoản tiếp nhận mở hồ sơ/thu; Admin không có chức vụ bác sĩ không ký; module ngoài phạm vi giữ quyền baseline.
- [x] Chạy để thấy ca mới thất bại trước khi bổ sung quyền, rồi triển khai ánh xạ chức vụ và nguồn đọc theo SQL.
- [x] Chặn mọi đường mở trực tiếp bằng MaLS/MaDT, kể cả sau khi xóa bộ lọc; với bệnh nhân được phân cho A vẫn cho đọc lịch sử đã xác nhận của B.
- [x] Kiểm tra quyền lại tại mutation; từ chối tài khoản đã khóa/chuyển chức vụ sau lúc mở form. Xóa cache có phạm vi khi đổi phiên đăng nhập.
- [x] Chạy lại các ca quyền và điều hướng; lưu thay đổi khi toàn bộ ca thuộc tác vụ đạt.

### P03 Migration và ánh xạ dữ liệu

**Files:** Bốn migration ở phần 9.5, `Database/QLBenhVienRangHamMat_ClinicalWorkflow.sql`; ET/DAL/BUS mới và cũ của bảng tại phần 9; cập nhật JSON map liên quan; `RangHamMat.SchemaChecks/Program.cs` và `.csproj`; `RangHamMat.ClinicalChecks/MigrationChecks.cs`.

**Interfaces:** Sáu bảng/cột tại phần 9 là hợp đồng chuẩn. DTO nghiệp vụ ở file Contracts không giả làm entity bảng; lớp entity phải có kiểu, nullable và `[Write(false)]` đúng cho tiền derived.

- [x] Viết kiểm tra schema, nullable, FK, nguồn giá và bảo toàn số liệu cũ; kiểm tra nâng cấp hai lần không nhân bản đối tượng.
- [x] Tạo script preflight, upgrade, verify và rollback có guard; thử trên hai bản: cài mới trống và restore baseline có dữ liệu.
- [x] Cập nhật ET, SQL ghi/đọc, validator và metadata đồng thời; chạy SchemaChecks trên hợp đồng sau migration, không xóa các assertion cũ để qua kiểm tra.
- [x] Kiểm tra trigger mới giữ nguyên nguồn hóa đơn/kho và bảo vệ nội dung đã ký; dữ liệu cũ null ở cột tiền đưa/thối không bị thay thành số bịa.
- [x] Thử rollback trước khi có dữ liệu mới và kiểm tra script dừng khi có chứng từ mới. Lưu kết quả và điểm thay đổi.

### P04 Hồ sơ và tiếp nhận

**Files:** Các file miền hồ sơ ở phần 10.1; tạo cặp `BUS_TIEPNHAN`/`DAL_TIEPNHAN`; bổ sung `QuyenVaTiepNhanChecks.cs` và smoke UI liên quan.

**Interfaces:** Cung cấp `LayBacSi`, `TiepNhan`; trả `KetQuaTiepNhan` gồm MaBN, MaHS, MaPhieu, MaBS, SoThuTu, TrangThai. Không tạo MaLS trong thao tác tiếp nhận.

- [x] Viết ca dịch vụ chuyên khoa X chỉ hiện bác sĩ X; đổi dịch vụ xóa bác sĩ cũ; lịch hẹn giữ đúng liên kết; không có bác sĩ thì từ chối lưu.
- [x] Làm bốn tab hồ sơ; tìm người trùng tên vẫn chọn theo mã; chỉ tạo hồ sơ mới khi BN chưa có hồ sơ.
- [x] Lưu tiếp nhận trong transaction; đối chiếu chuyên khoa và lịch, nhận nhân viên từ phiên, cấp số thứ tự từ SQL, kiểm tra duplicate lịch tiếp nhận đang hiệu lực dưới khóa.
- [x] Chạy hai yêu cầu cùng lúc để chứng minh không trùng số trong cùng ngày/phòng và không tiếp nhận một lịch hai lần; không sửa `LICH_KHAM` ngoài tích hợp có sẵn.
- [x] Kiểm tra hàng đợi bác sĩ đích xuất hiện sau tải lại; lịch sử hẹn/khám không trộn BN và không lộ bản nháp ngoài quyền. Lưu thay đổi.

### P05 Khám và kế hoạch điều trị

**Files:** Các file miền khám; cặp `BUS_KHAMDIEUTRI`/`DAL_KHAMDIEUTRI`; lớp mới cho danh mục/mẫu; `RangHamMat.ClinicalChecks/KhamVaKeHoachChecks.cs`.

**Interfaces:** Cung cấp `LayHangDoi`, `BatDauKham`, `LayChiTiet`, `LuuKham`, `GoiYKeHoach`, `ApDungMau`, `LuuKeHoach`, `XacNhanKeHoach`, `HoanTatKham`. `BatDauKham` trả MaLS hiện có khi phiếu đã được bắt đầu bởi đúng bác sĩ, không tạo bản thứ hai.

- [x] Viết ca bắt đầu một phiếu hai lần chỉ tạo một MaLS; bác sĩ khác bị từ chối dù biết mã; chuyên môn lưu nháp không chuyển sang chờ thu.
- [x] Làm form khám và tình trạng; lưu theo snapshot dưới khóa, báo xung đột khi hai cửa sổ sửa cùng nguồn.
- [x] Làm gợi ý mẫu theo mã và mức độ; kiểm tra mẫu Nháp không được gợi ý, không có mẫu vẫn lập thủ công, nhiều tình trạng không tự cộng bước.
- [x] Áp dụng cùng mẫu/nguồn hai lần không tạo kế hoạch trùng; bản đã sao chép giữ nội dung khi mẫu đổi. Duyệt kế hoạch không phát sinh dịch vụ thu phí.
- [x] Hoàn tất chỉ khi dữ liệu chuyên môn đủ và không còn đơn/dịch vụ nháp chưa xử lý; cho phép kế hoạch buổi sau chưa thực hiện. Chạy ca xác nhận rồi thử sửa/xóa nội dung và tình trạng, phải bị chặn. Lưu thay đổi.

### P06 Dịch vụ thực hiện

**Files:** `Views/DichVuThucHien.*`, phần kế hoạch trong `ChiTietKhamDieuTri.*`, `TreatmentModels.cs`; BUS/DAL dịch vụ, kế hoạch và chi tiết liên quan; `KhamVaKeHoachChecks.cs`.

**Interfaces:** Cung cấp `LuuThucHien`, `XacNhanThucHien`; giữ `MaDVThucHien`, `MaCTKH`, `MaLS` làm nguồn cho tính phí. Người phụ trách và người thực hiện là hai vai trò có thể khác nhau. `YeuCauThucHienDichVu` gồm MaDVThucHien tùy chọn, MaLS, MaCTKH tùy chọn, MaDV, MaNVThucHien, ThoiGianThucHien, SoLuong, DonGia, KetQua, DauVanTay và LyDoPhatSinh khi ngoài kế hoạch; giới hạn trường được sửa theo vai trò ở phần 4.3.

- [x] Viết ca chỉ có kế hoạch chưa làm thì tiền thu là 0; dịch vụ Nháp/Hủy không tính; dịch vụ xác nhận tính đúng một lần.
- [x] Tách tab dự kiến/thực tế; tạo thực hiện từ bước có sẵn không nhập lại mã dịch vụ tùy ý; dịch vụ phát sinh cần lý do.
- [x] Kiểm tra bước cùng hồ sơ, đúng dịch vụ, đủ lượng còn lại và thời gian hợp lệ; điều dưỡng chỉ sửa kết quả/thời gian dòng bác sĩ đã giao cho đúng MaNVThucHien của mình; bác sĩ của lượt xác nhận. Thử sửa dòng của điều dưỡng khác phải bị từ chối.
- [x] Kiểm tra liệu trình hai buổi: lượt thứ hai dùng bước còn lại cùng MaHS, hóa đơn mỗi lượt chỉ tính phần đã làm trong lượt đó; không sửa bước đã thu ở buổi trước.
- [x] Chạy kiểm tra nguồn, đồng thời xác nhận hai lần, và khóa sau thu; lưu thay đổi.

### P07 Đơn thuốc nhiều dòng

**Files:** Miền đơn thuốc ở phần 10.1; BUS/DAL đơn và cấu hình; ET chi tiết; `RangHamMat.ClinicalChecks/DonThuocChecks.cs`.

**Interfaces:** Cung cấp `TimThuoc`, `LuuDon`, `XacNhanDon`; `BoLocThuoc` có MaNhomThuoc, TuKhoa, dạng/hàm lượng tùy chọn; kết quả gồm mã, tên, hàm lượng, đơn vị, giá tham khảo, tồn tham khảo và cấu hình hướng dẫn.

- [x] Viết ca lọc theo nhóm, thêm ba thuốc, chặn thêm trùng, lượng/ngày không hợp lệ, thiếu hướng dẫn và thuốc ngừng dùng.
- [x] Làm bộ chọn thuốc nhiều lựa chọn; áp mẫu chỉ khi đã duyệt và bác sĩ xác nhận phù hợp đối tượng; thiếu mẫu không tự bịa hướng dẫn.
- [x] Lưu đầu đơn/các dòng/thay đơn trong cùng transaction. Bản ký có snapshot tên, hàm lượng, đơn vị, liều, đường dùng và hướng dẫn.
- [x] Chạy ca lỗi dòng thứ ba rollback cả đơn; thay đơn khi lượt đã hoàn tất nhưng chưa cấp/thu vẫn được; đơn cũ còn hiệu lực lúc soạn, chỉ sang Ngừng áp dụng khi ký mới; bảng tính chặn khi còn đơn thay thế nháp; đơn đã cấp/thu không cho thay; Không kê thuốc có lý do vẫn hoàn tất được.
- [x] Đối chiếu tồn trước/sau kê và ký phải bằng nhau; in đơn chỉ với dữ liệu đã xác nhận. Lưu thay đổi.

### P08 Bảng tính phí và hóa đơn

**Files:** `Views/HoaDon.*`, `CuaSoHoaDon.*`, các model mới; phần đọc của `BUS_THANHTOANTRONGOI`/`DAL_THANHTOANTRONGOI`; BUS/DAL hóa đơn/chi tiết; helper `DAL/PhanBoLoThuoc.cs` và phần tách helper tối thiểu ở `DAL_PHIEUXUAT.cs` nếu cần; `RangHamMat.ClinicalChecks/ThanhToanChecks.cs`.

**Interfaces:** Cung cấp `LapBangTinh(maLS, maKhoCapThuoc, nguoiDung)`; kho bắt buộc khi có thuốc và không bắt buộc khi chỉ có dịch vụ. `DauVanTay` tính từ dữ liệu nguồn ổn định, không phải một số tổng UI tự gán. Chưa gọi CreateInvoice hoặc DispensePrescription ở bước này.

- [x] Viết ca một dịch vụ 100.000 và hai dòng thuốc tổng 50.000 cho kết quả 150.000; thêm đơn hiệu lực thứ hai phải được cộng; Hủy/Ngừng áp dụng không cộng.
- [x] Truy vấn đúng lượt và nguồn còn hiệu lực; kiểm tra giá thuốc, dịch vụ và nguồn cũ chưa xử lý; không đọc kết quả chưa được quyền xem.
- [x] Tạo bảng kê và token nguồn; mô phỏng phân lô đúng logic ghi sổ, cộng tiền theo từng phần lô như trigger. Thử số lượng lẻ chia ba lô để phát hiện khác biệt làm tròn; thay giá, lượng hoặc lô dùng làm token đổi. Xem bảng tính không thay số dòng hóa đơn, xuất kho và thanh toán.
- [x] Với dữ liệu tài chính cũ, giữ hiển thị tổng và lịch sử chính xác; không biến tiền còn phải thu thành đã thu, không tạo nguồn thu lần hai.
- [x] Kiểm tra bản in dùng dữ liệu chốt và cách gộp lô vẫn đối chiếu được về MaCTPX. Lưu thay đổi.

### P09 Thanh toán đủ tiền và chống lặp

**Files:** `BUS_THANHTOANTRONGOI.cs`, `DAL_THANHTOANTRONGOI.cs`, `BUS/DAL_THANHTOAN.cs`, `BUS/DAL_HOADON.cs`, ET thanh toán/hóa đơn, ViewModel thu và `ThanhToanChecks.cs`; chỉ mở rộng luồng xuất thuốc bằng transaction sẵn có.

**Interfaces:** Cung cấp `ThanhToan` theo phần 10.3 và thứ tự phần 8.2. Constructor DAL/BUS con nhận cùng `IDbTransaction`. Không chuyển quyền commit xuống một lớp con.

- [ ] Viết ca phải thu 450.000: đưa 500.000 thì thu 450.000/thối 50.000; đưa 400.000 thì không ghi gì; chuyển khoản phải đủ và có mã tham chiếu.
- [ ] Viết ca hai thanh toán đồng thời cùng lượt, cùng mã yêu cầu, khác mã yêu cầu, mất kết nối sau commit rồi thử lại; đều chỉ có một kết quả thu/xuất.
- [ ] Viết ca giá đổi, tồn đổi, thiếu thuốc cuối đơn và lỗi chèn thanh toán; kiểm tra số dòng tất cả bảng và tồn sau rollback bằng trước.
- [ ] Triển khai điều phối với khóa ổn định, xác minh đầu vào, ghi tiền khách đưa/thối ngay khi INSERT Thu; tổng 0 có nguồn hợp lệ không tạo giao dịch 0. Kiểm tra đầu mối cũ bị chặn khi ghi thêm lên hóa đơn phiên bản 2.
- [ ] Chạy toàn bộ ca SQL bằng các connection độc lập khi kiểm tra tranh chấp; xác minh không có hóa đơn phiên bản 2 đã commit bị thiếu tiền, không quá thu và không trừ kho lặp. Lưu thay đổi.

### P10 Bàn giao thuốc và in chứng từ

**Files:** `Views/DonThuoc.*`, `XemTruocDonThuoc.*`, `XemTruocHoaDon.*`, `InvoiceDocument.*`, lớp bàn giao mới và `DonThuocChecks.cs`/`ThanhToanChecks.cs`.

**Interfaces:** `BUS_BANGIAOTHUOC.XacNhanGiao` chỉ chuyển trạng thái, ghi nhân viên dược và thời gian. Người thu trên chứng từ xuất kỹ thuật không thay thế người giao thuốc thực tế.

- [ ] Viết ca chưa thanh toán không giao được; đã thanh toán giao một lần; lần gọi thứ hai trả đã giao, không tạo thêm xuất kho.
- [ ] Làm danh sách chờ giao cho dược sĩ, xem đúng nội dung đơn, xác nhận người nhận phù hợp và hướng dẫn; mọi đơn của lượt đã giao thì phiếu Hoàn tất.
- [ ] In hóa đơn từ MaHD đã commit, đơn từ MaDT đã ký; in lại dùng cùng mã, thời gian và số tiền chốt. Bản trước thu chỉ ghi Bảng tính.
- [ ] Mô phỏng lỗi in: thu vẫn thành công, có nút in lại, không gọi ThanhToan lần nữa. Không sửa quyền sử dụng hoặc giao diện của phân hệ kho.
- [ ] Chạy kiểm tra bàn giao, tồn và các bản in nhiều dòng dài; lưu thay đổi.

### P11 Hoàn thiện trải nghiệm và liên kết

**Files:** Các view trong danh sách cho phép, `Themes/ClinicalWorkflowStyles.xaml`, ViewModel cục bộ, phần module liên quan của `QuyTacNhapLieu.xml`, `DuLieuGiaoDien.cs`, `BieuMauHoSo.cs`, `DieuHuongHoSo.cs`.

**Interfaces:** Điều hướng luôn mang MaBN/MaHS/MaPhieu/MaLS/MaDT hoặc MaHD cần thiết, và kiểm tra lại mối liên kết trước mở. `CanOpen` không được dùng như quyền ghi chung cho mọi nút.

- [ ] Rà trạng thái nút theo vai trò, trạng thái bản ghi và dữ liệu đang nhập; chuyển trang có thay đổi chưa lưu phải nhắc Lưu/Bỏ/Hủy rời trang.
- [ ] Áp style cục bộ; lỗi dưới trường, giữ lựa chọn và focus, báo khi dữ liệu nguồn vừa đổi. Không nuốt lỗi BUS/DAL rồi hiện thành công.
- [ ] Kiểm tra chuỗi liên kết hồ sơ → lượt → đơn → dịch vụ → hóa đơn luôn đúng BN; mã không tồn tại báo rõ, không tự chọn bản đầu danh sách.
- [ ] Chạy WPF thật ở ba kích thước, DPI 100/125/150 phần trăm; thử tên, địa chỉ, chẩn đoán và đơn dài, không có dữ liệu, đang tải và lỗi SQL.
- [ ] Kiểm tra tìm/phân trang với dữ liệu thử đủ lớn; lưu thay đổi sau khi UI và điều hướng đạt.

### P12 Nghiệm thu và bàn giao triển khai

**Files:** Toàn bộ thay đổi trong phạm vi; `docs/clinical-workflow/HUONG_DAN_CHAY.md`, `KET_QUA_KIEM_THU.md`, `CHANGE_SCOPE.md`; solution chỉ thêm project kiểm tra khi cần.

**Interfaces:** Đầu ra là source triển khai, migration/cài mới, hướng dẫn SQL và báo cáo các lệnh đã chạy thực tế; không chỉ gửi ảnh hoặc kế hoạch lần nữa.

- [ ] Chạy ma trận phần 13 trên database mới và database nâng cấp; fixture không để lại dữ liệu người bệnh hoặc chứng từ giả trong bản sử dụng.
- [ ] Chạy lại build, kiểm tra schema/persistence/tài chính/WPF và bộ hồi quy của các phân hệ liên quan tại phần 12; so với baseline.
- [ ] Kiểm tra diff file và DDL; mọi thay đổi ngoài danh sách phải được loại bỏ hoặc giải trình là tích hợp tối thiểu cho sáu miền.
- [ ] Thử khôi phục backup và kế hoạch quay lại; lập hướng dẫn thứ tự chạy SQL, cấu hình connection và thao tác theo từng chức vụ.
- [ ] Bàn giao danh sách file, migration, kết quả thực tế và các giới hạn chưa xử lý; chỉ ghi đạt nếu có bằng chứng. Không tự sửa tính năng khác để “tiện thể” cải tiến.

## 12 Lệnh kiểm tra dành cho Codex terminal

Chạy từ thư mục chứa solution trên Windows có .NET 8 SDK và SQL Server thử nghiệm. Đặt biến `RHM_CONNECTION_STRING` bằng cấu hình riêng của máy, không chép chuỗi kết nối có mật khẩu vào tài liệu/log. Khi bộ kiểm tra cần SQL chưa được cấu hình thì báo chưa chạy, không gắn nhãn PASS.

```powershell
dotnet build .\BenhVienRangHamMat.sln -c Debug
dotnet run --project .\RangHamMat.Checks\RangHamMat.Checks.csproj -- .
dotnet run --project .\RangHamMat.SchemaChecks\RangHamMat.SchemaChecks.csproj
dotnet run --project .\RangHamMat.PersistenceChecks\RangHamMat.PersistenceChecks.csproj
dotnet run --project .\RangHamMat.FinanceChecks\RangHamMat.FinanceChecks.csproj
dotnet run --project .\RangHamMat.ClinicalChecks\RangHamMat.ClinicalChecks.csproj
dotnet run --project .\RangHamMat.WpfSmoke\RangHamMat.WpfSmoke.csproj
```

`RangHamMat.ClinicalChecks` là project mới phải được tạo ở P02. Tổ chức tối thiểu gồm `Program.cs`, `Fixture.cs`, `QuyenVaTiepNhanChecks.cs`, `KhamVaKeHoachChecks.cs`, `DonThuocChecks.cs`, `ThanhToanChecks.cs`, `MigrationChecks.cs`. Làm theo mẫu console kiểm tra hiện có, không cần đưa thêm một framework test lớn. Kết quả mỗi ca có tên, PASS/FAIL và thông tin đủ tái hiện; fixture được dọn hoặc rollback.

Chạy thêm `RangHamMat.SupplierChecks`, `RangHamMat.MaintenanceChecks`, `RangHamMat.ReportChecks` để kiểm tra tác động của file dùng chung. Các kiểm tra tài chính cũ về thu/hoàn từng phần vẫn áp cho fixture phiên bản 1; bổ sung ca phiên bản 2 từ chối thu thiếu, không đổi fixture cũ thành mới rồi xóa ca kiểm tra.

Không chạy file có `GO` trực tiếp qua một `Execute` của Dapper. Dùng SSMS/sqlcmd hoặc bộ chạy script có tách batch đúng. Mọi script cài mới/nâng cấp chỉ nhắm database đã xác nhận; bước restore và rollback phải được thử độc lập với transaction kiểm tra thông thường.

## 13 Ma trận nghiệm thu tối thiểu

| Mã | Tình huống | Kết quả phải đạt |
| --- | --- | --- |
| AT01 | BN mới đến trực tiếp | Một BN, một hồ sơ tổng, một phiếu chờ đúng bác sĩ; chưa tự tạo lần khám |
| AT02 | BN cũ quay lại | Dùng hồ sơ cũ, thêm lượt mới; giữ lịch sử lịch hẹn và khám |
| AT03 | Hai BN trùng tên | Không trộn dữ liệu; chọn và lưu theo mã |
| AT04 | Chọn dịch vụ chuyên khoa X | Chỉ bác sĩ phù hợp; đổi dịch vụ kiểm tra lại bác sĩ/phòng |
| AT05 | Lịch hẹn sai BN/bác sĩ/phòng | Từ chối tiếp nhận, không sửa lịch ngầm |
| AT06 | Hai quầy cấp số cùng lúc | Không trùng số ngày/phòng, không hai phiếu cho cùng lịch đang hiệu lực |
| AT07 | Bác sĩ A dùng MaLS/MaDT của B | Không đọc nháp hoặc sửa/xác nhận, kể cả qua điều hướng trực tiếp |
| AT08 | A xem BN mình đang phụ trách từng khám B | Đọc được lịch sử đã xác nhận, không sửa lịch sử B |
| AT09 | Mẫu chưa duyệt hoặc không khớp | Không tự áp dụng; cho phép kế hoạch thủ công |
| AT10 | Bấm áp dụng mẫu hai lần | Một kế hoạch từ cùng tình trạng/mẫu, không lặp bước |
| AT11 | Kế hoạch hai buổi, mới làm buổi một | Chỉ thu phần thực hiện của lượt; buổi hai còn tiếp tục được ở lượt sau |
| AT12 | Dịch vụ chỉ có trong dự kiến | Không nằm trong tiền phải thu |
| AT13 | Kê nhiều thuốc và thêm trùng | Chọn theo nhóm được; có đủ liều/hướng dẫn/ngày/lượng; không trùng dòng âm thầm |
| AT14 | Lỗi lưu dòng thuốc cuối | Đơn không lưu dở và không làm mất bản cũ |
| AT15 | Không có chỉ định thuốc | Ghi lý do, không đơn rỗng; vẫn hoàn tất và thanh toán dịch vụ được |
| AT16 | Đổi mẫu hướng dẫn sau khi ký | Nội dung đơn đã ký và in lại giữ nguyên |
| AT17 | Kê hoặc ký đơn | Tồn kho không thay đổi |
| AT18 | Hai đơn hiệu lực cùng lượt | Bảng tính bao gồm đầy đủ, không chỉ lấy đơn đang chọn |
| AT19 | Phải thu 450.000, khách đưa 500.000 | Thu 450.000, lưu đưa 500.000, thối 50.000; một giao dịch Thu |
| AT20 | Phải thu 450.000, khách đưa 400.000 | Chặn thanh toán; không hóa đơn xác nhận mới, không thu, không xuất thuốc |
| AT21 | Giá hoặc tồn đổi sau xem bảng tính | Báo cập nhật lại; không thu số cũ hoặc cấp thiếu |
| AT22 | Lỗi giữa xuất thuốc và ghi thu | Rollback hóa đơn, chi tiết, thu, xuất và bàn giao; tồn như trước |
| AT23 | Hai lần bấm thu, hai quầy, thử lại sau mất mạng | Chỉ một bộ chứng từ được commit; trả lại đúng kết quả đã có |
| AT24 | Đã thu đủ nhưng chưa giao thuốc | Hiển thị chờ giao; dược sĩ giao xong không trừ tồn lần nữa |
| AT25 | In lỗi hoặc in lại | Dùng hóa đơn cũ, không thu hoặc xuất thêm |
| AT26 | Nâng cấp có chứng từ cũ thu/hoàn/nháp | Số liệu giữ nguyên; không suy tiền đưa/thối hoặc tự chốt nợ |
| AT27 | Hóa đơn nguồn hợp lệ tổng 0 | Không tạo giao dịch 0; chứng từ hiển thị đúng đã thanh toán 0 |
| AT28 | Tài khoản khóa, quyền bị đổi khi form đang mở | Lưu/xác nhận bị kiểm tra lại và từ chối khi không còn quyền |
| AT29 | Xung đột hai cửa sổ sửa một lượt | Người lưu sau được báo dữ liệu đổi; không ghi đè âm thầm |
| AT30 | Giao diện hẹp, DPI lớn, nội dung dài | Không che nút/cắt dữ liệu quan trọng, điều hướng bàn phím hoạt động |
| AT31 | Mở kho, lịch, nhân sự, báo cáo sau thay đổi | Quyền và nghiệp vụ cũ giữ nguyên; các kiểm tra hồi quy đạt theo baseline |
| AT32 | Thiếu thuốc sau khi bác sĩ đã hoàn tất lượt | Cùng bác sĩ thay đơn trước thu/xuất; không sửa kết luận đã ký; chặn thu khi đang soạn đơn thay thế |
| AT33 | Điều dưỡng mở dịch vụ giao người khác | Không sửa; chỉ cập nhật kết quả/thời gian dòng được giao cho mình |
| AT34 | Lượng thuốc lẻ chia nhiều lô | Bảng tính và tổng SQL cùng cộng tiền từng MaCTPX; không tự làm tròn bù chênh lệch |

## 14 Hướng dẫn giao tài liệu cho Codex terminal

Đặt file Markdown này ở thư mục `docs/clinical-workflow/` của dự án thực tế rồi dùng nội dung sau. Có thể mở bản Word để rà nghiệp vụ; Markdown là nguồn tiện đọc và đánh dấu tiến độ khi triển khai.

```text
Hãy đọc toàn bộ docs/clinical-workflow/KeHoach_CaiTo_QuyTrinhKham_6ChucNang.md
và mã nguồn hiện tại trước khi thay đổi. Triển khai lần lượt P01 đến P12.
Phần 1 đến 10 là đặc tả, phần 11 đến 13 là kế hoạch và điều kiện nghiệm thu.

Chỉ cải tổ hồ sơ bệnh nhân cùng tiếp nhận, khám điều trị, đơn thuốc,
dịch vụ thực hiện, hóa đơn và thanh toán. Giữ WPF .NET 8 và ET–BUS–DAL.
Không sửa chức năng khác ngoài điểm tích hợp tối thiểu đã liệt kê.

Trước khi ghi SQL, tạo bản sao database để thử; không chạy script cài mới
lên database đang có dữ liệu. Không tự xóa hoặc hợp thức hóa chứng từ cũ.
Đặc biệt giữ quyền bác sĩ theo MaNV, cơ chế bác sĩ duyệt gợi ý,
thu đủ một lần có tiền khách đưa và tiền thối, giao dịch chống trùng,
rollback cả xuất thuốc/hóa đơn/thu và bàn giao thuốc sau thanh toán.

Sau mỗi tác vụ, đánh dấu việc đã xong trong kế hoạch và ghi rõ các lệnh
kiểm tra đã chạy, kết quả thực tế, file thay đổi. Không báo PASS khi chưa chạy.
Nếu khác biệt mã nguồn/database ảnh hưởng nghiệp vụ hoặc có nguy cơ mất dữ liệu,
ghi bằng chứng, hoàn thành phần độc lập an toàn và nêu đúng điểm cần quyết định;
không tự mở rộng sang phân hệ khác.
```

### 14.1 Điều kiện bàn giao cuối cùng

Kết quả triển khai phải gồm source đã sửa trong phạm vi, migration nâng cấp và bản SQL cài mới, hướng dẫn chạy trên Windows/SQL Server, cấu hình quyền theo chức vụ, mẫu nghiệp vụ có trạng thái duyệt rõ ràng và báo cáo kiểm thử. Tài liệu này là kế hoạch để triển khai, không phải xác nhận phần mềm đã được triển khai hoặc được thẩm định chuyên môn.

## 15 Nguồn tham chiếu nghiệp vụ

1. Mã nguồn và `Database/QLBenhVienRangHamMat.sql` trong bản zip người dùng gửi, khảo sát ngày 24 tháng 10 năm 2026. Đây là căn cứ chính cho tên bảng, lớp, trigger, view và giới hạn thay đổi.
2. Bệnh viện Trường Đại học Y Dược Huế, “Quy trình khám bệnh”: https://bvydhue.vn/quy-trinh-kham-benh . Truy cập ngày 24 tháng 10 năm 2026. Trang mô tả một quy trình ngoại trú BHYT cụ thể từ tiếp đón, khám, thanh toán đến nhận thuốc; tài liệu này chỉ tham khảo thứ tự công việc, không lấy quy định BHYT để áp cho mô hình tự chi trả.
3. WHO, “Medication Without Harm”: https://www.who.int/initiatives/medication-without-harm . Truy cập ngày 24 tháng 10 năm 2026. WHO nêu rủi ro sai sót ở các bước kê đơn, cấp phát và sử dụng thuốc; từ đó người lập kế hoạch đề xuất bác sĩ duyệt gợi ý, giữ nội dung đơn đã ký và kiểm tra bàn giao. Đây là quyết định thiết kế, không phải trích dẫn một phác đồ điều trị hoặc tuyên bố chứng nhận tuân thủ.
