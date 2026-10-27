# Tình trạng chức năng và liên kết database — 13/10/2026

Đã rà source của toàn bộ phân hệ, mở 25/25 màn hình bằng nút thật trên trang chủ, chạy lại các bộ kiểm tra SQL và WPF. Có 6 phân hệ khám/tài chính đã nối đọc/lưu SQL, nhân sự có một phần thao tác SQL, 18 phân hệ còn lại chưa có luồng đọc/lưu hoàn chỉnh. Việc mở được màn hình XAML không có nghĩa chức năng lưu đã hoạt động.

## 1. Vì sao trước đây không thấy ET/BUS của vai trò?

Trước lượt sửa này, bộ vai trò có đủ ba lớp nhưng mang tên `ET_MAVAITRO`, `DAL_MAVAITRO`, `BUS_MAVAITRO`. ET đã có `[Table("VAI_TRO")]`. Đây là cách đặt tên gây nhầm lẫn: `MaVaiTro` là cột khóa của thực thể vai trò.

Đã đổi đồng bộ tên file, lớp, constructor, các lời gọi và tài liệu thành:

| Thành phần | File hiện tại | Trách nhiệm |
|---|---|---|
| ET | [ET_VAITRO.cs](../../ET/ET_VAITRO.cs) | Ánh xạ `VAI_TRO`: `MaVaiTro`, `TenVaiTro`, `MoTa`. |
| DAL | [DAL_VAITRO.cs](../../DAL/DAL_VAITRO.cs) | Đọc/thêm/sửa/xóa đúng bảng `dbo.VAI_TRO`, cấp mã và kiểm tra phiên bản. |
| BUS | [BUS_VAITRO.cs](../../BUS/BUS_VAITRO.cs) | Gọi DAL tương ứng, chuyển lỗi SQL thành thông báo tiếng Việt qua `XuLyLoi`. |

Tên hàm hiện tại: `layToanBoVaiTro`, `layVaiTroTheoMa`, `themVaiTro`, `suaVaiTro`, `xoaVaiTro`. Thuộc tính khóa vẫn là `MaVaiTro`, đúng cột SQL. `TAI_KHOAN.MaVaiTro` tham chiếu `VAI_TRO.MaVaiTro` bằng khóa ngoại của database.

CRUD vai trò đã được chạy trên SQL thật: thêm → đọc → sửa → chặn xóa với phiên bản cũ → xóa với phiên bản mới. Dữ liệu thử được rollback. **Màn hình Tài khoản/Vai trò chưa gọi bộ BUS này để đọc/lưu**; đổi tên lớp không tự triển khai chức năng quản trị tài khoản.

## 2. Đối chiếu ET–DAL–BUS với SQL

Hiện có **39 ET, 39 DAL, 39 BUS**, mỗi bộ cùng hậu tố; không có DAL/BUS thực thể nào thiếu ET tương ứng. Cả 39 ET khớp **303 cột** trong file SQL, gồm tên/kiểu/nullable/khóa/cột tính toán và constructor.

Quy tắc tên: bỏ dấu `_` của tên bảng để đặt hậu tố lớp. Ví dụ `VAI_TRO` → `VAITRO`, `HO_SO_BENH_AN` → `HOSOBENHAN`. `[Table]` giữ nguyên tên bảng SQL.

| Bảng SQL | Hậu tố chung của ET / DAL / BUS |
|---|---|
| BAO_TRI_THIET_BI | BAOTRITHIETBI |
| BENH_NHAN | BENHNHAN |
| CA_LAM | CALAM |
| CHI_TIET_DON_THUOC | CHITIETDONTHUOC |
| CHI_TIET_HOA_DON | CHITIETHOADON |
| CHI_TIET_KE_HOACH | CHITIETKEHOACH |
| CHI_TIET_PHIEU_NHAP | CHITIETPHIEUNHAP |
| CHI_TIET_PHIEU_XUAT | CHITIETPHIEUXUAT |
| CHUC_VU | CHUCVU |
| CHUNG_CHI_NHAN_VIEN | CHUNGCHINHANVIEN |
| CHUYEN_KHOA | CHUYENKHOA |
| DICH_VU | DICHVU |
| DICH_VU_THUC_HIEN | DICHVUTHUCHIEN |
| DON_THUOC | DONTHUOC |
| HANG_HOA | HANGHOA |
| HO_SO_BENH_AN | HOSOBENHAN |
| HOA_DON | HOADON |
| HOA_DON_NHAP | HOADONNHAP |
| KE_HOACH_DIEU_TRI | KEHOACHDIEUTRI |
| KHO | KHO |
| KHOA | KHOA |
| LICH_KHAM | LICHKHAM |
| LICH_LAM_VIEC | LICHLAMVIEC |
| LICH_SU_KHAM_BENH | LICHSUKHAMBENH |
| LO_HANG_HOA | LOHANGHOA |
| LOAI_HANG_HOA | LOAIHANGHOA |
| NHA_CUNG_CAP | NHACUNGCAP |
| NHAN_VIEN | NHANVIEN |
| PHIEU_KHAM_BENH | PHIEUKHAMBENH |
| PHIEU_NHAP | PHIEUNHAP |
| PHIEU_XUAT | PHIEUXUAT |
| PHONG | PHONG |
| TAI_KHOAN | TAIKHOAN |
| THANH_TOAN | THANHTOAN |
| THIET_BI | THIETBI |
| THUOC | THUOC |
| TINH_TRANG_BENH | TINHTRANGBENH |
| VAI_TRO | VAITRO |
| VAT_TU | VATTU |

Database SQL Server đang chạy trên `DESKTOP-C9VCE8A` có **44 bảng**. File [QLBenhVienRangHamMat.sql](../../Database/QLBenhVienRangHamMat.sql) hiện khai báo **45 bảng**, vì có thêm `CA_THEO_NGAY`. Kiểm tra `sys.tables` xác nhận bảng bổ sung này chưa có trong database đang chạy.

Các bảng dưới đây chưa có ET, nên cũng chưa có DAL/BUS tương ứng:

| Bảng | Tình trạng |
|---|---|
| DON_XIN_NGHI | Có trong SQL/database; chưa có bộ lớp và xử lý lưu từ giao diện. |
| PHAN_CONG_THAY_THE | Có trong SQL/database; chưa có bộ lớp và xử lý lưu từ giao diện. |
| CHINH_SACH_LUONG | Có trong SQL/database; chưa có bộ lớp và xử lý lưu từ giao diện. |
| BANG_LUONG | Có trong SQL/database; chưa có bộ lớp và xử lý tính/lưu lương từ giao diện. |
| NHAT_KY_THAO_TAC | Có trong SQL/database; chưa có bộ lớp và cơ chế tự ghi nhật ký. |
| CA_THEO_NGAY | Chỉ thấy khai báo trong file SQL; chưa có bảng ở database đang chạy và chưa có bộ lớp. |

`CA_THEO_NGAY.MaCa` trong file đang là `NVARCHAR(30)`, trong khi `CA_LAM.MaCa` là `NVARCHAR(50)`; lệnh tạo khóa ngoại đang được comment. Luồng ca theo ngày chưa triển khai. Lượt này chỉ đọc đối chiếu database, không chạy lại script khởi tạo hay tự thêm các bảng chưa dùng.

[ArchitectureChecks.cs](../../RangHamMat.Checks/ArchitectureChecks.cs) đã có kiểm tra chống lệch tên: ET phải trỏ bảng có trong SQL; hậu tố phải đúng tên bảng; lớp/file phải trùng tên; BUS phải gọi DAL cùng bộ; DAL không được dùng bảng `dbo` ngoài file SQL. Bảng chưa có ET được liệt kê riêng khi chạy kiểm tra.

## 3. Tình trạng đủ 25 phân hệ trên trang chủ

Trong bảng dưới, “SQL đã nối” nghĩa là có luồng đọc/lưu qua BUS/DAL thật. Các phép thử không bấm mọi nút tương tác cần xác nhận của người dùng; bằng chứng chi tiết nằm ở phần kiểm chứng cuối báo cáo.

| # | Phân hệ / màn hình | Chức năng hiện có | Phần còn thiếu hoặc giới hạn |
|---|---|---|---|
| 1 | [Tổng quan](../../RangHamMat.Wpf/Views/TongQuan.xaml.cs) | Mở bố cục XAML. | Các số 24/08/05/17 đang viết cố định trong XAML; chưa tổng hợp từ SQL. |
| 2 | [Bệnh nhân](../../RangHamMat.Wpf/Views/BenhNhan.xaml.cs) | **SQL đã nối:** danh sách, tìm/lọc/phân trang, thêm/sửa bệnh nhân; mở hồ sơ, bệnh án, lịch sử và ngữ cảnh liên phân hệ. | Bản ghi đã xác nhận được khóa theo nghiệp vụ; không coi việc có hàm xóa trong DAL là đã có nút xóa bệnh nhân trên UI. |
| 3 | [Lịch khám](../../RangHamMat.Wpf/Views/LichKham.xaml.cs) | **SQL đã nối:** danh sách/lịch, theo bệnh nhân, tạo lịch, các luồng đổi trạng thái/đổi hẹn/tiếp nhận trong code. | Lịch trong hồ sơ không có nút thêm bệnh nhân; các hộp xác nhận thao tác chưa được tự bấm hết trong smoke test. |
| 4 | [Khám điều trị](../../RangHamMat.Wpf/Views/ChiTietKhamDieuTri.xaml.cs) | **SQL đã nối:** lần khám, thông tin khám, tình trạng bệnh, kế hoạch nhiều dòng, dịch vụ thực hiện và liên kết đơn thuốc/hóa đơn. Xác nhận lưu cả trạng thái và ngày. | Không sửa nội dung bản đã xác nhận; phải giữ đúng lần khám nguồn. |
| 5 | [Dịch vụ thực hiện](../../RangHamMat.Wpf/Views/DichVuThucHien.xaml.cs) | **SQL đã nối:** danh sách đúng bệnh nhân/lần khám, thêm/sửa dịch vụ nháp qua form, số lượng/đơn giá/thành tiền. | Danh mục dịch vụ được đọc để lựa chọn; màn hình quản trị danh mục Dịch vụ riêng chưa lưu được. |
| 6 | [Đơn thuốc](../../RangHamMat.Wpf/Views/ChiTietDonThuoc.xaml.cs) | **SQL đã nối:** kê đơn nhiều dòng, sửa/xóa dòng khi nháp, xác nhận/hủy nháp, xem chi tiết; có cửa sổ xem trước/in đơn. | Cấp thuốc thực hiện tại Hóa đơn. Chưa kiểm chứng máy in vật lý; đơn đã xác nhận được giữ nguyên. |
| 7 | [Phiếu hướng dẫn](../../RangHamMat.Wpf/Views/PhieuHuongDan.xaml.cs) | Mở màn hình/form XAML. | Handler chọn dữ liệu còn rỗng; chưa có luồng lưu/in phiếu hoàn chỉnh, một số trường dùng cấu trúc cũ. |
| 8 | [Nhân sự](../../RangHamMat.Wpf/Views/NhanSu.xaml.cs) | **SQL một phần:** đọc nhân viên/chức vụ/chuyên khoa; code thêm nhân viên/chức vụ qua BUS; đọc ET dòng được chọn để mở chi tiết; kiểm tra đầu vào. | Chưa có thao tác sửa/xóa nhân viên/chức vụ trên UI. Đã thử đọc/validation; chưa bấm lưu nhân sự để tạo dữ liệu chính thức trong lượt rà này. |
| 9 | [Chứng chỉ nhân viên](../../RangHamMat.Wpf/Views/ChungChiNhanVien.xaml.cs) | Mở màn hình XAML. | Chưa nạp danh sách hoặc nối thao tác thêm/sửa/xóa từ màn hình; bộ lớp đã có. |
| 10 | [Ca làm việc](../../RangHamMat.Wpf/Views/CaLamViec.xaml.cs) | Đổi chế độ bảng/lịch; control lịch đã thử ca qua đêm/chồng giờ bằng nguồn kiểm tra. | Màn hình chưa nạp lịch làm SQL, chưa lưu phân ca/đơn nghỉ/thay thế; control chạy với dữ liệu truyền vào không chứng minh phân hệ đã nối SQL. |
| 11 | [Thiết bị](../../RangHamMat.Wpf/Views/ThietBi.xaml.cs) | Mở danh sách/form XAML. | Chưa nạp/lưu từ màn hình; còn trường của schema cũ. |
| 12 | [Bảo trì](../../RangHamMat.Wpf/Views/BaoTri.xaml.cs) | Mở danh sách/form XAML. | Chưa nạp/lưu từ màn hình; còn trường của schema cũ. |
| 13 | [Vật tư](../../RangHamMat.Wpf/Views/VatTu.xaml.cs) | Có hàm `SetMaterials` chia vật tư/thuốc từ nguồn bên ngoài. | Luồng mở hiện không gọi hàm này để nạp SQL; còn `LOAI_VAT_TU`, `LO_VAT_TU` của schema cũ; chưa lưu. |
| 14 | [Danh mục hàng hóa](../../RangHamMat.Wpf/Views/DanhMucHangHoa.xaml.cs) | Mở màn hình XAML. | Chưa nạp/lưu danh mục từ màn hình; bộ lớp hàng hóa/loại hàng đã có. |
| 15 | [Nhà cung cấp](../../RangHamMat.Wpf/Views/NhaCungCap.xaml.cs) | Mở danh sách/form XAML. | CRUD ở BUS/DAL đã có và được thử; màn hình chưa gọi để nạp/lưu. |
| 16 | [Nhập/xuất vật tư](../../RangHamMat.Wpf/Views/NhapXuatVatTu.xaml.cs) | Mở danh sách/form XAML. | Còn bảng `GIAO_DICH_KHO`, `HOA_DON_KHO` cũ; chưa nối luồng nhập/xuất theo schema hiện tại. |
| 17 | [Nhập kho](../../RangHamMat.Wpf/Views/NhapKho.xaml.cs) | Mở màn hình XAML. | Chưa lưu phiếu nhập từ màn hình. BUS/DAL phiếu nhập và trigger tổng đã được thử riêng. |
| 18 | [Xuất kho](../../RangHamMat.Wpf/Views/XuatKho.xaml.cs) | Mở màn hình XAML. | Chưa có luồng xuất kho tổng quát từ màn hình này. **Cấp thuốc/ghi sổ phiếu xuất tại Hóa đơn đã chạy SQL.** |
| 19 | [Hóa đơn](../../RangHamMat.Wpf/Views/HoaDon.xaml.cs) | **SQL đã nối:** khoản đủ điều kiện, hóa đơn nháp, xác nhận/hủy nháp, cấp thuốc FIFO theo kho/lô còn hạn, thu/hoàn, lịch sử/số dư, xem trước và phân trang hóa đơn. | Cần chọn nhân viên thực hiện. Bộ kiểm tra xác nhận dữ liệu/bản in; chưa thử máy in vật lý hoặc hoàn tất hộp thoại Print to PDF. |
| 20 | [Bảng lương](../../RangHamMat.Wpf/Views/BangLuong.xaml.cs) | Mở danh sách/form XAML. | Chưa có ET/DAL/BUS và luồng tính/lưu lương. |
| 21 | [Chính sách lương](../../RangHamMat.Wpf/Views/ChinhSachLuong.xaml.cs) | Mở màn hình XAML. | Chưa có ET/DAL/BUS và luồng quản trị/lưu chính sách. |
| 22 | [Báo cáo](../../RangHamMat.Wpf/Views/BaoCao.xaml.cs) | Đổi chế độ bảng/biểu đồ; có `SetChartData`, `SetWarehouseEntries` nhận dữ liệu ngoài. | Luồng mở chưa gọi các hàm này với số liệu SQL; chưa tính báo cáo tự động; còn bảng kho cũ. |
| 23 | [Dịch vụ](../../RangHamMat.Wpf/Views/DichVu.xaml.cs) | Mở danh sách/form XAML. | Chưa CRUD danh mục từ màn hình; còn bảng `PHONG_KHAM` cũ. Danh mục hiện vẫn được đọc SQL trong các form khám/hóa đơn. |
| 24 | [Tài khoản / Vai trò](../../RangHamMat.Wpf/Views/TaiKhoan.xaml.cs) | Mở hai tab/form XAML. ET/DAL/BUS cho cả tài khoản và vai trò đã có. | Chưa nạp/lưu từ màn hình; nút lưu bị khóa. Form còn `TAI_KHOAN.MatKhau`, trong khi SQL dùng `MatKhauHash`. |
| 25 | [Nhật ký](../../RangHamMat.Wpf/Views/NhatKy.xaml.cs) | Có `SetEntries` và bộ lọc khi được truyền nguồn. | Luồng mở chưa nạp nguồn SQL; chưa tự ghi nhật ký; chưa có ET/DAL/BUS bảng nhật ký. |

**Đăng nhập và phân quyền:** [DangNhap.xaml.cs](../../RangHamMat.Wpf/Views/DangNhap.xaml.cs) chỉ chặn ô trống/tên quá dài rồi trả `DialogResult=true`. Chưa đối chiếu mật khẩu với `TAI_KHOAN`. Vai trò ở [MainWindow.xaml.cs](../../RangHamMat.Wpf/MainWindow.xaml.cs) là lựa chọn xem giao diện với danh sách quyền viết trong code, chưa lấy quyền của tài khoản từ `VAI_TRO`.

**Các màn hình con:** `HoSoBenhNhan`, `ChiTietHoSo`, `ChiTietKhamDieuTri`, `ChiTietDonThuoc` dùng ngữ cảnh của luồng SQL; `TiepNhanKham` lưu phiếu tiếp nhận. Form bệnh nhân/lịch/khám/đơn và hóa đơn có luồng lưu. `ChiTietNhanSu` chỉ đọc ET được truyền vào. Các form mẫu như `CuaSoVatTu`, `CuaSoThietBi`, `CuaSoTaiKhoan`, `CuaSoBangLuong` mở được nhưng chưa lưu SQL. Control biểu đồ/lịch/chi tiết và bản hóa đơn có XAML hiện hữu; không tạo bố cục control production bằng C# khi chạy.

## 4. Giao diện mẫu còn lệch schema

Đã sửa `NhanSu.xaml`: Tag bảng chức vụ trước là `Table:dgvChucVu` (tên control), nay là `Table:CHUC_VU` đúng bảng SQL. Đã sửa chú thích AI đang nói nhầm nhân sự có “thêm/sửa/xóa”: màn hình hiện chỉ thêm và xem.

Sau sửa này, rà các thuộc tính `Tag` thực trong toàn bộ XAML của Views thấy **12 cặp file/bảng không có trong SQL** và **46 cặp file/trường không có trong SQL**, nằm ở giao diện mẫu. Những tên này không được các luồng lưu SQL đang hoạt động sử dụng. Không thể coi toàn bộ giao diện dự án đã khớp database.

| Form / giao diện mẫu | Vấn đề còn lại |
|---|---|
| VatTu / CuaSoVatTu | `LOAI_VAT_TU`, `LO_VAT_TU` không có; v8 dùng `LOAI_HANG_HOA`, `LO_HANG_HOA`. Tên/đơn vị/loại hàng thuộc `HANG_HOA`, không còn các cột `VAT_TU.TenVT`, `MaLVT`, `DonViTinh`, `TrangThai`. Tổng 15 Tag trường sai trong form. |
| NhapXuatVatTu / CuaSoNhapXuatVatTu / BaoCao | `GIAO_DICH_KHO`, `HOA_DON_KHO` không có; hiện có `PHIEU_NHAP`, `PHIEU_XUAT`, chi tiết và `HOA_DON_NHAP`. Tổng 13 Tag trường sai trong form nhập/xuất. |
| DichVu / CuaSoDichVu | `PHONG_KHAM` không có; bảng hiện tại là `PHONG`. 4 Tag trường sai trong form. |
| CuaSoThietBi | 5 Tag trường cũ: `LoaiTB`, `NgayBaoHanh`, `PhongSuDung`, `SoLuong`, `TenTB`. |
| CuaSoBaoTri | 3 Tag trường cũ: `LoaiBaoTri`, `NgayBaoTri`, `NguoiThucHien`. |
| CuaSoCaLamViec | 3 Tag trường thay thế cũ: `LyDoThayThe`, `MaLLVGoc`, `MaNVThayThe`. |
| CuaSoPhieuHuongDan | 2 Tag không có ở phiếu khám: `MaBN`, `MaKhoa`; phải đi qua quan hệ hồ sơ/ngữ cảnh phù hợp. |
| CuaSoTaiKhoan | 1 Tag `MatKhau`; SQL dùng `MatKhauHash`. Việc thay tên ô không tự tạo cơ chế kiểm tra mật khẩu. |

Các form mẫu này cần sửa cấu trúc theo v8 và nối đúng ET → DAL → BUS trước khi bật lưu. Báo cáo này đánh dấu các phần chưa triển khai; lượt sửa hiện tại không tự triển khai thêm quản trị kho, lương, tài khoản hoặc xác thực.

## 5. Bằng chứng kiểm chứng

| Kiểm tra đã chạy lại | Kết quả / ý nghĩa |
|---|---|
| Build Release toàn solution | 0 lỗi; còn 6 cảnh báo C# sẵn có tại `dbconnect` và các model truy vấn của `DAL_PHIEUXUAT`. |
| RangHamMat.Checks | 39 bộ ET–DAL–BUS, 61 XAML; kiểm tra bảng SQL thực, liên kết sự kiện/resource và quy tắc lịch/đơn. |
| RangHamMat.SchemaChecks | 39 bảng đã có ET, 303 cột; 0 lỗi ánh xạ. Không có nghĩa tất cả 45 bảng đã có ET. |
| Metadata database đang chạy đối chiếu QuyTacNhapLieu.xml | 349 cột ghi được, 349 quy tắc; 0 lệch kiểu/quy cách/nullable/khóa. Chỉ bao phủ 44 bảng đang chạy, chưa bao gồm `CA_THEO_NGAY`. |
| RangHamMat.PersistenceChecks | 16 nhóm kiểm tra: lưu/sửa/rollback, nullable, computed, xác nhận/khóa, chống phiên bản cũ, CRUD vai trò/nhà cung cấp/phiếu nhập; cả 39 BUS đọc SQL trả đúng ET. Không thử đủ mọi thao tác CRUD của cả 39 bộ. |
| RangHamMat.FinanceChecks | 54 kiểm tra giao dịch tài chính: nguồn thu, cấp thuốc/FIFO, hóa đơn, thu/hoàn, khóa và rollback. |
| RangHamMat.WpfSmoke | Luồng hồ sơ/form nhiều dòng/tài chính/bản in; biểu đồ/lịch/chi tiết XAML; 531 kiểm tra nhập liệu/schema/binding; mở 25/25 phân hệ bằng các nút trang chủ. |

Các bộ kiểm tra ghi thử trên SQL đều rollback dữ liệu. 531 kiểm tra nhập liệu xác nhận các trường/ràng buộc trong phạm vi đã nối và các tình huống form mẫu được thử; không chứng minh 46 Tag trường cũ đã được đổi sang v8.

Lệnh chạy lại (Windows, SQL Server hiện tại hoạt động):

```powershell
dotnet build BenhVienRangHamMat.sln -c Release --no-restore -m:1
dotnet run --project RangHamMat.Checks -c Release --no-restore
dotnet run --project RangHamMat.SchemaChecks -c Release --no-restore
dotnet run --project RangHamMat.PersistenceChecks -c Release --no-restore
dotnet run --project RangHamMat.FinanceChecks -c Release --no-restore
dotnet run --project RangHamMat.WpfSmoke -c Release --no-restore
```

`NU1900` ở hai project kiểm tra SQL là cảnh báo không lấy được dữ liệu kiểm tra lỗ hổng từ NuGet trong môi trường hiện tại; các bộ kiểm tra vẫn đạt. Giới hạn sandbox gây lỗi kết nối mã hóa được xử lý bằng chạy lại các kiểm tra SQL/WPF ngoài sandbox, không thay cấu hình bảo vệ của ứng dụng.
