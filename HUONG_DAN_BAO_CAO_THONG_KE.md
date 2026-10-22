# Báo cáo và thống kê — WPF kết nối database

Danh mục hiện có **4 nhóm / 18 chuyên đề**, đều đọc được database SQL Server theo cấu hình kết nối chung của dự án. Đã bỏ hai nhóm Tổng hợp và Hệ thống cùng các chuyên đề Tổng quan bệnh viện, Nhật ký thao tác khỏi màn hình báo cáo. Không xóa bảng hay dữ liệu gốc.

| Nhóm | Chuyên đề |
|---|---|
| Khám chữa bệnh — 7 | Hồ sơ bệnh nhân; Tiếp nhận và phiếu khám; Lịch khám và điều phối; Khám và điều trị; Kế hoạch điều trị; Dịch vụ thực hiện; Đơn thuốc |
| Tài chính — 3 | Thu – hoàn bệnh viện; Hóa đơn bệnh viện; Lương và hoa hồng |
| Kho và vật tư — 4 | Nhập – xuất kho; Dòng hàng vật tư; Hạn dùng lô hàng; Nhà cung cấp |
| Nhân sự và vận hành — 4 | Nhân sự và hiện diện; Phân ca; Đơn nghỉ và duyệt; Thiết bị và bảo trì |

## Cách xem xu hướng

Chọn chuyên đề, khoảng Từ/Đến và cách nhóm, rồi nhấn **Tải báo cáo**. Có thể tìm chuyên đề không dấu, ví dụ `thu hoan`.

- **Ngày:** mỗi cặp cột là đúng một ngày, có ngày/tháng và năm riêng. Khoảng 50 ngày có đủ 50 điểm, kể cả ngày không phát sinh; kéo thanh cuộn ngang để xem các ngày cuối.
- **Tuần:** mỗi cặp cột là một tuần từ thứ Hai đến Chủ nhật. Nhãn ghi khoảng ngày thực tế nằm trong bộ lọc; tuần đầu/cuối chỉ tính phần ngày đã chọn.
- **Tháng:** mỗi cặp cột là một tháng, nhãn `Tháng MM/yyyy`. Nếu lọc một phần tháng thì chỉ lấy dữ liệu trong phần đó.

Biểu đồ không tự cộng nhiều ngày thành một cột hoặc nhiều tuần/tháng thành một kỳ khác. Tooltip ghi chuyên đề, giá trị và khoảng ngày thực tế; bảng theo kỳ và CSV dùng cùng dữ liệu. Bảng giữ độ chính xác tiền hai chữ số thập phân; thu ròng âm được giữ nguyên.

## Các nguồn đã hoàn thiện bổ sung

**Lương và hoa hồng:** đọc `BANG_LUONG`, liên kết `NHAN_VIEN`; chỉ tính trạng thái Đã chốt theo Ngày chốt. KPI gồm lương theo ca, hoa hồng, tổng lương và số ca. Bảng chi tiết có mã bảng lương, mã/tên nhân viên, ngày chốt và kỳ lương. Cơ cấu dùng cả mã nhân viên để không gộp hai người trùng tên.

**Dòng hàng vật tư:** đọc chi tiết nhập/xuất có phiếu Đã ghi sổ, liên kết `HANG_HOA`. KPI gồm số dòng nhập, số dòng xuất, giá trị nhập và số mặt hàng khác nhau. Bảng chi tiết tổng hợp số lượng theo **cùng mã hàng và đơn vị tính**, không cộng Viên với Chai/Hộp. Tổng số mặt hàng là đếm khác nhau trên cả khoảng, không cộng số mặt hàng của các ngày.

**Hạn dùng lô hàng:** chọn Ngày chốt và ngưỡng 30/60/90 ngày. Đọc lô có hạn dùng, đã nhập đến ngày chốt và chưa hủy; phân biệt quá hạn, sắp đến hạn và ngoài ngưỡng. Hạn dùng bằng ngày chốt thuộc nhóm sắp đến hạn. Bảng có mã lô, hàng, phân loại, hạn dùng, ngày nhập, số ngày tới hạn và số lô/kho. Đây là rà soát danh mục lô; không khẳng định số dư tồn kho hoặc lô còn hàng tại ngày chốt.

**Đơn nghỉ và duyệt:** đọc đơn theo Ngày gửi, liên kết nhân viên và đếm phân công thay thế đã xác nhận mà không nhân số đơn khi có nhiều ca thay thế. Kết quả duyệt và thay thế là trạng thái hiện tại của các đơn gửi trong kỳ; không diễn giải thành lịch sử duyệt tại một thời điểm quá khứ. Số ngày lịch thuộc toàn khoảng nghỉ của các đơn đã duyệt, không tính nghỉ việc không xác định ngày kết thúc. Có bảng chi tiết đơn, nhân viên, khoảng nghỉ, ngày nghỉ và thay thế.

Các chuyên đề khác tiếp tục dùng nguồn BUS sẵn có. Quy tắc cụ thể hiện ngay dưới từng báo cáo. Thu/hoàn bệnh viện chỉ tính giao dịch xác nhận; giá trị hóa đơn hoặc xuất dùng nội bộ không phải tiền đã thu.

## Xuất và trạng thái

**Xuất báo cáo này** lưu CSV UTF-8 có BOM, gồm đúng tên/mã chuyên đề, bộ lọc, nguồn, cách tính, người/thời điểm xuất, tổng và các kỳ. Bốn chuyên đề nêu trên xuất thêm bảng chi tiết tương ứng. Hạn dùng xuất cả Ngày chốt và ngưỡng cảnh báo đã áp dụng. Excel/PDF không nằm trong bộ xuất hiện tại.

Đổi chuyên đề, sửa bộ lọc hoặc làm mới dữ liệu hệ thống sẽ xóa kết quả cũ và khóa xuất cho đến khi tải thành công. Phản hồi tải chậm không được ghi đè chuyên đề/bộ lọc mới. Ngày không tồn tại bị chặn dù DatePicker hoàn tác về ngày cũ. Mất kết nối có thông báo và Thử lại. Không mở rộng quyền hiện tại: tài khoản Admin được vào phân hệ; tài khoản User không được đọc hoặc xuất báo cáo. Nguồn báo cáo bổ sung kiểm tra quyền tại BUS trước khi đọc DAL.

## Kiểm tra và ảnh thật

```powershell
dotnet build RangHamMat.Wpf/RangHamMat.Wpf.csproj -c Release --no-restore
dotnet run --project RangHamMat.ReportChecks -c Release --no-restore
dotnet run --project RangHamMat.ReportChecks -c Release --no-restore -- --render --sql
```

Đã kiểm tra: danh mục 18 chuyên đề / 4 nhóm; tải SQL cả 18 chuyên đề; mỗi điểm biểu đồ khớp đúng một kỳ trong bảng; cuộn tới ngày cuối; nhóm ngày/tuần/tháng cắt đúng phạm vi; ngày không phát sinh; thu ròng âm; quyền; lỗi kết nối; phản hồi cũ; CSV; ngày nhập không tồn tại; binding và bố cục 1366×768, Full HD, 1024×768.

Lương đã chốt, giá trị nhập, đếm mặt hàng, hạn dùng 30/60/90 ngày và số đơn/thay thế được đối soát bằng SELECT độc lập trên database hiện tại. Kiểm tra này chỉ đọc SQL, không ghi hoặc thay đổi dữ liệu bệnh viện.

- [Từng ngày](XemTruoc/bao-cao-thong-ke/bao-cao-theo-ngay.png)
- [Ngày cuối kỳ sau khi cuộn ngang](XemTruoc/bao-cao-thong-ke/bao-cao-theo-ngay-cuoi-ky.png)
- [Từng tuần](XemTruoc/bao-cao-thong-ke/bao-cao-theo-tuan.png)
- [Từng tháng](XemTruoc/bao-cao-thong-ke/bao-cao-theo-thang.png)
- [Chi tiết lương](XemTruoc/bao-cao-thong-ke/bao-cao-chi-tiet-11.png)
- [Chi tiết vật tư theo đơn vị](XemTruoc/bao-cao-thong-ke/bao-cao-chi-tiet-13.png)
- [Hạn dùng từng lô](XemTruoc/bao-cao-thong-ke/bao-cao-chi-tiet-14.png)
- [Đơn nghỉ và thay thế](XemTruoc/bao-cao-thong-ke/bao-cao-chi-tiet-18.png)

Ảnh trong các liên kết này là render WPF với dữ liệu SQL thật. Bản SVG và đề xuất 20 chuyên đề cũ chỉ được giữ làm tài liệu lịch sử, không phải danh mục đang chạy.

Bộ `RangHamMat.WpfSmoke` chung vẫn có lỗi biên dịch có trước ở kiểm tra Nhân sự (`TryReadPosition` / `TryReadEmployee` không tồn tại). Bộ kiểm tra báo cáo được chạy riêng; không tuyên bố bộ kiểm tra chung đã đạt.
