// AI bổ sung kiểm tra nhập liệu: bổ sung nhãn tiếng Việt dùng trong thông báo nhập sai.
// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung giải thích nhãn trường dữ liệu và cách xác định trường thuộc bản ghi/phạm vi hồ sơ để hiển thị đúng trên form.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
namespace RangHamMat.Controls;
// Tên cột/nhãn được đối chiếu schema SQL Server đang dùng và tài liệu database v8.
/// <summary>
/// Từ điển bảng/cột và nhãn tiếng Việt, kèm cột chủ sở hữu giúp cơ chế form khóa quan hệ cha khi sửa.
/// </summary>
public static class TruongDuLieu
{
    /// <summary>
    /// Tra cột liên kết cha của từng loại hồ sơ để khóa lại khi chỉnh sửa, tránh chuyển bản ghi sang một hồ sơ khác.
    /// </summary>
    public static string OwnerField(string table) => table switch { "HO_SO_BENH_AN"=>"MaBN", "PHIEU_KHAM_BENH"=>"MaHS", "LICH_SU_KHAM_BENH"=>"MaPhieu", "TINH_TRANG_BENH" or "KE_HOACH_DIEU_TRI" or "DON_THUOC" or "DICH_VU_THUC_HIEN"=>"MaLS", "CHI_TIET_KE_HOACH"=>"MaKHDT", "CHI_TIET_DON_THUOC"=>"MaDT", _=>"" };
    public static readonly Dictionary<string,Dictionary<string,string>> Tables = new()
    {
        ["NHA_CUNG_CAP"] = new() { ["MaNCC"] = "Mã nhà cung cấp", ["TenNCC"] = "Tên nhà cung cấp", ["SDT"] = "Số điện thoại", ["Email"] = "Email", ["DiaChi"] = "Địa chỉ", ["NguoiLienHe"] = "Người liên hệ", ["TrangThai"] = "Trạng thái" },
        ["BAO_TRI_THIET_BI"] = new() { ["MaBT"] = "Mã bảo trì", ["MaTB"] = "Thiết bị", ["MaNV"] = "Nhân viên phụ trách", ["NgayBatDau"] = "Ngày bắt đầu", ["NgayKetThuc"] = "Ngày kết thúc", ["NoiDung"] = "Nội dung bảo trì", ["ChiPhi"] = "Chi phí", ["NgayBaoTriTiepTheo"] = "Bảo trì tiếp theo", ["TrangThai"] = "Trạng thái", ["GhiChu"] = "Ghi chú" },
        ["BENH_NHAN"] = new() { ["MaBN"] = "Mã bệnh nhân", ["HoTenBN"] = "Họ tên bệnh nhân", ["GioiTinh"] = "Giới tính", ["NgaySinh"] = "Ngày sinh", ["SDT"] = "Số điện thoại", ["DiaChi"] = "Địa chỉ", ["NhomMau"] = "Nhóm máu", ["TienSuBenh"] = "Tiền sử bệnh", ["DiUng"] = "Dị ứng", ["TrangThai"] = "Trạng thái" },
        ["HO_SO_BENH_AN"] = new() { ["MaHS"] = "Mã hồ sơ", ["MaBN"] = "Bệnh nhân", ["NgayLap"] = "Ngày lập", ["LyDoKhamBanDau"] = "Lý do khám ban đầu", ["GhiChu"] = "Ghi chú", ["TrangThai"] = "Trạng thái" },
        ["LICH_KHAM"] = new() { ["MaLich"] = "Mã lịch khám", ["MaBN"] = "Bệnh nhân", ["MaBS"] = "Bác sĩ", ["MaCa"] = "Ca làm", ["MaPhong"] = "Phòng", ["MaDV"] = "Dịch vụ dự kiến", ["NgayGioKham"] = "Ngày giờ khám", ["ThoiLuongDuKien"] = "Thời lượng dự kiến (phút)", ["UuTien"] = "Ưu tiên", ["LyDoKham"] = "Lý do khám", ["LyDoThayDoi"] = "Lý do thay đổi", ["TrangThai"] = "Trạng thái", ["GhiChu"] = "Ghi chú", ["NgayTao"] = "Ngày tạo", ["NgayCapNhat"] = "Ngày cập nhật" },
        ["LICH_SU_KHAM_BENH"] = new() { ["MaLS"] = "Mã lần khám", ["MaPhieu"] = "Phiếu tiếp nhận", ["MaBS"] = "Bác sĩ khám", ["MaPhong"] = "Phòng khám", ["ThoiGianKham"] = "Thời gian khám", ["TrieuChung"] = "Triệu chứng", ["ChanDoan"] = "Chẩn đoán", ["KeHoachXuLy"] = "Kế hoạch xử lý", ["TrangThai"] = "Trạng thái", ["NgayXacNhan"] = "Ngày xác nhận", ["GhiChu"] = "Ghi chú" },
        ["TINH_TRANG_BENH"] = new() { ["MaTTB"] = "Mã tình trạng", ["MaLS"] = "Lần khám", ["LoaiTinhTrang"] = "Loại tình trạng", ["MoTaTinhTrang"] = "Mô tả tình trạng", ["GhiChu"] = "Ghi chú" },
        ["KE_HOACH_DIEU_TRI"] = new() { ["MaKHDT"] = "Mã kế hoạch", ["MaLS"] = "Lần khám khởi tạo", ["MaBS"] = "Bác sĩ lập", ["NgayLap"] = "Ngày lập", ["NgayBatDau"] = "Ngày bắt đầu", ["NgayDuKienKetThuc"] = "Ngày dự kiến kết thúc", ["TrangThai"] = "Trạng thái", ["GhiChu"] = "Ghi chú" },
        ["CHI_TIET_KE_HOACH"] = new() { ["MaCTKH"] = "Mã chi tiết kế hoạch", ["MaKHDT"] = "Kế hoạch điều trị", ["MaDV"] = "Dịch vụ", ["LanThu"] = "Lần thứ", ["SoLuongDuKien"] = "Số lượng dự kiến", ["DonGiaDuKien"] = "Đơn giá dự kiến", ["TrangThai"] = "Trạng thái", ["GhiChu"] = "Ghi chú" },
        ["DICH_VU_THUC_HIEN"] = new() { ["MaDVThucHien"] = "Mã thực hiện", ["MaLS"] = "Lần khám", ["MaCTKH"] = "Bước kế hoạch (nếu có)", ["MaDV"] = "Dịch vụ", ["MaNVThucHien"] = "Người thực hiện", ["ThoiGianThucHien"] = "Thời gian thực hiện", ["SoLuong"] = "Số lượng", ["DonGia"] = "Đơn giá thực hiện", ["KetQua"] = "Kết quả", ["TrangThai"] = "Trạng thái" },
        ["DON_THUOC"] = new() { ["MaDT"] = "Mã đơn thuốc", ["MaLS"] = "Lần khám", ["MaBS"] = "Bác sĩ kê", ["NgayKe"] = "Ngày kê", ["LyDoThayThe"] = "Lý do thay đổi chỉ định", ["TrangThai"] = "Trạng thái", ["GhiChu"] = "Ghi chú" },
        ["CHI_TIET_DON_THUOC"] = new() { ["MaCTDT"] = "Mã dòng thuốc", ["MaDT"] = "Đơn thuốc", ["MaThuoc"] = "Thuốc", ["LieuDung"] = "Liều dùng", ["SoLuong"] = "Số lượng", ["SoNgayDung"] = "Số ngày dùng", ["HuongDan"] = "Hướng dẫn" },
        ["PHIEU_KHAM_BENH"] = new() { ["MaPhieu"] = "MaPhieu", ["MaHS"] = "MaHS", ["MaLich"] = "MaLich", ["MaPhong"] = "MaPhong", ["MaBS"] = "MaBS", ["MaNVTiepNhan"] = "MaNVTiepNhan", ["MaDV"] = "MaDV", ["SoThuTu"] = "SoThuTu", ["ThoiGianTiepNhan"] = "ThoiGianTiepNhan", ["LyDoKham"] = "LyDoKham", ["TrangThai"] = "TrangThai", ["GhiChu"] = "GhiChu" },
        ["THUOC"] = new() { ["MaThuoc"] = "MaThuoc", ["MaHH"] = "MaHH", ["HamLuong"] = "HamLuong", ["MucTonToiThieu"] = "MucTonToiThieu" },
        ["HANG_HOA"] = new() { ["MaHH"] = "MaHH", ["TenHH"] = "TenHH", ["MaLoaiHH"] = "MaLoaiHH", ["DonViTinh"] = "DonViTinh", ["MoTa"] = "MoTa", ["TrangThai"] = "TrangThai" },
        ["NHAN_VIEN"] = new() { ["MaNV"] = "MaNV", ["HoTen"] = "HoTen", ["GioiTinh"] = "GioiTinh", ["NgaySinh"] = "NgaySinh", ["SDT"] = "SDT", ["DiaChi"] = "DiaChi", ["MaChucVu"] = "MaChucVu", ["MaCK"] = "MaCK", ["BangCap"] = "BangCap", ["NgayVaoLam"] = "NgayVaoLam", ["TrangThai"] = "TrangThai" },
        ["PHONG"] = new() { ["MaPhong"] = "MaPhong", ["TenPhong"] = "TenPhong", ["MaKhoa"] = "MaKhoa", ["LoaiPhong"] = "LoaiPhong", ["TinhTrang"] = "TinhTrang", ["TrangThai"] = "TrangThai" },
        ["CA_LAM"] = new() { ["MaCa"] = "MaCa", ["GioBatDau"] = "GioBatDau", ["GioKetThuc"] = "GioKetThuc", ["TrangThai"] = "TrangThai", ["GhiChu"] = "GhiChu" },
        ["DICH_VU"] = new() { ["MaDV"] = "MaDV", ["TenDV"] = "TenDV", ["NhomDV"] = "NhomDV", ["MaCK"] = "MaCK", ["DonGia"] = "DonGia", ["ThoiGianDuKien"] = "ThoiGianDuKien", ["MoTa"] = "MoTa", ["NgayApDung"] = "NgayApDung", ["TrangThai"] = "TrangThai" },
    };
    /// <summary>
    /// Tạo đối tượng TruongDuLieu. Từ điển bảng/cột và nhãn tiếng Việt, kèm cột chủ sở hữu giúp cơ chế form khóa quan hệ cha khi sửa.
    /// </summary>
    static TruongDuLieu()
    {
        Tables["BENH_NHAN"]["MaBHYT"]="Mã BHYT";
        Tables["KE_HOACH_DIEU_TRI"]["TongTienDuKien"]="Tổng dự kiến";
        Tables["CHI_TIET_KE_HOACH"]["ThanhTienDuKien"]="Thành tiền dự kiến";
        Tables["DICH_VU_THUC_HIEN"]["ThanhTien"]="Thành tiền";
        Tables["THUOC"]["DonGiaBan"]="Giá bán hiện hành";
        Add("HOA_DON","MaHD MaLS MaNV NgayLap TongTien GiamGia SoTienPhaiThu TrangThai NgayXacNhan");
        Add("CHI_TIET_HOA_DON","MaCTHD MaHD MaDVThucHien MaCTPX NoiDung DonGia ThanhTien");
        Add("THANH_TOAN","MaTT MaHD MaNV NgayThanhToan SoTien LoaiGiaoDich PhuongThuc LyDo TrangThai");
        Add("PHIEU_XUAT","MaPX MaKho MaNV MaKhoa MaPhong MaLS NgayXuat LyDo TrangThai GhiChu");
        Add("CHI_TIET_PHIEU_XUAT","MaCTPX MaPX MaHH MaLo MaCTDT SoLuong GhiChu");
        Add("KHO","MaKho TenKho MaNV LoaiKho TrangThai");
        Add("LO_HANG_HOA","MaLo MaHH MaKho MaNCC SoLo NgayNhap HanSuDung TrangThai");
        Add("DM_TINH_TRANG_BENH","MaDMTT TenTinhTrang MoTa TrangThai");
        Add("MAU_KE_HOACH_DIEU_TRI","MaMauKH MaDMTT MucDo TenMau PhienBan TrangThai MaBSDuyet NgayDuyet");
        Add("CHI_TIET_MAU_KE_HOACH","MaCTMau MaMauKH MaDV ThuTu LanThu SoLuongDuKien NoiDung");
        Add("NHOM_THUOC_KE_DON","MaNhomThuoc TenNhom MoTa TrangThai");
        Add("CAU_HINH_THUOC_KE_DON","MaThuoc MaNhomThuoc DoiTuongApDung DuongDung LieuDungGoiY HuongDanGoiY PhienBan TrangThai MaNguoiDuyet NgayDuyet");
        Add("BAN_GIAO_THUOC","MaDT MaHD MaPX TrangThai MaNVGiao ThoiGianGiao GhiChu");
        foreach(var pair in new Dictionary<string,string>
        {
            ["TINH_TRANG_BENH"]="ViTriRang", ["KE_HOACH_DIEU_TRI"]="MaTTBNguon MaMauKH PhienBanMau",
            ["CHI_TIET_KE_HOACH"]="NoiDungBuoc", ["LICH_SU_KHAM_BENH"]="KhongKeThuoc LyDoKhongKeThuoc",
            ["DON_THUOC"]="MaDTThayThe", ["CHI_TIET_DON_THUOC"]="DuongDung TenThuocChot HamLuongChot DonViTinhChot PhienBanHuongDan",
            ["HOA_DON"]="PhienBanQuyTrinh MaYeuCau DauVanTayYeuCau", ["THANH_TOAN"]="SoTienKhachDua TienThua MaThamChieu"
        }) foreach(var field in pair.Value.Split(' ')) Tables[pair.Key][field]=field;
    }
    /// <summary>
    /// Đăng ký danh sách tên cột cho bảng bổ sung; khi chưa có nhãn tiếng Việt riêng thì dùng tên cột làm nhãn.
    /// </summary>
    private static void Add(string table,string fields) => Tables[table]=fields.Split(' ').ToDictionary(field=>field,field=>field);
    /// <summary>
    /// Tra nhãn tiếng Việt theo bảng/cột; nếu chưa định nghĩa thì giữ tên cột để vẫn nhận diện được dữ liệu.
    /// </summary>
    public static string Label(string table,string field)
    {
        var label=Tables.GetValueOrDefault(table)?.GetValueOrDefault(field);
        return label is not null && label!=field ? label : CommonLabels.GetValueOrDefault(field) ?? field;
    }
    // AI bổ sung: tên cột kỹ thuật được đổi sang nhãn người dùng hiểu trong thông báo nhập sai.
    private static readonly Dictionary<string,string> CommonLabels=new()
    {
        ["MaDMTT"]="Danh mục tình trạng", ["ViTriRang"]="Vị trí răng", ["MaTTBNguon"]="Tình trạng nguồn", ["MaMauKH"]="Mẫu kế hoạch",
        ["PhienBanMau"]="Phiên bản mẫu", ["NoiDungBuoc"]="Nội dung bước", ["KhongKeThuoc"]="Không kê thuốc", ["LyDoKhongKeThuoc"]="Lý do không kê thuốc",
        ["MaDTThayThe"]="Đơn thuốc được thay thế", ["DuongDung"]="Đường dùng", ["TenThuocChot"]="Tên thuốc lúc kê", ["HamLuongChot"]="Hàm lượng lúc kê",
        ["DonViTinhChot"]="Đơn vị lúc kê", ["PhienBanHuongDan"]="Phiên bản hướng dẫn", ["SoTienKhachDua"]="Tiền khách đưa", ["TienThua"]="Tiền thối", ["MaThamChieu"]="Mã giao dịch",
        ["TenTinhTrang"]="Tên tình trạng", ["TenMau"]="Tên mẫu", ["MaCTMau"]="Bước mẫu", ["PhienBan"]="Phiên bản", ["MaBSDuyet"]="Bác sĩ duyệt", ["NgayDuyet"]="Ngày duyệt",
        ["MaNhomThuoc"]="Nhóm thuốc", ["TenNhom"]="Tên nhóm thuốc", ["DoiTuongApDung"]="Đối tượng áp dụng", ["LieuDungGoiY"]="Liều gợi ý", ["HuongDanGoiY"]="Hướng dẫn gợi ý",
        ["MaNguoiDuyet"]="Người duyệt", ["MaNVGiao"]="Dược sĩ giao thuốc", ["ThoiGianGiao"]="Thời gian giao", ["ThuTu"]="Thứ tự", ["NoiDung"]="Nội dung",
        ["MaHS"]="Hồ sơ bệnh án", ["MaLich"]="Lịch hẹn", ["MaPhieu"]="Phiếu tiếp nhận", ["MaPhong"]="Phòng",
        ["MaBS"]="Bác sĩ", ["MaNVTiepNhan"]="Nhân viên tiếp nhận", ["MaNV"]="Nhân viên", ["MaDV"]="Dịch vụ",
        ["MaBN"]="Bệnh nhân", ["MaLS"]="Lần khám", ["MaChucVu"]="Chức vụ", ["MaCK"]="Chuyên khoa",
        ["MaCa"]="Ca làm", ["MaThuoc"]="Thuốc", ["MaDT"]="Đơn thuốc", ["MaHH"]="Hàng hóa", ["MaLoaiHH"]="Loại hàng hóa",
        ["MaKhoa"]="Khoa", ["MaKho"]="Kho", ["MaLo"]="Lô hàng", ["MaNCC"]="Nhà cung cấp", ["MaTB"]="Thiết bị",
        ["MaHD"]="Hóa đơn", ["MaHDN"]="Hóa đơn nhập", ["MaPN"]="Phiếu nhập", ["MaPX"]="Phiếu xuất",
        ["MaNVDuyet"]="Người duyệt", ["MaNVThucHien"]="Nhân viên thực hiện", ["MaKHDT"]="Kế hoạch điều trị",
        ["HoTen"]="Họ tên", ["HoTenBN"]="Họ tên bệnh nhân", ["GioiTinh"]="Giới tính", ["NgaySinh"]="Ngày sinh",
        ["SDT"]="Số điện thoại", ["DienThoai"]="Số điện thoại", ["Email"]="Email", ["DiaChi"]="Địa chỉ",
        ["BangCap"]="Bằng cấp", ["NgayVaoLam"]="Ngày vào làm", ["TenChucVu"]="Tên chức vụ", ["MoTa"]="Mô tả",
        ["TrangThai"]="Trạng thái", ["SoThuTu"]="Số thứ tự", ["ThoiGianTiepNhan"]="Thời gian tiếp nhận",
        ["LyDoKham"]="Lý do khám", ["GhiChu"]="Ghi chú", ["NgayLap"]="Ngày lập", ["NgayKe"]="Ngày kê",
        ["NgayCap"]="Ngày cấp", ["HanChungChi"]="Hạn chứng chỉ", ["NgayMua"]="Ngày mua", ["HanBaoHanh"]="Hạn bảo hành",
        ["NgayBatDau"]="Ngày bắt đầu", ["NgayKetThuc"]="Ngày kết thúc", ["NgayDuKienKetThuc"]="Ngày dự kiến kết thúc",
        ["TuNgay"]="Từ ngày", ["DenNgay"]="Đến ngày", ["NgayNhap"]="Ngày nhập", ["HanSuDung"]="Hạn sử dụng",
        ["NgayHieuLuc"]="Ngày hiệu lực", ["NgayHetHieuLuc"]="Ngày hết hiệu lực",
        ["GioVao"]="Giờ vào", ["GioRa"]="Giờ ra", ["GioBatDau"]="Giờ bắt đầu", ["GioKetThuc"]="Giờ kết thúc",
        ["SoLuong"]="Số lượng", ["SoLuongDuKien"]="Số lượng dự kiến", ["SoNgayDung"]="Số ngày dùng",
        ["DonGia"]="Đơn giá", ["DonGiaDuKien"]="Đơn giá dự kiến", ["DonGiaBan"]="Đơn giá bán", ["SoTien"]="Số tiền",
        ["TongTien"]="Tổng tiền", ["GiamGia"]="Giảm giá", ["ThanhTien"]="Thành tiền", ["LyDo"]="Lý do",
        ["LoaiGiaoDich"]="Loại giao dịch", ["PhuongThuc"]="Phương thức thanh toán", ["UuTien"]="Ưu tiên",
        ["LieuDung"]="Liều dùng", ["HuongDan"]="Hướng dẫn", ["MucTonToiThieu"]="Mức tồn tối thiểu",
        ["TenDV"]="Tên dịch vụ", ["TenHH"]="Tên hàng hóa", ["DonViTinh"]="Đơn vị tính", ["HamLuong"]="Hàm lượng",
        ["TenPhong"]="Tên phòng", ["TenKhoa"]="Tên khoa", ["TenCK"]="Tên chuyên khoa", ["TenNCC"]="Tên nhà cung cấp",
        ["TenKho"]="Tên kho", ["TenLoaiHH"]="Tên loại hàng hóa", ["TenChungChi"]="Tên chứng chỉ",
        ["PhienBan"]="Phiên bản", ["LuongCoBan"]="Lương cơ bản", ["SoCaDinhMuc"]="Số ca định mức",
        ["SoCaTinh"]="Số ca tính", ["TyLeHoaHong"]="Tỷ lệ hoa hồng", ["ChiPhi"]="Chi phí", ["TienHoaHong"]="Tiền hoa hồng",
        ["TenDangNhap"]="Tên đăng nhập", ["MatKhauHash"]="Mật khẩu", ["ThoiLuongDuKien"]="Thời lượng dự kiến",
        ["ThoiGianDuKien"]="Thời gian dự kiến", ["LanThu"]="Lần thứ"
    };
}
