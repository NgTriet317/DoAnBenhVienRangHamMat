namespace RangHamMat.Controls;
// Names and labels aligned with the installed SQL Server schema and DB_Nhom4_v8.docx.
public static class ClinicalSchema
{
    public static readonly Dictionary<string,Dictionary<string,string>> Tables = new()
    {
        ["BENH_NHAN"] = new() { ["MaBN"] = "Mã bệnh nhân", ["HoTenBN"] = "Họ tên bệnh nhân", ["GioiTinh"] = "Giới tính", ["NgaySinh"] = "Ngày sinh", ["SDT"] = "Số điện thoại", ["DiaChi"] = "Địa chỉ", ["NhomMau"] = "Nhóm máu", ["TienSuBenh"] = "Tiền sử bệnh", ["DiUng"] = "Dị ứng", ["TrangThai"] = "Trạng thái" },
        ["HO_SO_BENH_AN"] = new() { ["MaHS"] = "Mã hồ sơ", ["MaBN"] = "Bệnh nhân", ["NgayLap"] = "Ngày lập", ["LyDoKhamBanDau"] = "Lý do khám ban đầu", ["GhiChu"] = "Ghi chú", ["TrangThai"] = "Trạng thái" },
        ["LICH_KHAM"] = new() { ["MaLich"] = "Mã lịch khám", ["MaBN"] = "Bệnh nhân", ["MaBS"] = "Bác sĩ", ["MaCa"] = "Ca làm", ["MaPhong"] = "Phòng", ["MaDV"] = "Dịch vụ dự kiến", ["NgayGioKham"] = "Ngày giờ khám", ["ThoiLuongDuKien"] = "Thời lượng dự kiến (phút)", ["UuTien"] = "Ưu tiên", ["LyDoKham"] = "Lý do khám", ["LyDoThayDoi"] = "Lý do thay đổi", ["TrangThai"] = "Trạng thái", ["GhiChu"] = "Ghi chú", ["NgayTao"] = "Ngày tạo", ["NgayCapNhat"] = "Ngày cập nhật" },
        ["LICH_SU_KHAM_BENH"] = new() { ["MaLS"] = "Mã lần khám", ["MaPhieu"] = "Phiếu tiếp nhận", ["MaBS"] = "Bác sĩ khám", ["MaPhong"] = "Phòng khám", ["ThoiGianKham"] = "Thời gian khám", ["TrieuChung"] = "Triệu chứng", ["ChanDoan"] = "Chẩn đoán", ["KeHoachXuLy"] = "Kế hoạch xử lý", ["TrangThai"] = "Trạng thái", ["NgayXacNhan"] = "Ngày xác nhận", ["GhiChu"] = "Ghi chú" },
        ["TINH_TRANG_BENH"] = new() { ["MaTTB"] = "Mã tình trạng", ["MaLS"] = "Lần khám", ["LoaiTinhTrang"] = "Loại tình trạng", ["MoTaTinhTrang"] = "Mô tả tình trạng", ["MucDo"] = "Mức độ", ["GhiChu"] = "Ghi chú" },
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
    static ClinicalSchema()
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
    }
    private static void Add(string table,string fields) => Tables[table]=fields.Split(' ').ToDictionary(field=>field,field=>field);
    public static string Label(string table,string field) => Tables.GetValueOrDefault(table)?.GetValueOrDefault(field) ?? field;
}
