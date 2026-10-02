# Đối chiếu giao diện với database

Nguồn: `Database/QLBenhVienRangHamMat.sql`, giữ nguyên nội dung SQL đã gửi. Không chạy hay chỉnh schema.

Đã ánh xạ 34 bảng / 245 thuộc tính vào các phân hệ. Nhãn dùng tiếng Việt; Tag DB:BANG.Cot và SortMemberPath lưu tên cột để đối chiếu source.

| Phân hệ | Các bảng |
|---|---|
| Hồ sơ bệnh nhân (`BenhNhan`) | `BENH_NHAN`, `HO_SO_BENH_AN` |
| Khám và điều trị (`KhamDieuTri`) | `LICH_SU_KHAM_BENH`, `TINH_TRANG_BENH`, `KE_HOACH_DIEU_TRI`, `CHI_TIET_KE_HOACH` |
| Đơn thuốc (`DonThuoc`) | `DON_THUOC`, `CHI_TIET_DON_THUOC` |
| Bác sĩ và nhân sự (`NhanSu`) | `NHAN_VIEN`, `CHUC_VU` |
| Ca làm việc và nghỉ phép (`CaLamViec`) | `CA_LAM`, `LICH_LAM_VIEC`, `DON_XIN_NGHI`, `PHAN_CONG_THAY_THE` |
| Trang thiết bị (`ThietBi`) | `THIET_BI` |
| Bảo trì thiết bị (`BaoTri`) | `BAO_TRI_THIET_BI` |
| Vật tư, lô hàng và kho (`VatTu`) | `VAT_TU`, `LOAI_VAT_TU`, `LO_VAT_TU`, `KHO` |
| Nhà cung cấp (`NhaCungCap`) | `NHA_CUNG_CAP` |
| Nhập xuất vật tư (`NhapXuatVatTu`) | `GIAO_DICH_KHO`, `HOA_DON_KHO` |
| Điều phối lịch khám (`LichKham`) | `LICH_KHAM` |
| Dịch vụ và danh mục khám (`DichVu`) | `DICH_VU`, `KHOA`, `CHUYEN_KHOA`, `PHONG_KHAM` |
| Hóa đơn và thanh toán (`HoaDon`) | `HOA_DON`, `CHI_TIET_HOA_DON`, `THANH_TOAN` |
| Tài khoản và vai trò (`TaiKhoan`) | `TAI_KHOAN`, `VAI_TRO` |
| Phiếu khám bệnh (`PhieuHuongDan`) | `PHIEU_KHAM_BENH` |

## Bố cục mới

Mỗi phân hệ hiển thị danh sách; trường nhập nằm trong `CuaSo<PhanHe>.xaml`. Các tab chính có nút thêm mới; bảng chi tiết con chỉ xuất hiện sau double-click bản ghi chính. Nút thêm mục con nằm trong chi tiết cha và khóa mã liên kết. Danh sách mặc định chỉ hiện các cột chính; nút Hiện đủ cột mở các thuộc tính còn lại. Mật khẩu luôn bị loại khỏi danh sách.

## Quy ước giao diện

- Khóa chính IDENTITY: chỉ đọc, hiển thị “Tự sinh”. Khóa ngoại: ComboBox, chưa nạp dữ liệu.
- DATE: DatePicker; DATETIME2: ngày và ô giờ HH:mm:ss; TIME: ô giờ. Văn bản giữ MaxLength theo SQL; NVARCHAR(MAX) dùng vùng nhiều dòng.
- NOT NULL không có giá trị mặc định: đánh dấu *. Ràng buộc được chú thích; chưa viết xử lý kiểm tra/lưu.
- Trạng thái là văn bản vì SQL không định nghĩa tập giá trị; không tự đặt enum.
- MatKhau dùng PasswordBox cho đầu vào mật khẩu mới và không xuất hiện trong cột danh sách. Khi phát triển chức năng, phải băm có salt trước khi ghi TAI_KHOAN.MatKhau; bản giao diện chưa băm/lưu.
- HOA_DON.ThanhTien chỉ đọc, chưa có code tính toán. GiamGia là số tiền. CHI_TIET_HOA_DON không tự thêm MaDV.
- PhongSuDung và NguoiThu là TextBox, đúng kiểu văn bản trong SQL. Thuốc là TenThuoc, không có mã thuốc/danh mục thuốc riêng.
- Tồn kho tổng hợp từ SUM(GIAO_DICH_KHO.SoLuongLo), không cộng LO_VAT_TU.SoLuongNhap lần nữa. Chưa có code tổng hợp.
- Phiếu hướng dẫn trên Home dùng PHIEU_KHAM_BENH: phiếu khám/tiếp nhận, không tạo bảng hướng dẫn mới.
- Lương/hoa hồng và nhật ký: giữ app với thông báo chưa có schema; không tạo trường giả. Tổng quan và Báo cáo chỉ trình bày dữ liệu nguồn hiện có, không giả số liệu/chỉ tiêu chưa tính.

## Chi tiết từng thuộc tính

### BENH_NHAN

Phân hệ: `BenhNhan` — Bệnh nhân.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaBN` | Mã bệnh nhân | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_BENH_NHAN] PRIMARY KEY` |
| `HoTenBN` | Họ tên bệnh nhân | `NVARCHAR(255) NOT NULL` |
| `GioiTinh` | Giới tính | `NVARCHAR(50) NULL` |
| `NgaySinh` | Ngày sinh | `DATE NULL` |
| `SDT` | Số điện thoại | `NVARCHAR(50) NULL` |
| `DiaChi` | Địa chỉ | `NVARCHAR(255) NULL` |
| `MaBHYT` | Mã bảo hiểm y tế | `NVARCHAR(50) NULL` |
| `NhomMau` | Nhóm máu | `NVARCHAR(50) NULL` |
| `TienSuBenh` | Tiền sử bệnh | `NVARCHAR(MAX) NULL` |
| `DiUng` | Dị ứng | `NVARCHAR(MAX) NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |

### HO_SO_BENH_AN

Phân hệ: `BenhNhan` — Hồ sơ bệnh án.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaHS` | Mã hồ sơ bệnh án | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_HO_SO_BENH_AN] PRIMARY KEY` |
| `MaBN` | Bệnh nhân | `INT NOT NULL` |
| `NgayLap` | Ngày lập | `DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME())` |
| `LyDoKhamBanDau` | Lý do khám ban đầu | `NVARCHAR(MAX) NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |

### PHIEU_KHAM_BENH

Phân hệ: `PhieuHuongDan` — Tiếp nhận và hướng dẫn.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaPhieu` | Mã phiếu khám | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_PHIEU_KHAM_BENH] PRIMARY KEY` |
| `MaBN` | Bệnh nhân | `INT NOT NULL` |
| `MaHS` | Hồ sơ bệnh án | `INT NOT NULL` |
| `MaLich` | Lịch khám | `INT NULL` |
| `MaKhoa` | Khoa | `INT NOT NULL` |
| `MaPhong` | Phòng khám | `INT NOT NULL` |
| `MaBS` | Bác sĩ | `INT NULL` |
| `MaDV` | Dịch vụ | `INT NOT NULL` |
| `SoThuTu` | Số thứ tự | `INT NOT NULL` |
| `ThoiGianTiepNhan` | Thời gian tiếp nhận | `DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME())` |
| `LyDoKham` | Lý do khám | `NVARCHAR(MAX) NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### LICH_SU_KHAM_BENH

Phân hệ: `KhamDieuTri` — Lịch sử khám.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaLS` | Mã lần khám | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_LICH_SU_KHAM_BENH] PRIMARY KEY` |
| `MaHS` | Hồ sơ bệnh án | `INT NOT NULL` |
| `MaLich` | Lịch khám | `INT NULL` |
| `ThoiGianKham` | Thời gian khám | `DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME())` |
| `TrieuChung` | Triệu chứng | `NVARCHAR(MAX) NULL` |
| `ChanDoan` | Chẩn đoán | `NVARCHAR(MAX) NULL` |
| `KeHoachXuLy` | Kế hoạch xử lý | `NVARCHAR(MAX) NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### TINH_TRANG_BENH

Phân hệ: `KhamDieuTri` — Tình trạng bệnh.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaTTB` | Mã tình trạng bệnh | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_TINH_TRANG_BENH] PRIMARY KEY` |
| `MaLS` | Lần khám | `INT NOT NULL` |
| `TinhTrang` | Tình trạng | `NVARCHAR(255) NOT NULL` |
| `ChanDoan` | Chẩn đoán | `NVARCHAR(MAX) NULL` |
| `MucDo` | Mức độ | `NVARCHAR(255) NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### DON_THUOC

Phân hệ: `DonThuoc` — Đơn thuốc.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaDT` | Mã đơn thuốc | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DON_THUOC] PRIMARY KEY` |
| `MaLS` | Lần khám | `INT NOT NULL` |
| `MaBS` | Bác sĩ | `INT NOT NULL` |
| `NgayKe` | Ngày kê | `DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME())` |
| `ChanDoan` | Chẩn đoán | `NVARCHAR(MAX) NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### CHI_TIET_DON_THUOC

Phân hệ: `DonThuoc` — Chi tiết đơn thuốc.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaCTDT` | Mã chi tiết đơn thuốc | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CHI_TIET_DON_THUOC] PRIMARY KEY` |
| `MaDT` | Đơn thuốc | `INT NOT NULL` |
| `TenThuoc` | Tên thuốc | `NVARCHAR(255) NOT NULL` |
| `LieuDung` | Liều dùng | `NVARCHAR(255) NULL` |
| `SoLuong` | Số lượng | `DECIMAL(18,3) NOT NULL` |
| `DonViTinh` | Đơn vị tính | `NVARCHAR(255) NOT NULL` |
| `SoNgayDung` | Số ngày dùng | `INT NOT NULL` |
| `HuongDan` | Hướng dẫn | `NVARCHAR(MAX) NULL` |

### DICH_VU

Phân hệ: `DichVu` — Dịch vụ.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaDV` | Mã dịch vụ | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DICH_VU] PRIMARY KEY` |
| `TenDV` | Tên dịch vụ | `NVARCHAR(255) NOT NULL` |
| `MaCK` | Chuyên khoa | `INT NULL` |
| `DonGia` | Đơn giá | `DECIMAL(18,2) NOT NULL` |
| `MoTa` | Mô tả | `NVARCHAR(MAX) NULL` |
| `ThoiGianDuKien` | Thời gian dự kiến | `INT NOT NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |

### VAI_TRO

Phân hệ: `TaiKhoan` — Vai trò.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaVaiTro` | Mã vai trò | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_VAI_TRO] PRIMARY KEY` |
| `TenVaiTro` | Tên vai trò | `NVARCHAR(255) NOT NULL` |
| `MoTa` | Mô tả | `NVARCHAR(MAX) NULL` |

### CHUC_VU

Phân hệ: `NhanSu` — Chức vụ.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaChucVu` | Mã chức vụ | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CHUC_VU] PRIMARY KEY` |
| `TenChucVu` | Tên chức vụ | `NVARCHAR(255) NOT NULL` |
| `MoTa` | Mô tả | `NVARCHAR(MAX) NULL` |

### TAI_KHOAN

Phân hệ: `TaiKhoan` — Tài khoản.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaTK` | Mã tài khoản | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_TAI_KHOAN] PRIMARY KEY` |
| `TenDangNhap` | Tên đăng nhập | `NVARCHAR(100) NOT NULL` |
| `MatKhau` | Mật khẩu mới | `NVARCHAR(255) NOT NULL` |
| `MaVaiTro` | Vai trò | `INT NOT NULL` |
| `MaNV` | Nhân viên | `INT NOT NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |

### NHAN_VIEN

Phân hệ: `NhanSu` — Nhân viên.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaNV` | Mã nhân viên | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_NHAN_VIEN] PRIMARY KEY` |
| `HoTen` | Họ tên | `NVARCHAR(255) NOT NULL` |
| `GioiTinh` | Giới tính | `NVARCHAR(50) NULL` |
| `NgaySinh` | Ngày sinh | `DATE NULL` |
| `SDT` | Số điện thoại | `NVARCHAR(50) NULL` |
| `DiaChi` | Địa chỉ | `NVARCHAR(255) NULL` |
| `MaChucVu` | Chức vụ | `INT NOT NULL` |
| `MaCK` | Chuyên khoa | `INT NULL` |
| `BangCap` | Bằng cấp | `NVARCHAR(255) NULL` |
| `ChungChiHanhNghe` | Chứng chỉ hành nghề | `NVARCHAR(255) NULL` |
| `NgayCapChungChi` | Ngày cấp chứng chỉ | `DATE NULL` |
| `NoiCapChungChi` | Nơi cấp chứng chỉ | `NVARCHAR(255) NULL` |
| `HanChungChi` | Hạn chứng chỉ | `DATE NULL` |
| `NgayVaoLam` | Ngày vào làm | `DATE NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |

### KHOA

Phân hệ: `DichVu` — Khoa.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaKhoa` | Mã khoa | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_KHOA] PRIMARY KEY` |
| `TenKhoa` | Tên khoa | `NVARCHAR(255) NOT NULL` |
| `DienThoai` | Điện thoại | `NVARCHAR(50) NULL` |
| `MoTa` | Mô tả | `NVARCHAR(MAX) NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |

### CHUYEN_KHOA

Phân hệ: `DichVu` — Chuyên khoa.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaCK` | Mã chuyên khoa | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CHUYEN_KHOA] PRIMARY KEY` |
| `MaKhoa` | Khoa | `INT NOT NULL` |
| `TenCK` | Tên chuyên khoa | `NVARCHAR(255) NOT NULL` |
| `MoTa` | Mô tả | `NVARCHAR(MAX) NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |

### KE_HOACH_DIEU_TRI

Phân hệ: `KhamDieuTri` — Kế hoạch điều trị.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaKHDT` | Mã kế hoạch điều trị | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_KE_HOACH_DIEU_TRI] PRIMARY KEY` |
| `MaLS` | Lần khám | `INT NOT NULL` |
| `MaBN` | Bệnh nhân | `INT NOT NULL` |
| `MaBS` | Bác sĩ | `INT NOT NULL` |
| `NgayLap` | Ngày lập | `DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME())` |
| `NgayBatDau` | Ngày bắt đầu | `DATE NULL` |
| `NgayDuKienKetThuc` | Ngày dự kiến kết thúc | `DATE NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### CHI_TIET_KE_HOACH

Phân hệ: `KhamDieuTri` — Chi tiết kế hoạch.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaCTKH` | Mã chi tiết kế hoạch | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CHI_TIET_KE_HOACH] PRIMARY KEY` |
| `MaKHDT` | Kế hoạch điều trị | `INT NOT NULL` |
| `MaDV` | Dịch vụ | `INT NOT NULL` |
| `LanThu` | Lần thứ | `INT NOT NULL` |
| `DonGia` | Đơn giá | `DECIMAL(18,2) NOT NULL` |
| `ThanhTien` | Thành tiền | `DECIMAL(18,2) NOT NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### THIET_BI

Phân hệ: `ThietBi` — Thiết bị.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaTB` | Mã thiết bị | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_THIET_BI] PRIMARY KEY` |
| `TenTB` | Tên thiết bị | `NVARCHAR(255) NOT NULL` |
| `LoaiTB` | Loại thiết bị | `NVARCHAR(255) NULL` |
| `SoSerial` | Số serial | `NVARCHAR(50) NULL` |
| `NgayMua` | Ngày mua | `DATE NULL` |
| `MaNCC` | Nhà cung cấp | `INT NULL` |
| `PhongSuDung` | Phòng sử dụng | `NVARCHAR(255) NULL` |
| `SoLuong` | Số lượng | `DECIMAL(18,3) NOT NULL` |
| `TinhTrang` | Tình trạng | `NVARCHAR(255) NOT NULL` |
| `NgayBaoHanh` | Ngày bảo hành | `DATE NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### BAO_TRI_THIET_BI

Phân hệ: `BaoTri` — Bảo trì.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaBT` | Mã lần bảo trì | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_BAO_TRI_THIET_BI] PRIMARY KEY` |
| `MaTB` | Thiết bị | `INT NOT NULL` |
| `NgayBaoTri` | Ngày bảo trì | `DATE NULL` |
| `LoaiBaoTri` | Loại bảo trì | `NVARCHAR(255) NULL` |
| `NoiDung` | Nội dung | `NVARCHAR(MAX) NOT NULL` |
| `ChiPhi` | Chi phí | `DECIMAL(18,2) NOT NULL` |
| `NguoiThucHien` | Người thực hiện | `NVARCHAR(255) NULL` |
| `NgayBaoTriTiepTheo` | Ngày bảo trì tiếp theo | `DATE NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### NHA_CUNG_CAP

Phân hệ: `NhaCungCap` — Nhà cung cấp.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaNCC` | Mã nhà cung cấp | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_NHA_CUNG_CAP] PRIMARY KEY` |
| `TenNCC` | Tên nhà cung cấp | `NVARCHAR(255) NOT NULL` |
| `SDT` | Số điện thoại | `NVARCHAR(50) NULL` |
| `Email` | Email | `NVARCHAR(255) NULL` |
| `DiaChi` | Địa chỉ | `NVARCHAR(255) NULL` |
| `NguoiLienHe` | Người liên hệ | `NVARCHAR(255) NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |

### PHONG_KHAM

Phân hệ: `DichVu` — Phòng khám.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaPhong` | Mã phòng khám | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_PHONG_KHAM] PRIMARY KEY` |
| `TenPhong` | Tên phòng | `NVARCHAR(255) NOT NULL` |
| `MaCK` | Chuyên khoa | `INT NULL` |
| `TinhTrang` | Tình trạng | `NVARCHAR(255) NOT NULL` |

### KHO

Phân hệ: `VatTu` — Kho.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaKho` | Mã kho | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_KHO] PRIMARY KEY` |
| `TenKho` | Tên kho | `NVARCHAR(255) NOT NULL` |
| `MaNV` | Nhân viên phụ trách | `INT NOT NULL` |
| `LoaiKho` | Loại kho | `NVARCHAR(255) NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |

### VAT_TU

Phân hệ: `VatTu` — Vật tư.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaVT` | Mã vật tư | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_VAT_TU] PRIMARY KEY` |
| `TenVT` | Tên vật tư | `NVARCHAR(255) NOT NULL` |
| `DonViTinh` | Đơn vị tính | `NVARCHAR(255) NOT NULL` |
| `MaLVT` | Loại vật tư | `INT NOT NULL` |
| `MucTonToiThieu` | Mức tồn tối thiểu | `DECIMAL(18,3) NOT NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |

### LOAI_VAT_TU

Phân hệ: `VatTu` — Loại vật tư.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaLVT` | Mã loại vật tư | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_LOAI_VAT_TU] PRIMARY KEY` |
| `TenLVT` | Tên loại vật tư | `NVARCHAR(255) NOT NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### LO_VAT_TU

Phân hệ: `VatTu` — Lô vật tư.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaLo` | Mã lô vật tư | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_LO_VAT_TU] PRIMARY KEY` |
| `MaVT` | Vật tư | `INT NOT NULL` |
| `MaNCC` | Nhà cung cấp | `INT NOT NULL` |
| `MaKho` | Kho | `INT NOT NULL` |
| `NgayNhap` | Ngày nhập | `DATE NOT NULL` |
| `HanSuDung` | Hạn sử dụng | `DATE NULL` |
| `SoLuongNhap` | Số lượng nhập | `DECIMAL(18,3) NOT NULL` |
| `DonGiaNhap` | Đơn giá nhập | `DECIMAL(18,2) NOT NULL` |

### CA_LAM

Phân hệ: `CaLamViec` — Ca làm.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaCa` | Mã ca làm | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CA_LAM] PRIMARY KEY` |
| `GioBatDau` | Giờ bắt đầu | `TIME(0) NOT NULL` |
| `GioKetThuc` | Giờ kết thúc | `TIME(0) NOT NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### LICH_LAM_VIEC

Phân hệ: `CaLamViec` — Lịch làm việc.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaLLV` | Mã lịch làm việc | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_LICH_LAM_VIEC] PRIMARY KEY` |
| `MaNV` | Nhân viên | `INT NOT NULL` |
| `MaCa` | Ca làm | `INT NOT NULL` |
| `NgayLam` | Ngày làm | `DATE NOT NULL` |
| `MaPhong` | Phòng khám | `INT NOT NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |

### DON_XIN_NGHI

Phân hệ: `CaLamViec` — Đơn xin nghỉ.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaDonNghi` | Mã đơn xin nghỉ | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DON_XIN_NGHI] PRIMARY KEY` |
| `MaNV` | Nhân viên | `INT NOT NULL` |
| `TuNgay` | Từ ngày | `DATE NOT NULL` |
| `DenNgay` | Đến ngày | `DATE NOT NULL` |
| `LyDo` | Lý do | `NVARCHAR(MAX) NOT NULL` |
| `NgayGui` | Ngày gửi | `DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME())` |
| `TrangThaiDuyet` | Trạng thái duyệt | `NVARCHAR(50) NOT NULL` |
| `MaNVDuyet` | Nhân viên duyệt | `INT NULL` |
| `NgayDuyet` | Ngày duyệt | `DATETIME2(0) NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### PHAN_CONG_THAY_THE

Phân hệ: `CaLamViec` — Phân công thay thế.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaPCTT` | Mã phân công thay thế | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_PHAN_CONG_THAY_THE] PRIMARY KEY` |
| `MaDonNghi` | Đơn xin nghỉ | `INT NOT NULL` |
| `MaLLVGoc` | Lịch làm việc gốc | `INT NOT NULL` |
| `MaNVThayThe` | Nhân viên thay thế | `INT NOT NULL` |
| `MaLLVThayThe` | Lịch làm việc thay thế | `INT NULL` |
| `NgayXuLy` | Ngày xử lý | `DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME())` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |
| `LyDoThayThe` | Lý do thay thế | `NVARCHAR(MAX) NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### LICH_KHAM

Phân hệ: `LichKham` — Lịch khám.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaLich` | Mã lịch khám | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_LICH_KHAM] PRIMARY KEY` |
| `MaBN` | Bệnh nhân | `INT NOT NULL` |
| `MaBS` | Bác sĩ | `INT NULL` |
| `NgayGioKham` | Ngày giờ khám | `DATETIME2(0) NOT NULL` |
| `MaPhong` | Phòng khám | `INT NULL` |
| `LyDoKham` | Lý do khám | `NVARCHAR(MAX) NULL` |
| `MaDV` | Dịch vụ | `INT NOT NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |
| `NgayTao` | Ngày tạo | `DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME())` |

### HOA_DON

Phân hệ: `HoaDon` — Hóa đơn.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaHD` | Mã hóa đơn | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_HOA_DON] PRIMARY KEY` |
| `MaBN` | Bệnh nhân | `INT NOT NULL` |
| `MaLS` | Lần khám | `INT NULL` |
| `NgayLap` | Ngày lập | `DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME())` |
| `TongTien` | Tổng tiền | `DECIMAL(18,2) NOT NULL` |
| `GiamGia` | Giảm giá (số tiền) | `DECIMAL(18,2) NOT NULL DEFAULT (0)` |
| `ThanhTien` | Thành tiền | `DECIMAL(18,2) NOT NULL` |
| `TrangThai` | Trạng thái | `NVARCHAR(50) NOT NULL` |

### CHI_TIET_HOA_DON

Phân hệ: `HoaDon` — Chi tiết hóa đơn.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaCTHD` | Mã chi tiết hóa đơn | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CHI_TIET_HOA_DON] PRIMARY KEY` |
| `MaHD` | Hóa đơn | `INT NOT NULL` |
| `MaKHDT` | Kế hoạch điều trị | `INT NULL` |
| `NoiDung` | Nội dung | `NVARCHAR(MAX) NOT NULL` |
| `ThanhTien` | Thành tiền | `DECIMAL(18,2) NOT NULL` |

### THANH_TOAN

Phân hệ: `HoaDon` — Thanh toán.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaTT` | Mã thanh toán | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_THANH_TOAN] PRIMARY KEY` |
| `MaHD` | Hóa đơn | `INT NOT NULL` |
| `NgayThanhToan` | Ngày thanh toán | `DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME())` |
| `SoTien` | Số tiền | `DECIMAL(18,2) NOT NULL` |
| `PhuongThuc` | Phương thức | `NVARCHAR(255) NOT NULL` |
| `NguoiThu` | Người thu | `NVARCHAR(255) NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### GIAO_DICH_KHO

Phân hệ: `NhapXuatVatTu` — Giao dịch kho.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaGD` | Mã giao dịch kho | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_GIAO_DICH_KHO] PRIMARY KEY` |
| `MaLo` | Lô vật tư | `INT NOT NULL` |
| `MaKho` | Kho | `INT NOT NULL` |
| `NgayGD` | Ngày giao dịch | `DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME())` |
| `SoLuongLo` | Biến động số lượng lô | `DECIMAL(18,3) NOT NULL` |
| `LyDo` | Lý do | `NVARCHAR(MAX) NOT NULL` |
| `GhiChu` | Ghi chú | `NVARCHAR(MAX) NULL` |

### HOA_DON_KHO

Phân hệ: `NhapXuatVatTu` — Hóa đơn kho.

| Cột SQL | Nhãn | Kiểu / ràng buộc |
|---|---|---|
| `MaHDK` | Mã hóa đơn kho | `INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_HOA_DON_KHO] PRIMARY KEY` |
| `MaGD` | Giao dịch kho | `INT NOT NULL` |
| `PhuongThucThanhToan` | Phương thức thanh toán | `NVARCHAR(255) NOT NULL` |
| `DonGia` | Đơn giá | `DECIMAL(18,2) NOT NULL` |
| `ThanhTien` | Thành tiền | `DECIMAL(18,2) NOT NULL` |
| `NgayLap` | Ngày lập | `DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME())` |

## Thuốc, nhật ký và nguồn tài chính

Thuốc là nhóm của VAT_TU thông qua LOAI_VAT_TU, lô thuốc dùng LO_VAT_TU. Không có bảng THUOC mới. Nhật ký dùng mô hình trình bày riêng vì SQL chưa định nghĩa bảng. Thu–chi kho dùng chứng từ HOA_DON_KHO, tách khỏi HOA_DON/THANH_TOAN của bệnh viện. UI nhận phân loại thu/chi riêng từ tầng dữ liệu tương lai, không sửa schema hoặc tự quy đổi nhập/xuất thành dòng tiền.
