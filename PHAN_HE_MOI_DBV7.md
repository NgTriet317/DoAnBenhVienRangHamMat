# Phân hệ bổ sung theo DB_Nhom4_v7

## Nguyên tắc thực hiện

Bản này được làm lại từ **project gốc** `RangHamMat_WPF_Nhom4(1).zip`.
Không tạo lại toàn bộ 44 bảng thành các phân hệ mới. Các màn hình đã tồn tại trong project gốc được giữ nguyên tối đa; chỉ bổ sung giao diện cho các nghiệp vụ/bảng mới trong phân tích database v7 mà project cũ chưa có màn hình độc lập phù hợp.

## So sánh với database cũ của project

Database SQL hiện có trong project gốc chứa 34 bảng. DB v7 có 44 bảng và đồng thời chuẩn hóa lại một số cấu trúc cũ.
Các tên bảng xuất hiện ở DB v7 nhưng không có trong SQL cũ của project gồm:

- DICH_VU_THUC_HIEN
- CHUNG_CHI_NHAN_VIEN
- PHONG
- LOAI_HANG_HOA
- HANG_HOA
- THUOC
- LO_HANG_HOA
- HOA_DON_NHAP
- PHIEU_NHAP
- CHI_TIET_PHIEU_NHAP
- PHIEU_XUAT
- CHI_TIET_PHIEU_XUAT
- CHINH_SACH_LUONG
- BANG_LUONG
- NHAT_KY_THAO_TAC

Một số tên cũ được mô hình mới thay thế/chuẩn hóa: `PHONG_KHAM`, `LOAI_VAT_TU`, `LO_VAT_TU`, `GIAO_DICH_KHO`, `HOA_DON_KHO`.

## 6 phân hệ mới đã thêm

### 1. Dịch vụ thực hiện
Bảng: `DICH_VU_THUC_HIEN`.

Giao diện dạng **work queue + biểu mẫu ghi nhận nhanh**. Có vùng thống kê trạng thái, bảng theo dõi dịch vụ thực tế và biểu mẫu ngay bên phải để giảm thao tác chuyển màn hình.

### 2. Chứng chỉ nhân viên
Bảng: `CHUNG_CHI_NHAN_VIEN`.

Giao diện dạng **master-detail**: chọn/tìm nhân viên bên trái, danh sách chứng chỉ và vùng nhập nhanh bên phải. Phù hợp quan hệ một nhân viên có nhiều chứng chỉ.

### 3. Danh mục hàng hóa
Bảng: `LOAI_HANG_HOA`, `HANG_HOA`, `THUOC`, `LO_HANG_HOA`.

Giao diện dạng **catalog cockpit 2 × 2**. Bốn khu vực được hiển thị đồng thời để làm rõ mô hình chuẩn hóa: loại hàng → hàng hóa → thuộc tính thuốc / lô hàng.

Lưu ý: màn hình `Vật tư và tồn kho` cũ không bị xóa hoặc viết lại.

### 4. Nhập kho theo chứng từ
Bảng: `HOA_DON_NHAP`, `PHIEU_NHAP`, `CHI_TIET_PHIEU_NHAP`.

Giao diện dạng **quy trình 3 bước**: hóa đơn nhà cung cấp → phiếu nhập thực tế → chi tiết hàng nhập. Mục đích là thể hiện đúng việc hóa đơn mua hàng và ngày/kho thực nhập là hai lớp chứng từ khác nhau.

### 5. Xuất kho
Bảng: `PHIEU_XUAT`, `CHI_TIET_PHIEU_XUAT`.

Giao diện dạng **form điều phối bên trái + lịch sử và chi tiết bên phải**. Có khu vực chọn phạm vi nhận theo khoa, phòng hoặc lần khám.

### 6. Chính sách lương
Bảng: `CHINH_SACH_LUONG`.

Giao diện dạng **quản lý phiên bản chính sách**. Có bộ lọc theo chức vụ/trạng thái, vùng tóm tắt các tham số chính và bảng lịch sử phiên bản. Màn hình `Lương và hoa hồng`/`Bảng lương` cũ vẫn được giữ nguyên.

## Các bảng mới không tạo tile mới vì project đã có chức năng tương ứng

- `BANG_LUONG`: project đã có phân hệ **Lương và hoa hồng**.
- `NHAT_KY_THAO_TAC`: project đã có phân hệ **Nhật ký thao tác**.
- `PHONG`: project đã có quản lý phòng trong **Danh mục dịch vụ**; DB v7 đổi tên/chuẩn hóa từ `PHONG_KHAM` sang `PHONG` nên không cần tạo thêm một tile chỉ vì đổi tên bảng.

## Mức tác động lên phân hệ cũ

Các file XAML/code-behind của 19 phân hệ cũ không bị viết lại. Những thay đổi chính chỉ gồm:

- thêm 6 cặp file XAML/C# mới;
- thêm 6 launcher ở màn hình chính;
- thêm route/phân quyền cho 6 launcher mới trong `MainWindow.xaml.cs`;
- cập nhật kiểm tra số lượng launcher từ 19 lên 25;
- thêm file ánh xạ riêng `Database/ui-table-map-dbv7-new.json`.

File `Database/ui-table-map.json` và database SQL cũ được giữ nguyên để không phá vỡ phần kiểm tra/giao diện cũ trước khi nhóm chính thức migrate database sang v7.
