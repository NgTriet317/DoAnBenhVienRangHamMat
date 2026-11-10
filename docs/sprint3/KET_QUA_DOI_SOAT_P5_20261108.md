# Kết quả P5 — đối soát dữ liệu và trình diễn ba kỳ

Đã triển khai P5 theo mục7–9 của `KeHoachTrienKhai.docx`, tiếp nối P0–P4.
Giữ nhánh `Sprint3_tan`, bốn project, thay đổi package của người dùng và file SQL
triển khai duy nhất. P5 không bổ sung DDL sản phẩm, không nâng cấp database đang
dùng, không sửa bệnh án/chứng từ cũ.

## Đối soát chỉ đọc

Phân hệ Nhật ký thao tác đã có; P5 bổ sung công cụ đối soát chất lượng dữ liệu.
`BUS_DOISOATDULIEU` → `DAL_DOISOATDULIEU` chạy SELECT và trả DTO ET, không tạo
nhật ký truy cập hoặc bản sửa trong database. CLI dùng Windows Integrated Security
và quyền SELECT của người vận hành; không phải đầu mối cấp quyền mới trên UI.

Kết quả gồm số dòng từng bảng và quy tắc có mã, mức độ, tổng số phát hiện, chi
tiết PK/mã liên quan, nguyên nhân. Không xuất tên/ngày sinh/nội dung bệnh án.
Giới hạn mặc định200 chi tiết/quy tắc, cho phép1–500; tổng số vẫn tính toàn bộ,
cờ `DaGioiHan` báo rõ nếu chưa lấy hết. Một bản ghi có thể xuất hiện ở nhiều quy tắc.

| Nhóm | Kiểm tra |
|---|---|
| Cấu trúc | Phần schema P1/P4 còn thiếu; FK/CHECK chưa trusted/bị tắt, trigger bị tắt; FK mồ côi lấy từ metadata, gồm FK ghép và NULL |
| Bệnh nhân/hồ sơ | Ngày sinh/ngày lập bất hợp lý; nghi trùng theo tên/ngày sinh/điện thoại không rỗng; không gộp hồ sơ |
| Khám/nhân sự | Thứ tự tiếp nhận/khám/xác nhận, bác sĩ phiếu/lượt, chức vụ/chuyên khoa; tài khoản/nhân viên hiện tại và ca/phòng của phiếu dịch vụ |
| Tình trạng | Mã thiếu, lượt xác nhận không có mã, cùng mã lặp trên lượt; không đoán từ chữ chẩn đoán |
| Hóa đơn/thu | Tổng chi tiết, thu ròng cuối và từng thời điểm, nguồn/thời gian thu, dịch vụ/thuốc/phụ thu đúng lượt, lượng nguồn × giá chốt, chống tính đồng thời nguồn chờ cấp và đã xuất |
| Kho | Tổng nhập, tồn cuối và lịch sử theo hàng/kho/lô, nguồn thuốc/đơn/lượt, ngày xuất/ngày nhập lô/hạn dùng |

`SaiXacDinh` dành cho quan hệ/số tiền/tồn/ràng buộc không khớp. `CanDoiChieu`
dành cho dữ liệu thiếu nguồn hoặc thông tin hiện tại chưa chứng minh được lịch
sử: tài khoản hôm nay bị khóa, ca đã thay đổi, ngày áp dụng danh mục, ngày sinh
nghi vấn, mã tình trạng thiếu. Không tự sửa các dòng này.

Đối soát lịch sử gộp các giao dịch cùng timestamp trước tính số dư, tránh suy
thứ tự không được lưu. Thuốc `MaCTDT` chờ cấp trên hóa đơn Nháp của luồng chốt
khám mới là hợp lệ; hóa đơn đã xác nhận còn nguồn chờ cấp là bất thường. So tiền
thuốc bằng **giá chốt trên hóa đơn**, không dùng giá thuốc danh mục hiện tại.

```powershell
# Chỉ đọc database đang dùng; output mới trong obj/P5Evidence nếu không chỉ định:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/DoiSoatDuLieu.ps1 -MayChu . -Database QLBenhVienRangHamMat -GioiHanMoiQuyTac 200
```

`AUDIT_COMPLETE` chỉ xác nhận chạy đủ các truy vấn, không đồng nghĩa dữ liệu sạch.
Xem `audit.json`, `anomalies.csv`, `tables.csv`, `summary.md`. Thư mục đã tồn tại
bị từ chối; CSV UTF-8 BOM xử lý dấu nháy, xuống dòng và chuỗi có thể thành công
thức. Công cụ giữ giao dịch Serializable trong lần chạy độc lập; nên chọn lúc ít
ghi nghiệp vụ. Caller truyền giao dịch chịu trách nhiệm mức cô lập tương ứng.

## Kết quả trên database đang dùng

Đã đọc `QLBenhVienRangHamMat`: **62 bảng,11 bệnh nhân,11 lượt khám,11 hóa đơn**;
chưa có schema P1/P4. Không chạy file SQL nâng cấp lên database này.

| Quy tắc | Số phát hiện | Kết luận |
|---|---:|---|
| `SCHEMA_PHIENBAN` |3 | Thiếu cột loại khám/phí và hai bảng ảnh/phân công; các kiểm mới tương ứng chưa chạy |
| `BENH_THIEU_MA` |11 |11 dòng tình trạng thiếu `MaDMTT`, cần nguồn bệnh án để bổ sung |
| `BENH_KHONG_GHI_NHAN` |11 | Chính11 lượt trên không có tình trạng đã gắn mã |
| Các quy tắc sai xác định đã chạy |0 | Chưa phát hiện sai lệch quan hệ/tổng tiền/tồn trong phạm vi kiểm hiện có |

Tổng25 phát hiện là3 mục schema và hai cách nhìn của **cùng11 lượt**, không phải25
bản ghi sai độc lập. Không hồi tố mã bệnh hoặc hoàn tất lượt để làm đẹp thống kê.
Không có bản UPDATE sửa dữ liệu: hiện không có lỗi xác định cần sửa, và không có
nguồn bệnh án được cung cấp để điền mã. Bảng kê đầy đủ PK/nguyên nhân giữ cục bộ:
`obj/P5Evidence/audit-QLBenhVienRangHamMat-cd5c9a5a85504f019b8a8e832121bfca/`.
Bằng chứng đưa vào docs chỉ chứa số tổng hợp, không chứa bảng kê hồ sơ đang dùng.

## Bộ trình diễn phi định danh

Database chính để nghiệm thu P5:
**`RHM_Hospital_Test_P5Demo_20261108_125355`**. Bản sao SQL kiểm thử bỏ snapshot
phục hồi; toàn bộ người bệnh/chứng từ được dựng mới, tên/mã có nhãn DEMO và marker
database `RHM.Demo.P5=1`. Báo cáo WPF và CSV xuất từ ứng dụng hiện nhãn **DEMO —
dữ liệu giả lập, không phải hồ sơ thật**, lấy từ SQL có kiểm quyền/phiên.

Chuỗi mỗi lượt: người giả → hồ sơ → ca bác sĩ/phòng/hiện diện → phiếu → lần khám
Nháp → mã tình trạng → dịch vụ xác nhận → đơn thuốc/xuất đúng lô khi có → xác
nhận lượt → `DAL_HOADON.CreateInvoice` lấy các nguồn thật → xác nhận hóa đơn → thu.
Chuyển trạng thái qua trigger/ràng buộc thật, không tắt trigger hay chỉnh tổng tiền.

Lịch trình08–10/2026 là dữ liệu mô phỏng xác định từ khi tạo mới, **không phải
nhập bổ sung bệnh án thật trong quá khứ**. Ngày hóa đơn chỉ đặt khi còn Nháp;
không đổi ngày chứng từ đã xác nhận. Bộ mẫu dùng nhánh hóa đơn legacy được hỗ trợ
để kiểm nguồn dịch vụ/xuất thuốc/phụ thu theo thời gian. Có ca kiểm riêng chạy
`BUS_KHAMDIEUTRI`/`BUS_DONTHUOC` của luồng chốt khám mới để tránh đánh nhầm nguồn
thuốc chờ cấp. Không thêm API ghi lùi ngày cho ứng dụng.

Danh mục gốc `DV001` mới hiệu lực tháng11. Vì vậy tạo **mã DEMO riêng**:
`P5_DV_KHAM`100.000đ từ01/08/2026, `P5_TH` giá bán10.000đ, nhập5.000đ. Đây là
giá mô phỏng được ghi trong manifest, không phải giá lịch sử của dịch vụ/thuốc
thật. Không thay mã/giá/ngày áp dụng đã có. Thuốc ghi **KHÔNG DÙNG**, không có ý
nghĩa điều trị. Không tạo dữ liệu năm2025 để giả so sánh YoY.

| Kỳ DEMO | Lượt xác nhận | Người duy nhất trong tháng | Phải thu theo hóa đơn | Thu ròng theo thanh toán |
|---|---:|---:|---:|---:|
|08/2026 |8 |8 |1.000.000 |900.000 |
|09/2026 |12 |12 |1.500.000 |1.500.000 |
|10/2026 |6 |6 |750.000 |750.000 |
| Toàn kỳ |26 |**18 người DISTINCT** |**3.250.000** |**3.150.000** |

Khoản100.000đ cuối mỗi tháng được thu ngày02 tháng sau. Tháng11 có100.000đ thu
cho hóa đơn tháng10; đến lúc dựng mẫu mọi hóa đơn đã thu đủ. Thu tháng09/10 có
khoản của tháng trước nên không dùng tổng hóa đơn tháng để thay tiền thực thu.
MoM lượt tháng09 là+50%, tháng10 là−50%; không cộng số người của ba tháng thành26.

- 26 lượt khám ×100.000 =2.600.000;13 phụ thu ×30.000 =390.000;
  13 đơn ×2 đơn vị ×10.000 =260.000; tổng phải thu3.250.000.
- Ba tình trạng `REF28_CARIES`, `REF28_GINGIVITIS`, `REF28_TARTAR`:9/9/8 lượt,
  mỗi nhóm6 người duy nhất; đủ26 lượt gắn mã, không trùng mã trên lượt. Đây là
  tình trạng trong mẫu, không phải tỷ lệ mắc bệnh trong cộng đồng.
- Ba phiếu nhập, mỗi tháng100 đơn vị;13 phiếu xuất theo13 đơn, mỗi đơn2 đơn vị:
  nhập300, xuất26, tồn274 đơn vị DEMO.

Các số trên đã đối chiếu bằng SQL độc lập, BUS báo cáo và render WPF. CSV bệnh lý
và tài chính được lấy từ **BUS báo cáo thật** trong bộ kiểm; `lineage.csv` truy
ngược từng MaBN/MaHS/ca/phiếu/MaLS/mã tình trạng/dịch vụ/đơn/xuất/MaHD/MaTT.

## Tạo mới, mở và khôi phục minh chứng

```powershell
# Tạo DB mới tự đặt tên RHM_Hospital_Test_P5Demo_<mốc> và output mới:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/TaoDuLieuTrinhDien.ps1

# Kiểm đầu mối audit + số liệu DEMO + render báo cáo trên DB đã tạo:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraDoiSoatP5.ps1 -Database RHM_Hospital_Test_P5Demo_20261108_125355 -KiemTraTrinhDien

# Chạy ứng dụng trong phiên PowerShell với DB DEMO, không đổi appsettings:
$env:RHM_CONNECTION_STRING='Data Source=.;Initial Catalog=RHM_Hospital_Test_P5Demo_20261108_125355;Integrated Security=True;TrustServerCertificate=True;'
dotnet run --project RangHamMat.Wpf
# Đăng nhập, mở Báo cáo; chọn01/08–31/10/2026, nhóm Tháng, Không so sánh.
# Đóng ứng dụng rồi bỏ biến để trở lại cấu hình thông thường:
Remove-Item Env:RHM_CONNECTION_STRING

# Nếu thất lạc/hỏng output, xuất lại từ DB đã đánh dấu, chỉ SELECT:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/TaoDuLieuTrinhDien.ps1 -Database RHM_Hospital_Test_P5Demo_20261108_125355 -ChiXuatMinhChung

# Kiểm tên DB, output, junction, không ghi đè và phục hồi output:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraCongCuP5.ps1 -Database RHM_Hospital_Test_P5Demo_20261108_125355
```

Tạo mới chỉ nhận tiền tố `RHM_Hospital_Test_P5Demo_...`, từ chối DB hiện hữu, không
ghi đè hoặc tự xóa DB. Mỗi lần chạy có scratch GUID riêng và khóa build; khóa SQL
kiểm lại quyền sở hữu DB mới trước cài schema. Kiểm chạy đồng thời hai tên DB
khác nhau đã thành công. Cờ `-ChiXuatMinhChung` không cài SQL/seed, chỉ nhận DB
DEMO đã đánh dấu và xuất vào thư mục mới.

Chuẩn bị thư mục và marker `INCOMPLETE_DEMO.txt` trước tạo database/dữ liệu;
chặn output tồn tại hoặc đi qua junction. Chỉ `DEMO_COMPLETE`/`DEMO_EXPORT_COMPLETE`
và marker đã bỏ mới xác nhận minh chứng đầy đủ. Nếu hỏng đĩa sau commit, giữ DB
đã đánh dấu và dùng xuất lại, không cố seed lần hai. Không tự dọn DB/output thử.

Manifest tách `ExpectedSeed` khỏi số thực tế `Actual...`, `Monthly`, `Cash`.
`Visits` đếm DISTINCT, `LineageRows` có thể nhiều hơn khi có thu/hoàn. Đã kiểm
biến thể ở DB DEMO phụ: thêm hoàn5.000đ hợp lệ →26 lượt/27 dòng lineage,
thu quý3.145.000đ, không gây mâu thuẫn với kỳ vọng ban đầu. DB DEMO chính giữ nguyên.

## Kiểm chứng và giới hạn

| Nhóm | Kết quả |
|---|---|
| P5 audit/quyền reader/FK ghép/giới hạn/CSV/nhân sự/tiền/kho/DEMO/BUS/WPF |**21/21 PASS** |
| Cơ chế bảo vệ công cụ và khôi phục output |**7/7 PASS** |
| Xuất minh chứng chỉ đọc |**64/64** fingerprint bảng giữ nguyên |
| Tạo hai DEMO đồng thời | Cả hai thành công, đúng DB/manifest riêng |
| Biến thể phục hồi sau hoàn |26 lượt,27 dòng nguồn,3.145.000đ thu quý; PASS |
| Nền tảng P0/P1 |**35/35 PASS** |
| Đồng bộ ET/DAL/công/lương |**32/32 PASS** |
| Tiếp nhận/phụ thu P2/P3 |**28/28 PASS** |
| Luồng khám/thuốc/hóa đơn/kho/WPF |**47/47 PASS** |
| X-quang P4 |**32/32 PASS** |
| Báo cáo SQL/render |**41/41 PASS** |
| Build Debug/Release và cấu trúc/SQL64 | Thành công,0 lỗi; Release122 cảnh báo ở mã hiện có, không có cảnh báo từ file P5/báo cáo đã sửa |
| Review độc lập | Đã xử lý toàn bộ finding đã báo, không còn Critical/Important/Minor tồn đọng trong phạm vi P5 |

Review đã dẫn tới kiểm RED/GREEN cho sai nguồn thuốc, hoàn trước thu và tồn lịch
sử âm; kiểm thêm trạng thái thuốc chờ cấp, giá đã chốt và timestamp trùng.
Cơ chế output/race/recovery có kiểm thực thi. Bằng chứng:
[evidence/20261108-p5](evidence/20261108-p5/). Chỉ đưa dữ liệu DEMO và số tổng hợp
vào docs; log đầy đủ/database/output nằm trong `obj/P5Evidence`.

Các tệp chính: `ET/ET_DOISOATDULIEU.cs`, `BUS/BUS_DOISOATDULIEU.cs`,
`DAL/DAL_DOISOATDULIEU.cs` và hai phần lâm sàng/tài chính-kho; cờ DEMO ở
`BUS_BAOCAO`, `DAL_BAOCAO.TongHop`, `Views/BaoCao.xaml(.cs)`. Công cụ và bộ
kiểm nằm trong `tools/DoiSoatDuLieu.ps1`, `TaoDuLieuTrinhDien.ps1`,
`KiemTraDoiSoatP5.ps1`, `KiemTraCongCuP5.ps1` và các nguồn `.cs.txt` tương ứng.

Chưa đo hiệu năng/plan trên hàng triệu dòng; các quy tắc tập trung các nhóm yêu
cầu P5, không suy được bệnh sử/giá/lịch nhân sự thiếu nguồn. Không sửa bất thường
chỉ để có số báo cáo. Chưa triển khai lên SQL đang dùng, chưa publish/commit/push.
**P6** còn nghiệm thu tổng thể, cấu hình/vận hành thật và kiểm kho UNC giữa hai máy.
