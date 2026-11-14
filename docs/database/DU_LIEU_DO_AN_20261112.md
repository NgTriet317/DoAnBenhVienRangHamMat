# Dữ liệu đồ án đã chuẩn hóa — 12/11/2026

> Tài liệu lịch sử của lần bổ sung đầu. Các mã `DL26_`, tên đăng nhập `doan_*`, hàng chờ và nhãn hiển thị bên dưới đã được thay thế. Dùng [hướng dẫn dữ liệu hiện tại](DU_LIEU_THONG_NHAT_20261112.md) để thao tác; giữ tài liệu này để đối chiếu nguồn gốc.

Database tại mốc bổ sung đầu có **64 bảng,3.221 dòng**. Người dùng xác nhận đây hoàn toàn là đồ án và cho phép chuẩn hóa hồ sơ cũ, bổ sung dữ liệu giả lập liên thông. Tên sản phẩm/chủng loại có nguồn nhận diện; người bệnh, nhân sự mới, giá mô phỏng, lô, serial và chứng từ mới không phải hoạt động thực tế.

## Số lượng và liên kết

| Dữ liệu | Trước | Sau |
|---|---:|---:|
| Bệnh nhân / hồ sơ | 11 / 11 | 47 / 47 |
| Phiếu tiếp nhận / lần khám | 12 / 12 | 42 / 36 |
| Dịch vụ / tình trạng / liên kết dịch vụ–tình trạng | 50 / 47 / 112 | 58 / 52 / 121 |
| Thuốc / vật tư / thiết bị theo serial | 62 / 10 / 10 | 68 / 24 / 18 |
| Nhân viên / tài khoản / phòng | 10 / 10 / 10 | 14 / 14 / 15 |
| Kho / nhà cung cấp | 10 / 10 | 11 / 12 |
| Hóa đơn / thanh toán | 11 / 11 | 29 / 23 |
| Bàn giao thuốc | 1 | 18 (6 chờ giao,12 đã giao) |
| Lô / phiếu nhập / hóa đơn nhập | 10 / 10 / 10 | 120 / 120 / 120 |
| Phiếu xuất | 11 | 35 |

Thuốc có nguồn nhập ghi sổ tại KHO001. Vật tư cũ vào đúng kho bảo hộ/băng gạc/vô khuẩn/xét nghiệm theo loại hàng; 14 vật tư nha khoa mới vào DL26_KHO_NHAKHOA. Đơn vị cơ sở được dùng thống nhất giữa hàng hóa, thuốc, lô và chi tiết nhập/xuất. Thiết bị có nguồn mua cùng nhà cung cấp với hồ sơ tài sản và nguồn phân bổ cho phòng; tài sản chưa phân phòng vẫn nằm trong kho.

Đã bổ sung bác sĩ cho CK002/CK003/CK004/CK007, phòng khám tương ứng, hai ca 08:00–12:00 và 13:00–17:00, lịch từ 14 ngày trước đến 30 ngày sau ngày tạo (trừ Chủ nhật và nghỉ đã duyệt). Bộ sinh kiểm tra trùng giờ bác sĩ/phòng, kể cả ca qua đêm. Hồ sơ mô phỏng dùng buổi sáng làm việc đã diễn ra, không phụ thuộc việc chạy công cụ trước giờ mở cửa hay Chủ nhật.

## Chuẩn hóa hồ sơ cũ

- Giữ mã, tài khoản, mật khẩu cũ và 10 phiếu lương lịch sử được dự án hỗ trợ.
- Gắn mã tình trạng cho 11 lượt cũ; liên kết kế hoạch với dòng tình trạng nguồn. LSKB011 được thống nhất thành tình huống sẹo sau chấn thương/phục hồi hàm mặt, khớp tình trạng đã có.
- Bổ sung tên/hàm lượng/đơn vị chốt của 10 dòng thuốc cũ; tách đường dùng khỏi hàm lượng của DT011. Nội dung chỉ định ghi rõ mô phỏng.
- LS009/LS010 chỉ chăm sóc/theo dõi: đơn Hủy được giữ, cờ không kê thuốc và lý do được bổ sung.
- Nối đủ dịch vụ và thuốc đã xuất vào HD001–HD010; tính tổng từ nguồn, giữ giảm giá đã có. Thay nhiều khoản thu/hoàn **mẫu** bằng một khoản Thu đủ v2 đúng tổng mới; bản gốc lưu trong backup và bằng chứng trước/sau.
- Ghi bàn giao hoàn tất cho DT001–DT008 bằng chính phiếu xuất cũ. Không phát sinh xuất kho lần thứ hai cho những đơn này. DT011 giữ bản ghi đã giao.
- Sửa ngày đối soát/nhật ký chấm công và hai nhật ký đăng nhập cùng phiên đã đóng bị ghi nhầm sang tháng 12. Giữ đúng thứ tự ngày/UTC, không đổi phiên đang mở.

## Các ca dùng thử

| Mã bệnh nhân | Trạng thái / nơi dùng |
|---|---|
| DL26_BN_01–06 | Sáu lịch hẹn sắp tới, có bác sĩ/phòng/ca |
| DL26_BN_07–12 | Sáu phiếu Chờ khám, mở Hàng chờ khám để bắt đầu |
| DL26_BN_13–18 | Sáu lượt Đang khám; có kế hoạch để sửa/duyệt/thực hiện |
| DL26_BN_17 / LSKB017 | Đã thực hiện nguồn chụp toàn cảnh, đang chờ nhập ảnh; NV004 được giao hỗ trợ ảnh qua API thực. Chưa có tệp ảnh, không tạo metadata trỏ tệp không tồn tại |
| DL26_BN_19–24 | Sáu hóa đơn chờ thu, dùng tiếp được đầu mối thanh toán trọn gói |
| DL26_BN_25–30 | Sáu đơn đã thu đủ, Chờ nhận thuốc; vào Đơn thuốc để đối chiếu và bàn giao |
| DL26_BN_31–36 | Sáu hồ sơ Hoàn tất, có thể xem/in/đối chiếu lịch sử |

Ví dụ: DL26_BN_19 có LSKB019 / DT012 / HD012 chờ thanh toán; DL26_BN_25 có LSKB025 / DT015 / HD018 chờ giao. Có cả khám thông thường và khám dịch vụ với phụ thu 30.000đ; có lượt không kê thuốc, không tạo đơn rỗng hay giao dịch Thu 0đ.

Thiết bị: DL26_TB_01–04 đang dùng; DL26_TB_05 đã thanh lý qua DAL thật, có bản chốt thông tin thanh lý; DL26_TB_06 đang bảo trì; DL26_TB_07 hư hại để thực hành lập phiếu hoặc thanh lý; DL26_TB_08 hoạt động/chờ bàn giao cho phòng. Mười thiết bị và bảo trì cũ vẫn được giữ.

## Tài khoản mới

Mật khẩu khởi tạo cho bốn tài khoản **đồ án** mới: `DoAnRHM@2026`. Mỗi tài khoản có salt PBKDF2 riêng, cùng định dạng đăng nhập hiện tại. Các mật khẩu cũ không bị đặt lại.

| Tài khoản | Chuyên khoa |
|---|---|
| doan_baoton | CK002 – điều trị bảo tồn |
| doan_phauthuat | CK003 – phẫu thuật hàm mặt |
| doan_hinhanh | CK004 – chẩn đoán hình ảnh |
| doan_phuchoi | CK007 – phục hồi chức năng |

Admin xem toàn bộ; tài khoản bác sĩ chỉ có quyền và phạm vi tương ứng theo chính sách sẵn có của dự án. NV005 tiếp nhận, NV008 thu ngân, NV006 giao thuốc, NV007 phụ trách kho/thiết bị; NV004 được giao ca hỗ trợ ảnh theo quyền CV003 hiện có.

## Nguồn nhận diện và giá

Sáu thuốc mới đối chiếu tên/hàm lượng từ nhà sản xuất: [Uprofen 400](https://stellapharm.com/products/uprofen-400/), [Azicine 500](https://stellapharm.com/products/azicine-500/), [Clarithromycin STELLA 250 mg](https://stellapharm.com/products/clarithromycin-stella-250-mg/), [Cotrimoxazole 800/160](https://stellapharm.com/products/cotrimoxazole-800-160/) và [danh mục kháng khuẩn của STELLA](https://stellapharm.com/product-category/antibacterials-for-systemic-use/) có Clindamycin STELLA 150 mg / Spirastad 3 M.I.U. Nguồn vật liệu: [GC glass ionomer](https://www.gc.dental/america/products/operatory/glass-ionomer-restoratives); nguồn chủng loại thiết bị nội nha: [COLTENE](https://products.coltene.com/EN/US/products/endodontics/endo-motors/endo-motors). Không gán model/chứng nhận của hãng cho serial giả.

Giá của các mục bổ sung và 12 thuốc trước đây chưa thiết lập giá là **giá giả lập để thực hành**. Giá tham khảo đã có được giữ khi hợp lý; không diễn giải giá giả lập thành giá hiện hành được hãng/bệnh viện công bố. Các bảng nhóm/hướng dẫn thuốc không được điền mẫu liều hoặc người phê duyệt giả để vượt quy trình kê đơn.

## Chạy và kiểm tra

Database trên máy hiện tại đã được áp dụng. Cài máy mới bằng duy nhất file SQL chính; snapshot có đủ dữ liệu và marker phiên bản. Chạy lại file SQL trên database đã có dữ liệu giữ nguyên dữ liệu hiện tại.

```powershell
# Chỉ đối soát dữ liệu và thử API trong transaction rollback:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ChuanHoaDuLieuDoAn.ps1 -MayChu .
# Chủ động chuẩn hóa nguồn mẫu cũ tương thích; đã có marker thì không thêm hay hồi trạng thái:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ChuanHoaDuLieuDoAn.ps1 -MayChu . -ApDung
```

Công cụ chỉ nhận QLBenhVienRangHamMat hoặc fixture RHM_Hospital_Test_*. Lần áp dụng đầu vào database chính tự tạo backup COPY_ONLY/CHECKSUM và VERIFYONLY trước khi mở giao dịch ghi. Không tự chạy công cụ khi mở WPF.

Bản backup trước áp dụng: `QLBenhVienRangHamMat_before_academic_data_F7B208B0830F411F88A06436CFD90BB9.bak` trong thư mục Backup mặc định của SQL Server. Đường dẫn đầy đủ và kết quả kiểm nằm trong bằng chứng. Công cụ lưu dữ liệu trước/sau, số dòng, đối soát, kết quả API và marker `RHM.AcademicData.Version=1`.

Đã kiểm 21 đơn ký in được,18 bàn giao đúng nguồn,6 hóa đơn có thể thu tiếp; thử thu/giao và retry được rollback. Chạy lại giữ nguyên SHA256 cả 64 bảng. Thử thiếu neo giữa giao dịch hoàn nguyên đủ 64 bảng, không để lại trigger tắt/dữ liệu thêm dở/marker giả. Các kết quả cuối và bản kê snapshot tại `evidence/20261112-academic-data/`.
