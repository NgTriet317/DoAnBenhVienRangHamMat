# Quản lý Bệnh viện Răng Hàm Mặt

WPF .NET8 + SQL Server2016SP1 trở lên. Solution có đúng bốn project: RangHamMat.Wpf (giao diện), ET, DAL và BUS. Views chỉ có các cặp XAML/code-behind; C# phụ trợ ở Presentation/Controls.

## Database: một file SQL duy nhất

Dùng SSMS hoặc sqlcmd chạy **toàn bộ** [Database/QLBenhVienRangHamMat.sql](Database/QLBenhVienRangHamMat.sql). Không còn thư mục hay bước chạy Migrations.

Bản SQL đồng bộ ngày10/11/2026 có checker tổng cho đủ schema64/khám dịch vụ/phụ thu/ảnh và nhật ký, kiểm liên kết ET/DAL và chặn báo sẵn sàng khi pha trước lỗi. Xem [thay đổi, bằng chứng và cách áp dụng](docs/sprint3/DONG_BO_DATABASE_20261110.md). File SQL không tự nâng cấp database khi mở ứng dụng.

```powershell
sqlcmd -S . -E -C -b -f65001 -i Database/QLBenhVienRangHamMat.sql
```

File chỉ tạo một database **QLBenhVienRangHamMat**, schema hiện tại **64 bảng**. Khi chưa có bảng, script nạp đầy đủ schema và bộ dữ liệu đồ án đã chuẩn hóa ngày12/11/2026 (gồm hồ sơ phục hồi và dữ liệu giả lập liên thông), sau đó kiểm tra đồng bộ. Khi chạy lại trên database đã có bảng, script giữ dữ liệu hiện tại, không nạp đè snapshot và không tự dọn dữ liệu. Sao lưu trước khi đồng bộ database ClinicalWorkflow v2/v3 được hỗ trợ.

Đã hợp nhất các phần còn thiếu từ SQL_V9_CAI_MOI_TEST_F5.sql: lịch khám có mức nghiêm trọng/bác sĩ dự bị; chấm công, nhật ký duyệt; lương, phụ cấp, khấu trừ, bảo hiểm, hoa hồng và khóa kỳ đã chốt. Chỉ chạy file SQL chính; không cần chạy thêm các file nâng cấp trong SQL/. File V9 gốc chỉ dùng đối chiếu, có dữ liệu mẫu và không dùng để chạy tiếp trên database đang sử dụng. Xem [đối chiếu và cách bảo toàn lương cũ](docs/database/HOP_NHAT_V9.md).

Phiếu lương từ công thức cũ giữ nguyên số tiền, ghi chú và trạng thái bằng bản tiền trước đồng bộ. Phiếu đã chốt không được sửa; không tự điền người chốt khi dữ liệu cũ chưa có. Bấm Tính lương mới chuyển các phiếu Nháp sang công thức V9 và vẫn giữ bản tiền gốc để đối chiếu. Nhập lịch nghỉ đã được bệnh viện duyệt vào NGAY_NGHI_LE trước khi tính kỳ liên quan; bản cài không tự áp lịch nghỉ hoặc tạo chứng từ lương mẫu.

Cài mới có **47 bệnh nhân,58 dịch vụ,68 thuốc,24 vật tư,18 thiết bị,14 nhân viên/tài khoản,36 hóa đơn** cùng nguồn kho, lịch làm việc, hồ sơ và bàn giao liên thông. Snapshot ngày **12/11/2026** có **64 bảng,3.285 dòng**, giữ **36 lượt khám lịch sử**, không nạp sẵn hàng chờ khám. Toàn bộ **1.102 lịch làm việc** đã có phòng phù hợp. Có 13 hóa đơn chờ thu và sáu đơn chờ giao để thao tác tiếp. Dữ liệu hiển thị đã bỏ nhãn đồ án, dùng mã cùng tiền tố dữ liệu cũ; nguồn gốc dữ liệu giả lập của đồ án được ghi trong tài liệu. Mật khẩu cũ và tiền lương lịch sử được giữ; Admin **admin001 / 123456**. Xem [tài khoản, mã và kiểm chứng hiện tại](docs/database/DU_LIEU_THONG_NHAT_20261112.md).

Bộ dữ liệu hiện có53 tình trạng,58 dịch vụ,121 liên kết; gồm chấn thương, khớp thái dương hàm, niêm mạc, tuyến nước bọt, nhiễm trùng, nang/u, khe hở, thần kinh, hậu phẫu và bệnh răng. Danh mục nguồn và giá tham khảo ban đầu tại [DANH_MUC_RHM.md](docs/clinical-workflow/DANH_MUC_RHM.md); nguồn gốc giá cấu hình bổ sung được ghi trong tài liệu dữ liệu hiện tại.

Danh mục gốc có52 sản phẩm có nguồn và20 nhóm độc lập mẫu đơn:40 giá tham khảo công khai,12 giá chưa thiết lập. Bộ dữ liệu hiện có68 thuốc sau thêm sáu sản phẩm; 12 giá thiếu đã được đặt giá cấu hình cho đồ án. Đã nhập lô và tồn bằng chứng từ để dùng thử cấp thuốc. Không tự tạo mẫu liều hay người phê duyệt hướng dẫn. Cơ chế ứng dụng vẫn coi thuốc giá0 là **Chưa thiết lập giá** và chặn thanh toán. Xem [nguồn danh mục gốc](docs/clinical-workflow/DANH_MUC_THUOC_RHM.md) và [bộ dữ liệu hiện tại](docs/database/DU_LIEU_THONG_NHAT_20261112.md).

## Cấu hình và chạy

Mở BenhVienRangHamMat.sln bằng Visual Studio2022 có .NET desktop development/.NET8 SDK, chọn RangHamMat.Wpf làm startup rồi F5. Hoặc mở PhatHanh/RangHamMat.Wpf.exe với .NET8 Desktop Runtime trên Windows.

Máy mới cần cài SQL Server2016SP1 trở lên, mở toàn bộ file SQL chính trong SSMS rồi F5 và cấu hình đúng instance/quyền truy cập trước khi F5 ứng dụng. Không cần SQLCMD mode hoặc đường dẫn backup của máy cũ. File hiện tại chứa dữ liệu phục hồi; dữ liệu nhập sau lần đóng gói cần sao lưu/khôi phục hoặc cập nhật snapshot bằng tools/CapNhatDuLieuSql.ps1 trước khi chuyển máy.

Ứng dụng kết nối **SQL Server**, không kết nối trực tiếp vào file .sql. File SQL là nguồn khởi tạo/đồng bộ duy nhất. Sửa RangHamMat.Wpf/appsettings.json (hoặc bản cạnh exe):

```json
{"ConnectionStrings":{"RangHamMat":"Data Source=.;Initial Catalog=QLBenhVienRangHamMat;Integrated Security=True;TrustServerCertificate=True;"}}
```

Biến môi trường RHM_CONNECTION_STRING được ưu tiên hơn file. Instance có tên cần viết hai dấu gạch chéo trong JSON, ví dụ TEN_MAY\\SQLEXPRESS. Giữ toàn bộ thư mục PhatHanh khi chuyển máy, không chỉ chép exe.

## Báo cáo và thống kê Sprint3

Kế hoạch `KeHoachTrienKhai.docx` ngày07/11/2026 đã triển khai **P0–P5**: nền tảng SQL/ET, tiếp nhận khám dịch vụ, phụ thu xuyên suốt hóa đơn/thu/in, ảnh X-quang và đối soát/dữ liệu trình diễn báo cáo. Tại **Đến trực tiếp**, khám thông thường giữ dịch vụ → bác sĩ; khám dịch vụ chọn bác sĩ có ca/hiện diện/phòng hợp lệ trước rồi lọc dịch vụ đúng chuyên khoa. Phụ thu **30.000đ/lượt** có dòng hóa đơn riêng, không được giảm giá vào khoản bắt buộc này; phiếu cũ và lịch hẹn giữ Thông thường/0đ. **P6 nội bộ ngày10/11 đạt32/32 bước,20/21 tiêu chí**; XR04 còn chờ kiểm kho UNC và phục hồi qua máy khác, chưa phát hành. Xem [nghiệm thu P6](docs/sprint3/NGHIEM_THU_P6_20261110.md), [triển khai/rollback](docs/sprint3/HUONG_DAN_TRIEN_KHAI_VA_ROLLBACK.md), [tiến độ](docs/sprint3/KE_HOACH_TRIEN_KHAI_20261107.md), [kiểm nền tảng](docs/sprint3/KET_QUA_NEN_TANG_20261107.md), [kết quả khám dịch vụ](docs/sprint3/KET_QUA_KHAM_DICH_VU_20261107.md), [ảnh X-quang](docs/sprint3/KET_QUA_ANH_XQUANG_20261107.md), [đối soát và DEMO ba kỳ](docs/sprint3/KET_QUA_DOI_SOAT_P5_20261108.md).

## Ảnh X-quang theo lần khám

Bác sĩ mở tab **Ảnh X-quang** trong lần khám để tải JPG/JPEG/PNG hoặc **DICOM (.dcm, .dicom)**, xem trước/phóng thu hoặc ẩn ảnh. JPG/PNG giữ nguyên byte gốc; DICOM tự chuyển khung đầu tiên sang PNG, áp mức xám/window level của ảnh nguồn và giữ tên DICOM gốc trong metadata. Kho lưu PNG để xem, không lưu bản DICOM nguồn. Cả tệp tải và ảnh PNG sau chuyển đổi đều tối đa20 MiB/40 triệu điểm ảnh; ẩn giữ lại tệp trong kho và metadata. Lượt đã chốt chỉ xem. Bác sĩ có thể giao dịch vụ chụp đã ghi nhận ở bước **4. Thực hiện** cho kỹ thuật viên; kỹ thuật viên mở **Dịch vụ thực hiện → Ảnh X-quang được giao** và chỉ thấy phần được giao. Phân công được lưu riêng, giữ nguyên người thực hiện và giá của chứng từ dịch vụ.

Chuyển đổi dùng fo-dicom/ImageSharp và fo-dicom.Codecs (JPEG, JPEG-LS, JPEG 2000, RLE). Máy Windows đọc DICOM nén cần Visual C++ Redistributable v14 và các thư viện native đi kèm bản build/publish theo [hướng dẫn của bộ codec](https://github.com/Efferent-Health/fo-dicom.Codecs#dependencies). Không cần cập nhật schema SQL. Chạy `powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraDicomXquang.ps1` để kiểm chuyển đổi, lưu/mở lại và bộ xem WPF bằng ảnh tổng hợp, không kết nối SQL.

Cấu hình kho chung trong `XQuang` của `appsettings.json` cạnh ứng dụng:

```json
"XQuang": {
  "ThuMucDungChung": "\\\\MAYCHU\\RHM_XQuang",
  "DungLuongToiDaMB": 20,
  "DiemAnhToiDa": 40000000,
  "ChoPhepDuongDanCucBo": false
}
```

Biến môi trường `RHM_XRAY_ROOT` ưu tiên hơn đường dẫn trong file. Quản trị viên cấp quyền share/NTFS cho tài khoản Windows chạy ứng dụng và cấu hình cùng một kho cho các máy. Kho hiện để trống chờ cấu hình triển khai; UI hiển thị thông báo khi chưa sẵn sàng. Xem [kiểm kho, sao lưu SQL kèm ảnh và phục hồi thử](docs/sprint3/KET_QUA_ANH_XQUANG_20261107.md#vận-hành-kho-và-phục-hồi).

Đã chuẩn bị [bộ chạy XR04 cho máy nghiệm thu](docs/sprint3/BO_NGHIEM_THU_XR04.md), gồm binary/manifest/hash và wrapper dùng runtime. [Kết quả gói ngày10/11](docs/sprint3/KET_QUA_BO_NGHIEM_THU_XR04_20261110.md):17/17 guard,12/12 bước từ ZIP giải nén, phục hồi đủ15 ảnh và viewer đạt. Các ca vẫn trên một máy qua UNC loopback; XR04 còn chờ hai máy/SQL từ xa, chưa chuyển môi trường nghiệp vụ.

Tổng quan ưu tiên lượt khám, bệnh nhân duy nhất, thu ròng và tỷ lệ lượt đã phân loại; bảng **Bệnh/tình trạng thường gặp** đặt ngay sau KPI, xếp theo số người hoặc số lượt. Xem [thống kê trọng tâm và nguyên nhân dữ liệu bệnh lý hiện chưa đủ](docs/sprint3/BAO_CAO_TRONG_TAM.md). Luồng Chọn nhanh của bác sĩ giữ mã tình trạng khi lưu lượt khám mới; không tự gán mã cho hồ sơ cũ.

Phân hệ Báo cáo mở cho nhân viên có phiên hợp lệ, có dashboard và thống kê tình trạng theo mã, giữ đủ 18 chuyên đề cũ. Hỗ trợ MoM/YoY, so sánh hai năm, chuỗi tối đa 24 tháng, MTD, CSV và phân quyền riêng cho chi tiết tài chính/lương/kho. Xem [hướng dẫn, định nghĩa KPI và bằng chứng kiểm thử](docs/sprint3/BAO_CAO_TRIEN_KHAI.md). Chạy `powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBaoCao.ps1 -Sql -Render` để kiểm báo cáo bằng SQL và render WPF; không cần chạy thêm SQL nâng cấp cho thay đổi báo cáo này.

## Đối soát dữ liệu và bộ trình diễn

`tools/DoiSoatDuLieu.ps1` chỉ SELECT, xuất JSON/CSV/Markdown gồm PK/nguyên nhân, tách sai xác định và cần đối chiếu; không tự sửa mã bệnh hoặc chứng từ. Công cụ nhận quyền đọc SQL của người vận hành. Kiểm trên database đang dùng62 bảng ghi nhận11 lượt thiếu mã tình trạng và3 mục schema chưa nâng cấp; không có phép ghi vào database này.

`tools/TaoDuLieuTrinhDien.ps1` tạo **DB mới** `RHM_Hospital_Test_P5Demo_...`, không chép snapshot hay ghi đè DB hiện hữu. Bộ giả lập có18 người/26 lượt qua08–10/2026 (8→12→6), đủ nguồn dịch vụ/thuốc/kho/phụ thu/hóa đơn/thanh toán. Hóa đơn3.250.000đ khác thu trong quý3.150.000đ vì100.000đ thu tháng11. Báo cáo/CSV có nhãn DEMO. Dùng `-ChiXuatMinhChung` để xuất lại chỉ đọc nếu mất output; xem [lệnh tạo/mở DEMO, bảng số liệu và kết quả kiểm](docs/sprint3/KET_QUA_DOI_SOAT_P5_20261108.md).

## Bảo trì thiết bị theo serial

Lịch sử bảo trì quản lý theo serial: chọn thiết bị hư hại để **lập phiếu bảo trì hoặc thanh lý**. Xác nhận phiếu chuyển thẳng **Đang thực hiện**, thiết bị chuyển **Đang bảo trì**; hoàn thành đưa thiết bị về Đang hoạt động. Tab **Thanh lý** lưu ngày, nhân viên, lý do, giá trị thu hồi và thông tin thiết bị tại lúc thanh lý; thiết bị được ngưng sử dụng, thu hồi khỏi phòng và không được sửa/xóa lịch sử. Các thay đổi SQL và dữ liệu giao diện cùng giao dịch. Chạy lại toàn bộ file SQL chính trước khi dùng bản mới. Xem [hướng dẫn bảo trì và thanh lý theo serial](docs/database/BAO_TRI_SERIAL.md).

## Luồng khám và thu tiền

1. Tiếp nhận chọn bác sĩ/phòng/dịch vụ; bác sĩ mở Hàng chờ khám theo giờ tiếp nhận, ai đến trước hiện trước. Admin xem được tất cả, User theo chức vụ và phạm vi SQL.
2. Bác sĩ ghi chẩn đoán/tình trạng, chọn nhanh theo nhóm bệnh và chọn dịch vụ liên quan. Kế hoạch có sẵn mã/giá từ SQL; chỉnh lượng/buổi rồi lưu và duyệt. Không có Mức độ hoặc Vị trí răng trong tình trạng bệnh.
3. Sau khi đã làm xong toàn bộ kế hoạch, mở **4. Thực hiện**, đối chiếu dịch vụ và xác nhận. Hệ thống ghi dịch vụ thực tế theo giá đã duyệt, gồm phí khám một lần; chưa tạo hóa đơn. Kế hoạch nháp/chưa xong chặn bước kê thuốc và hoàn tất. Lượt chỉ khám, không có kế hoạch thủ thuật, vẫn xác nhận dịch vụ khám tại bước này.
4. Mở **5. Kê thuốc**, bấm **Kê thuốc** để mở cửa sổ. Lọc nhóm/tên/hàm lượng ở bên trái, thêm thuốc rồi chọn từng dòng bên phải để nhập số lượng, số ngày, liều, đường dùng và hướng dẫn. Bấm **Xác nhận đơn thuốc** để lưu nháp và đưa danh sách về lần khám; còn sửa được bằng **Sửa danh sách thuốc**. Không dùng mẫu/phiên bản hay tự áp liều. Nếu không kê, chọn Không kê thuốc, nhập lý do và Lưu khám. Đóng cửa sổ khi chưa lưu có lựa chọn lưu/bỏ/ở lại; giữ một nút Hủy nháp.
5. **6. Tổng kết** đọc dịch vụ thực tế và thuốc chờ chốt/đang hiệu lực từ SQL, không cộng lại đơn nguồn đang được thay. Kiểm tra xong chọn **Chốt và lập hóa đơn**. Hệ thống kiểm bản đã xem, ký đơn nháp, khóa lượt và tạo một hóa đơn chờ thu trong cùng giao dịch; lỗi giữ lại đơn nháp để sửa. Không tự ghi thêm phần điều trị chưa xác nhận.
6. Quầy thu dùng lại chính hóa đơn đó; giữ giá đã chốt, thu tiền/xuất thuốc phối hợp theo giao dịch. Không thêm dịch vụ sau hoàn tất; retry không sinh hóa đơn hoặc khoản thu thứ hai. Mục **Đơn thuốc** chỉ tra cứu/in đơn đã kê và bàn giao sau khi thu đủ, theo quyền.
7. Dịch vụ thực hiện chỉ là **Lịch sử dịch vụ đã thực hiện** của lượt hoàn tất. Không có thêm/sửa/xác nhận hoặc dịch vụ phát sinh. Điều dưỡng chỉ xem phần được giao; thu ngân không xem kết quả lâm sàng.

Admin toàn quyền chức năng nhưng vẫn tuân thủ trạng thái chứng từ, kiểm tra dữ liệu, tiền và tồn kho. Xem [cửa sổ kê thuốc và chốt hóa đơn](docs/clinical-workflow/CUA_SO_KE_THUOC.md). Các mốc HospitalWorkflow3/PrescriptionFlow1 mô tả nền tảng trước; hướng dẫn hiện tại áp dụng cửa sổ xác nhận nháp rồi chốt ký/hóa đơn.

## Đăng nhập và đăng xuất

Sau khi đăng nhập thành công, phần mềm ghi nhớ tài khoản trên người dùng Windows hiện tại. Bấm **Thoát ứng dụng** hoặc đóng cửa sổ thì lần mở tiếp theo vào thẳng màn hình chính bằng tài khoản đó. Bấm **Đăng xuất** ở cuối màn hình chính để xóa phiên ghi nhớ và quay lại đăng nhập; có thể đăng nhập bằng tài khoản khác ngay trong cùng lần chạy.

Phiên lưu tại `%LOCALAPPDATA%\RangHamMat\phien-dang-nhap.dat`, được Windows mã hóa, không chứa mật khẩu rõ. Mỗi lần mở lại đều kiểm tra tài khoản và quyền hiện hành từ SQL; tài khoản bị khóa/xóa, đổi mật khẩu hoặc đổi nhân viên liên kết phải đăng nhập lại. Các kiểm tra dữ liệu đang nhập vẫn áp dụng khi đăng xuất.

## Kiểm tra và đóng gói

Hướng dẫn đọc từng tầng, quy ước chú thích tiếng Việt và phạm vi bổ sung ngày30/10/2026 tại [HƯỚNG DẪN CHÚ THÍCH CODE](docs/HUONG_DAN_CHU_THICH_CODE.md). Chú thích bổ sung không thay đổi token thực thi C#, cấu trúc XAML hoặc nội dung file SQL chính.

[Rà soát ngày14/11/2026](docs/database/RA_SOAT_LIEN_KET_NGHIEP_VU_20261114.md) đối chiếu toàn bộ64 bảng/3.285 dòng với luồng nghiệp vụ, giữ riêng các bước chờ thu/giao/chấm công và lương lịch sử; đồng thời bổ sung chú thích cho120 tệp còn thiếu. Có manifest SHA256 và kết quả kiểm chứng kèm theo.

```powershell
.\KiemTra.bat -KiemTraSql -MayChu . -Database QLBenhVienRangHamMat
# Kiểm tra lưu phiên và luồng đăng nhập/đăng xuất qua nhiều tiến trình; tạo database thử mới:
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraPhienDangNhap.ps1 -KiemTraSql
# Dựng/chạy bộ kiểm trên database thử riêng; mọi dữ liệu nghiệp vụ fixture rollback:
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraQuyTrinhBenhVien.ps1 -TaoDatabaseKiemThu
# Chỉ kiểm đăng nhập Admin/mở27 phân hệ, gồm chấm công, không ghi hồ sơ khám:
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraQuyTrinhBenhVien.ps1 -ChiGiaoDien -Database QLBenhVienRangHamMat
# Kiểm đồng bộ ET/DAL và nghiệp vụ chấm công/lương trên database thử đã cài SQL chính:
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraDongBoSql.ps1 -Database RHM_Hospital_Test_20261029
# Nghiệm thu P6 nội bộ: tự tạo các DB thử mới, log/ma trận/hash trong obj/P6Evidence:
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraNghiemThu.ps1 -MayChu .

dotnet publish RangHamMat.Wpf -c Release -o PhatHanh
```

Kiểm SQL gọi thủ tục chỉ đọc được cài từ file SQL chính. Project kiểm tạm sinh trong obj, không thêm project sản phẩm. Bộ kiểm ghi chỉ nhận database tiền tố RHM_Hospital_Test_. Hướng dẫn và bằng chứng các mốc cũ có nhắc Migrations/dữ liệu demo là lịch sử đã ngừng sử dụng; dùng README và file SQL hiện tại để cài/chạy.
