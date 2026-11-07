# Triển khai báo cáo Sprint3_tan

Kế hoạch: `KeHoach_CaiTao_BaoCao_ThongKe_Sprint3_tan.docx`, BC-01 đến BC-11.

**Cập nhật theo phản hồi 06/11/2026:** [Báo cáo trọng tâm](BAO_CAO_TRONG_TAM.md) thay KPI chứng từ trên tổng quan bằng bệnh nhân duy nhất và độ bao phủ, đưa bảng bệnh lý lên trước, sửa việc làm mất mã ở Chọn nhanh. Bộ kiểm mới nhất: **41 PASS, 0 FAIL**; các log 35 ca bên dưới là mốc triển khai trước điều chỉnh này.

## Quyết định triển khai

- Làm trực tiếp trên nhánh `Sprint3_tan` hiện có theo yêu cầu tài liệu; không tạo checkout khác hoặc commit thay người dùng.
- Giữ 18 mã 02–19, thêm 00 Tổng quan và 20 Tình trạng ghi nhận. Chưa có phân nhóm bệnh được duyệt nên không gọi thứ hạng là tỷ lệ mắc bệnh.
- Dùng schema hiện hữu; không cần BC-12, không sửa SQL chính hoặc dữ liệu vận hành. Kiểm thử dữ liệu giả dùng giao dịch rollback, chỉ trên database kiểm thử có sẵn.
- Quyền tổng hợp áp dụng cho nhân viên có tài khoản hoạt động; quyền nhạy cảm ánh xạ chức vụ hiện hữu, xác thực lại SQL và phiên ở DAL. Quyền module BaoCao hiện mặc định chặn User được thay bằng chính sách tổng hợp; không mở quyền các module nghiệp vụ khác.
- Các giao diện dùng chung: DTO kỳ [từ, đến loại trừ] → DAL tổng hợp → BUS so sánh → WPF/CSV; nhóm bệnh phải dùng cùng tập lượt khám xác nhận và cùng bộ lọc. DISTINCT bệnh nhân không được cộng từ các tháng.

## Tiến độ và bằng chứng

- Khảo sát: baseline build WPF Release thành công, 0 lỗi, 124 cảnh báo có sẵn. SQL Server tại `.` truy cập được; database chính có đủ bảng nguồn và chức vụ CV001–CV010.
- BC-01: đã đối chiếu đủ 18 chuyên đề và các nguồn trong tài liệu.
- BC-02–BC-10: hoàn tất DTO, SQL tổng hợp, chất lượng mã, so sánh kỳ, phân quyền, giao diện và CSV.
- BC-11: hoàn tất ngày 06/11/2026. **ReportChecks 35 PASS, 0 FAIL**, có SQL thực và render WPF. Kiểm bàn giao chuẩn: build Debug/Release thành công, 0 lỗi; kiểm schema 62 bảng thành công. Lần build Debug cuối có 122 cảnh báo cũ của các phân hệ khác; build Release tăng dần báo 0 cảnh báo. Không phát sinh cảnh báo từ các file báo cáo mới/sửa.

Bằng chứng:

- [Log 35 ca kiểm báo cáo](evidence/bao-cao/ReportChecks.log).
- [Log build và schema đã lược bỏ dòng chứng từ định danh](evidence/bao-cao/Build-Schema.log).
- [Ca tái hiện trước sửa các phát hiện rà soát](evidence/bao-cao/Review-RED.log).
- Render dữ liệu SQL thật: [1024×768](evidence/bao-cao/admin-1024.png), [1366×768](evidence/bao-cao/admin-1366.png), [Full HD](evidence/bao-cao/admin-1920.png), [nhân viên](evidence/bao-cao/user-1366.png), [chuyên đề tình trạng](evidence/bao-cao/admin-disease.png).
- [CSV kiểm thử tổng hợp từ dữ liệu giả, không có hồ sơ thật](evidence/bao-cao/comparison.csv).

Rà soát độc lập phát hiện hai lỗi Important: xuất dữ liệu chi tiết đã cache sau khi mất quyền, và năm gốc trỏ tới kỳ tương lai/chưa hoàn chỉnh. Đã tái hiện RED, sửa và chạy GREEN toàn bộ bộ kiểm. Quyền chi tiết được kiểm lại trước hiển thị và trước xuất, độc lập quyền tổng hợp. Hai điểm ban đầu xếp Minor về nguồn CSV tổng quan và trạng thái bộ lọc không phát sinh được nâng thành yêu cầu cần sửa vì ảnh hưởng truy vết và ý nghĩa số liệu; cả hai đã có kiểm hồi quy RED→GREEN. Không còn mục Minor hoãn lại. Hồi quy cơ cấu lương theo nhân viên cũng đã tái hiện và sửa, vẫn tổng hợp tại SQL.

Phạm vi chưa đo: execution plan/hiệu năng trên dữ liệu lớn và lịch sử chuyên khoa của bác sĩ. Ánh xạ quyền theo chức vụ, liên kết chuyên khoa hiện tại và dịch vụ tiếp nhận là các lựa chọn đã công khai ở trên/dưới; thay đổi cơ cấu bệnh viện có thể cần cập nhật chính sách. Không cần DDL/index nên không có kiểm chạy migration F5 hai lần.

Mã nguồn giữ ở nhánh `Sprint3_tan`, chưa commit/push/merge. File kế hoạch DOCX người dùng cung cấp được giữ nguyên. File kế hoạch quy trình khám khác bị xóa bởi thay đổi ngoài phạm vi trong lúc triển khai; tác vụ báo cáo không sửa/khôi phục thay đổi đó.

## File thay đổi

| Nhóm | File |
| --- | --- |
| DTO và chính sách | `ET/ET_THONGKE.cs` |
| Nghiệp vụ | `BUS/BUS_BAOCAO.cs`, `BUS/SoSanhThongKe.cs` |
| Truy vấn | `DAL/DAL_BAOCAO.cs`, `DAL_BAOCAO.TongHop.cs`, `DAL_BAOCAO.BenhLy.cs`, `DAL_BAOCAO.TaiChinh.cs` |
| Giao diện | `RangHamMat.Wpf/Views/BaoCao.xaml`, `BaoCao.xaml.cs`, `Themes/BaoCaoStyles.xaml` |
| Xử lý trình bày | `RangHamMat.Wpf/Controls/BaoCaoDuLieu.cs`, `BaoCaoSoSanh.cs`, `DuLieuSoSanhBaoCao.cs`, `ChartView.xaml`, `ChartView.xaml.cs` |
| Điều hướng/phiên | `RangHamMat.Wpf/MainWindow.xaml.cs`, `PhienDangNhap.cs` |
| Kiểm thử/tài liệu | `tools/KiemTraBaoCao.ps1`, `tools/ReportChecks.cs.txt`, `README.md`, tài liệu này và `docs/sprint3/evidence/bao-cao/` |

## Sử dụng

Mở **Báo cáo** sau khi đăng nhập. Mặc định là tháng hoàn chỉnh liền trước, so với tháng trước nữa. Chọn **12 tháng hoàn chỉnh**, **Năm trước** hoặc nhập hai ngày thuộc tháng bắt đầu/kết thúc; chế độ **Tháng** lấy trọn tháng, tối đa 24 tháng. Chọn MTD để xem tháng hiện tại đến hết ngày đang diễn ra. Ngày/tuần của 18 chuyên đề cũ vẫn dùng được, không áp phép so sánh tháng.

**Tình trạng ghi nhận** phân biệt lượt khám và bệnh nhân duy nhất, hiển thị bao phủ mã, lượt chưa phân loại, dòng NULL và dòng trùng. Bộ lọc chuyên khoa dùng chuyên khoa hiện tại của bác sĩ; bộ lọc dịch vụ dùng dịch vụ tiếp nhận trên phiếu khám. Không suy ra lịch sử chuyển khoa hay mọi dịch vụ điều trị từ hai bộ lọc này. Danh mục ngừng áp dụng vẫn được tính trong lịch sử. Số liệu và chênh lệch của nhóm dưới 5 bệnh nhân được che.

Biểu đồ chọn một chỉ tiêu để hai chuỗi luôn cùng đơn vị. Thu ròng âm giữ nguyên. Thẻ KPI có chênh lệch, bảng đối chiếu có kỳ này/kỳ gốc, giá trị tuyệt đối và phần trăm. Kỳ gốc bằng 0 không tính phần trăm. Chỉ thêm kỳ bằng 0 sau khi truy vấn thành công; lỗi tải xóa kết quả và khóa xuất.

Xuất CSV UTF-8 BOM gồm chuyên đề, nguồn, trạng thái/công thức, bộ lọc, kỳ so sánh, đơn vị, người/thời điểm xuất và thời điểm tải. Các bảng chi tiết chứng từ/lương/lô/đơn nghỉ giới hạn 200 dòng, KPI vẫn tính trên toàn kỳ. Không thêm XLSX vì dự án chưa có thư viện/yêu cầu xuất XLSX.

## Quyền

| Phạm vi | Vai trò/chức vụ hiện hữu |
| --- | --- |
| Tổng hợp và xuất tổng hợp | Nhân viên đang làm việc, tài khoản hoạt động, phiên hợp lệ |
| Chi tiết tài chính | Admin; CV007 thu ngân; CV010 trưởng phòng hành chính |
| Lương và đơn nghỉ toàn viện | Admin; CV010 |
| Chi tiết kho/lô | Admin; CV005 dược sĩ; CV006 thủ kho; CV010 |
| Quyền lâm sàng | Giữ nguyên luồng khám; quyền xem báo cáo không cấp quyền mở hồ sơ |

Các mã chức vụ là ánh xạ trong `ChinhSachBaoCao`, không tạo quyền/bảng SQL mới. `IsAdmin` trên giao diện hoặc cờ bool từ caller không cấp quyền ở DAL. DAL đọc lại tài khoản, nhân viên, chức vụ, vai trò và kiểm phiên SQL trước truy vấn. Khách, phiên hết hạn, tài khoản/nhân viên ngừng hoạt động bị từ chối. Lượt xem tài chính/lương và yêu cầu xuất ghi vào nhật ký hiện hữu, chỉ chứa mã chuyên đề và khoảng ngày, không sao chép nội dung hồ sơ.

## Định nghĩa KPI và đối soát

MoM: tháng A so A−1; YoY: tháng A/năm N so tháng A/năm N−1; năm được chọn: cùng tập tháng của hai năm. Mọi chỉ tiêu dùng Δ=A−B, %Δ=Δ/|B|×100 khi B≠0. MTD cắt đến cùng ngày, chặn tại cuối tháng gốc nếu tháng đó ngắn hơn. Tất cả truy vấn dùng biên `[từ ngày, đến ngày loại trừ)`.

| KPI | Nguồn, mốc và trạng thái | Đơn vị / kiểm tra |
| --- | --- | --- |
| K01 lượt xác nhận | LICH_SU_KHAM_BENH.ThoiGianKham; Đã xác nhận, DISTINCT MaLS | lượt; TC-08/09 và SQL fixture |
| K02 bệnh nhân | Qua PHIEU_KHAM_BENH → HO_SO_BENH_AN, cùng tập lượt K01; DISTINCT MaBN trên toàn kỳ | người; đối soát SQL độc lập, không cộng DISTINCT tháng |
| K03 tình trạng | TINH_TRANG_BENH.MaDMTT → danh mục; DISTINCT (MaLS,MaDMTT), cùng K01 | lượt/người/% lượt có mã; TC-08–11 |
| K04 bao phủ | Lượt K01 có ít nhất 1 mã / tổng K01; NULL không được suy từ chữ chẩn đoán | %; fixture và bảng chất lượng |
| K05 tiếp nhận | PHIEU_KHAM_BENH.ThoiGianTiepNhan; tách có lịch/tự đến/hủy | phiếu; tải chuyên đề 03 bằng SQL thật |
| K06 dịch vụ | DICH_VU_THUC_HIEN.ThoiGianThucHien; chỉ số xác nhận riêng | bản ghi/VNĐ; tải chuyên đề 07, giữ định nghĩa cũ |
| K07 thu/hoàn/ròng | THANH_TOAN.NgayThanhToan; Đã xác nhận; Thu−Hoàn | VNĐ/giao dịch; TC-12 đối soát độc lập |
| K08 phải thu | HOA_DON.NgayLap; SoTienPhaiThu chỉ Đã xác nhận | VNĐ/hóa đơn; chuyên đề 10; không cộng vào K07 |
| K09 nhập/xuất | NgayNhap/NgayXuat; Đã ghi sổ; chi tiết nhóm MaHH+DonViTinh | phiếu/dòng/lượng theo đơn vị; TC-13/14 |
| K10 giá trị nhập | CHI_TIET_PHIEU_NHAP.ThanhTien, NgayNhap, Đã ghi sổ | VNĐ; chuyên đề 13, không cộng hóa đơn mua |
| K11 hạn dùng | LO_HANG_HOA.HanSuDung so ngày chốt, lô đã nhập, loại Hủy/Đã hủy | lô, ngưỡng 30/60/90 ngày; không khẳng định tồn quá khứ |
| K12 ca/hiện diện | LICH_LAM_VIEC.NgayLam, trạng thái ca và hiện diện hiện tại | ca; chuyên đề 16/17 |
| K13 quỹ lương | BANG_LUONG.NgayChot; chỉ Đã chốt | VNĐ/ca; chuyên đề 11, quyền và đối soát nguồn |

Chuyên đề 19 dùng trạng thái bảo trì hiện hành **Đang thực hiện/Đã hoàn thành**, đồng bộ với thay đổi bảo trì trước Sprint báo cáo. Không thêm snapshot tồn kho, không ước giá trị xuất bằng giá mua gần nhất.

## Chạy kiểm tra

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBaoCao.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBaoCao.ps1 -Sql -Render -MayChu 'lpc:.'
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBanGiao.ps1 -KiemTraSql -MayChu 'lpc:.'
```

`lpc:.` là kết nối local đã kiểm trên máy này; máy khác chọn instance phù hợp. Bộ kiểm dùng project tạm `obj/ReportChecks`, giữ solution đúng bốn project. Fixture bệnh lý tạo bảng tạm trong kết nối, không tạo database hoặc ghi hồ sơ nghiệp vụ. Phiên đăng nhập phục vụ kiểm quyền và nhật ký phát sinh trong bộ kiểm đều rollback bằng TransactionScope; bộ kiểm không gọi Complete(). Ảnh render chỉ chứa số tổng hợp.

TC-18 không áp dụng vì không có migration. Không coi việc tải SQL thật một chuyên đề là bằng chứng đã chạy mọi tổ hợp dữ liệu giả của chuyên đề đó; các ca fixture, đối soát và giao diện được liệt kê riêng trong log.
