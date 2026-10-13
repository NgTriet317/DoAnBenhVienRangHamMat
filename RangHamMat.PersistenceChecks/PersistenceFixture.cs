// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using RangHamMat.Controls;
using BUS;
using ET;
using System.Data;
using System.ComponentModel;
using System.Globalization;
/// <summary>
/// Hạ tầng kiểm tra đọc/ghi SQL qua BUS trong giao dịch thử; giữ transaction/kết nối và hỗ trợ rollback các bản ghi kiểm tra.
/// </summary>
internal sealed class PersistenceFixture : IDisposable
{
    /// <summary>
    /// Đọc các property của ET thành dictionary để so sánh giá trị trước/sau khi lưu trong kiểm tra persistence.
    /// </summary>
    private static Dictionary<string, object?> Values(object record) => GiaTri(record);
    private IDbTransaction? transaction;
    private IDbConnection? connection;
    /// <summary>
    /// Mở transaction kiểm tra bằng BUS và giữ nó trong fixture để các bản ghi thử cùng được rollback.
    /// </summary>
    public void Begin() { transaction = new BUS_DONTHUOC().BatDauGiaoDich(); connection = transaction.Connection; }
    /// <summary>
    /// Rollback transaction đang mở, giải phóng kết nối và xóa tham chiếu để dữ liệu kiểm tra không tồn tại sau khi kết thúc.
    /// </summary>
    public void Rollback() { transaction?.Rollback(); Dispose(); }
    /// <summary>
    /// Dispose transaction và kết nối rồi xóa tham chiếu; transaction chưa commit được provider hủy khi giải phóng.
    /// </summary>
    public void Dispose() { transaction?.Dispose(); transaction=null; connection?.Dispose(); connection=null; }
    /// <summary>
    /// Đọc các bảng thử qua BUS sử dụng transaction của fixture, giúp đối chiếu dữ liệu SQL ngay trong giao dịch thử.
    /// </summary>
    public Dictionary<string, object[]> Load() {
                return new Dictionary<string, object[]>
                {
                    ["BENH_NHAN"] = new BUS_BENHNHAN(transaction).layToanBoBenhNhan().Cast<object>().ToArray(),
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
                    ["NHAN_VIEN"] = new BUS_NHANVIEN(transaction).layToanBoNhanVien().Cast<object>().ToArray(),
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
    }
    /// <summary>
    /// Chọn BUS theo bảng và chuyển việc thêm/sửa/đọc ET cho SaveEntity; mọi thay đổi dùng transaction của fixture.
    /// </summary>
    public Dictionary<string, object?> Save(string table, IDictionary<string, object?> values, string? id = null, IDictionary<string, object?>? expected = null, bool statusOnly = false)
    {
                Dictionary<string, object?> saved;
                switch (table)
                {
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
        /// Chuyển dictionary sang ET, gọi thêm/sửa kèm snapshot và đọc lại bản đã lưu để kiểm tra mã tự cấp/cột tính toán.
        /// </summary>
        private Dictionary<string, object?> SaveEntity<T>(string key, IDictionary<string, object?> values,
            string? id, IDictionary<string, object?>? expected, bool statusOnly,
            Func<T, bool> add, Func<T, T?, bool, bool> update, Func<string, T?> get) where T : class, new()
        {
            var merged = id is null ? new Dictionary<string,object?>() : Values(get(id)
                ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại."));
            foreach(var pair in values) merged[pair.Key]=pair.Value;
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
        /// Chọn hàm xóa của BUS tương ứng và truyền dữ liệu kỳ vọng để thử các quy tắc xóa/đồng thời.
        /// </summary>
        public void Delete(string table, string id, IDictionary<string, object?>? expected = null)
        {
            switch (table)
            {
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
        /// Đọc các property của thực thể thành dictionary tên trường/giá trị để đối chiếu trong kiểm tra persistence.
        /// </summary>
        public static Dictionary<string,object?> GiaTri(object record) => TypeDescriptor.GetProperties(record)
            .Cast<PropertyDescriptor>().ToDictionary(property=>property.Name,property=>property.GetValue(record));

}
