using System.Globalization;
using System.Text;
using BUS;

namespace RangHamMat.Controls;

/// <summary>Danh mục và đơn vị của từng chuyên đề; không chứa số liệu minh họa.</summary>
public sealed record ChuyenDeBaoCao(string Ma, string Nhom, string Ten, string MoTa,
    string[] ChiSo, string[] DonVi, string Nguon, string QuyTac, bool DaKetNoi = true, bool TheoThoiDiem = false)
{
    public string TuKhoa => BaoCaoDuLieu.BoDau(Ten + " " + Nhom);
}

public sealed record PhatSinhBaoCao(DateTime Ngay, decimal Mot, decimal Hai, decimal Ba, decimal Bon,
    string Nhom = "", decimal GiaTriCoCau = 1, ChiTietBaoCao? ChiTiet = null);
public sealed record ChiTietBaoCao(string Ma, string Ten, string PhanLoai, string DonVi, DateTime? Ngay, DateTime? DenNgay,
    decimal? Mot, decimal? Hai, decimal? Ba, decimal? Bon, string TrangThai, string GhiChu);
public sealed record DongBaoCao(string Ky, decimal Mot, decimal Hai, decimal Ba, decimal? Bon, DateTime? TuKy = null, DateTime? DenKy = null);
public sealed record CoCauBaoCao(string Ten, decimal GiaTri, double TiLe);
public sealed record KetQuaBaoCao(DongBaoCao Tong, DongBaoCao[] CacKy, CoCauBaoCao[] CoCau, int SoPhatSinh, ChiTietBaoCao[] ChiTiet);

/// <summary>Đọc qua BUS hiện có. Chỉ tổng hợp trong bộ nhớ; không ghi hoặc sửa chứng từ.</summary>
public static class BaoCaoDuLieu
{
    public static IReadOnlyList<ChuyenDeBaoCao> DanhMuc { get; } = new ChuyenDeBaoCao[]
    {
        new("02", "Khám chữa bệnh", "Hồ sơ bệnh nhân", "Theo dõi hồ sơ mới và hoạt động tiếp nhận, khám chữa bệnh.",
            new[]{"Hồ sơ bệnh án mới", "Phiếu tiếp nhận", "Lượt khám xác nhận", "Hồ sơ có ghi chú"}, new[]{"hồ sơ", "phiếu", "lượt", "hồ sơ"},
            "HO_SO_BENH_AN · PHIEU_KHAM_BENH · LICH_SU_KHAM_BENH", "Hồ sơ mới tính theo NgayLap; không diễn giải thành số bệnh nhân mới. Mỗi loại phát sinh dùng ngày nghiệp vụ riêng."),
        new("03", "Khám chữa bệnh", "Tiếp nhận và phiếu khám", "Phân biệt tiếp nhận từ lịch hẹn và bệnh nhân đến trực tiếp.",
            new[]{"Tổng phiếu tiếp nhận", "Phiếu có lịch hẹn", "Phiếu tự đến", "Phiếu hủy"}, new[]{"phiếu", "phiếu", "phiếu", "phiếu"},
            "PHIEU_KHAM_BENH", "Theo ThoiGianTiepNhan. Có lịch khi MaLich được điền; trạng thái là trạng thái hiện tại."),
        new("04", "Khám chữa bệnh", "Lịch khám và điều phối", "Theo dõi lịch hẹn và cơ cấu trạng thái trong khoảng đã chọn.",
            new[]{"Tổng lịch hẹn", "Lịch đã khám", "Lịch hủy", "Lịch có dịch vụ"}, new[]{"lịch", "lịch", "lịch", "lịch"},
            "LICH_KHAM", "Theo NgayGioKham, không theo ngày tạo. Trạng thái hiện tại không phải lịch sử thay đổi lịch hoặc điều phối."),
        new("05", "Khám chữa bệnh", "Khám và điều trị", "Đối chiếu các lần khám và tình trạng xác nhận hồ sơ.",
            new[]{"Tổng lượt khám", "Lượt đã xác nhận", "Lượt nháp", "Có chẩn đoán"}, new[]{"lượt", "lượt", "lượt", "lượt"},
            "LICH_SU_KHAM_BENH", "Theo ThoiGianKham. Lượt khám xác nhận được đếm từ bản ghi gốc, không JOIN chi tiết gây trùng."),
        new("06", "Khám chữa bệnh", "Kế hoạch điều trị", "Theo dõi các kế hoạch được lập trong kỳ và giá trị dự kiến.",
            new[]{"Kế hoạch mới", "Có ngày bắt đầu", "Giá trị dự kiến", "Có hạn hoàn tất"}, new[]{"kế hoạch", "kế hoạch", "VNĐ", "kế hoạch"},
            "KE_HOACH_DIEU_TRI", "Lọc theo NgayLap; ngày bắt đầu và hạn hoàn tất phản ánh thông tin hiện tại của kế hoạch. Giá trị dự kiến không phải tiền đã thu."),
        new("07", "Khám chữa bệnh", "Dịch vụ thực hiện", "Theo dõi các bản ghi dịch vụ và giá trị đã xác nhận.",
            new[]{"Bản ghi dịch vụ", "Đã xác nhận", "Đã hủy", "Giá trị xác nhận"}, new[]{"bản ghi", "bản ghi", "bản ghi", "VNĐ"},
            "DICH_VU_THUC_HIEN", "Theo ThoiGianThucHien. Giá trị chỉ cộng ThanhTien của bản đã xác nhận; không coi là tiền thực thu."),
        new("08", "Khám chữa bệnh", "Đơn thuốc", "Theo dõi đơn kê và cơ cấu trạng thái xác nhận.",
            new[]{"Tổng đơn kê", "Đã xác nhận", "Đơn nháp", "Đơn hủy"}, new[]{"đơn", "đơn", "đơn", "đơn"},
            "DON_THUOC", "Theo NgayKe; không suy ra lượng thuốc đã cấp phát từ trạng thái đơn kê."),
        new("09", "Tài chính", "Thu – hoàn bệnh viện", "Đối chiếu tiền thu, tiền hoàn và thu ròng đã xác nhận.",
            new[]{"Tiền thu xác nhận", "Tiền hoàn xác nhận", "Thu ròng", "Giao dịch xác nhận"}, new[]{"VNĐ", "VNĐ", "VNĐ", "giao dịch"},
            "THANH_TOAN", "Theo NgayThanhToan. Chỉ tính Đã xác nhận: thu ròng = Thu − Hoàn; không cộng giá trị hóa đơn hoặc xuất kho."),
        new("10", "Tài chính", "Hóa đơn bệnh viện", "Theo dõi hóa đơn lập trong kỳ và số tiền phải thu.",
            new[]{"Hóa đơn đã lập", "Đã xác nhận", "Hóa đơn hủy", "Phải thu đã xác nhận"}, new[]{"hóa đơn", "hóa đơn", "hóa đơn", "VNĐ"},
            "HOA_DON", "Theo NgayLap. SoTienPhaiThu của hóa đơn xác nhận là giá trị phải thu, không phải tiền đã thu."),
        new("11", "Tài chính", "Lương và hoa hồng", "Theo dõi quỹ lương và hoa hồng theo kỳ đã chốt.",
            new[]{"Lương đã chốt", "Hoa hồng chốt", "Tổng quỹ lương", "Ca tính lương"}, new[]{"VNĐ", "VNĐ", "VNĐ", "ca"},
            "BANG_LUONG · NHAN_VIEN", "Theo ngày chốt, chỉ lấy bảng lương Đã chốt. Lương theo ca, hoa hồng, tổng lương và số ca lấy đúng giá trị đã lưu; không cộng nháp hoặc bản hủy."),
        new("12", "Kho và vật tư", "Nhập – xuất kho", "Theo dõi phiếu nhập, xuất đã ghi sổ và phiếu hủy.",
            new[]{"Phiếu nhập ghi sổ", "Phiếu xuất ghi sổ", "Phiếu nhập hủy", "Phiếu xuất hủy"}, new[]{"phiếu", "phiếu", "phiếu", "phiếu"},
            "PHIEU_NHAP · PHIEU_XUAT", "Theo NgayNhap / NgayXuat. Đây là số chứng từ, không phải số lượng hàng hóa hoặc doanh thu kho."),
        new("13", "Kho và vật tư", "Dòng hàng vật tư", "Đối chiếu nhập, xuất theo từng mặt hàng và đơn vị tính.",
            new[]{"Dòng nhập ghi sổ", "Dòng xuất ghi sổ", "Giá trị nhập ghi sổ", "Mặt hàng có phát sinh"}, new[]{"dòng", "dòng", "VNĐ", "mặt hàng"},
            "CHI_TIET_PHIEU_NHAP · CHI_TIET_PHIEU_XUAT · HANG_HOA", "Theo ngày nhập / xuất của phiếu Đã ghi sổ. Mặt hàng đếm khác nhau trong từng kỳ và cả khoảng, không cộng các kỳ để ra tổng mặt hàng. Số lượng chỉ cộng theo cùng mã hàng và đơn vị tại bảng chi tiết."),
        new("14", "Kho và vật tư", "Hạn dùng lô hàng", "Rà soát lô hàng đến hạn theo ngày chốt và ngưỡng cảnh báo.",
            new[]{"Lô có hạn dùng", "Đã quá hạn", "Sắp đến hạn", "Lô thuốc đến / quá hạn"}, new[]{"lô", "lô", "lô", "lô"},
            "LO_HANG_HOA · HANG_HOA · THUOC", "Kiểm tra danh mục lô đã nhập đến ngày chốt, chưa hủy. Quá hạn khi hạn dùng trước ngày chốt; sắp đến hạn trong 30/60/90 ngày kể cả ngày chốt. Không suy ra số dư tồn kho hoặc số lô còn hàng tại ngày chốt.", true, true),
        new("15", "Kho và vật tư", "Nhà cung cấp", "Theo dõi hóa đơn mua từ nhà cung cấp trong kỳ.",
            new[]{"Hóa đơn mua", "Hóa đơn nháp", "Hóa đơn hủy", "Giá trị xác nhận"}, new[]{"hóa đơn", "hóa đơn", "hóa đơn", "VNĐ"},
            "HOA_DON_NHAP", "Theo NgayHoaDon; giá trị chỉ lấy hóa đơn Đã xác nhận, không cộng lại giá trị phiếu nhập hoặc lô hàng."),
        new("16", "Nhân sự và vận hành", "Nhân sự và hiện diện", "Đối chiếu hiện diện trên các ca làm việc đã xác nhận.",
            new[]{"Ca đã xác nhận", "Ca có mặt", "Ca vắng mặt", "Chưa ghi hiện diện"}, new[]{"ca", "ca", "ca", "ca"},
            "LICH_LAM_VIEC", "Theo NgayLam; chỉ ca đã xác nhận. Hiện diện là trạng thái hiện tại, chưa có ảnh chụp lịch sử theo ngày duyệt."),
        new("17", "Nhân sự và vận hành", "Phân ca", "Theo dõi ca được phân và cơ cấu xác nhận lịch làm việc.",
            new[]{"Ca được phân", "Ca đã xác nhận", "Ca chờ xác nhận", "Ca hủy"}, new[]{"ca", "ca", "ca", "ca"},
            "LICH_LAM_VIEC", "Theo NgayLam; cơ cấu trạng thái lấy từ bản ghi hiện tại. Một dòng lịch làm việc được tính là một ca được phân."),
        new("18", "Nhân sự và vận hành", "Đơn nghỉ và duyệt", "Theo dõi đơn nghỉ, kết quả duyệt và bố trí thay thế.",
            new[]{"Đơn gửi trong kỳ", "Hiện đã được duyệt", "Ngày nghỉ của đơn duyệt", "Thay thế đã xác nhận"}, new[]{"đơn", "đơn", "ngày lịch", "ca"},
            "DON_XIN_NGHI · PHAN_CONG_THAY_THE", "Chọn đơn theo ngày gửi. Trạng thái duyệt và thay thế là trạng thái hiện tại của các đơn đó, không phải lịch sử duyệt. Số ngày lịch thuộc toàn khoảng nghỉ của đơn đã duyệt; không tính nghỉ việc không xác định ngày kết thúc."),
        new("19", "Nhân sự và vận hành", "Thiết bị và bảo trì", "Theo dõi công việc bảo trì và chi phí được xác nhận.",
            new[]{"Lượt bảo trì", "Đã xác nhận", "Đã kết thúc, xác nhận", "Chi phí xác nhận"}, new[]{"lượt", "lượt", "lượt", "VNĐ"},
            "BAO_TRI_THIET_BI", "Theo NgayBatDau; chi phí chỉ tính bản đã xác nhận. Không suy ra thiết bị quá hạn từ số lần bảo trì.")
    };

    public static string BoDau(string value) => string.Concat(value.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        .Replace('đ', 'd').Replace('Đ', 'D').ToLowerInvariant();

    public static PhatSinhBaoCao[] Doc(string ma, DateTime? tuNgay = null, DateTime? denNgay = null, int canhBao = 30)
    {
        if (!PhienDangNhap.IsAdmin) throw new UnauthorizedAccessException("Tài khoản hiện tại không có quyền đọc báo cáo.");
        if (!DanhMuc.Any(r => r.Ma == ma)) throw new ArgumentException("Chuyên đề báo cáo không tồn tại.");
        var rows = new List<PhatSinhBaoCao>();
        static int Co(bool value) => value ? 1 : 0;
        switch (ma)
        {
            case "11": case "13": case "14": case "18":
                var from = tuNgay?.Date ?? (ma == "14" ? DateTime.Today : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
                var to = denNgay?.Date ?? DateTime.Today;
                rows.AddRange(new BUS_BAOCAO().Doc(ma, from, to, canhBao, PhienDangNhap.IsAdmin).Select(r => new PhatSinhBaoCao(
                    r.Ngay, r.Mot, r.Hai, r.Ba, r.Bon, r.Nhom, r.GiaTriCoCau,
                    new ChiTietBaoCao(r.Ma, r.Ten, ma == "14" ? r.Nhom : r.PhanLoai, r.DonVi, r.NgayChiTiet, r.DenNgayChiTiet,
                        r.ChiTietMot, r.ChiTietHai, r.ChiTietBa, r.ChiTietBon, r.TrangThai, r.GhiChu))));
                break;
            case "02":
                rows.AddRange(new BUS_HOSOBENHAN().layToanBoHoSoBenhAn().Select(r => new PhatSinhBaoCao(r.NgayLap, 1, 0, 0, Co(!string.IsNullOrWhiteSpace(r.GhiChu)), r.TrangThai)));
                rows.AddRange(new BUS_PHIEUKHAMBENH().layToanBoPhieuKhamBenh().Select(r => new PhatSinhBaoCao(r.ThoiGianTiepNhan, 0, 1, 0, 0)));
                rows.AddRange(new BUS_LICHSUKHAMBENH().layToanBoLichSuKhamBenh().Where(r => r.TrangThai == "Đã xác nhận").Select(r => new PhatSinhBaoCao(r.ThoiGianKham, 0, 0, 1, 0)));
                break;
            case "03":
                rows.AddRange(new BUS_PHIEUKHAMBENH().layToanBoPhieuKhamBenh().Select(r => new PhatSinhBaoCao(r.ThoiGianTiepNhan, 1, Co(!string.IsNullOrWhiteSpace(r.MaLich)), Co(string.IsNullOrWhiteSpace(r.MaLich)), Co(r.TrangThai == "Hủy"), r.TrangThai)));
                break;
            case "04":
                rows.AddRange(new BUS_LICHKHAM().layToanBoLichKham().Select(r => new PhatSinhBaoCao(r.NgayGioKham, 1, Co(r.TrangThai == "Đã khám"), Co(r.TrangThai == "Hủy"), Co(!string.IsNullOrWhiteSpace(r.MaDV)), r.TrangThai)));
                break;
            case "05":
                rows.AddRange(new BUS_LICHSUKHAMBENH().layToanBoLichSuKhamBenh().Select(r => new PhatSinhBaoCao(r.ThoiGianKham, 1, Co(r.TrangThai == "Đã xác nhận"), Co(r.TrangThai == "Nháp"), Co(!string.IsNullOrWhiteSpace(r.ChanDoan)), r.TrangThai)));
                break;
            case "06":
                rows.AddRange(new BUS_KEHOACHDIEUTRI().layToanBoKeHoachDieuTri().Select(r => new PhatSinhBaoCao(r.NgayLap, 1, Co(r.NgayBatDau.HasValue), r.TongTienDuKien, Co(r.NgayDuKienKetThuc.HasValue), r.TrangThai)));
                break;
            case "07":
                rows.AddRange(new BUS_DICHVUTHUCHIEN().layToanBoDichVuThucHien().Select(r => new PhatSinhBaoCao(r.ThoiGianThucHien, 1, Co(r.TrangThai == "Đã xác nhận"), Co(r.TrangThai == "Hủy"), r.TrangThai == "Đã xác nhận" ? r.ThanhTien : 0, r.TrangThai)));
                break;
            case "08":
                rows.AddRange(new BUS_DONTHUOC().layToanBoDonThuoc().Select(r => new PhatSinhBaoCao(r.NgayKe, 1, Co(r.TrangThai == "Đã xác nhận"), Co(r.TrangThai == "Nháp"), Co(r.TrangThai == "Hủy"), r.TrangThai)));
                break;
            case "09":
                rows.AddRange(new BUS_THANHTOAN().LayDanhSach().Where(r => r.TrangThai == "Đã xác nhận").Select(r => new PhatSinhBaoCao(r.NgayThanhToan,
                    r.LoaiGiaoDich == "Thu" ? r.SoTien : 0, r.LoaiGiaoDich == "Hoàn" ? r.SoTien : 0,
                    r.LoaiGiaoDich == "Thu" ? r.SoTien : -r.SoTien, 1, r.LoaiGiaoDich + " · " + r.PhuongThuc, r.SoTien)));
                break;
            case "10":
                rows.AddRange(new BUS_HOADON().LayDanhSach().Select(r => new PhatSinhBaoCao(r.NgayLap, 1, Co(r.TrangThai == "Đã xác nhận"), Co(r.TrangThai == "Hủy"), r.TrangThai == "Đã xác nhận" ? r.SoTienPhaiThu : 0, r.TrangThai)));
                break;
            case "12":
                rows.AddRange(new BUS_PHIEUNHAP().layToanBoPhieuNhap().Select(r => new PhatSinhBaoCao(r.NgayNhap, Co(r.TrangThai == "Đã ghi sổ"), 0, Co(r.TrangThai == "Hủy"), 0, "Nhập · " + r.TrangThai)));
                rows.AddRange(new BUS_PHIEUXUAT().LayDanhSach().Select(r => new PhatSinhBaoCao(r.NgayXuat, 0, Co(r.TrangThai == "Đã ghi sổ"), 0, Co(r.TrangThai == "Hủy"), "Xuất · " + r.TrangThai)));
                break;
            case "15":
                rows.AddRange(new BUS_HOADONNHAP().layToanBoHoaDonNhap().Select(r => new PhatSinhBaoCao(r.NgayHoaDon, 1, Co(r.TrangThai == "Nháp"), Co(r.TrangThai == "Hủy"), r.TrangThai == "Đã xác nhận" ? r.TongTien : 0, r.TrangThai)));
                break;
            case "16":
                rows.AddRange(new BUS_LICHLAMVIEC().layToanBoLichLamViec().Where(r => r.TrangThai == "Đã xác nhận").Select(r => new PhatSinhBaoCao(r.NgayLam, 1,
                    Co(r.TrangThaiHienDien == "Có mặt"), Co(r.TrangThaiHienDien == "Vắng mặt"), Co(r.TrangThaiHienDien != "Có mặt" && r.TrangThaiHienDien != "Vắng mặt"), r.TrangThaiHienDien)));
                break;
            case "17":
                rows.AddRange(new BUS_LICHLAMVIEC().layToanBoLichLamViec().Select(r => new PhatSinhBaoCao(r.NgayLam, 1, Co(r.TrangThai == "Đã xác nhận"), Co(r.TrangThai == "Chờ xác nhận"), Co(r.TrangThai == "Hủy"), r.TrangThai)));
                break;
            case "19":
                rows.AddRange(new BUS_BAOTRITHIETBI().layToanBoBaoTriThietBi().Select(r => new PhatSinhBaoCao(r.NgayBatDau, 1, Co(r.TrangThai == "Đã xác nhận"), Co(r.TrangThai == "Đã xác nhận" && r.NgayKetThuc.HasValue), r.TrangThai == "Đã xác nhận" ? r.ChiPhi : 0, r.TrangThai)));
                break;
            default: throw new ArgumentException("Chuyên đề báo cáo không tồn tại.");
        }
        return rows.ToArray();
    }

    /// <summary>Lọc trước khi nhóm kỳ. Không lấy thêm ngày ngoài khoảng cho tuần/tháng giao nhau.</summary>
    public static KetQuaBaoCao TongHop(string ma, IEnumerable<PhatSinhBaoCao> source, DateTime tu, DateTime den, string nhom)
    {
        tu = tu.Date; den = den.Date;
        if (tu > den) throw new ArgumentException("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        if ((den - tu).TotalDays > 3660) throw new ArgumentException("Vui lòng chọn khoảng báo cáo tối đa 10 năm.");
        if (den == DateTime.MaxValue.Date) throw new ArgumentException("Ngày kết thúc nằm ngoài phạm vi hỗ trợ.");
        if (nhom is not ("Ngày" or "Tuần" or "Tháng")) throw new ArgumentException("Chọn cách nhóm theo ngày, tuần hoặc tháng.");
        DateTime Key(DateTime date) => nhom == "Tháng" ? new(date.Year, date.Month, 1)
            : nhom == "Tuần" ? date.Date.AddDays(-((int)date.DayOfWeek + 6) % 7) : date.Date;
        (DateTime Start, DateTime End) Bounds(DateTime date)
        {
            var start = date < tu ? tu : date;
            var end = nhom == "Tháng" ? new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month))
                : nhom == "Tuần" ? date.AddDays(Math.Min(6, (DateTime.MaxValue.Date - date).Days)) : date;
            if (end > den) end = den;
            return (start, end);
        }
        string Label(DateTime key, DateTime start, DateTime end)
        {
            return nhom == "Tháng" ? $"Tháng {key:MM/yyyy}" : nhom == "Tuần"
                ? $"Tuần {start:dd/MM} – {end:dd/MM/yyyy}" : start.ToString("dd/MM/yyyy");
        }
        DongBaoCao Sum(string label, IEnumerable<PhatSinhBaoCao> rows)
        {
            var items = rows.ToArray(); var first = items.Sum(r => r.Mot); var third = items.Sum(r => r.Ba);
            var fourth = ma == "13" ? items.Where(r => r.ChiTiet is not null).Select(r => r.ChiTiet!.Ma).Distinct().Count() : items.Sum(r => r.Bon);
            return new(label, first, items.Sum(r => r.Hai), third, fourth);
        }
        var data = source.Where(r => r.Ngay >= tu && r.Ngay < den.AddDays(1)).ToArray();
        var groups = data.GroupBy(r => Key(r.Ngay)).ToDictionary(g => g.Key, g => g.AsEnumerable());
        var periods = new SortedSet<DateTime>();
        for (var day = tu; day <= den; day = day.AddDays(1)) periods.Add(Key(day));
        var rows = periods.Select(key =>
        {
            var bounds = Bounds(key);
            return Sum(Label(key, bounds.Start, bounds.End), groups.TryGetValue(key, out var list) ? list : Array.Empty<PhatSinhBaoCao>())
                with { TuKy = bounds.Start, DenKy = bounds.End };
        }).ToArray();
        var categories = data.Where(r => !string.IsNullOrWhiteSpace(r.Nhom)).GroupBy(r => r.Nhom)
            .Select(g => (Ten: g.Key, GiaTri: g.Sum(r => r.GiaTriCoCau))).OrderByDescending(r => r.GiaTri).ThenBy(r => r.Ten).ToArray();
        var max = categories.Length == 0 ? 0 : categories.Max(r => r.GiaTri);
        var details = data.Where(r => r.ChiTiet is not null).Select(r => r.ChiTiet!).ToArray();
        if (ma == "13") details = details.GroupBy(r => (r.Ma, r.DonVi)).Select(group => group.First() with
        {
            Ngay = null, DenNgay = null, Mot = group.Sum(r => r.Mot), Hai = group.Sum(r => r.Hai), Ba = group.Sum(r => r.Ba),
            GhiChu = "Tổng hợp theo mã hàng và đơn vị trong khoảng đã chọn."
        }).OrderBy(r => r.Ten).ToArray();
        return new(Sum("Tổng kỳ", data), rows, categories.Select(r => new CoCauBaoCao(r.Ten, r.GiaTri, max == 0 ? 0 : (double)(r.GiaTri / max * 100))).ToArray(), data.Length, details);
    }

    public static string[] CotChiTiet(string ma) => ma switch
    {
        "11" => new[]{"Mã bảng lương", "Nhân viên", "Mã nhân viên", "", "Ngày chốt", "", "Lương theo ca (VNĐ)", "Hoa hồng (VNĐ)", "Tổng lương (VNĐ)", "Số ca", "Trạng thái", "Kỳ lương"},
        "13" => new[]{"Mã hàng", "Tên hàng", "", "Đơn vị tính", "", "", "Số lượng nhập", "Số lượng xuất", "Giá trị nhập (VNĐ)", "", "", ""},
        "14" => new[]{"Mã lô", "Tên hàng", "Phân loại hạn dùng", "Đơn vị tính", "Hạn sử dụng", "Ngày nhập lô", "Số ngày tới hạn", "", "", "", "Trạng thái lô", "Số lô / kho"},
        "18" => new[]{"Mã đơn", "Nhân viên", "Mã nhân viên", "", "Từ ngày nghỉ", "Đến ngày nghỉ", "Ngày nghỉ đã duyệt", "Thay thế xác nhận", "", "", "Trạng thái duyệt", "Thông tin gửi đơn"},
        _ => Array.Empty<string>()
    };

    public static string TaoCsv(ChuyenDeBaoCao report, KetQuaBaoCao data, DateTime tu, DateTime den, string nhom, string nguoiXuat, DateTime lucXuat, int canhBao = 30)
    {
        static string Cell(string value)
        {
            // Tránh công thức khi mở CSV trong phần mềm bảng tính.
            if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('@') || value.TrimStart().StartsWith('-')) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        var csv = new StringBuilder();
        void Line(params string[] cells) => csv.AppendLine(string.Join(",", cells.Select(Cell)));
        Line("Báo cáo", report.Ma, report.Ten);
        if (report.TheoThoiDiem) Line("Ngày chốt", tu.ToString("dd/MM/yyyy"), "Ngưỡng cảnh báo", canhBao + " ngày");
        else Line("Khoảng ngày", tu.ToString("dd/MM/yyyy"), den.ToString("dd/MM/yyyy"), "Nhóm theo", nhom);
        Line("Người xuất", nguoiXuat, "Thời điểm xuất", lucXuat.ToString("dd/MM/yyyy HH:mm:ss"));
        Line("Nguồn", report.Nguon); Line("Quy tắc", report.QuyTac);
        Line(new[]{"Kỳ"}.Concat(report.ChiSo.Select((name, i) => name + " (" + report.DonVi[i] + ")")).ToArray());
        foreach (var row in new[]{data.Tong}.Concat(data.CacKy))
            // Giá trị số được tạo nội bộ, không áp dụng escape công thức để giữ nguyên thu ròng âm.
            csv.AppendLine(Cell(row.Ky) + "," + string.Join(",", new[]{row.Mot, row.Hai, row.Ba, row.Bon}.Select(n => n?.ToString(CultureInfo.InvariantCulture) ?? "")));
        var headers = CotChiTiet(report.Ma);
        if (headers.Length > 0)
        {
            Line(); Line("Bảng chi tiết");
            var visible = Enumerable.Range(0, headers.Length).Where(i => headers[i].Length > 0).ToArray();
            Line(visible.Select(i => headers[i]).ToArray());
            foreach (var row in data.ChiTiet)
            {
                var values = new[]{row.Ma, row.Ten, row.PhanLoai, row.DonVi, row.Ngay?.ToString("dd/MM/yyyy") ?? "", row.DenNgay?.ToString("dd/MM/yyyy") ?? "",
                    row.Mot?.ToString(CultureInfo.InvariantCulture) ?? "", row.Hai?.ToString(CultureInfo.InvariantCulture) ?? "", row.Ba?.ToString(CultureInfo.InvariantCulture) ?? "",
                    row.Bon?.ToString(CultureInfo.InvariantCulture) ?? "", row.TrangThai, row.GhiChu};
                csv.AppendLine(string.Join(",", visible.Select(i => i is >= 6 and <= 9 ? values[i] : Cell(values[i]))));
            }
        }
        return csv.ToString();
    }
}
