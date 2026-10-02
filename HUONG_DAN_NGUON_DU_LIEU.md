# Các điểm nối nguồn dữ liệu giao diện

Đây là tài liệu dành cho bước nối dữ liệu sau này, không bổ sung lưu, xóa, xác thực, tính doanh thu hoặc thay đổi SQL.

## Bảng và chi tiết

Các cột DataGrid đã có Binding theo tên cột SQL. Có thể nạp DataView hoặc các đối tượng có property trùng tên cột vào ItemsSource. Ví dụ bảng chính `Grid_BENH_NHAN`, bảng con `Grid_HO_SO_BENH_AN`. Các bảng con được ẩn khỏi tab chính nhưng vẫn giữ nguồn và metadata để dựng chi tiết.

AppWorkspace bắt double-click trên DataGridRow thật, đọc Tag Table:TEN_BANG và chỉ lọc những mục con có khóa ngoại khớp khóa chính. Khóa cha thiếu không được dùng để gom các dòng con trống. Các bảng con được truy cập qua RecordDetails; nút Thêm mục con truyền CreationContext để khóa liên kết cha. Password không đưa vào màn xem chi tiết.

Quay lại/Esc dùng history nội bộ: form tạo con → chi tiết cha → danh sách. Home xóa workspace. Nội dung chưa lưu không được giữ khi rời app.

## Lịch

- LichKham.Schedule nhận `Grid_LICH_KHAM.Items`, đọc NgayGioKham; đánh dấu giờ bắt đầu, không giả định thời lượng khám.
- CaLamViec.Schedule nhận `Grid_LICH_LAM_VIEC.Items` và `Grid_CA_LAM.Items`. Đọc NgayLam + GioBatDau/GioKetThuc. Ca qua đêm tiếp tục ở ngày kế.
- Thiếu ca tham chiếu/giờ: dòng vẫn ở bảng, lịch báo số dòng chưa đủ giờ thay vì đặt vào giờ giả.
- Quan sát thay đổi collection khi control được nạp; Refresh khi đổi chế độ/ngày. Với sửa property trong một row, gọi Schedule.Refresh sau cập nhật.

## Thuốc

`VatTu.SetMaterials(materials, types)` nhận các dòng VAT_TU và LOAI_VAT_TU. Tìm TenLVT bằng “Thuốc” (không phân biệt hoa thường, bỏ khoảng trắng đầu/cuối), so MaLVT để tách hai ListCollectionView độc lập. Không gắn cùng default view vì sẽ làm bộ lọc hai tab ảnh hưởng nhau. Khi dữ liệu loại vật tư thay đổi, gọi lại SetMaterials.

Lô hàng vẫn theo MaVT; mục các lô thuốc xuất hiện trong chi tiết vật tư/thuốc. Nhãn tên thuốc dùng TenVT; không tự thêm hoạt chất/hàm lượng vì chưa được định nghĩa trong schema đã duyệt.

## Thanh toán và đồ thị

`HoaDon.SetWarehouseEntries(receipts, payments)` và `BaoCao.SetWarehouseEntries(receipts, payments)` nạp chứng từ vào hai bảng Thu kho / Chi kho độc lập. Mỗi dòng giữ thuộc tính HOA_DON_KHO. Phân loại chứng từ do tầng nghiệp vụ cung cấp sau này: schema chưa có trường thu/chi, do đó UI không coi dấu của SoLuongLo là dòng tiền. Xuất dùng nội bộ không tự biến thành doanh thu.

`BaoCao.SetChartData(hospital, warehouse)` nhận hai chuỗi ChartPoint:

- Label: nhãn kỳ báo cáo.
- Primary: doanh thu bệnh viện ở chuỗi hospital; thu kho ở chuỗi warehouse.
- Secondary: chi kho ở chuỗi warehouse.

Đơn vị VNĐ. ChartView chỉ chuẩn hóa chiều cao hình để hiển thị, không tính tổng tiền, thuế, giảm giá hoặc cộng các nguồn doanh thu. Dữ liệu trống hiển thị thông báo, không vẽ đường số 0 như một số liệu thật. Trước khi nạp, tầng báo cáo phải chọn cùng kỳ và định nghĩa doanh thu phù hợp.

## Nhật ký

`NhatKy.SetEntries(entries)` nhận các property: ThoiGian, TaiKhoan, HoatDong, PhanHe, DoiTuong, NoiDung. Chỉ nhận bốn nhóm HoatDong: Đăng nhập, Đăng xuất, Phân quyền, Thay đổi thông tin. Hoạt động ngoài nhóm bị loại khỏi view. Nội dung thay đổi nên mô tả nghiệp vụ, không chứa mật khẩu/token. Không có ghi log tự động hoặc bảng SQL mới trong bản giao diện này.
