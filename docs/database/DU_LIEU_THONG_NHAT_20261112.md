# Dữ liệu hiện tại — 12/11/2026

Đã cập nhật cả SQL Server trên máy và `Database/QLBenhVienRangHamMat.sql`. Snapshot có **64 bảng,3.285 dòng**. Người dùng cho phép chuẩn hóa dữ liệu của đồ án và yêu cầu bỏ nhãn hiển thị; người bệnh, nhân sự bổ sung, giá cấu hình, lô và serial vẫn là dữ liệu giả lập, không phải hồ sơ hoặc giao dịch thật. Nguồn nhận diện danh mục và giá tham khảo đã có được giữ; xem [mốc bổ sung đầu và nguồn danh mục](DU_LIEU_DO_AN_20261112.md).

## Khám và điều trị

- Giữ đủ **36 lượt khám**; hoàn tất 7 lượt còn nháp qua BUS khám, duyệt kế hoạch, xác nhận dịch vụ và lập hóa đơn theo quy trình. Không ghi thu tiền hoặc bàn giao thay người sử dụng.
- Xóa đúng **6 phiếu chờ chưa có lịch sử khám**; giữ bệnh nhân và hồ sơ bệnh án. Không thêm hàng chờ mới.
- Snapshot không có phiếu Chờ khám/Đang khám hoặc lượt khám Nháp. Có **13 phiếu Chờ thanh toán,6 Chờ nhận thuốc,17 Hoàn tất**; toàn bộ có thể xem trong lịch sử. Lịch hẹn tương lai được giữ, chỉ vào hàng chờ khi tiếp nhận.
- Giữ 18 bàn giao thuốc và nguồn phiếu xuất cũ, không xuất kho lần hai. Sáu đơn chờ giao vẫn dùng được chức năng bàn giao.

## Lịch làm việc và phòng

Đã phân phòng cho **624 ca thiếu phòng**; cả **1.102 lịch làm việc** có phòng. Giữ phân công bác sĩ đã có, không tạo trùng giờ nhân viên hoặc phòng.

| Nhân viên / chức năng | Phòng |
|---|---|
| NV001 quản trị | PH010 hành chính |
| NV004 điều dưỡng | PH007 chăm sóc/thay băng |
| NV005 tiếp nhận | PH016 quầy tiếp nhận, KHOA001 |
| NV006 dược | PH008 cấp phát thuốc |
| NV007 kho | PH017 điều phối kho, KHOA008 |
| NV008 thu ngân | PH018 thanh toán viện phí, KHOA010 |
| NV009 kỹ thuật viên | PH004 chẩn đoán hình ảnh |
| NV010 nhân sự | PH019 nhân sự, KHOA010 |

Tổng số phòng hiện tại **19**. Chưa phân phòng của thiết bị đang nằm trong kho là trạng thái nghiệp vụ tài sản, được giữ để bàn giao tiếp.

## Mã và tài khoản

Đã đổi mã bổ sung cùng tất cả khóa ngoại, tham chiếu chứng từ và chuỗi JSON. Bảng đối chiếu đầy đủ nằm trong `evidence/20261112-data-conventions/code-map.json`. Mã lịch sử còn hợp lệ được giữ.

| Loại | Mã bổ sung hiện tại |
|---|---|
| Bệnh nhân / hồ sơ | BN012–BN047 / HS011–HS046 |
| Nhân viên / tài khoản | NV011–NV014 / TK011–TK014 |
| Lần khám bổ sung | LS013–LS036 |
| Phiếu tiếp nhận có lịch sử | PKB013–PKB036 |
| Dịch vụ / thuốc | DV011–DV018 / TH011–TH016 |
| Hàng hóa / vật tư / thiết bị | HH031–HH058 / VT011–VT024 / TB011–TB018 |
| Kho / nhà cung cấp | KHO011 / NCC011–NCC012 |
| Lô / nhập / hóa đơn nhập | LO011–LO120 / PN011–PN120 / HDN011–HDN120 |
| Ca / lịch hẹn | CA011–CA012 / LK011–LK016 |
| Phòng bổ sung | PH011–PH019 |

Ví dụ: BN030 / LS019 / DT012 / HD012 chờ thanh toán; BN036 / LS025 / DT015 / HD018 chờ giao. NV004 vẫn được phân công hỗ trợ ảnh cho LS017; chưa tạo tệp ảnh hoặc metadata giả trỏ tới tệp không tồn tại.

Tên đăng nhập mới: **user011** (CK002), **user012** (CK003), **user013** (CK004), **user014** (CK007). Mật khẩu khởi tạo vẫn `DoAnRHM@2026`; mật khẩu cũ và các bản hash được giữ. Admin `admin001 / 123456`.

Hàm cấp mã của ứng dụng đã thống nhất tiền tố PH/TH/KHO/CA/HS/LS/LO/BT/DMTT… theo dữ liệu cũ. Chỉ đếm hậu tố số thuộc dãy mã nghiệp vụ; không lấy số trong `REF_DV_ENDO123` hoặc `THRHM052` làm số thứ tự. D3 giữ tối thiểu ba chữ số và cho phép số dài hơn, như LLV1102. Mã danh mục tham khảo cũ vẫn được giữ nguyên.

## Kiểm tra và khôi phục

```powershell
# Kiểm tra, không lưu các phép thử thu/giao:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ChuanHoaDuLieuDoAn.ps1 -HienThi -MayChu lpc:DESKTOP-C9VCE8A
# Chuyển đổi bản dữ liệu trước lần này; đã có marker thì chỉ kiểm tra, không thêm/hồi trạng thái:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ChuanHoaDuLieuDoAn.ps1 -HienThi -ApDung -MayChu lpc:DESKTOP-C9VCE8A
```

Chỉ áp dụng chuyển đổi cho bộ dữ liệu trước lần chuẩn hóa này. Máy mới cài bằng toàn bộ file SQL chính đã có dữ liệu hoàn chỉnh và marker, không cần chạy công cụ bổ sung. Chạy lại SQL trên database đã có bảng giữ nguyên dữ liệu, không tự nạp đè snapshot. Đóng và mở lại ứng dụng để bỏ dữ liệu đang lưu trong bộ nhớ.

Backup trước lần cập nhật này đã COPY_ONLY/CHECKSUM và RESTORE VERIFYONLY: `QLBenhVienRangHamMat_before_academic_data_94E87CD75769425A83978E1E430A3513.bak`, trong thư mục Backup mặc định của SQL Server. Đường dẫn đầy đủ ở bằng chứng. Trước ghi chính có 3.224 dòng: nhiều hơn snapshot trước 3 dòng nhật ký/phiên đăng nhập; các dòng phát sinh này đã được giữ.

Bằng chứng tại `evidence/20261112-data-conventions/`: đối soát không lỗi, kiểm FK/CHECK/trigger, 21 đơn in được,18 bàn giao hợp lệ,13 hóa đơn tính phí được; thử thu/giao và retry rollback. Có kiểm chạy lại với SHA256 đủ 64 bảng, thử lỗi CHECK giữa chuyển đổi hoàn nguyên đủ 64 bảng và toàn bộ ràng buộc/trigger/marker, cùng kiểm cài mới/chạy lại SQL và build ứng dụng.
