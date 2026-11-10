# Gói nghiệm thu XR04 — 10/11/2026

Đã tạo gói công cụ cục bộ, đạt **17/17 ca guard/toàn vẹn** và **12/12 bước
kiểm từ ZIP giải nén**. Gói cho phép chạy Probe, kiểm kho, backup/restore,
viewer và so proof bằng runtime, không build project trên máy kiểm.
**XR04 vẫn chờ máy thứ hai; `ReadyForDeployment=false`.**

## Gói bàn giao

Gói cuối nằm trong thư mục:
`obj/XR04Handover/20261110_120954_a7405046/`.

- ZIP: `XR04-20261110_120954_a7405046.zip`,18.916.481 byte.
- Tệp đối chiếu: cùng tên ZIP với đuôi `.sha256`.
- SHA-256: `C070FE8F431F58066668A131FA2C3845B3C0DAA551C6A139A7A59CC0ED97D9E5`.
- `Package/`: bản chưa nén,213 tệp payload và `package.json`.
- Ba entry point: `Probe.dll`, `Storage.dll`, `Verify.dll`, kèm phụ thuộc,
  wrapper, helper, hướng dẫn và manifest nguồn.

Gói không chứa `.sql`, `.bak`, kho ảnh hoặc source C#/project. Cấu hình được
kéo theo bởi tham chiếu WPF đã thay bằng tên DB thử chưa cấu hình,
`RHM_Hospital_Test_ConfigureBeforeUse`; không mặc định trỏ DB nghiệp vụ.
Không thay thư mục `PhatHanh` cũ và không triển khai lên máy khác.

Máy chạy cần Windows x64/.NET8 runtime; viewer cần Windows Desktop Runtime8
và phiên desktop. Các lần kiểm thực tế ở đây vẫn trên máy có SDK: đã xác minh
lệnh gọi DLL từ bản giải nén không build/restore hay đọc source, chưa kiểm
một máy sạch chỉ cài runtime. Hướng dẫn độc lập có sẵn trong ZIP và tại
[bộ chạy XR04](BO_NGHIEM_THU_XR04.md).

## Truy nguồn bản đã kiểm

P6 gốc: `20261110_114040_eae3f295`,32 bước nội bộ đạt. Builder đối chiếu đủ
**481 mục nguồn cũ** trước/sau publish, không thay mã C#/XAML/SQL đã nghiệm thu.
Manifest hiện tại có485 mục do thêm đúng bốn script bàn giao. Kết quả P6
lịch sử không bị sửa hoặc gắn lại vào mã nghiệp vụ khác.

Các tệp thêm:

| Tệp | Chức năng |
|---|---|
| `tools/TaoBoNghiemThuXR04.ps1` | So nguồn P6, dựng ba tool, tạo ZIP/manifest/SHA, giữ marker nếu chưa hoàn tất |
| `tools/ChayNghiemThuXR04.ps1` | Chạy bảy chế độ từ binary, guard DB/UNC, kiểm runtime và ghi kết quả/log mới |
| `tools/XR04BanGiaoSupport.ps1` | So hash/danh mục gói, nguồn P6 và đầu vào; không kết nối SQL |
| `tools/KiemTraBoBanGiaoXR04.ps1` | Kiểm guard/toàn vẹn và process từ chối sớm |

Nhánh `Sprint3_tan`, HEAD vẫn `1cafa7d2096a3e954d381e473520cba8bab4b9dd`;
thay đổi còn trong working tree, chưa commit/push. ZIP là hiện vật thử nội
bộ trong `obj`, không phải xác nhận phát hành. Đóng gói không kết nối SQL.

## Kiểm từ gói giải nén

Lần kiểm `verify-3cfa54e282b349c4a78160823e49457b`, máy `DESKTOP-C9VCE8A`.
Đọc [kết quả JSON](evidence/20261110-xr04-kit/integration.json),
[12 bước](evidence/20261110-xr04-kit/steps.csv) và
[log](evidence/20261110-xr04-kit/xr04-integration.log).

| Bước | Kết quả thực tế |
|---|---|
| Kiểm SHA ZIP/danh mục sau giải nén | Đạt, không có project/source/snapshot/backup |
| Kiểm kho qua UNC | 15 metadata,0 lỗi kho |
| Probe A và B, hai lần chạy thật trên cùng máy | Mỗi lần15 tệp đúng hash,13 ảnh mở bằng BUS,2 ảnh ẩn giữ nguyên |
| Backup | SQL COPY_ONLY/CHECKSUM/VERIFYONLY và15 ảnh, manifest hoàn tất |
| Restore | DB thử mới/kho UNC mới;15 ảnh và SQL khớp, gồm ảnh ẩn |
| Mở lại sau restore | 2/2 ca BUS/viewer/phóng thu đạt |
| So hai proof cùng hostname | Từ chối đúng, kết quả Compare=FAIL có chủ đích |
| Restore vào DB đã tồn tại | Từ chối trước tạo kho đích mới |
| Sửa README trong payload có kiểm soát | Kiểm gói từ chối khác hash; sau đó phục hồi đúng byte |
| INCOMPLETE ở thư mục cha | Kiểm gói từ chối; marker kiểm giả lập được dỡ sau ca |
| Kiểm toàn vẹn cuối | Đạt sau các ca lỗi có kiểm soát |

DB đích thử là `RHM_Hospital_Test_XR04Kit_20261110_121045_88f639`;
kho đích nằm trong `obj/XR04Handover/verify-.../RestoredStore`, truy cập bằng
`\\localhost\D$`. SQL staging dùng đường dẫn cục bộ trên cùng máy, chưa xác
minh staging UNC cho dịch vụ SQL từ xa. Bản sao thật/ảnh fixture và log đầy
đủ được giữ trong `obj`; bằng chứng gọn chỉ có log/JSON và một ảnh viewer giả.

Hai proof A/B có hostname thực giống nhau; không dùng chúng để chứng minh
hai máy. [Kết quả từ chối](evidence/20261110-xr04-kit/same-host-comparison.json)
có `RemoteSqlRestoreAccepted=false`. Các ca từ chối có mã thoát khác0 được
ghi riêng `ExpectedRejection=true` trong bảng bước; đây là PASS của phép
kiểm guard, không phải PASS của thao tác bị từ chối.

## Rà soát và sửa lỗi

Rà soát độc lập phát hiện ba điểm, đã sửa cùng lượt và kiểm lại:

- `KiemTraKho` phải ánh xạ sang `--mode KiemTra` của DLL cũ. Gói đầu đã tái
  hiện `FAIL: Unknown operation`; gói cuối kiểm kho thật đạt15 metadata/0 lỗi.
- Marker `INCOMPLETE.txt` của builder nằm ở thư mục cha. Validator nay kiểm
  cả vị trí này khi chạy gói; builder chỉ bỏ qua marker cha trong bước tự
  kiểm payload khi đang dựng, rồi kiểm lại bình thường lúc hoàn tất.
- Quét cả Hidden/System bằng `-Force`, phát hiện payload ngoài manifest trong
  tệp/thư mục ẩn. Ca marker/Hidden RED14/17→GREEN17/17.

Log RED/GREEN nằm trong [evidence](evidence/20261110-xr04-kit/README.md).
Gói đầu `20261110_120427_16bfedd0` chỉ giữ để tái hiện lỗi, không dùng thay
gói cuối. Hash kiểm tính toàn vẹn khi sao chép; không phải chữ ký xác thực.

## Chạy lại và phần còn chờ

SQL chính được cập nhật tiếp sau lần tạo ZIP trên. Khi dựng gói từ nguồn
hiện tại, dùng [bộ nghiệm thu SQL mới](DONG_BO_DATABASE_20261110.md) như lệnh
dưới đây; gói và hash đã ghi ở trên vẫn là hiện vật lịch sử của lần đóng
gói trước, không được đổi metadata để nhận là bản mới.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraBoBanGiaoXR04.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\TaoBoNghiemThuXR04.ps1 `
  -ThuMucP6 .\docs\sprint3\evidence\20261110-database-sync
```

Mỗi lần tạo mã gói mới, từ chối ghi đè. Gói được dựng lại có SHA riêng; dùng
SHA xuất từ lần đó. Nếu bất kỳ nguồn cũ nào thay đổi, phải chạy P6 lại trước
khi đóng gói. Chỉ bốn script bàn giao trong bảng trên được cho phép bổ sung.

Còn thiếu máy/SQL Server thứ hai, UNC thử và quyền tương ứng để chạy Probe
từ hai hostname rồi phục hồi trên SQL khác/mở viewer từ máy khác. Đã hỏi
thông tin này, chưa nhận cấu hình. Cần thêm nghiệm thu vận hành trước phát
hành; không tự nâng cấp database đang dùng hoặc sửa share/ACL để chốt XR04.
