# Ma trận phạm vi đồng bộ WPF

Ngày kiểm: 16/11/2026. Ma trận phân biệt đăng ký mã nguồn với nghiệm thu giao diện/hai máy. Không dùng build thành công để kết luận đã nghiệm thu hai máy vật lý.

Bổ sung ngày 17/11: [ma trận đường chạy và lỗ phụ thuộc đã sửa](DONG_BO_RUNTIME_MA_TRAN.md), gồm child Hướng dẫn thuốc có adapter riêng và hồi quy Nhật ký/LOGIN/LOGOUT.

## Quy ước

- **Prepared**: chụp tham số trên Dispatcher, đọc BUS/DAL và vật hóa POCO/DataTable ở nền, sau đó áp dụng trên Dispatcher. Không đọc/đổi control hoặc ObservableCollection từ luồng nền.
- **Cache**: dùng đăng ký `HoTroGiaoDien.Observe` và cache `DuLieuGiaoDien` của bộ điều phối; tránh đăng ký adapter trùng.
- **Hoãn**: chủ động giữ form/giao dịch đang mở. Lượt thay đổi vẫn chờ để áp dụng sau khi kết thúc thao tác.
- **Tĩnh + build**: đã đọc đường gọi, kiểm ánh xạ bảng/điều kiện hoãn và biên dịch; chưa tương đương kiểm giao diện tương tác hoặc hai tiến trình SQL.
- Mọi adapter chịu thêm chặn modal, `IClinicalPendingWork`, DataGrid đang sửa, trạng thái workspace và generation/interaction guard của bộ điều phối.

## Các phân hệ tải riêng

| View | Kết nối và hàm đọc/áp dụng | Bảng phụ thuộc | Giữ nội dung/lựa chọn | Bằng chứng adapter |
| --- | --- | --- | --- | --- |
| `NhanSu` | Prepared → `LoadDuLieuNhanVien`, `LoadDuLieuChucVu`, `LoadDuLieuChuyenKhoa` | NHAN_VIEN, CHUC_VU, CHUYEN_KHOA, DON_XIN_NGHI | Hoãn khi có nội dung form nhân viên/chức vụ hoặc lựa chọn danh mục đang nhập; giữ mã nhân viên/chức vụ và tìm kiếm/trạng thái | Tĩnh + build |
| `ChungChiNhanVien` | Prepared → `LoadDanhSachNhanVien`, `LoadDanhSachChungChi` | CHUNG_CHI_NHAN_VIEN, NHAN_VIEN, CHUC_VU, CHUYEN_KHOA | Giữ MaNV, MaCC, bộ lọc chứng chỉ; modal thêm/sửa hoãn toàn workspace | Tĩnh + build |
| `CaLamViec` | Prepared → `LoadDuLieu`, `CapNhatThongBaoDonXinNghi` | CA_LAM, CA_THEO_NGAY, LICH_LAM_VIEC, NHAN_VIEN, PHONG, CHUC_VU, CHUYEN_KHOA, DON_XIN_NGHI, PHAN_CONG_THAY_THE, XAC_NHAN_DOI_LICH_KHAM, LICH_KHAM | Giữ tuần/bộ lọc; hoãn overlay duyệt/từ chối/tạo/cài ca/đơn nghỉ; không gọi `dongBoLichKhamVaoCaLam` | Tĩnh + build |
| `ChamCong` | Prepared → `hienThiDuLieu` | CHAM_CONG, NHAT_KY_DUYET_CHAM_CONG, LICH_LAM_VIEC, NHAN_VIEN, CHUC_VU, CA_LAM, DON_XIN_NGHI, BANG_LUONG | So trạng thái giờ vào/ra, lý do, kết quả với mốc nạp form; giữ MaLLV và kỳ/bộ lọc; không tự duyệt | Tĩnh + build |
| `BangLuong` | Prepared → `hienThiDuLieu` | BANG_LUONG, CHI_TIET_LUONG, CHI_TIET_HOA_HONG_LUONG, CHAM_CONG, LICH_LAM_VIEC, NHAN_VIEN, CHUC_VU, CHINH_SACH_LUONG, DON_XIN_NGHI, RHM_NGAY_PHEP_NAM, HOA_DON, THANH_TOAN, DICH_VU_THUC_HIEN | Giữ MaBL, kỳ, chức vụ/trạng thái; hoãn xem phiếu; chỉ đọc lương đã lưu/đối soát, không tính/chốt | Tĩnh + build |
| `ChinhSachLuong` | Prepared → `hienThiDuLieu` | CHINH_SACH_LUONG, CHUC_VU | Hoãn khi form cho phép lưu nháp, kể cả chưa gõ; giữ MaCSL và tìm kiếm | Tĩnh + build |
| `Kho` | Prepared → `hienThiDuLieu`, `TaiTonKho` | KHO, NHAN_VIEN, HANG_HOA, LOAI_HANG_HOA, VAT_TU, THUOC, LO_HANG_HOA, PHIEU_NHAP, CHI_TIET_PHIEU_NHAP, PHIEU_XUAT, CHI_TIET_PHIEU_XUAT | Giữ kho, MaHH, tìm kiếm/trạng thái; nguồn lô/tồn vật hóa trước khi áp dụng | Tĩnh + build |
| `DanhMucHangHoa` | Prepared → `LoadDuLieu` | HANG_HOA, LOAI_HANG_HOA, VAT_TU, THUOC, THIET_BI, LO_HANG_HOA, KHO, PHIEU_NHAP, CHI_TIET_PHIEU_NHAP, PHIEU_XUAT, CHI_TIET_PHIEU_XUAT | Hoãn `dangThem`/`dangSua`; giữ tab, MaHH, loại và trạng thái lọc | Tĩnh + build |
| `ThietBi` | Prepared → `LoadDuLieuThietBi` | THIET_BI, BAO_TRI_THIET_BI, THANH_LY_THIET_BI, HANG_HOA, LOAI_HANG_HOA, PHONG, KHOA, NHA_CUNG_CAP | Thay Observe cũ bằng đúng một adapter có `dangSua`; giữ MaTB, phòng và trạng thái lọc | Tĩnh + build |
| `NhapXuatVatTu` | Prepared qua `DongBoNguonKho` → `TaiDuLieu` | KHO, NHAN_VIEN, HANG_HOA, LO_HANG_HOA, KHOA, PHONG, NHA_CUNG_CAP, HOA_DON_NHAP, PHIEU_NHAP, CHI_TIET_PHIEU_NHAP, PHIEU_XUAT, CHI_TIET_PHIEU_XUAT | Giữ loại/mã phiếu và bộ lọc; các tab chứng từ con có adapter riêng | Tĩnh + build |
| `NhapKho` | Prepared qua `DongBoNguonKho` → `TaiDuLieu` | KHO, NHAN_VIEN, NHA_CUNG_CAP, HANG_HOA, LOAI_HANG_HOA, VAT_TU, THUOC, LO_HANG_HOA, HOA_DON_NHAP, PHIEU_NHAP, CHI_TIET_PHIEU_NHAP, CHI_TIET_PHIEU_XUAT | Hoãn suốt khi `formTaoMoi` mở (thêm/sửa); giữ mã phiếu và bộ lọc; không lưu/ghi sổ | Tĩnh + build |
| `XuatKho` | Prepared qua `DongBoNguonKho` → `TaiDuLieu` | KHO, NHAN_VIEN, KHOA, PHONG, HANG_HOA, LO_HANG_HOA, PHIEU_NHAP, CHI_TIET_PHIEU_NHAP, PHIEU_XUAT, CHI_TIET_PHIEU_XUAT, DON_THUOC, CHI_TIET_DON_THUOC | Hoãn `duocSua`; giữ MaPX và bộ lọc; không lưu/ghi sổ | Tĩnh + build |
| `HoaDonNhaCungCap` | Prepared qua `DongBoNguonKho` → `TaiDuLieu` | HOA_DON_NHAP, NHA_CUNG_CAP, NHAN_VIEN, PHIEU_NHAP, CHI_TIET_PHIEU_NHAP | Hoãn tạo mới hoặc `CoThayDoiChuaLuu`; giữ MaHDN, nhà cung cấp/bộ lọc; không xác nhận/hủy | Tĩnh + build |
| `DichVu` | Prepared → `LoadRows`/`ShowForm` | Các bảng trong `DanhMucHeThong.TatCa`: KHOA, CHUYEN_KHOA, PHONG, DICH_VU, DM_TINH_TRANG_BENH, DICH_VU_TINH_TRANG, CHUC_VU, LOAI_HANG_HOA, NHOM_THUOC_KE_DON, NGAY_NGHI_LE | So giá trị control với mốc form; đọc trước cả lựa chọn danh mục cha; giữ danh mục, tìm kiếm/trạng thái và khóa đơn/ghép | Tĩnh + build |
| `PhieuHuongDan` | Prepared → `hienThiDuLieu` | PHIEU_KHAM_BENH, BENH_NHAN, HO_SO_BENH_AN, LICH_SU_KHAM_BENH, LICH_KHAM, NHAN_VIEN, PHONG, DICH_VU, AI_KET_QUA_TAO_PHIEU | Giữ MaPhieu, tìm kiếm, trạng thái và giới hạn hôm nay; không tự tiếp nhận/in/chuyển khám | Tĩnh + build |
| `TaiKhoan` | Prepared → `hienThiDuLieu`, editor nhúng và `hienThiPhanQuyen` | TAI_KHOAN, VAI_TRO, PHAN_QUYEN_VAI_TRO, NHAN_VIEN, KHOA, PHONG | Giữ MaTK/MaVaiTro/bộ lọc; hoãn tạo mới, thay đổi editor hoặc quyền; dữ liệu editor và quyền cũng được đọc nền | Tĩnh + build |
| `NhatKy` | Prepared → `Load(false)` | NHAT_KY_THAO_TAC, NHAT_KY_PHIEN, PHIEN_DANG_NHAP, TAI_KHOAN, NHAN_VIEN | Giữ trang, bộ lọc đã áp dụng và MaNK; heartbeat chỉ liên quan màn hình nhật ký | Tĩnh + build |
| `XinNghiViec` | Prepared → `TaiQuyPhep`, `CapNhatDuKienPhep` | RHM_NGAY_PHEP_NAM, RHM_CAU_HINH_QUY_PHEP, DON_XIN_NGHI, NHAN_VIEN, LICH_LAM_VIEC, NGAY_NGHI_LE | Chỉ đổi số dư/ước tính; không đổi ngày, lý do, bàn giao, trạng thái hoặc gửi đơn nháp | Tĩnh + build |
| `DonXinNghiQuanLy` | Prepared → `TaiDuLieu` | DON_XIN_NGHI, NHAN_VIEN, CHUYEN_KHOA, CHUC_VU, PHONG, CA_LAM, LICH_LAM_VIEC, LICH_KHAM, PHAN_CONG_THAY_THE, XAC_NHAN_DOI_LICH_KHAM, RHM_NGAY_PHEP_NAM, RHM_CAU_HINH_QUY_PHEP, NGAY_NGHI_LE | Giữ MaDonNghi/bộ lọc; hoãn overlay chi tiết và đơn khẩn cấp (có thao tác duyệt/phân công) | Tĩnh + build |
| `QuyPhepNhanSu` | Prepared → `TaiDuLieu` và chi tiết ngày phép | RHM_NGAY_PHEP_NAM, RHM_CAU_HINH_QUY_PHEP, DON_XIN_NGHI, NHAN_VIEN, LICH_LAM_VIEC, NGAY_NGHI_LE | Giữ năm, MaNV, tìm kiếm; đọc trước cả chi tiết người đang chọn | Tĩnh + build |
| `AnhXquangLanKham` | Prepared → `TaiLai(..., xemTruoc:false)` | ANH_XQUANG_LAN_KHAM, PHAN_CONG_ANH_XQUANG, DICH_VU_THUC_HIEN, LICH_SU_KHAM_BENH, PHIEU_KHAM_BENH, BENH_NHAN, NHAN_VIEN, DICH_VU | Giữ MaAnh; hoãn upload/xử lý, ghi chú hoặc chọn kỹ thuật viên; chỉ metadata/kiểm tra kho, không gọi `MoAnh` ghi nhật ký ở nhịp đồng bộ | Tĩnh + build |
| `AnhXquangDuocGiao` | Prepared → danh sách `LayLuotDuocGiao` | PHAN_CONG_ANH_XQUANG, DICH_VU_THUC_HIEN, LICH_SU_KHAM_BENH, PHIEU_KHAM_BENH, BENH_NHAN, NHAN_VIEN, DICH_VU | Giữ MaLS và instance control ảnh con; không dựng lại trang; hoãn theo nháp ảnh con | Tĩnh + build |
| `TiepNhanKham` | Adapter có điều kiện hoãn khi form mở | BENH_NHAN, PHIEU_KHAM_BENH, LICH_KHAM, NHAN_VIEN, LICH_LAM_VIEC, PHONG, DICH_VU | Giữ toàn bộ người bệnh/lịch/bác sĩ/phòng/dịch vụ đến khi đóng form; không tự nhận bệnh | Tĩnh + build; cố ý hoãn |

## Màn hình dùng cache và màn hình con

| View/phạm vi | Cơ chế | Phạm vi bảng/điều kiện | Bằng chứng |
| --- | --- | --- | --- |
| `TongQuan` | Không cần adapter | Code-behind chỉ `InitializeComponent`, không có nguồn SQL động | Đọc mã nguồn |
| `BenhNhan`, `HoSoBenhNhan` | Cache/Observe | BENH_NHAN, HO_SO_BENH_AN và bảng lâm sàng liên quan theo quyền; form nhập và danh tính đang chọn do workspace bảo vệ | Tĩnh + build |
| `KhamDieuTri`, `ChiTietKhamDieuTri` | Cache/Observe | PHIEU_KHAM_BENH, LICH_SU_KHAM_BENH, kế hoạch/tình trạng/dịch vụ; nháp qua IClinicalPendingWork | Tĩnh + build |
| `KhamLamSang` | Prepared riêng tại view | Khám, kế hoạch, dịch vụ, thuốc, hóa đơn, bệnh nhân; hoãn toàn bộ nháp lâm sàng | Tĩnh + build |
| `DichVuThucHien` | Dữ liệu cache và editor lâm sàng | DICH_VU_THUC_HIEN, khám/ảnh; editor/giao dịch chờ được chặn | Tĩnh + build |
| `DonThuoc`, `ChiTietDonThuoc` | Cache/Observe | DON_THUOC, CHI_TIET_DON_THUOC, khám, thuốc/lô, trạng thái hóa đơn/bàn giao | Tĩnh + build |
| `PrescriptionHistoryDetail` | Adapter riêng tại view | Đơn thuốc, dòng thuốc, bàn giao, thanh toán/hóa đơn; hoãn nguồn đang chờ và xác nhận nhận thuốc | Tĩnh + build |
| `KeDonThuocEditor`, `HuongDanThuocEditor`, `BanGiaoThuocEditor`, `BangTinhPhiEditor`, `ThucHienDichVuEditor` | Hoãn khi đang nhập/chờ xử lý | Form/nghiệp vụ con dùng cache và quy tắc pending; không tự lưu, thu, giao hoặc chốt | Tĩnh + build |
| `LichKham` | Cache/Observe; giữ bộ lọc/lựa chọn | LICH_KHAM, NHAN_VIEN, CHUYEN_KHOA, PHONG, ca và nghỉ liên quan | Tĩnh + build |
| `ThemLichKham`, `CuaSoLichKham`, `ChiTietLichKham` | Hoãn form/modal | Không tải đè ngày/bác sĩ/phòng/lý do đang nhập | Tĩnh + build |
| `HoaDon` | Cache/Observe | HOA_DON, CHI_TIET_HOA_DON, THANH_TOAN, khám/dịch vụ/thuốc; pending thanh toán chặn refresh | Tĩnh + build |
| `BaoTri` và thanh lý | Cache/Observe | THIET_BI, BAO_TRI_THIET_BI, THANH_LY_THIET_BI, nguồn danh mục; modal nhập liệu hoãn | Tĩnh + build |
| `NhaCungCap` | Cache/Observe | NHA_CUNG_CAP, hóa đơn nhập/lô/thiết bị liên quan; modal nhập liệu hoãn | Tĩnh + build |
| `BaoCao` | RegisterAsync tại view | Nguồn theo báo cáo hiện tại; giữ kỳ/bộ lọc; chặn khi đang tải | Tĩnh + build |
| `TroLyAI` | Giữ phiên chat cục bộ | Không đăng ký phát lại hội thoại/AI_TIN_NHAN; bản khai đã lưu nhận qua luồng tiếp nhận AI | Đọc mã nguồn |
| `TiepNhanAI` | Window modal, hoãn | Các bản khai AI đã lưu và lựa chọn tiếp nhận; không refresh Window khi đang đối chiếu/nhập xác nhận; mở lại/tải lại chủ động đọc trạng thái mới | Đọc mã nguồn; cố ý hoãn |
| `CuaSoTaiKhoan` | Prepared cùng adapter `TaiKhoan` | Đọc trước tài khoản/vai trò/nhân viên/khoa/phòng; mốc nội dung riêng bảo vệ editor nhúng | Tĩnh + build |
| `ChiTietNhanSu`, `ChiTietChungChi`, `CuaSoThemChungChi`, các Window thêm/sửa/in/xem ảnh | Hoãn bởi modal/cửa sổ đang mở | Giữ nội dung/bản gốc lúc bắt đầu sửa; đóng cửa sổ rồi áp dụng dữ liệu còn chờ | Đọc mã nguồn |
| `ChiTietHoSo`, `CuaSoBenhNhan`, `CuaSoKhamDieuTri`, `CuaSoDonThuoc`, `CuaSoDichVu`, `CuaSoHoaDon`, `CuaSoPhieuHuongDan`, `CuaSoNhapXuatVatTu`, `CuaSoBaoTri`, `CuaSoBangLuong`, `CuaSoNhaCungCap` | Form/modal workspace; Observe nếu có | Không đăng ký tải đè form nhập; phần hiển thị cache cập nhật sau khi workspace hết pending | Tĩnh + build |
| `VatTu` | View legacy không có lối khởi tạo trong điều hướng hiện tại | Không có `new VatTu()` trong mã WPF; đường đang dùng là `Kho` + `DanhMucHangHoa`; nếu phục hồi điều hướng legacy cần adapter riêng với `dangThem`/`dangSua` | Tìm kiếm mã nguồn; deferred legacy |
| `ScheduleView`, `ChartView`, các document/invoice/prescription print controls | Theo nguồn cache hoặc tài liệu đã dựng của view cha | Không mở một vòng đọc SQL/đăng ký đồng bộ riêng cho control trình bày | Đọc mã nguồn |

## Kiểm đã chạy và giới hạn bằng chứng

- `dotnet build RangHamMat.Wpf/RangHamMat.Wpf.csproj --no-restore -v quiet`: thành công, 0 lỗi (lượt biên dịch có 118 cảnh báo hiện hữu). Log cục bộ: `obj/sync-adapter-build.log`.
- Kiểm tĩnh 23/23 view có `DangKyDongBoManHinh()` ngay sau `InitializeComponent()`, có partial adapter tường minh; 22 adapter Prepared và một form tiếp nhận cố ý hoãn.
- Kiểm nguồn adapter không có reflection, `Activator.CreateInstance`, `TryReceive`, upload/ẩn ảnh hoặc `dongBoLichKhamVaoCaLam`.
- Đã phát hiện đường đọc nhân viên cũ thực hiện UPDATE trạng thái nghỉ. Bổ sung `BUS_NHANVIEN.DocDanhSachKhongGhi`/`DAL_NHANVIEN.DocDanhSachKhongGhi` thuần SELECT cho prefetch; tải chủ động giữ hành vi nghiệp vụ cũ qua `DongBoChuanBi.NhanVien`.
- Catch tải tự động phát lại exception trước khi xóa dữ liệu/mở MessageBox, cho phép bộ điều phối giữ pending và thử lại; lỗi tải chủ động vẫn theo luồng hiện có.
- Kiểm tích hợp cùng nhánh đã báo pass: 5 tình huống polling thật, giữ nháp nhân sự và cập nhật/xóa cache nhà cung cấp trên database test riêng, 21 kiểm SQL Change Tracking, 47 hồi quy lâm sàng, 2 kiểm mở các màn hình WPF và kiểm trạng thái AI. Phạm vi này không chứng minh mọi đường tương tác của từng adapter hoặc nghiệm thu mạng hai máy vật lý.
- Các observer lâm sàng cũ có thể còn lời gọi SQL đồng bộ ngắn sau khi cache đã được chuẩn bị ở nền; đây là giới hạn của đường tải cũ, không coi chúng là toàn bộ đường apply đã loại SQL. Các adapter Prepared tải riêng trong bảng trên dùng dữ liệu vật hóa để áp dụng.
- Chưa thực hiện nghiệm thu giao diện tương tác cho từng hàng hoặc hai máy vật lý trong phần kiểm adapter này. Các kiểm runtime/SQL và hướng dẫn triển khai được tổng hợp riêng trong `DONG_BO_GIUA_CAC_MAY.md`; cần ghi đúng cấp bằng chứng của chúng.
