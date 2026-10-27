# Bàn giao P12 — 27/10/2026

Phạm vi: hoàn tất kế hoạch cải tổ và thu gọn solution về RangHamMat.Wpf, ET, DAL, BUS; Views dùng cặp XAML/code-behind; SQL chính đồng bộ52 bảng/version2. Không thay database gốc hoặc bản thử chính từ P01. Database cũ46 bảng cần chạy migration01→02→03; installer chính không tự nâng cấp database có dữ liệu.

## Kết quả thực thi

| Nhóm | Kết quả |
| --- | --- |
| Cài mới/nâng cấp | SQL chính cài52 bảng; nâng cấp hai lần không nhân bản; rollback trước dữ liệu về46 bảng; có dữ liệu mới bị chặn rollback;30 kiểm hợp đồng đạt mỗi DB |
| Chạy lại installer | Dừng50001; số bảng/tổng hóa đơn cũ không đổi |
| SQL nghiệp vụ |172/172 trên cài mới và172/172 trên nâng cấp, fixture rollback; gồm SQL1000 BN/10000 lượt |
| Deployment |4/4: cấu hình/env, SQL chính version2, nhân viên cùng transaction, admin seed đọc hồ sơ |
| Liên tục |3/3 trên mỗi DB: khám/ký→thu→giao→in/retry; một đơn/hai đơn/không thuốc tổng0; một bộ chứng từ, không thu/xuất lại |
| Thu/giao |25/25 thanh toán,13/13 bàn giao; lỗi cuối/rollback, tranh chấp hai kết nối và idempotency |
| WPF lâm sàng |39/39 mỗi DB;127/127 P11 mỗi DB sau sửa cuối:28 nghiệp vụ+99 tổ hợp11 view×3 kích thước×3 DPI |
| WPF thu/giao |5/5 thanh toán,8/8 bàn giao/in |
| Bàn phím |Tab/Shift-Tab thật trên form khám1024×768, focus đến ô hiển thị và giữ nhập liệu; đạt cả hai DB |
| Schema/persistence |45 entity/363 cột,52 bảng;16/16 persistence rollback |
| Hồi quy |Finance54, Supplier, Maintenance, Report18 kèm --sql đạt |
| WPF toàn ứng dụng |Đăng nhập modal PBKDF2 từ seed SQL và25/25 module; smoke hồ sơ/lịch/form/dịch vụ/phí/in/rollback;616 assertion nhập liệu/schema/binding đạt |
| Kết nối từ file |Không env, dùng appsettings cạnh executable, đăng nhập/mở25 module đạt |
| Đồng thời |Tiếp nhận/khám trên hai DB; dịch vụ/ký-thay đơn/bảng phí trên ba DB restore riêng; lượng, xung đột, tiền/tồn đúng |
| Recovery |COPY_ONLY CHECKSUM, VERIFYONLY, restore tên mới, CHECKDB; hash dữ liệu52 bảng khớp |

Các suite tạo tranh khóa không được chạy chồng với fixture ma trận: một lượt chồng có4 deadlock1205; lượt nối tiếp cuối127/127 trên từng DB đạt. Không thêm retry tự động hoặc đổi khóa để che kết quả đó. Chưa benchmark thông lượng nhiều quầy; writer thu/giao vẫn tuần tự.

## Sửa lỗi và hợp đồng kiểm thử

- SQL tên chính trước đây46 bảng: thay bằng installer52 bảng đã thử, gộp bản cài mới trùng; giữ migration dữ liệu cũ.
- Kết nối gắn tên máy cũ: env→appsettings cạnh executable→SQL local; copy cấu hình khi build/publish.
- Nhân viên mở kết nối riêng trong transaction BUS: chuyển DAL sang ThucHien cùng transaction; ca đọc thay đổi rồi rollback đạt.
- Đăng nhập modal gắn principal trong ExecutionContext tạm: khởi tạo default principal một lần tại App.Startup; chỉ cập nhật định danh khi guard đổi phiên cho phép. Quyền/trạng thái vẫn đọc lại SQL ở từng thao tác. Đăng nhập seed/mở25 module RED→GREEN. Cơ sở kỹ thuật: [DispatcherOperation WPF](https://raw.githubusercontent.com/dotnet/wpf/main/src/Microsoft.DotNet.Wpf/src/WindowsBase/System/Windows/Threading/DispatcherOperation.cs), [AppDomain .NET](https://raw.githubusercontent.com/dotnet/runtime/main/src/libraries/System.Private.CoreLib/src/System/AppDomain.cs).

Persistence và WpfSmoke từng không biên dịch nay đã chạy đạt. Harness được cập nhật theo hợp đồng hiện hành: mã D3;39 entity nền đọc chung, sáu entity mới qua API quyền riêng; mảng là IEnumerable; validator nhân viên thật; cột Quantity và nút lịch ở vùng hiển thị. Không nới nghiệp vụ để chiều kỳ vọng cũ.

Source-check cũ báo112 lỗi theo quy ước hết hiệu lực: cấm tên Clinical/partial, cấm FlowDocument/adaptive layout, parser metadata và chỉ tìm event trong một file. Bộ này được checkpoint rồi loại khỏi bản giao; không gọi112 lỗi đó là đã sửa. Cổng mới tools/KiemTraBanGiao.ps1 và build XAML thật kiểm bốn project, cặp view, SQL52/version2, cấu hình, Debug/Release; Verify03 đối chiếu SQL chỉ đọc.

## Ma trận AT01–AT34

Đây là kiểm chứng phần mềm SQL/WPF, không xác nhận y khoa hoặc tự đối soát ngân hàng. Source harness ở checkpoint trước dọn; log trong evidence/P12.

| Mã | Bằng chứng đạt |
| --- | --- |
| AT01–AT03 |Tiếp nhận BN mới/cũ/trùng tên; UI lưu/đọc biên nhận đúng mã; hai quầy |
| AT04–AT06 |Dịch vụ/chuyên khoa/bác sĩ/phòng/lịch sai; khóa lịch tới commit, một phiếu/số |
| AT07–AT08 |MaLS/MaDT trực tiếp chặn nháp người khác, đọc lịch sử ký đúng BN |
| AT09–AT10 |Mẫu nháp/không khớp không tự áp, thủ công; áp hai lần không nhân bước |
| AT11–AT12 |Hai buổi cùng hồ sơ dùng lượng còn lại, phí tách lượt; dự kiến không thu |
| AT13–AT17 |RX07 nhiều dòng/trùng/thiếu; lỗi dòng cuối rollback; không kê có lý do; snapshot ký và tồn không đổi |
| AT18 |Đủ mọi đơn hiệu lực; acceptance hai đơn và P09 xuất đủ |
| AT19–AT20 |450000/đưa500000/thối50000; thiếu tiền không ghi chứng từ/tồn |
| AT21–AT23 |Token giá/tồn đổi; lỗi audit/thu cuối rollback; UUID/alias/hai kết nối trả cùng bộ |
| AT24–AT25 |Chờ giao/giao lại cùng đơn, tiền/tồn không đổi; mã in cố định, lỗi in chỉ in lại |
| AT26 |Hash46 bảng/trigger/view qua nâng cấp; số dư và phần lẻ giữ nguyên; baseline đối chiếu riêng |
| AT27 |Nguồn0 không tạo Thu0, acceptance không thuốc tổng0 |
| AT28–AT29 |Khóa tài khoản/đổi chức vụ khi mở form; hai snapshot một commit/một xung đột; giữ nháp |
| AT30 |99 tổ hợp kích thước/native-DPI mỗi DB; nội dung dài, nút/hàng/parent shell; Tab/Shift-Tab |
| AT31 |Login25 module, Finance54/Supplier/Maintenance/ReportSQL18; persistence16/schema; lịch độc lập |
| AT32–AT33 |Thay đơn trước thu/xuất, chặn thu khi đang soạn thay; điều dưỡng chỉ sửa dòng được giao; concurrency |
| AT34 |Ba lô lượng lẻ cộng từng phần lô đúng trigger/SQL; FIFO và tiền lẻ lịch sử giữ nguyên |

## Vận hành và giới hạn

Bản cuối chỉ bốn project; chín helper/partial rời Views sang Presentation, giữ namespace/sự kiện. Source kiểm thử được checkpoint trước khi bỏ, không phụ thuộc chúng để build/chạy. Không xóa Word/chứng từ SQL/journal YEU_CAU_THANH_TOAN vì journal là dữ liệu chống thu lặp. Tài liệu cũ gom docs/legacy; README/HUONG_DAN_CHAY là hướng dẫn hiện tại.

Default appsettings dùng SQL local/database QLBenhVienRangHamMat. Database gốc trên máy không bị tự nâng cấp; bằng chứng app dùng DB thử riêng đã đồng bộ. Cài mới hoặc chạy migration rồi Verify03 trên database được chọn trước khi chạy. Không restore đè dữ liệu đang dùng.

Chưa gửi in ra máy in vật lý hoặc di chuyển giữa nhiều màn hình thật. Đã thử paginator/FlowDocument, nội dung in, native-DPI và focus; không khẳng định nghiệm thu driver/máy in hay mọi công nghệ hỗ trợ tiếp cận. Không seed nội dung điều trị/liều dùng. Form mẫu ngoài sáu miền không tự trở thành tính năng mới chỉ vì kiểm mở25 màn hình; P12 giữ phạm vi nghiệp vụ đã duyệt.

## Chốt bàn giao

Checkpoint source kiểm thử, kết quả build/publish cuối, danh sách file và dọn DB fixture được bổ sung sau lượt dọn/review. Không cộng các lần chạy lặp thành số ca độc lập.
