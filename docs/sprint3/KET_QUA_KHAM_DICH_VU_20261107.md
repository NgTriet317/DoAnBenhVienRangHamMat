# Kết quả P2/P3 — tiếp nhận và thu phí khám dịch vụ

Đã hoàn thành P2/P3 tiếp nối nền tảng P0/P1 trong `KeHoachTrienKhai.docx`.
Giữ nhánh `Sprint3_tan`, bốn tầng WPF/BUS/DAL/ET và file SQL triển khai duy nhất.
Không thay đổi việc người dùng đã bỏ package `fo-dicom.Desktop` trong bốn project.

## Hành vi đã triển khai

1. Tại **Hồ sơ bệnh nhân → Tiếp nhận → Đến trực tiếp**, chọn Khám thông thường
   hoặc Khám dịch vụ. Lịch hẹn luôn dùng luồng thông thường, không phát sinh phụ thu.
2. Khám dịch vụ chọn **bác sĩ trước → dịch vụ → phòng theo ca**. Danh sách chỉ có
   bác sĩ CV002 đang làm việc, tài khoản hoạt động, ca xác nhận/đang hoạt động,
   Có mặt, không nghỉ phép và có phòng khám hợp lệ. Xử lý cả ca qua ngày.
3. Dịch vụ phải đang hoạt động, cùng chuyên khoa, đã đến ngày áp dụng. Đổi bác sĩ
   xóa dịch vụ/phòng cũ; đổi loại hoặc nguồn xóa lựa chọn phụ thuộc. Không mở điều
   phối ngoài lịch cho khám dịch vụ, kể cả Admin.
4. DAL kiểm lại trong giao dịch Serializable, giữ khóa dữ liệu lịch/phòng/tài khoản.
   Backend lưu **Dịch vụ/30000**; UI không gửi số tiền. Phiếu cũ cùng nguồn phải
   khớp loại khám khi retry; không nâng loại âm thầm. Có audit bác sĩ/dịch vụ/loại/phí.
5. Tổng kết trước chốt, hóa đơn khi hoàn tất, nhánh hóa đơn cũ, bảng tính phí và
   bản in cùng tính **một dòng phụ thu** riêng. Không đưa phụ thu vào dịch vụ thực hiện.
6. Giảm giá chỉ áp dụng phần ngoài phụ thu. Thu trọn gói, đối chiếu nguồn, fingerprint,
   tách thuốc theo lô và retry giữ nguyên phí/giá đã chốt. Nhánh hóa đơn cũ vẫn hỗ
   trợ thu nhiều đợt và hoàn hợp lệ.

Ví dụ đã chạy: khám 100.000 + phụ thu 30.000 = 130.000; khách đưa 150.000,
thối 20.000. Với thuốc 30.000 chia hai lô: tổng 160.000; đổi giá danh mục sau
chốt không thay số phải thu; bản in gộp đúng một dòng thuốc và một dòng phụ thu.

## Điều chỉnh khi đối chiếu dự án

- Kiểm giờ ở nhánh hóa đơn cũ dùng thời gian SQL `datetime2(0)`, cùng độ chính
  xác với lần khám, tránh báo nhầm “chưa đến” do đồng hồ client hoặc làm tròn giây.
- Nạp danh mục dịch vụ khi đổi về thông thường bằng BUS và đúng giao dịch của
  biểu mẫu; không tải lại cache toàn dự án bằng kết nối khác. Đã có kiểm RED/GREEN
  cho trường hợp này.
- Giữ cơ chế nhóm/in hiện có: dòng phụ thu không có mã đơn thuốc nên đã được
  giữ riêng. Bổ sung số lượng 1, đơn vị Lượt và mã nguồn từ DAL, không viết lại bộ in.
- SQL P1 đã có đầy đủ cột/ràng buộc cho P2/P3; gói này không tạo migration hoặc
  file SQL triển khai cạnh tranh.

## Bằng chứng kiểm thực tế

| Nhóm | Kết quả |
|---|---|
| Kiểm mới ET/BUS/DAL, SQL và WPF | **28/28 PASS** |
| Ba quầy bằng kết nối SQL độc lập | **1/1 PASS**: yêu cầu trùng trả cùng phiếu, hai người có hai số thứ tự khác nhau |
| Nền tảng SQL P1 | **35/35 PASS** |
| Đồng bộ ET/DAL và công/lương | **32/32 PASS** |
| Luồng khám, đơn thuốc, kho, hóa đơn, WPF hiện có | **47/47 PASS** |
| Báo cáo SQL và render WPF | **41/41 PASS** |
| Build Debug và Release | Thành công, 0 lỗi ở cả hai cấu hình |
| Review độc lập | Không có finding Critical/Important/Minor được xác nhận trong P2/P3 |

Các ca mới gồm bác sĩ mất/hủy/chưa xác nhận ca, vắng/chưa hiện diện, nghỉ phép,
khóa tài khoản, phòng không hợp lệ, ca ngừng, bác sĩ nghỉ việc; ca 22h–06h được
kiểm với mốc cố định 22h/01h và chặn trước ca/đúng giờ kết thúc. Có kiểm tiếp nhận
không cần Admin, lịch hẹn, khác chuyên khoa/ngày áp dụng, retry khác loại, giảm giá,
thu/hoàn, thuốc hai lô, giá chốt và paginator in thực.

Log rút gọn và ảnh chụp fixture tại [evidence/20261107-service-reception](evidence/20261107-service-reception).
Log đầy đủ trong `obj/ServiceReceptionEvidence/`; các screenshot gốc trong
`obj/RhmFinal/`. Ảnh và dữ liệu đều là fixture ghi rõ nguồn, không phải bệnh án thật.

## Các file chính

- ET: `ET_DULIEUQUYTRINHKHAM.cs`, `ET_DULIEUTINHPHI.cs`, `ET_TONGKETKHAM.cs`, `ET_DULIEUBANGIAOTHUOC.cs`.
- Tiếp nhận: `BUS_TIEPNHAN.cs`, `DAL_TIEPNHAN.cs`, `Views/TiepNhanKham.xaml(.cs)`.
- Tài chính: `DAL_CHITIETHOADON.cs`, `DAL_HOADON.cs`, các phần `DAL_THANHTOANTRONGOI`, `DAL_KHAMDIEUTRI.ThucHien.cs`.
- Hiển thị: `Views/BangTinhPhiEditor.xaml(.cs)`, `Presentation/KhamLamSang.KeThuoc.cs`.
- Kiểm thử: `tools/KiemTraKhamDichVu.ps1`, `tools/ServiceReceptionChecks.cs.txt`.

## Chạy lại

```powershell
# Bộ kiểm mới: SQL cài không snapshot phục hồi, tất cả fixture thông thường rollback.
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraKhamDichVu.ps1 -TaoDatabaseKiemThu

# Kiểm đồng thời dùng DB mới riêng; cố ý giữ fixture để ba kết nối nhìn thấy cùng dữ liệu.
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraKhamDichVu.ps1 -Database RHM_Hospital_Test_ServiceReception_Concurrent -TaoDatabaseKiemThu -DongThoi

powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraQuyTrinhBenhVien.ps1 -Database RHM_Hospital_Test_ServiceReception_Clinical -TaoDatabaseKiemThu
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraNenTangSprint3.ps1 -Database RHM_Hospital_Test_S3Foundation_Final
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraDongBoSql.ps1 -Database RHM_Hospital_Test_S3Foundation_Control
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBaoCao.ps1 -Database RHM_Hospital_Test_S3Foundation_Control -Sql -Render
dotnet build BenhVienRangHamMat.sln -c Debug --no-restore
dotnet build BenhVienRangHamMat.sln -c Release --no-restore
```

Chế độ `-DongThoi` chỉ nhận DB có đuôi `_Concurrent`, từ chối nếu fixture đã tồn
tại. Muốn chạy lại, chọn tên DB thử mới có cùng hậu tố; không dùng database ứng dụng.

## Trạng thái bàn giao

Chưa cập nhật `QLBenhVienRangHamMat` đang dùng; mọi phép ghi thử chỉ trong database
`RHM_Hospital_Test_...`. Trước khi chạy bản ứng dụng mới cần database đã được đồng
bộ bằng toàn bộ `Database/QLBenhVienRangHamMat.sql` theo hướng dẫn sao lưu của dự án.
Chưa commit/push hoặc publish lại `PhatHanh`.

**Tiếp theo: P4** upload/xem/ẩn ảnh X-quang theo lần khám, kho dùng chung và quyền/audit.
P5 audit/dữ liệu báo cáo nhiều kỳ và P6 nghiệm thu tổng thể vẫn chưa hoàn tất. Các
kiểm báo cáo ở trên là hồi quy hiện có, không phải nghiệm thu dữ liệu trình diễn P5.
