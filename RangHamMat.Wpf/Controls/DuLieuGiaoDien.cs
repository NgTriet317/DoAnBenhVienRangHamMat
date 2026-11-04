// AI bổ sung kiểm tra nhập liệu: kiểm tra giá trị đã gộp với bản ghi trước khi gọi BUS, giữ cache chỉ nhận dữ liệu lưu thành công.
// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ dữ liệu dùng ở giao diện: cache hiển thị, liên kết hồ sơ, điều phối gọi BUS và thông báo dữ liệu thay đổi sau lưu; đã chú thích cách quản lý giao dịch và làm mới cache.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BUS;
using ET;
using System.Data;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Controls;

// Cache hiển thị: thay đổi phải lưu thành công qua BUS/DAL trước khi đưa vào các collection này.
/// <summary>
/// Cache trình bày của các bảng đọc qua BUS; điều phối giao dịch lưu và đồng bộ cache sau SQL, phát Changed để các màn hình làm mới.
/// </summary>
public static class DuLieuGiaoDien
{
    /// <summary>
    /// Phạm vi lâm sàng.
    /// </summary>
    public static bool PhamViLamSang { get; private set; }
    /// <summary>
    /// Áp dụng phạm vi dữ liệu lâm sàng của phiên vào các thao tác đọc/ghi giao diện.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ChonPhamViLamSang</c>.</remarks>
    /// <param name="enabled">Giá trị đã bật dùng khi thực hiện thao tác; kiểu bool.</param>
    public static void ChonPhamViLamSang(bool enabled)
        =>ChonPhamViLamSang(enabled,clinicalScope);
    /// <summary>
    /// Áp dụng phạm vi dữ liệu lâm sàng của phiên vào các thao tác đọc/ghi giao diện.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XoaCachePhien</c>, <c>RefreshFromDatabase</c>.</remarks>
    /// <param name="enabled">Giá trị đã bật dùng khi thực hiện thao tác; kiểu bool.</param>
    /// <param name="scope">Giá trị phạm vi dùng khi thực hiện thao tác; kiểu NguCanhHoSo?.</param>
    internal static void ChonPhamViLamSang(bool enabled,NguCanhHoSo? scope)
    {
        if (PhamViLamSang != enabled) { XoaCachePhien(); PhamViLamSang = enabled; }
        clinicalScope=scope??new();
        if (enabled) RefreshFromDatabase();
    }
    /// <summary>
    /// Lâm sàng người dùng được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static NguCanhNghiepVu? clinicalUser;
    /// <summary>
    /// Lâm sàng phạm vi được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static NguCanhHoSo clinicalScope=new();
    /// <summary>
    /// Giữ lại bản ghi đã commit trong nguồn dữ liệu giao diện để cập nhật lựa chọn sau khi lưu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>tables.TryGetValue</c>, <c>Key</c>, <c>UiRecord.Text</c>, <c>rows.ToList</c>.</remarks>
    /// <param name="table">Giá trị bảng dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="saved">Giá trị đã lưu dùng khi thực hiện thao tác; kiểu object.</param>
    internal static void GiuBanGhiDaCommit(string table,object saved)
    {
        if(!tables.TryGetValue(table,out var rows)){rows=new ObservableCollection<object>();tables[table]=rows;}
        var key=Key(table);var id=UiRecord.Text(saved,key);var index=rows.ToList().FindIndex(r=>UiRecord.Text(r,key)==id);
        if(index<0)rows.Add(saved);else rows[index]=saved;
    }
    /// <summary>
    /// Kiểm tra ngữ cảnh phiên còn hợp lệ trước khi cung cấp dữ liệu nghiệp vụ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Find</c>, <c>QuyenNghiepVu.LayNguCanh</c>, <c>RefreshFromDatabase</c>.</remarks>
    /// <param name="context">Giá trị ngữ cảnh dùng khi thực hiện thao tác; kiểu NguCanhHoSo.</param>
    internal static void BaoDamNguCanh(NguCanhHoSo context)
    {
        if(!PhamViLamSang)return;
        if(clinicalScope.MaBN==context.MaBN&&(context.MaLS is null||clinicalScope.MaLS==context.MaLS)&&Find("BENH_NHAN",context.MaBN) is not null&&(context.MaDT is null||Find("DON_THUOC",context.MaDT) is not null)&&(context.MaHD is null||Find("HOA_DON",context.MaHD) is not null))return;
        var valid=new BUS_KHAMDIEUTRI().XacMinhNguCanh(context,QuyenNghiepVu.LayNguCanh());
        clinicalScope=valid;RefreshFromDatabase();
    }
    /// <summary>
    /// Giá trị Người dùng lâm sàng; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public static NguCanhNghiepVu NguoiDungLamSang { get { EnsureLoaded(); return clinicalUser ?? throw new TuChoiQuyenNghiepVuException(); } }
    /// <summary>
    /// Khởi tạo dữ liệu giao diện.
    /// </summary>
    static DuLieuGiaoDien() => PhienDangNhap.DinhDanhThayDoi += XoaCachePhien;
    /// <summary>
    /// Xóa dữ liệu đệm theo phiên để lần đăng nhập/ngữ cảnh tiếp theo không dùng lại dữ liệu cũ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>rows.Clear</c>, <c>tables.Clear</c>.</remarks>
    private static void XoaCachePhien()
    {
        if (batching) throw new InvalidOperationException("Không đổi phiên khi đang lưu chứng từ.");
        foreach (var rows in tables.Values) rows.Clear();
        tables.Clear(); loaded = false; clinicalUser = null;clinicalScope=new();
    }
    /// <summary>
    /// Các bảng được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static readonly Dictionary<string, ObservableCollection<object>> tables = new();
    /// <summary>
    /// Giao dịch được giữ để các thao tác liên quan dùng cùng một phạm vi commit/rollback.
    /// </summary>
    private static IDbTransaction? transaction;
    /// <summary>
    /// Cờ Cờ đã nạp dữ liệu ban đầu; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private static bool loaded;
    /// <summary>
    /// Cờ Đang cập nhật theo lô; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private static bool batching;
    /// <summary>
    /// Các khóa được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static readonly Dictionary<string, string> keys = new()
    {
        ["BENH_NHAN"] = "MaBN",
        ["BAO_TRI_THIET_BI"] = "MaBT",
        ["THIET_BI"] = "MaTB",
        ["NHA_CUNG_CAP"] = "MaNCC",
        ["HOA_DON_NHAP"] = "MaHDN",
        ["HO_SO_BENH_AN"] = "MaHS",
        ["PHIEU_KHAM_BENH"] = "MaPhieu",
        ["LICH_KHAM"] = "MaLich",
        ["LICH_SU_KHAM_BENH"] = "MaLS",
        ["TINH_TRANG_BENH"] = "MaTTB",
        ["DM_TINH_TRANG_BENH"] = "MaDMTT",
        ["MAU_KE_HOACH_DIEU_TRI"] = "MaMauKH",
        ["CHI_TIET_MAU_KE_HOACH"] = "MaCTMau",
        ["NHOM_THUOC_KE_DON"] = "MaNhomThuoc",
        ["CAU_HINH_THUOC_KE_DON"] = "MaThuoc",
        ["BAN_GIAO_THUOC"] = "MaDT",
        ["KE_HOACH_DIEU_TRI"] = "MaKHDT",
        ["CHI_TIET_KE_HOACH"] = "MaCTKH",
        ["DICH_VU_THUC_HIEN"] = "MaDVThucHien",
        ["DON_THUOC"] = "MaDT",
        ["CHI_TIET_DON_THUOC"] = "MaCTDT",
        ["NHAN_VIEN"] = "MaNV",
        ["PHONG"] = "MaPhong",
        ["CA_LAM"] = "MaCa",
        ["DICH_VU"] = "MaDV",
        ["THUOC"] = "MaThuoc",
        ["HANG_HOA"] = "MaHH",
        ["HOA_DON"] = "MaHD",
        ["CHI_TIET_HOA_DON"] = "MaCTHD",
        ["THANH_TOAN"] = "MaTT",
        ["PHIEU_XUAT"] = "MaPX",
        ["CHI_TIET_PHIEU_XUAT"] = "MaCTPX",
        ["KHO"] = "MaKho",
        ["LO_HANG_HOA"] = "MaLo"
    };
    /// <summary>
    /// Tham chiếu các bảng được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    public static readonly Dictionary<string, string> ForeignTables = new()
    {
        ["MaBN"] = "BENH_NHAN",
        ["MaTB"] = "THIET_BI",
        ["MaNCC"] = "NHA_CUNG_CAP",
        ["MaHS"] = "HO_SO_BENH_AN",
        ["MaPhieu"] = "PHIEU_KHAM_BENH",
        ["MaLich"] = "LICH_KHAM",
        ["MaLS"] = "LICH_SU_KHAM_BENH",
        ["MaBS"] = "NHAN_VIEN",
        ["MaPhong"] = "PHONG",
        ["MaCa"] = "CA_LAM",
        ["MaDV"] = "DICH_VU",
        ["MaKHDT"] = "KE_HOACH_DIEU_TRI",
        ["MaCTKH"] = "CHI_TIET_KE_HOACH",
        ["MaDMTT"] = "DM_TINH_TRANG_BENH",
        ["MaMauKH"] = "MAU_KE_HOACH_DIEU_TRI",
        ["MaCTMau"] = "CHI_TIET_MAU_KE_HOACH",
        ["MaNhomThuoc"] = "NHOM_THUOC_KE_DON",
        ["MaTTBNguon"] = "TINH_TRANG_BENH",
        ["MaDTThayThe"] = "DON_THUOC",
        ["MaBSDuyet"] = "NHAN_VIEN",
        ["MaNguoiDuyet"] = "NHAN_VIEN",
        ["MaNVGiao"] = "NHAN_VIEN",
        ["MaNVTiepNhan"] = "NHAN_VIEN",
        ["MaHH"] = "HANG_HOA",
        ["MaNVThucHien"] = "NHAN_VIEN",
        ["MaDT"] = "DON_THUOC",
        ["MaThuoc"] = "THUOC",
        ["MaHD"] = "HOA_DON",
        ["MaDVThucHien"] = "DICH_VU_THUC_HIEN",
        ["MaCTPX"] = "CHI_TIET_PHIEU_XUAT",
        ["MaPX"] = "PHIEU_XUAT",
        ["MaCTDT"] = "CHI_TIET_DON_THUOC",
        ["MaKho"] = "KHO",
        ["MaLo"] = "LO_HANG_HOA",
        ["MaNV"] = "NHAN_VIEN"
    };

    /// <summary>
    /// Thông báo thay đổi cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public static event EventHandler? Changed;

    /// <summary>
    /// Phát sự kiện Changed khi không đang lưu theo lô; các màn hình đăng ký sẽ tải lại phần hiển thị sau khi dữ liệu thay đổi.
    /// </summary>
    private static void Notify() { if (!batching) Changed?.Invoke(null, EventArgs.Empty); }
    /// <summary>
    /// Đọc đủ snapshot từ SQL trước, sau đó thay nội dung cache và chỉ phát Changed một lần; lỗi đọc không xóa dữ liệu đang hiển thị.
    /// </summary>
    public static void RefreshFromDatabase()
    {
        if (batching) throw new InvalidOperationException("Không tải lại khi đang lưu chứng từ.");
        var snapshot = LoadRecords(); // Đọc xong trước khi thay cache để lỗi kết nối không xóa dữ liệu đang xem.
        batching = true; loaded = true;
        try { foreach (var table in snapshot) Replace(table.Key, table.Value); }
        finally { batching = false; }
        Notify();
    }
    /// <summary>
    /// Nạp cache từ SQL ở lần sử dụng đầu tiên; các lần sau dùng dữ liệu hiện tại đến khi có yêu cầu tải lại.
    /// </summary>
    public static void EnsureLoaded()
    {
        if (PhamViLamSang && clinicalUser is not null && clinicalUser.MaNV != PhienDangNhap.MaNV) XoaCachePhien();
        if (!loaded) RefreshFromDatabase();
    }
    /// <summary>
    /// Dùng chung IDbTransaction cho các thao tác lồng nhau; chụp cache, commit khi action trả true, rollback SQL và khôi phục cache khi false hoặc có lỗi; luôn giải phóng kết nối.
    /// </summary>
    public static bool Transaction(Func<bool> action)
    {
        EnsureLoaded();
        // Lưu form con trong một giao dịch đang mở: dùng lại transaction, không commit độc lập.
        if (batching) return action();
        // Giữ cả đối tượng dòng và giá trị để rollback không phá các tham chiếu binding đang dùng.
        var snapshot = tables.ToDictionary(t => t.Key, t => t.Value.Select(row => (Row: row, Values: Values(row))).ToArray());
        using var pending = new BUS_DONTHUOC().BatDauGiaoDich();
        var connection = pending.Connection!; transaction = pending; batching = true;
        var committed = false;
        try
        {
            if (!action()) return false;
            // SQL phải commit thành công trước khi coi dữ liệu cache là kết quả lưu chính thức.
            pending.Commit(); committed = true; return true;
        }
        finally
        {
            try
            {
                if (!committed)
                {
                    // Dù rollback SQL bị lỗi do mất kết nối, vẫn khôi phục dữ liệu giao diện trong finally.
                    try { if (pending.Connection is not null) pending.Rollback(); }
                    finally
                    {
                        foreach (var table in snapshot)
                        {
                            var rows = Rows(table.Key); rows.Clear();
                            foreach (var entry in table.Value)
                            {
                                var values = (IDictionary<string, object?>)entry.Row; values.Clear();
                                foreach (var pair in entry.Values) values[pair.Key] = pair.Value;
                                rows.Add(entry.Row);
                            }
                        }
                    }
                }
            }
            finally
            {
                // Mọi đường thoát đều bỏ trạng thái đang lưu để các thao tác sau không dùng transaction đã kết thúc.
                transaction = null; batching = false;
                try { connection.Dispose(); }
                finally { Notify(); }
            }
        }
    }

    /// <summary>
    /// Đọc các bảng hồ sơ, danh mục và tài chính qua BUS trong cùng một giao dịch đọc để tạo snapshot nhất quán cho giao diện.
    /// </summary>
    private static Dictionary<string, object[]> LoadRecords()
    {
        using var pending = new BUS_DONTHUOC().BatDauGiaoDich();
        using var connection = pending.Connection!;
        var transaction = pending;
        var scoped = PhamViLamSang ? new BUS_KHAMDIEUTRI(transaction).LayDuLieuHoSoGiaoDien(clinicalScope,new BUS_QUYENNGHIEPVU(transaction).LayNguCanh(PhienDangNhap.MaNV)) : null;
        clinicalUser = scoped?.NguoiDung;
        if (scoped is not null)
        {
            pending.Commit();
            return scoped.CacBang.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        }
        var snapshot = new Dictionary<string, object[]>
        {
            ["BENH_NHAN"] = new BUS_BENHNHAN(transaction).layToanBoBenhNhan().Cast<object>().ToArray(),
            ["BAO_TRI_THIET_BI"] = new BUS_BAOTRITHIETBI(transaction).layToanBoBaoTriThietBi().Cast<object>().ToArray(),
            ["THIET_BI"] = new BUS_THIETBI(transaction).layToanBoThietBi().Cast<object>().ToArray(),
            ["NHA_CUNG_CAP"] = new BUS_NHACUNGCAP(transaction).layToanBoNhaCungCap().Cast<object>().ToArray(),
            ["HOA_DON_NHAP"] = new BUS_HOADONNHAP(transaction).layToanBoHoaDonNhap().Cast<object>().ToArray(),
            ["HO_SO_BENH_AN"] = new BUS_HOSOBENHAN(transaction).layToanBoHoSoBenhAn().Cast<object>().ToArray(),
            ["PHIEU_KHAM_BENH"] = new BUS_PHIEUKHAMBENH(transaction).layToanBoPhieuKhamBenh().Cast<object>().ToArray(),
            ["LICH_KHAM"] = new BUS_LICHKHAM(transaction).layToanBoLichKham().Cast<object>().ToArray(),
            ["LICH_SU_KHAM_BENH"] = new BUS_LICHSUKHAMBENH(transaction).layToanBoLichSuKhamBenh().Cast<object>().ToArray(),
            ["TINH_TRANG_BENH"] = new BUS_TINHTRANGBENH(transaction).layToanBoTinhTrangBenh().Cast<object>().ToArray(),
            ["KE_HOACH_DIEU_TRI"] = new BUS_KEHOACHDIEUTRI(transaction).layToanBoKeHoachDieuTri().Cast<object>().ToArray(),
            ["CHI_TIET_KE_HOACH"] = new BUS_CHITIETKEHOACH(transaction).layToanBoChiTietKeHoach().Cast<object>().ToArray(),
            ["DICH_VU_THUC_HIEN"] = new BUS_DICHVUTHUCHIEN(transaction).layToanBoDichVuThucHien().Cast<object>().ToArray(),
            ["DON_THUOC"] = new BUS_DONTHUOC(transaction).layToanBoDonThuoc().Cast<object>().ToArray(),
            ["CHI_TIET_DON_THUOC"] = new BUS_CHITIETDONTHUOC(transaction).layToanBoChiTietDonThuoc().Cast<object>().ToArray(),
            ["NHAN_VIEN"] = new BUS_NHANVIEN().layToanBoNhanVien().Cast<object>().ToArray(),
            ["PHONG"] = new BUS_PHONG(transaction).layToanBoPhong().Cast<object>().ToArray(),
            ["CA_LAM"] = new BUS_CALAM(transaction).layToanBoCaLam().Cast<object>().ToArray(),
            ["DICH_VU"] = new BUS_DICHVU(transaction).layToanBoDichVu().Cast<object>().ToArray(),
            ["THUOC"] = new BUS_THUOC(transaction).layToanBoThuoc().Cast<object>().ToArray(),
            ["HANG_HOA"] = new BUS_HANGHOA(transaction).layToanBoHangHoa().Cast<object>().ToArray(),
            ["KHO"] = new BUS_KHO(transaction).layToanBoKho().Cast<object>().ToArray(),
            ["LO_HANG_HOA"] = new BUS_LOHANGHOA(transaction).layToanBoLoHangHoa().Cast<object>().ToArray(),
            ["HOA_DON"] = new BUS_HOADON(transaction).LayDanhSach().Cast<object>().ToArray(),
            ["CHI_TIET_HOA_DON"] = new BUS_CHITIETHOADON(transaction).LayDanhSach().Cast<object>().ToArray(),
            ["THANH_TOAN"] = new BUS_THANHTOAN(transaction).LayDanhSach().Cast<object>().ToArray(),
            ["PHIEU_XUAT"] = new BUS_PHIEUXUAT(transaction).LayDanhSach().Cast<object>().ToArray(),
            ["CHI_TIET_PHIEU_XUAT"] = new BUS_CHITIETPHIEUXUAT(transaction).LayDanhSach().Cast<object>().ToArray(),
        };
        pending.Commit();
        return snapshot;
    }
    /// <summary>
    /// Chọn BUS đúng bảng, truyền transaction và gọi SaveEntity với các hàm thêm/sửa/đọc của thực thể; chứng từ ngoài luồng chung phải đi qua BUS chuyên dụng.
    /// </summary>
    private static Dictionary<string, object?> Save(string table, IDictionary<string, object?> values, string? id = null, IDictionary<string, object?>? expected = null, bool statusOnly = false)
    {
        if(PhamViLamSang&&table=="DICH_VU_THUC_HIEN")throw new LoiKhamException("MaDVThucHien","Lưu và xác nhận tại Dịch vụ thực hiện để kiểm tra nguồn, phần lượng còn lại và người được giao.");
        if (PhamViLamSang && table is not ("NHA_CUNG_CAP" or "BAO_TRI_THIET_BI"))
        {
            if (table == "LICH_KHAM") { if (!NguoiDungLamSang.LaAdmin) throw new TuChoiQuyenNghiepVuException(); }
            else new BUS_QUYENNGHIEPVU(transaction).KiemTraGhiHoSo(table, values, id, false, NguoiDungLamSang);
            if(table is "DON_THUOC" or "CHI_TIET_DON_THUOC")throw new LoiKhamException("MaDT","Lưu/ký cả đơn tại Đơn thuốc để kiểm tra mọi dòng và chốt chỉ định.");
        }
        Dictionary<string, object?> saved;
        switch (table)
        {
            case "NHA_CUNG_CAP":
                {
                    var bus = new BUS_NHACUNGCAP(transaction);
                    saved = SaveEntity<ET_NHACUNGCAP>("MaNCC", values, id, expected, statusOnly,
                        bus.themNhaCungCap, bus.suaNhaCungCap, bus.layNhaCungCapTheoMa);
                    break;
                }
            case "BAO_TRI_THIET_BI":
                {
                    var bus = new BUS_BAOTRITHIETBI(transaction);
                    saved = SaveEntity<ET_BAOTRITHIETBI>("MaBT", values, id, expected, statusOnly,
                        bus.themBaoTriThietBi, bus.suaBaoTriThietBi, bus.layBaoTriThietBiTheoMa);
                    break;
                }
            case "BENH_NHAN":
                {
                    var bus = new BUS_BENHNHAN(transaction);
                    saved = SaveEntity<ET_BENHNHAN>("MaBN", values, id, expected, statusOnly,
                        bus.themBenhNhan, bus.suaBenhNhan, bus.layBenhNhanTheoMa);
                    break;
                }
            case "HO_SO_BENH_AN":
                {
                    var bus = new BUS_HOSOBENHAN(transaction);
                    saved = SaveEntity<ET_HOSOBENHAN>("MaHS", values, id, expected, statusOnly,
                        bus.themHoSoBenhAn, bus.suaHoSoBenhAn, bus.layHoSoBenhAnTheoMa);
                    break;
                }
            case "PHIEU_KHAM_BENH":
                {
                    var bus = new BUS_PHIEUKHAMBENH(transaction);
                    saved = SaveEntity<ET_PHIEUKHAMBENH>("MaPhieu", values, id, expected, statusOnly,
                        bus.themPhieuKhamBenh, bus.suaPhieuKhamBenh, bus.layPhieuKhamBenhTheoMa);
                    break;
                }
            case "LICH_KHAM":
                {
                    var bus = new BUS_LICHKHAM(transaction);
                    saved = SaveEntity<ET_LICHKHAM>("MaLich", values, id, expected, statusOnly,
                        bus.themLichKham, bus.suaLichKham, bus.layLichKhamTheoMa);
                    break;
                }
            case "LICH_SU_KHAM_BENH":
                {
                    var bus = new BUS_LICHSUKHAMBENH(transaction);
                    saved = SaveEntity<ET_LICHSUKHAMBENH>("MaLS", values, id, expected, statusOnly,
                        bus.themLichSuKhamBenh, bus.suaLichSuKhamBenh, bus.layLichSuKhamBenhTheoMa);
                    break;
                }
            case "TINH_TRANG_BENH":
                {
                    var bus = new BUS_TINHTRANGBENH(transaction);
                    saved = SaveEntity<ET_TINHTRANGBENH>("MaTTB", values, id, expected, statusOnly,
                        bus.themTinhTrangBenh, bus.suaTinhTrangBenh, bus.layTinhTrangBenhTheoMa);
                    break;
                }
            case "KE_HOACH_DIEU_TRI":
                {
                    var bus = new BUS_KEHOACHDIEUTRI(transaction);
                    saved = SaveEntity<ET_KEHOACHDIEUTRI>("MaKHDT", values, id, expected, statusOnly,
                        bus.themKeHoachDieuTri, bus.suaKeHoachDieuTri, bus.layKeHoachDieuTriTheoMa);
                    break;
                }
            case "CHI_TIET_KE_HOACH":
                {
                    var bus = new BUS_CHITIETKEHOACH(transaction);
                    saved = SaveEntity<ET_CHITIETKEHOACH>("MaCTKH", values, id, expected, statusOnly,
                        bus.themChiTietKeHoach, bus.suaChiTietKeHoach, bus.layChiTietKeHoachTheoMa);
                    break;
                }
            case "DICH_VU_THUC_HIEN":
                {
                    var bus = new BUS_DICHVUTHUCHIEN(transaction);
                    saved = SaveEntity<ET_DICHVUTHUCHIEN>("MaDVThucHien", values, id, expected, statusOnly,
                        bus.themDichVuThucHien, bus.suaDichVuThucHien, bus.layDichVuThucHienTheoMa);
                    break;
                }
            case "DON_THUOC":
                {
                    var bus = new BUS_DONTHUOC(transaction);
                    saved = SaveEntity<ET_DONTHUOC>("MaDT", values, id, expected, statusOnly,
                        bus.themDonThuoc, bus.suaDonThuoc, bus.layDonThuocTheoMa);
                    break;
                }
            case "CHI_TIET_DON_THUOC":
                {
                    var bus = new BUS_CHITIETDONTHUOC(transaction);
                    saved = SaveEntity<ET_CHITIETDONTHUOC>("MaCTDT", values, id, expected, statusOnly,
                        bus.themChiTietDonThuoc, bus.suaChiTietDonThuoc, bus.layChiTietDonThuocTheoMa);
                    break;
                }
            default: throw new InvalidOperationException("Chứng từ này phải được xử lý qua BUS của bảng tương ứng.");
        }
        return saved;
    }
    /// <summary>
    /// Gộp giá trị form với bản ghi SQL hiện tại, chuyển dictionary thành ET, gọi thêm hoặc sửa kèm dữ liệu gốc rồi đọc lại bản đã lưu để nhận mã và cột tính toán.
    /// </summary>
    private static Dictionary<string, object?> SaveEntity<T>(string key, IDictionary<string, object?> values,
        string? id, IDictionary<string, object?>? expected, bool statusOnly,
        Func<T, bool> add, Func<T, T?, bool, bool> update, Func<string, T?> get) where T : class, new()
    {
        var merged = id is null ? new Dictionary<string, object?>() : Values(get(id)
            ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại."));
        foreach (var pair in values) merged[pair.Key] = pair.Value;
        var entity = UiRecord.ChuyenDoi<T>(merged);
        if (id is null) add(entity);
        else
        {
            System.ComponentModel.TypeDescriptor.GetProperties(entity)[key]!.SetValue(entity, id);
            update(entity, expected is null ? null : UiRecord.ChuyenDoi<T>(expected), statusOnly);
        }
        var savedId = Convert.ToString(Values(entity)[key])!;
        return Values(get(savedId) ?? throw new InvalidOperationException("Không đọc lại được bản ghi vừa lưu."));
    }

    /// <summary>
    /// Chọn BUS theo bảng và gọi xóa kèm snapshot dữ liệu gốc; không tự dựng câu SQL trong tầng giao diện.
    /// </summary>
    private static void DeleteRecord(string table, string id, IDictionary<string, object?>? expected = null)
    {
        if(PhamViLamSang&&table=="DICH_VU_THUC_HIEN")throw new LoiKhamException("MaDVThucHien","Hủy nháp tại Dịch vụ thực hiện để giữ lịch sử và trả phần lượng dự kiến.");
        if (PhamViLamSang && table is not ("NHA_CUNG_CAP" or "BAO_TRI_THIET_BI"))
        {
            if (table == "LICH_KHAM") { if (!NguoiDungLamSang.LaAdmin) throw new TuChoiQuyenNghiepVuException(); }
            else new BUS_QUYENNGHIEPVU(transaction).KiemTraGhiHoSo(table, expected ?? new Dictionary<string, object?>(), id, true, NguoiDungLamSang);
            if(table is "DON_THUOC" or "CHI_TIET_DON_THUOC")throw new LoiKhamException("MaDT","Hủy nháp tại Đơn thuốc để giữ lịch sử và nguồn thay thế.");
        }
        switch (table)
        {
            case "NHA_CUNG_CAP":
                new BUS_NHACUNGCAP(transaction).xoaNhaCungCap(id, expected is null ? null : UiRecord.ChuyenDoi<ET_NHACUNGCAP>(expected)); break;
            case "BAO_TRI_THIET_BI":
                new BUS_BAOTRITHIETBI(transaction).xoaBaoTriThietBi(id, expected is null ? null : UiRecord.ChuyenDoi<ET_BAOTRITHIETBI>(expected)); break;
            case "BENH_NHAN":
                new BUS_BENHNHAN(transaction).xoaBenhNhan(id, expected is null ? null : UiRecord.ChuyenDoi<ET_BENHNHAN>(expected)); break;
            case "HO_SO_BENH_AN":
                new BUS_HOSOBENHAN(transaction).xoaHoSoBenhAn(id, expected is null ? null : UiRecord.ChuyenDoi<ET_HOSOBENHAN>(expected)); break;
            case "PHIEU_KHAM_BENH":
                new BUS_PHIEUKHAMBENH(transaction).xoaPhieuKhamBenh(id, expected is null ? null : UiRecord.ChuyenDoi<ET_PHIEUKHAMBENH>(expected)); break;
            case "LICH_KHAM":
                new BUS_LICHKHAM(transaction).xoaLichKham(id, expected is null ? null : UiRecord.ChuyenDoi<ET_LICHKHAM>(expected)); break;
            case "LICH_SU_KHAM_BENH":
                new BUS_LICHSUKHAMBENH(transaction).xoaLichSuKhamBenh(id, expected is null ? null : UiRecord.ChuyenDoi<ET_LICHSUKHAMBENH>(expected)); break;
            case "TINH_TRANG_BENH":
                new BUS_TINHTRANGBENH(transaction).xoaTinhTrangBenh(id, expected is null ? null : UiRecord.ChuyenDoi<ET_TINHTRANGBENH>(expected)); break;
            case "KE_HOACH_DIEU_TRI":
                new BUS_KEHOACHDIEUTRI(transaction).xoaKeHoachDieuTri(id, expected is null ? null : UiRecord.ChuyenDoi<ET_KEHOACHDIEUTRI>(expected)); break;
            case "CHI_TIET_KE_HOACH":
                new BUS_CHITIETKEHOACH(transaction).xoaChiTietKeHoach(id, expected is null ? null : UiRecord.ChuyenDoi<ET_CHITIETKEHOACH>(expected)); break;
            case "DICH_VU_THUC_HIEN":
                new BUS_DICHVUTHUCHIEN(transaction).xoaDichVuThucHien(id, expected is null ? null : UiRecord.ChuyenDoi<ET_DICHVUTHUCHIEN>(expected)); break;
            case "DON_THUOC":
                new BUS_DONTHUOC(transaction).xoaDonThuoc(id, expected is null ? null : UiRecord.ChuyenDoi<ET_DONTHUOC>(expected)); break;
            case "CHI_TIET_DON_THUOC":
                new BUS_CHITIETDONTHUOC(transaction).xoaChiTietDonThuoc(id, expected is null ? null : UiRecord.ChuyenDoi<ET_CHITIETDONTHUOC>(expected)); break;
            default: throw new InvalidOperationException("Chứng từ này phải được xử lý qua BUS của bảng tương ứng.");
        }
    }

    /// <summary>
    /// Đảm bảo dữ liệu đã nạp và trả ObservableCollection của bảng để control binding; tạo collection trống nếu chưa có bảng trong cache.
    /// </summary>
    public static ObservableCollection<object> Rows(string table)
    {
        EnsureLoaded();
        if (!tables.TryGetValue(table, out var rows)) tables[table] = rows = new();
        return rows;
    }

    // Chạy trên luồng giao diện WPF; chuẩn hóa các bản ghi ET/đối tượng, dictionary và DataRowView.
    /// <summary>
    /// Chuẩn hóa ET/dictionary/DataRowView về bản ghi động, thay nội dung collection và báo thay đổi; gọi trên luồng giao diện WPF.
    /// </summary>
    public static void Replace(string table, IEnumerable<object> rows)
    {
        var normalized = rows.Select(row =>
        {
            IDictionary<string, object?> result = new ExpandoObject();
            if (row is IDictionary<string, object?> dictionary)
                foreach (var pair in dictionary) result[pair.Key.Equals("sdt", StringComparison.OrdinalIgnoreCase) ? "SDT" : pair.Key] = pair.Value;
            else if (row is IDictionary values)
                foreach (DictionaryEntry pair in values) result[Convert.ToString(pair.Key)!] = pair.Value;
            else
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(row))
                    result[property.Name.Equals("sdt", StringComparison.OrdinalIgnoreCase) ? "SDT" : property.Name] = property.GetValue(row);
            return (object)result;
        }).ToArray();
        var target = Rows(table);
        target.Clear();
        foreach (var row in normalized) target.Add(row);
        Notify();
    }

    /// <summary>
    /// Tra tên cột khóa chính đã khai báo cho bảng; trả chuỗi rỗng nếu bảng không có trong danh sách.
    /// </summary>
    public static string Key(string table) => keys.GetValueOrDefault(table, "");
    /// <summary>
    /// Sao chép giá trị bản ghi thành dictionary; dùng dictionary trực tiếp hoặc TypeDescriptor cho đối tượng ET.
    /// </summary>
    public static Dictionary<string, object?> Values(object row)
    {
        if (row is IDictionary<string, object?> values) return new(values);
        return TypeDescriptor.GetProperties(row).Cast<PropertyDescriptor>().ToDictionary(p => p.Name, p => p.GetValue(row));
    }
    /// <summary>
    /// So sánh từng giá trị snapshot với bản ghi hiện tại; báo lỗi nếu bản ghi bị xóa hoặc thay đổi sau khi mở form.
    /// </summary>
    public static void AssertUnchanged(string table, string id, IDictionary<string, object?> original)
    {
        var current = Find(table, id);
        if (current is null || original.Any(pair => !Equals(pair.Value, UiRecord.Get(current, pair.Key))))
            throw new InvalidOperationException("Dữ liệu đã thay đổi sau khi mở form. Đóng form và mở lại để giữ nội dung mới nhất.");
    }
    /// <summary>
    /// Xác định khả năng sửa theo trạng thái và quyền sửa bản cha; không cho sửa bước có dịch vụ xác nhận, giữ nội dung chứng từ đã chốt.
    /// </summary>
    public static bool CanEdit(string table, object row)
    {
        if (table == "BAO_TRI_THIET_BI") return UiRecord.Text(row, "TrangThai") == "Chưa xác nhận";
        if (PhamViLamSang && !QuyenNghiepVu.DuocGhi(table, row)) return false;
        if (table is "BENH_NHAN" or "HO_SO_BENH_AN" or "NHA_CUNG_CAP") return true;
        if (table == "CHI_TIET_KE_HOACH" && Children("DICH_VU_THUC_HIEN", "MaCTKH", UiRecord.Get(row, "MaCTKH")).Any(r => UiRecord.Text(r, "TrangThai") == "Đã xác nhận")) return false;
        var parentLink = table switch { "CHI_TIET_DON_THUOC" => ("DON_THUOC", "MaDT"), "CHI_TIET_KE_HOACH" => ("KE_HOACH_DIEU_TRI", "MaKHDT"), "TINH_TRANG_BENH" => ("LICH_SU_KHAM_BENH", "MaLS"), _ => ("", "") };
        if (parentLink.Item1.Length > 0)
        {
            var parent = Find(parentLink.Item1, UiRecord.Get(row, parentLink.Item2));
            if (parent is null || !CanEdit(parentLink.Item1, parent)) return false;
        }
        var state = UiRecord.Text(row, "TrangThai");
        return state is "" or "Nháp" or "Chưa xác nhận" or "Chờ xác nhận" or "Đang lập" or "Dự kiến" or "Chờ thực hiện";
    }
    /// <summary>
    /// Tự mở giao dịch nếu cần, kiểm tra bản nháp/liên kết cha/snapshot rồi lưu qua BUS; chỉ đưa bản ghi đã lưu và đọc lại từ SQL vào cache, cập nhật tổng kế hoạch.
    /// </summary>
    public static object Upsert(string table, IDictionary<string, object?> values, object? existing = null, IDictionary<string, object?>? expected = null)
    {
        if (!batching)
        {
            object? saved = null;
            Transaction(() => { saved = Upsert(table, values, existing, expected); return true; });
            return saved!;
        }
        var key = Key(table);
        var id = (values.TryGetValue(key, out var suppliedId) ? suppliedId : null)?.ToString() ?? "";
        var target = existing is null ? Find(table, id) : Find(table, UiRecord.Get(existing, key));
        if (target is not null && !CanEdit(table, target)) throw new InvalidOperationException("Bản ghi đã xác nhận: hãy lập bản mới để thay đổi nội dung.");
        if (target is not null) id = UiRecord.Text(target, key);
        if (target is not null && expected is not null) AssertUnchanged(table, id, expected);
        IDictionary<string, object?> record = target as IDictionary<string, object?> ?? new ExpandoObject();
        if (target is not null && target is not IDictionary<string, object?>) throw new InvalidOperationException("Hãy chuẩn hóa nguồn qua DuLieuGiaoDien.Replace.");
        var merged = target is null ? new Dictionary<string, object?>() : Values(target);
        foreach (var pair in values) merged[pair.Key] = pair.Value;
        var parentField = table switch { "CHI_TIET_DON_THUOC" => "MaDT", "CHI_TIET_KE_HOACH" => "MaKHDT", "TINH_TRANG_BENH" => "MaLS", _ => "" };
        if (parentField.Length > 0)
        {
            var parentId = values.TryGetValue(parentField, out var suppliedParent) ? suppliedParent : target is null ? null : UiRecord.Get(target, parentField);
            var parentTable = ForeignTables[parentField]; var parent = Find(parentTable, parentId);
            if (parent is null || !CanEdit(parentTable, parent)) throw new InvalidOperationException("Chỉ thêm hoặc sửa nội dung trong bản nháp có liên kết hợp lệ.");
        }
        // AI bổ sung: mọi luồng lưu từ giao diện đều kiểm tra kiểu/phạm vi/độ dài trước khi gọi BUS.
        KiemTraNhapLieu.ValidateValues(table, merged);
        var persisted = Save(table, merged, target is null ? null : id, target is null ? null : expected ?? Values(target));
        record.Clear();
        foreach (var pair in persisted) record[pair.Key] = pair.Value is DBNull ? null : pair.Value;
        if (target is null) Rows(table).Add(record);
        UpdatePlanTotal(table, record);
        Notify();
        return record;
    }
    /// <summary>
    /// Cập nhật một trường qua BUS trong giao dịch; kiểm soát trường hợp đổi trạng thái và gắn NgayXacNhan khi xác nhận lần khám, sau đó đồng bộ cache.
    /// </summary>
    public static void SetValue(string table, object row, string field, object? value)
    {
        if(table=="BAO_TRI_THIET_BI" && field=="TrangThai") { ChuyenTrangThaiBaoTri(row,Convert.ToString(value)??""); return; }
        if (!batching) { Transaction(() => { SetValue(table, row, field, value); return true; }); return; }
        if (!TruongDuLieu.Tables.TryGetValue(table, out var schema) || !schema.ContainsKey(field)) throw new InvalidOperationException("Trường cập nhật không có trong database.");
        if (table == "LICH_KHAM" && field == "TrangThai" && (UiRecord.Text(row, "TrangThai") is "Hủy" or "Đã đổi" or "Hoàn thành") && Convert.ToString(value) != UiRecord.Text(row, "TrangThai")) throw new InvalidOperationException("Lịch đã kết thúc được giữ nguyên. Hãy tạo một lịch mới.");
        if (!CanEdit(table, row) && field is not "TrangThai" and not "LyDoThayDoi" and not "NgayXacNhan" and not "NgayCapNhat") throw new InvalidOperationException("Không sửa nội dung đã xác nhận.");
        if (row is not IDictionary<string, object?> values) throw new InvalidOperationException("Nguồn dữ liệu cần được chuẩn hóa.");
        var changes = new Dictionary<string, object?> { [field] = value };
        if (table == "LICH_SU_KHAM_BENH" && field == "TrangThai" && Convert.ToString(value) == "Đã xác nhận") changes["NgayXacNhan"] = DateTime.Now;
        var changesToSave = Values(row); foreach (var change in changes) changesToSave[change.Key] = change.Value;
        var persisted = Save(table, changesToSave, UiRecord.Text(row, Key(table)), Values(row), statusOnly: field is "TrangThai" or "LyDoThayDoi" or "NgayCapNhat");
        values.Clear(); foreach (var pair in persisted) values[pair.Key] = pair.Value is DBNull ? null : pair.Value;
        UpdatePlanTotal(table, row); Notify();
    }
    /// <summary>
    /// Chỉ xóa bản nháp còn được sửa và không có dòng liên kết trong cache; gọi BUS xóa, cập nhật collection và tổng kế hoạch trong cùng giao dịch.
    /// </summary>
    public static void Delete(string table, object row)
    {
        if(table=="BAO_TRI_THIET_BI")throw new InvalidOperationException("Không xóa lịch sử; chỉ phiếu Chưa xác nhận được chuyển sang Hủy.");
        if (!batching) { Transaction(() => { Delete(table, row); return true; }); return; }
        if (!CanEdit(table, row)) throw new InvalidOperationException("Chỉ xóa bản nháp chưa xác nhận.");
        var key = Key(table); var id = UiRecord.Text(row, key);
        foreach (var foreign in ForeignTables.Where(p => p.Value == table))
            if (tables.Any(t => t.Key != table && t.Value.Any(r => UiRecord.Text(r, foreign.Key) == id)))
                throw new InvalidOperationException("Bản ghi đang có dữ liệu liên kết. Không thể xóa.");
        DeleteRecord(table, id, Values(row));
        Rows(table).Remove(row); UpdatePlanTotal(table, row); Notify();
    }

    /// <summary>Lưu phiếu và đọc lại cả thiết bị trong giao dịch hiện tại; chỉ thông báo sau khi commit.</summary>
    public static object ChuyenTrangThaiBaoTri(object record,string target)
    {
        if(!batching){object? saved=null;Transaction(()=>{saved=ChuyenTrangThaiBaoTri(record,target);return true;});return saved!;}
        var original=UiRecord.ChuyenDoi<ET_BAOTRITHIETBI>(Values(record));
        var bus=new BUS_BAOTRITHIETBI(transaction);
        bus.chuyenTrangThaiBaoTri(original.MaBT,target,original);
        var fresh=bus.layBaoTriThietBiTheoMa(original.MaBT)??throw new InvalidOperationException("Không đọc lại được phiếu bảo trì.");
        var current=Find("BAO_TRI_THIET_BI",original.MaBT) as IDictionary<string,object?>
            ??throw new InvalidOperationException("Phiếu không còn trong danh sách; hãy tải lại.");
        current.Clear();foreach(var field in Values(fresh))current[field.Key]=field.Value;
        var asset=new BUS_THIETBI(transaction).layThietBiTheoMa(fresh.MaTB);
        if(asset is not null && Find("THIET_BI",fresh.MaTB) is IDictionary<string,object?> cached){cached.Clear();foreach(var field in Values(asset))cached[field.Key]=field.Value;}
        Notify();return current;
    }

    /// <summary>
    /// Khi chi tiết kế hoạch thay đổi, cộng ThanhTienDuKien của các bước chưa hủy và cập nhật tổng trên bản kế hoạch trong cache.
    /// </summary>
    private static void UpdatePlanTotal(string table, object row)
    {
        if (table != "CHI_TIET_KE_HOACH") return;
        var plan = Find("KE_HOACH_DIEU_TRI", UiRecord.Get(row, "MaKHDT"));
        if (plan is IDictionary<string, object?> values)
            values["TongTienDuKien"] = Children(table, "MaKHDT", values["MaKHDT"]).Where(r => UiRecord.Text(r, "TrangThai") != "Hủy")
                .Sum(r => Convert.ToDecimal(UiRecord.Get(r, "ThanhTienDuKien") ?? 0));
    }

    /// <summary>
    /// Tìm đúng khóa chính trong cache theo tên bảng; mã trống hoặc bảng chưa đăng ký trả null, không lấy một bản ghi khác thay thế.
    /// </summary>
    public static object? Find(string table, object? id)
    {
        var text = Convert.ToString(id, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text) || !keys.TryGetValue(table, out var key)) return null;
        return Rows(table).FirstOrDefault(row => UiRecord.Text(row, key) == text);
    }

    /// <summary>
    /// Lấy các bản ghi có cột khóa ngoại bằng mã cha; mã cha trống trả danh sách trống để tránh lộ dữ liệu không liên quan.
    /// </summary>
    public static object[] Children(string table, string field, object? id)
    {
        var text = Convert.ToString(id, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(text) ? Array.Empty<object>() : Rows(table).Where(row => UiRecord.Text(row, field) == text).ToArray();
    }

    /// <summary>
    /// Lần theo chuỗi khóa ngoại từ bảng hiện tại về BENH_NHAN; trả mã bệnh nhân hoặc chuỗi rỗng khi liên kết không tồn tại.
    /// </summary>
    public static string PatientId(string table, object row)
    {
        if (table is "BENH_NHAN" or "HO_SO_BENH_AN" or "LICH_KHAM") return UiRecord.Text(row, "MaBN");
        var parent = table switch
        {
            "PHIEU_KHAM_BENH" => ("HO_SO_BENH_AN", "MaHS"),
            "LICH_SU_KHAM_BENH" => ("PHIEU_KHAM_BENH", "MaPhieu"),
            "TINH_TRANG_BENH" or "KE_HOACH_DIEU_TRI" or "DICH_VU_THUC_HIEN" or "DON_THUOC" => ("LICH_SU_KHAM_BENH", "MaLS"),
            "CHI_TIET_KE_HOACH" => ("KE_HOACH_DIEU_TRI", "MaKHDT"),
            "CHI_TIET_DON_THUOC" => ("DON_THUOC", "MaDT"),
            "HOA_DON" or "PHIEU_XUAT" => ("LICH_SU_KHAM_BENH", "MaLS"),
            "CHI_TIET_HOA_DON" or "THANH_TOAN" => ("HOA_DON", "MaHD"),
            "CHI_TIET_PHIEU_XUAT" => ("PHIEU_XUAT", "MaPX"),
            _ => ("", "")
        };
        var linked = Find(parent.Item1, UiRecord.Get(row, parent.Item2));
        return linked is null ? "" : PatientId(parent.Item1, linked);
    }

    /// <summary>
    /// Ghép mã với tên dễ đọc của bệnh nhân/danh mục; thuốc lấy tên hàng hóa và hàm lượng, các chứng từ dùng tên bệnh nhân liên kết.
    /// </summary>
    public static string Label(string table, object row)
    {
        var name = table switch
        {
            "BENH_NHAN" => "HoTenBN",
            "NHAN_VIEN" => "HoTen",
            "PHONG" => "TenPhong",
            "DICH_VU" => "TenDV",
            "HANG_HOA" => "TenHH",
            "NHA_CUNG_CAP" => "TenNCC",
            "KHO" => "TenKho",
            _ => ""
        };
        var key = keys.GetValueOrDefault(table, "");
        var label = UiRecord.Text(row, name);
        if (table == "THIET_BI")
        {
            var hangHoa = Find("HANG_HOA", UiRecord.Get(row, "MaHH"));
            label = (hangHoa is null ? "" : UiRecord.Text(hangHoa, "TenHH")) + " · " + UiRecord.Text(row, "SoSerial");
        }
        if (table == "THUOC")
        {
            var goods = Find("HANG_HOA", UiRecord.Get(row, "MaHH"));
            label = (goods is null ? "" : UiRecord.Text(goods, "TenHH")) + " " + UiRecord.Text(row, "HamLuong");
        }
        if (table == "CA_LAM") label = UiRecord.Text(row, "GioBatDau") + " – " + UiRecord.Text(row, "GioKetThuc");
        if (name.Length == 0 && table is not "THUOC" and not "CA_LAM" and not "THIET_BI")
        {
            var patient = Find("BENH_NHAN", PatientId(table, row));
            label = patient is null ? "" : UiRecord.Text(patient, "HoTenBN");
        }
        return string.Join(" · ", new[] { UiRecord.Text(row, key), label.Trim() }.Where(value => value.Length > 0));
    }

    /// <summary>
    /// Tạo các cặp Value/Label cho ComboBox theo bảng và phạm vi bệnh nhân; có thể thêm lựa chọn Tất cả với mã rỗng.
    /// </summary>
    public static IEnumerable<LuaChonDanhMuc> Options(string table, bool all = false, string? patient = null)
    {
        if (all) yield return new("", "Tất cả");
        foreach (var row in Rows(table).Where(row => string.IsNullOrEmpty(patient) || PatientId(table, row) == patient))
            yield return new(UiRecord.Text(row, keys.GetValueOrDefault(table, "")), Label(table, row));
    }

    /// <summary>
    /// Đổi mã khóa ngoại thành nhãn danh mục; trường thông thường giữ nguyên giá trị, mã không tìm thấy vẫn được hiện để nhận biết liên kết lỗi.
    /// </summary>
    public static string Lookup(string field, object? id)
    {
        if (id is null) return "Chưa có thông tin";
        var text = Convert.ToString(id, CultureInfo.CurrentCulture) ?? "";
        if (!ForeignTables.TryGetValue(field, out var table)) return text;
        var row = Find(table, id);
        return row is null ? text : Label(table, row);
    }
}

