# Đề xuất thiết kế Báo cáo và thống kê

> Đây là thiết kế đề xuất ban đầu. Theo yêu cầu cập nhật, giao diện đã triển khai hiện có 4 nhóm / 18 chuyên đề, bỏ Tổng hợp và Hệ thống, kết nối đủ 18 nguồn và hiển thị đúng từng ngày/tuần/tháng không gộp kỳ. Xem bản hiện hành tại [HUONG_DAN_BAO_CAO_THONG_KE.md](../../HUONG_DAN_BAO_CAO_THONG_KE.md) và các ảnh `bao-cao-theo-ngay/tuan/thang.png` trong thư mục này.

Bản thiết kế để duyệt trước khi chỉnh giao diện WPF. Chưa triển khai chức năng hoặc thay đổi dữ liệu. Xem hình bố cục trong [thiet-ke-de-xuat.svg](thiet-ke-de-xuat.svg).

## Cơ sở và phạm vi

Đã đọc toàn bộ nội dung văn bản của `KeHoach_BaoCao_ThongKe_ToanDuAn_WPF_V3.docx`, xem `BaoCao.xaml`, code-behind, `ChartView`, quy ước trong `DESIGN.md` và ảnh giao diện bảo trì hiện có. Hình nhúng trong DOCX chưa được dùng làm căn cứ thẩm mỹ.

Mục tiêu do người dùng đưa ra: giao diện đẹp, màu sắc dễ chịu, trực quan, đủ nội dung và dễ dùng cho nhân viên nội bộ bệnh viện. Người dùng cho phép điều chỉnh ý tưởng so với tài liệu.

Phạm vi đề xuất là thay màn hình báo cáo hiện có, giữ khung ứng dụng WPF và cách dùng XAML của dự án. Công việc giao diện không mặc nhiên bao gồm hoàn thiện toàn bộ truy vấn, phân quyền và xuất Excel/PDF của các giai đoạn sau trong V3. Những phần chưa có nguồn hoặc chưa tích hợp phải thể hiện đúng trạng thái.

## Lựa chọn bố cục

Chọn danh mục bên trái và một vùng làm việc bên phải. So với giữ hàng tab hiện tại, bố cục này chứa được 20 chuyên đề và tên dài. So với trang danh mục bằng các thẻ lớn, người dùng có thể chuyển giữa các chuyên đề mà vẫn thấy bộ lọc và ngữ cảnh.

Danh mục rộng 248 DIP, có tìm theo tên không phân biệt dấu, sáu nhóm thu gọn/mở rộng và cuộn riêng. Báo cáo đang chọn có nền xanh nhạt và chữ đậm. Khi tìm, tự mở các nhóm có kết quả; không có kết quả thì có hướng dẫn xóa từ khóa. Số chuyên đề cạnh nhóm tính trên danh mục người dùng thực sự được phép xem.

Vùng làm việc gồm tiêu đề và mô tả ngắn, nút xuất đúng chuyên đề, bộ lọc, trạng thái tải, bốn chỉ số, hai biểu đồ và bảng đối chiếu. Bảng và đồ thị xuất hiện cùng ngữ cảnh, không cần chuyển qua hai chế độ tách rời như hiện tại.

## Danh mục đầy đủ

V3 liệt kê 20 chuyên đề, nhưng bảng danh sách hiện có năm tiêu đề nhóm dù phần tổng thể yêu cầu sáu. Tách Tổng quan bệnh viện thành nhóm Tổng hợp giúp khớp sáu nhóm mà vẫn giữ đủ 20 chuyên đề.

| Nhóm | Chuyên đề |
|---|---|
| Tổng hợp — 1 | Tổng quan bệnh viện |
| Khám chữa bệnh — 7 | Hồ sơ bệnh nhân; Tiếp nhận và phiếu khám; Lịch khám và điều phối; Khám và điều trị; Kế hoạch điều trị; Dịch vụ thực hiện; Đơn thuốc |
| Tài chính — 3 | Thu – hoàn bệnh viện; Hóa đơn bệnh viện; Lương và hoa hồng |
| Kho và vật tư — 4 | Nhập – xuất kho; Dòng hàng vật tư; Hạn dùng lô hàng; Nhà cung cấp |
| Nhân sự và vận hành — 4 | Nhân sự và hiện diện; Phân ca; Đơn nghỉ và duyệt; Thiết bị và bảo trì |
| Hệ thống — 1 | Nhật ký thao tác |

## Phong cách thị giác

Giữ bản sắc của ứng dụng: Segoe UI, nền #F3F6FA, bề mặt trắng, chữ navy #102F54, nội dung #21344B, chữ phụ #586D85 và viền #D9E2ED. Màu hành động chính #2468CD; xanh ngọc #087A73, tím #76519E, cam #986010 dùng tiết chế cho các chỉ số cần phân biệt. Đỏ dành cho lỗi hoặc cảnh báo thực tế.

Tiêu đề 24–26 DIP; tiêu đề vùng 16; nội dung 14; nhãn phụ 12–13. Bảng có số tiền căn phải, ngày tháng thống nhất, số có phân cách hàng nghìn và đơn vị tại đầu cột. Số tiền giữ độ chính xác của nguồn, không tự rút gọn làm mất thông tin trong bảng/xuất.

Thay bốn thẻ tô màu lớn trong V3 bằng một dải thống kê trắng, bốn ô có đường phân cách. Giảm nhiễu màu, tăng diện tích số liệu. Tránh tạo nhãn phần trăm tăng/giảm khi chưa có phép so sánh hợp lệ.

## Bộ lọc và thao tác

- Mặc định từ đầu tháng đến hôm nay. Nút nhanh Hôm nay, 7 ngày qua, Tháng này thay đổi khoảng; nút Tải báo cáo áp dụng. Nhóm theo Ngày/Tuần/Tháng là cách tổng hợp, không tự mở rộng khoảng lọc.
- Khoảng ngày và cách nhóm được giữ khi chuyển chuyên đề có cùng loại bộ lọc. Chuyên đề mới xóa số liệu cũ ngay, rồi mới tải nguồn của mình.
- Có thay đổi bộ lọc chưa áp dụng: hiện “Bộ lọc đã thay đổi — tải lại để cập nhật”, khóa xuất và làm rõ kết quả cũ không thuộc bộ lọc mới. Ngày bắt đầu lớn hơn ngày kết thúc hoặc ngày không hợp lệ có lỗi ngay cạnh trường.
- Tuần bắt đầu thứ Hai; tuần/tháng nằm một phần trong khoảng lọc được ghi rõ khoảng thực tế. Lấy dữ liệu trước rồi nhóm, không lấy thêm ngày ngoài phạm vi.
- Hạn dùng lô hàng dùng Ngày chốt và Ngưỡng 30/60/90 ngày. Dữ liệu trạng thái hiện tại phải ghi “Tại thời điểm tải”; không giả vờ là ảnh chụp trạng thái lịch sử.
- Một nút xuất báo cáo hiện tại; chỉ đưa CSV vào lựa chọn khi có dữ liệu và đã triển khai bộ xuất. Excel/PDF chỉ xuất hiện khi bộ xuất thực sự sẵn sàng.

## Chỉ số, biểu đồ và đối chiếu

Không gán cùng bộ chỉ số cho cả 20 chuyên đề. Tên, đơn vị, mô tả nguồn và biểu đồ thay đổi theo báo cáo. Không đủ bốn chỉ tiêu đáng tin thì hiển thị ít hơn; chỉ tiêu chưa tích hợp dùng “Chưa kết nối”, không dùng 0.

Ví dụ Tổng quan bệnh viện: Tổng lịch hẹn theo ngày giờ khám; Lượt khám xác nhận theo thời gian khám; Số lịch trong tập đã chọn có trạng thái đã khám; Tỷ lệ lịch đã khám / tổng lịch hẹn trong tập. Khi mẫu số bằng 0, tỷ lệ là “Không áp dụng”. Số lượt khám xác nhận có thể bao gồm khám không đặt lịch, nên không dùng nó làm tử số của tỷ lệ lịch đã khám. Trạng thái hiện tại của lịch không chứng minh trạng thái tại thời điểm lịch sử.

Biểu đồ trái ưu tiên xu hướng; biểu đồ phải ưu tiên cơ cấu hoặc xếp hạng dạng thanh ngang có nhãn. Không bắt mọi chuyên đề phải có cùng hai biểu đồ. Số liệu âm hợp lệ như thu ròng cần trục có số âm; `ChartView` hiện lọc bỏ số âm và tooltip cố định VNĐ, nên cần chỉnh đúng hai điểm này trước khi dùng cho các nghiệp vụ khác.

Bảng tổng hợp theo kỳ có tiêu đề và đơn vị riêng; chọn kỳ mở vùng bản ghi nguồn bên dưới hoặc cùng màn hình khi dữ liệu chi tiết được tích hợp. Chưa có dữ liệu chi tiết thì không tạo nút truy vết giả. Xuất bao gồm toàn bộ kỳ trong bộ lọc, kể cả khi biểu đồ rút gọn nhãn. Có thể đưa dòng 0 vào các kỳ không phát sinh sau khi truy vấn thành công; lỗi tải không được biến thành số 0.

Các ràng buộc V3 giữ nguyên: thu ròng từ giao dịch thu/hoàn xác nhận, không từ tổng hóa đơn; xuất nội bộ kho không phải doanh thu; không cộng số lượng khác đơn vị; lương chỉ tính bản chốt theo ngày chốt; hồ sơ mới theo Ngày lập hồ sơ, không tự gọi là bệnh nhân mới; chống cộng trùng khi JOIN chi tiết.

## Trạng thái và khả năng sử dụng

| Trạng thái | Cách thể hiện |
|---|---|
| Chưa tải/chưa kết nối | Dấu “—”, lời hướng dẫn; không có biểu đồ giả; khóa xuất |
| Đang tải | Hiện tiến trình tại vùng trạng thái; khóa tải lặp và xuất; không hiển thị kết quả chuyên đề trước |
| Đã tải | Hiện kỳ áp dụng, thời điểm cập nhật và nguồn; cho phép xuất nếu có bộ xuất và đúng quyền |
| Không phát sinh | Chỉ số có giá trị 0 khi truy vấn thành công; giải thích không phát sinh trong khoảng; cho phép đổi kỳ |
| Lỗi | Thông báo không tải được, có Thử lại; xóa kết quả cũ; không xuất dữ liệu cũ |
| Không có quyền | Không đọc nguồn hoặc xuất; ẩn mục không được phép; kiểm tra quyền thực tế ở luồng dữ liệu |

Các nút có hover, focus và disabled; nhãn đi kèm màu. Tab theo thứ tự danh mục → bộ lọc → tải → xuất → nội dung. DatePicker giữ thao tác native. Không thêm popup khi người dùng chỉ cần đổi báo cáo hoặc xem bảng.

1366×768: danh mục 248 DIP và nội dung cuộn dọc độc lập; tiêu đề/hành động ở trên vùng cuộn, bộ lọc và các biểu đồ không bị cắt ngang. Với vùng nội dung hẹp, bộ lọc xuống dòng và hai biểu đồ xếp dọc; dải KPI chuyển hai cột. Không thu nhỏ toàn màn hình hoặc giảm cỡ chữ để nhét đủ mọi phần. Bảng có thanh cuộn ngang khi thật sự cần. Full HD tăng diện tích dữ liệu thay vì phóng to các thẻ.

## Dự kiến triển khai sau khi duyệt

Chỉnh `RangHamMat.Wpf/Views/BaoCao.xaml` và code-behind; thêm style riêng cho báo cáo, kế thừa HospitalStyles. Giữ phần bố cục trong XAML. Chỉ sửa `ChartView` nếu cần hiển thị đơn vị và số âm; không thay cấu trúc chung ET/DAL/BUS chỉ để làm giao diện. Tích hợp nguồn thực và xuất trong phạm vi đã có, những chuyên đề chưa có nguồn phải báo rõ.

Xác minh bằng build WPF; chọn lần lượt 20 chuyên đề; đổi bộ lọc ngày/tuần/tháng; thử ngày sai, không dữ liệu và lỗi; kiểm tra không giữ số liệu/xuất của chuyên đề trước; kiểm tra render 1366×768 và Full HD. Phần truy vấn và xuất thực tế cần kiểm tra dữ liệu/quyền riêng trước khi tuyên bố hoàn thiện.
