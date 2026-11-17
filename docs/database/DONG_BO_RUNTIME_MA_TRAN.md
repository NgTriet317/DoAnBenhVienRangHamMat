# Rà soát đường chạy đồng bộ WPF ngày 17/11/2026

Đối chiếu `MainWindow.OpenAppCore`, `OpenModuleWindow`, `AppWorkspace`, các lời gọi tạo view và control khai báo trong XAML. Ma trận này bổ sung [phạm vi adapter](DONG_BO_PHAM_VI.md), không thay bằng chứng nghiệm thu hai máy vật lý.

## Phát hiện đã sửa

| Tình huống tái hiện | Nguyên nhân | Thay đổi |
| --- | --- | --- |
| Nhật ký đã tải; nhập khoảng ngày đảo rồi Tải lại; thay đổi tiếp theo từ máy khác không tải được | `Load(true)` gán `applied` trước khi DAL kiểm tra; nhịp đồng bộ tiếp tục dùng bộ lọc sai | Kiểm tra `BoLocNhatKy.KiemTra()` trước khi chấp nhận; giữ bảng/chi tiết trước khi có lỗi |
| Máy A đổi tên nhà cung cấp; máy B mở chi tiết lô trong Kho | SQL `layLoTonTheoKho` JOIN NHA_CUNG_CAP nhưng adapter thiếu phụ thuộc | Thêm NHA_CUNG_CAP |
| Đổi tên khoa; phiếu hướng dẫn ở máy B còn tên cũ | `DAL_PHIEUHUONGDAN.layDanhSachPhieu` JOIN KHOA | Thêm KHOA |
| Đổi dòng hóa đơn ảnh hưởng đối soát hoa hồng; Bảng lương ở B không tải lại | `layDoiSoatTheoKy` đọc CHI_TIET_HOA_DON | Thêm CHI_TIET_HOA_DON |
| Đổi danh mục tình trạng/liên kết dịch vụ khi màn khám sạch đang mở | `LayKhongGianKham` đọc DM_TINH_TRANG_BENH, DICH_VU_TINH_TRANG và nguồn phiếu/hồ sơ | Bổ sung các bảng nguồn vào adapter KhamLamSang |
| Đổi thuốc, cấu hình, tồn kho hoặc trạng thái thực hiện; trình kê đơn còn danh mục/quyền cũ | Adapter kê đơn thiếu nguồn `TimThuoc`, `RxVisit`, `Processed`, `DaThucHienXong`; callback không đọc lại nhóm | Bổ sung phụ thuộc; nạp lại nhóm và giữ mã nhóm trong `loading` |
| Đổi tên người bệnh/dịch vụ/nhân viên hoặc liên kết hóa đơn; lịch sử thực hiện còn nhãn/trạng thái cũ | `LayKhongGianThucHien` đọc các bảng phụ ngoài DICH_VU_THUC_HIEN | Bổ sung nguồn người bệnh, phiếu, hồ sơ, danh mục và CHI_TIET_HOA_DON |
| Giá thuốc, nguồn phụ thu AI hoặc chứng từ tồn kho đổi; bảng tính sạch chưa đổi | `LapBangTinh`, `SurchargeSourcesSql`, `PhanBoLoThuoc.LayLo` đọc thêm bảng giá/AI/nhập/xuất | Bổ sung nguồn THUOC, HANG_HOA, DICH_VU, phiếu/hồ sơ/bệnh nhân, AI_KET_QUA_TAO_PHIEU, AI_YEU_CAU_TAO_PHIEU, dòng nhập/xuất |
| Đổi chi tiết xuất/hóa đơn hoặc tên người bệnh/nhân viên; bàn giao/lịch sử đơn chưa đổi | Hàng chờ và chi tiết bàn giao JOIN chứng từ và nguồn định danh | Bổ sung phụ thuộc ở BanGiaoThuocEditor và PrescriptionHistoryDetail |
| Đường legacy: mở Hướng dẫn thuốc từ BanGiaoThuocEditor; A sửa/duyệt cấu hình; B giữ dữ liệu cũ | Child được gắn bằng `Content=editor`, không có Observe/Register; chưa có lối tạo BanGiaoThuocEditor từ menu hiện tại | Thêm adapter Prepared, giữ mã thuốc và tìm kiếm, đọc trước nhóm/thuốc/hướng dẫn; hoãn khi có nháp hoặc tên nhóm đang nhập |
| Đường legacy: nhập hướng dẫn/tên nhóm rồi bỏ focus | Child hướng dẫn không tham gia IClinicalPendingWork | Bổ sung hợp đồng pending tường minh; không tự duyệt, không tự lưu nhóm khi rời |
| Refresh cha khám gọi lại ngữ cảnh kê đơn; truy vấn con thất bại | `KeDonThuocEditor.Run` bắt lỗi rồi trả false trong nhịp tự động, làm bộ điều phối có thể ghi nhận thành công | Phát lại lỗi khi đồng bộ tự động để giữ pending và thử lại; xử lý thao tác thủ công giữ nguyên |

Kiểm độc lập Nhật ký xác nhận mặc định không đặt ngày, hành động rỗng; LOGIN_SUCCESS/LOGOUT có trong lựa chọn và được chuyển nhãn đúng; MaNK giữ lựa chọn qua tải lại; UTC hiển thị cộng 7 giờ; dòng bị xóa để trống chi tiết. Các đường này không tái hiện lỗi mặc định loại LOGIN/LOGOUT. Chặn bởi focus ô lọc thuộc bộ điều phối dùng chung, không phải payload hoặc mã hành động Nhật ký.

## Toàn bộ 26 lối vào chính

`Prepared` đọc nền rồi áp dụng trên Dispatcher. `Cache` dùng Observe/RegisterCache và tải cache theo phạm vi. `SQL adapter` là callback tường minh hiện có, vẫn có thể đọc SQL trên Dispatcher. Các adapter đều chịu guard modal, pending, edit và generation của bộ điều phối. Bảng dưới là đối chiếu mã; các kiểm riêng được ghi ở cuối.

| Lối vào | Cơ chế/đường tải chính | Phần con thực sự tạo hoặc nạp |
| --- | --- | --- |
| TongQuan | Tĩnh, không truy vấn động | Không có child SQL |
| BenhNhan | Cache + truy vấn phân trang | HoSoBenhNhan; trong hồ sơ có LichKham, ChiTietKhamDieuTri |
| LichKham | Cache | ScheduleView; ThemLichKham và ChiTietLichKham qua form/modal |
| KhamDieuTri | Cache + hàng chờ | ChiTietKhamDieuTri → KhamLamSang; editor kê thuốc và ảnh theo lượt |
| DichVuThucHien | Child SQL adapter | ThucHienDichVuEditor; cửa sổ AnhXquangDuocGiao → AnhXquangLanKham |
| DonThuoc | Cache + truy vấn lịch sử | PrescriptionHistoryDetail và cửa sổ bản in |
| NhanSu | Prepared | ChiTietNhanSu modal; QuyPhepNhanSu khai báo trong XAML có Prepared riêng |
| ChungChiNhanVien | Prepared | CuaSoThemChungChi, ChiTietChungChi modal |
| CaLamViec | Prepared | DonXinNghiQuanLy trong overlay có adapter riêng; overlay nhập/duyệt giữ nguyên |
| ThietBi | Prepared | Form nhúng hoãn theo dangSua |
| BaoTri | Cache | Tab thanh lý; CuaSoBaoTri, CuaSoThanhLy trong workspace |
| Kho | Prepared | Tồn/lô cùng payload; không tự ghi sổ |
| DanhMucHangHoa | Prepared | Tab hàng hóa/thuốc/vật tư/thiết bị; hoãn dangThem/dangSua |
| NhaCungCap | Cache | CuaSoNhaCungCap trong workspace |
| NhapXuatVatTu | Prepared | HoaDonNhaCungCap, NhapKho, XuatKho được tạo một lần cho các tab; mỗi child có adapter riêng |
| DichVu | Prepared | Trường danh mục theo DanhMucHeThong; giữ bản form khi thay đổi |
| HoaDon | Cache + finance query | BangTinhPhiEditor Prepared; CuaSoHoaDon; bản in |
| BangLuong | Prepared | Overlay phiếu lương; hoãn khi mở |
| ChamCong | Prepared | Form giờ/lý do/kết quả so với mốc; hoãn khi khác mốc |
| ChinhSachLuong | Prepared | Giữ form có thể lưu nháp, kể cả chưa gõ; không tự tạo/duyệt bản |
| TaiKhoan | Prepared | Hai CuaSoTaiKhoan nhúng trong XAML dùng cùng snapshot; hoãn editor/quyền thay đổi |
| BaoCao | RegisterAsync | ChartView và tài liệu xuất dùng dữ liệu báo cáo cha |
| PhieuHuongDan | Prepared | TiepNhanAI mở modal; dữ liệu phiếu đọc lại khi modal đóng |
| TroLyAI | Phiên hội thoại cục bộ, không phát lại chat | Bản khai đang nhập và cấu hình/prompt của máy hiện tại |
| NhatKy | Prepared | Bảng/chi tiết cùng trang đã áp dụng; chỉ các bảng nhật ký/phiên/định danh |
| XinNghiViec | Prepared chỉ số dư/ước tính | Giữ ngày/lý do/bàn giao trong đơn đang nhập |

`VatTu` được điều hướng sang Kho; không còn `new VatTu()` trong luồng chính. Alias NhapKho/XuatKho mở NhapXuatVatTu rồi chọn tab tương ứng. Các bản legacy không được tính là màn hình đang có đường chạy riêng.

## Child và phạm vi giữ trạng thái

| Child / nơi mở | Đọc dữ liệu và guard |
| --- | --- |
| HoSoBenhNhan / BenhNhan, AppWorkspace | Cache; giữ MaBN, tab, lượt đang mở; LichKham child có đăng ký riêng |
| ChiTietKhamDieuTri / KhamDieuTri, HoSoBenhNhan | Cache; KhamLamSang và ảnh lịch sử có adapter riêng; giữ nguồn MaLS |
| KhamLamSang / ChiTietKhamDieuTri | Prepared nguồn khám chính; áp dụng tổng kết vẫn đọc SQL cũ; tình trạng, kế hoạch, dịch vụ và trạng thái lần khám; nháp qua IClinicalPendingWork |
| KeDonThuocEditor / KhamLamSang | SQL adapter; giữ lượt/đơn/nhóm/tìm kiếm; dirty và KeDonThuocWindow modal hoãn |
| ThucHienDichVuEditor / DichVuThucHien | SQL adapter; lịch sử đã chốt, nguồn khám/phiếu/hồ sơ/người bệnh/dịch vụ/nhân viên/hóa đơn |
| BangTinhPhiEditor / HoaDon | Prepared; nguồn phí/giá/tồn/phụ thu; tiền đang nhập hoặc yêu cầu chờ xử lý hoãn qua pending |
| BanGiaoThuocEditor / legacy, chưa có lối tạo từ menu/XAML hiện tại | SQL adapter; nhận thuốc/hướng dẫn đã tích hoặc yêu cầu chờ xử lý hoãn. Luồng bàn giao chính hiện tại nằm trong PrescriptionHistoryDetail |
| HuongDanThuocEditor / BanGiaoThuocEditor.Content, thuộc chuỗi legacy trên | Prepared mới; CAU_HINH_THUOC_KE_DON, NHOM_THUOC_KE_DON, THUOC, HANG_HOA; dirty/tên nhóm bảo vệ toàn workspace nếu đường này được sử dụng |
| PrescriptionHistoryDetail / DonThuoc | SQL adapter; bản đơn và trạng thái bàn giao; giữ nguồn đơn, chặn yêu cầu bàn giao |
| AnhXquangDuocGiao / ThucHienDichVuEditor | Prepared; giữ MaLS và instance ảnh child; bỏ nguồn khi lượt không còn |
| AnhXquangLanKham / lượt khám, ảnh được giao | Prepared metadata; giữ MaAnh; ghi chú/upload/chọn kỹ thuật viên hoãn; không tự mở ảnh ghi audit |
| HoaDonNhaCungCap, NhapKho, XuatKho / NhapXuatVatTu | Prepared riêng; giữ chứng từ/tab/lọc; hoãn draft hoặc form tạo/sửa |
| QuyPhepNhanSu / NhanSu XAML | Prepared; giữ năm/MaNV và chi tiết quỹ đang xem |
| DonXinNghiQuanLy / CaLamViec | Prepared; overlay chi tiết/đơn khẩn cấp hoãn |
| CuaSoTaiKhoan / TaiKhoan XAML | Snapshot cùng adapter cha; mốc form/password bảo vệ thay đổi chưa lưu |
| ChiTietDonThuoc legacy / chưa có lối tạo hiện tại | Observe; nội dung dựa cache; không tính là chi tiết đơn đang được mở từ menu |
| CuaSoBenhNhan, CuaSoLichKham, CuaSoKhamDieuTri, CuaSoDonThuoc, CuaSoHoaDon, CuaSoBaoTri, CuaSoNhaCungCap, CuaSoNhapXuatVatTu, CuaSoBangLuong, CuaSoDichVu, CuaSoPhieuHuongDan / MainWindow hoặc AppWorkspace | Form workspace; không nạp đè input. Những form dùng Observe vẫn chịu guard workspace |
| TiepNhanKham / AppWorkspace | Cố ý hoãn suốt khi form tiếp nhận mở |
| ThemLichKham, ChiTietLichKham, ChiTietHoSo / lịch và workspace | Form/modal giữ bản gốc; ThemLichKham có dirty input tường minh |
| ChiTietNhanSu, CuaSoThemChungChi, ChiTietChungChi / nhân sự và chứng chỉ | Window modal; đóng xong mới áp dụng pending |
| TiepNhanAI / PhieuHuongDan | Prepared danh sách bản khai ngay trong modal; giữ phần đối chiếu/danh tính/phòng/bác sĩ và bản gốc; bản khai đổi từ xa thì khóa tạo phiếu, bấm Làm mới để đối chiếu lại. Không tự tạo phiếu hoặc tự duyệt |
| CauHinhMayChu / khởi động | Cấu hình kết nối của máy, trước vòng đồng bộ; không nhận thay đổi cấu hình máy khác |
| XemAnhXquang, XemTruocHoaDon, XemTruocDonThuoc, các document/print controls | Tài liệu hoặc tệp đã dựng; không tự đổi bản in đang xem. Các danh sách read-only trong Window thường vẫn có adapter riêng |
| ScheduleView, ChartView, PrescriptionCard, ExaminationCard, InvoiceDocument, PrescriptionPrintLine | Trình bày nguồn cha; không có vòng đồng bộ SQL độc lập |

Không đưa MAU_KE_HOACH_DIEU_TRI/CHI_TIET_MAU_KE_HOACH vào adapter KhamLamSang chỉ vì bảng có trong schema: luồng UI hiện tại không gọi DAL mẫu kế hoạch. Việc phục hồi UI mẫu cần đối chiếu thêm đường tải và guard riêng.

## Bằng chứng và giới hạn

- `dotnet run --project Test/RuntimeCoverageChecks --no-restore -v quiet`: **15/15 pass** sau khi các hồi quy filter, phụ thuộc bảng, child hướng dẫn và nuốt lỗi từng thất bại trước bản sửa.
- Ba kiểm Nhật ký dùng UserControl và DataGrid thật với payload đã vật hóa trong bộ nhớ; kiểm default LOGIN/LOGOUT/UTC, filter invalid và lựa chọn/chi tiết. Không gọi SQL và không mô phỏng hai máy.
- Ba kiểm Kho/Phiếu hướng dẫn/Bảng lương phát Loaded thật với initial-load guard để kiểm registry; không chạy BUS/DAL.
- Bảy kiểm adapter lâm sàng đọc hằng đăng ký từ IL đã biên dịch, không chạy constructor có SQL. Đây là bằng chứng phụ thuộc đã có trong binary, không chứng minh mọi callback hoạt động trên SQL.
- Một kiểm pending hướng dẫn dùng TextBox thật để bảo vệ nháp mất focus. Adapter Prepared hướng dẫn cần thêm nghiệm thu SQL/giao diện cho việc thuốc bị ngừng/xóa và thay đổi quyền giữa chừng.
- Một kiểm thực gọi bộ bắt lỗi kê đơn dưới `ApplyPrepared`, xác nhận lỗi đọc con thoát ra cho bộ điều phối thử lại.
- Các SQL callback cũ như kê đơn/lịch sử thực hiện/bàn giao và phần tổng kết của KhamLamSang vẫn có truy vấn trên Dispatcher. Bản sửa này không gọi chúng là toàn bộ luồng tải nền.
- Nghiệm thu nhiều màn hình và hai tiến trình SQL do các bộ WpfSyncChecks/RuntimeSyncChecks/SqlSyncChecks cung cấp riêng; không dùng kết quả 15 kiểm trên để khẳng định đã nghiệm thu hai máy vật lý hoặc mọi nút nghiệp vụ.

Chạy kiểm mới không cần database. Mọi kiểm SQL phải dùng database fixture `RHM_Hospital_Test_*`; không dùng database nghiệp vụ.

## Kiểm tích hợp bổ sung toàn bộ lối vào

`AllModulesSyncChecks`: 29/29 pass với WPF và SQL thật, gồm 26 lối vào từ `MainWindow.AllPages` và 3 tab con nhập/xuất. Mỗi màn hình dữ liệu phải có adapter hiển thị, đọc/áp dụng thành công và xác nhận hết pending; TongQuan và TroLyAI được kiểm là nội dung tĩnh/cục bộ. Tab nhập kho chọn phiếu đã lưu để thoát form tạo mới mặc định. Invalidation được đưa chủ động qua bộ điều phối; không đánh đồng kết quả này với 29 tình huống polling từ máy khác.

`RuntimeLoginSyncChecks`: 6/6 pass với vòng polling thật và các kết nối SQL riêng, bổ sung hai tình huống mở modal tiếp nhận AI: bản khai mới xuất hiện trong danh sách, trạng thái bản khai đang đối chiếu đổi từ xa làm khóa tạo phiếu. Cả hai giữ ô đang nhập; số bệnh nhân/phiếu khám không đổi do đồng bộ.

`WpfSyncChecks`: 28/28 pass, thêm bảo vệ nháp AI và danh sách modal độc lập, không xác nhận nhầm pending của cửa sổ cha. Các giới hạn hai máy vật lý và các nút nghiệp vụ vẫn như trên.

Đã sửa lỗi dùng kết nối phụ khi cache ngoài lâm sàng tải nhân sự: kết nối đó chờ khóa phiên do chính giao dịch cache giữ. BUS nhân sự nay nhận cùng transaction; toàn bộ 29 lượt điều hướng/áp dụng đã chạy qua sau sửa.
