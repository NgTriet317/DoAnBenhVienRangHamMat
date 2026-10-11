using DAL;
using ET;

namespace BUS
{
    // Điều phối các BUS từng bảng cho cache WPF và giao dịch liên bảng.
    // SQL của mỗi bảng nằm trong DAL tương ứng.
    public sealed class BUS_LAMSANG : IDisposable
    {
        private readonly DAL_GIAODICH giaoDich = new();

        public void Begin() => XuLyLoi.ThucHien(() => { giaoDich.Begin(); return true; });
        public void Commit() => XuLyLoi.ThucHien(() => { giaoDich.Commit(); return true; });
        public void Rollback() => giaoDich.Rollback();
        public void Dispose() => giaoDich.Dispose();

        public Dictionary<string, object[]> Load()
        {
            var owns = !giaoDich.DangGiaoDich;
            if (owns) Begin();
            try
            {
                var result = new Dictionary<string, object[]>
                {
                    ["BENH_NHAN"] = new BUS_BENHNHAN(giaoDich).layToanBoBenhNhan().Cast<object>().ToArray(),
                    ["HO_SO_BENH_AN"] = new BUS_HOSOBENHAN(giaoDich).layToanBoHoSoBenhAn().Cast<object>().ToArray(),
                    ["PHIEU_KHAM_BENH"] = new BUS_PHIEUKHAMBENH(giaoDich).layToanBoPhieuKhamBenh().Cast<object>().ToArray(),
                    ["LICH_KHAM"] = new BUS_LICHKHAM(giaoDich).layToanBoLichKham().Cast<object>().ToArray(),
                    ["LICH_SU_KHAM_BENH"] = new BUS_LICHSUKHAMBENH(giaoDich).layToanBoLichSuKhamBenh().Cast<object>().ToArray(),
                    ["TINH_TRANG_BENH"] = new BUS_TINHTRANGBENH(giaoDich).layToanBoTinhTrangBenh().Cast<object>().ToArray(),
                    ["KE_HOACH_DIEU_TRI"] = new BUS_KEHOACHDIEUTRI(giaoDich).layToanBoKeHoachDieuTri().Cast<object>().ToArray(),
                    ["CHI_TIET_KE_HOACH"] = new BUS_CHITIETKEHOACH(giaoDich).layToanBoChiTietKeHoach().Cast<object>().ToArray(),
                    ["DICH_VU_THUC_HIEN"] = new BUS_DICHVUTHUCHIEN(giaoDich).layToanBoDichVuThucHien().Cast<object>().ToArray(),
                    ["DON_THUOC"] = new BUS_DONTHUOC(giaoDich).layToanBoDonThuoc().Cast<object>().ToArray(),
                    ["CHI_TIET_DON_THUOC"] = new BUS_CHITIETDONTHUOC(giaoDich).layToanBoChiTietDonThuoc().Cast<object>().ToArray(),
                    ["NHAN_VIEN"] = new BUS_NHANVIEN(giaoDich).layToanBoNhanVien().Cast<object>().ToArray(),
                    ["PHONG"] = new BUS_PHONG(giaoDich).layToanBoPhong().Cast<object>().ToArray(),
                    ["CA_LAM"] = new BUS_CALAM(giaoDich).layToanBoCaLam().Cast<object>().ToArray(),
                    ["DICH_VU"] = new BUS_DICHVU(giaoDich).layToanBoDichVu().Cast<object>().ToArray(),
                    ["THUOC"] = new BUS_THUOC(giaoDich).layToanBoThuoc().Cast<object>().ToArray(),
                    ["HANG_HOA"] = new BUS_HANGHOA(giaoDich).layToanBoHangHoa().Cast<object>().ToArray(),
                    ["KHO"] = new BUS_KHO(giaoDich).layToanBoKho().Cast<object>().ToArray(),
                    ["LO_HANG_HOA"] = new BUS_LOHANGHOA(giaoDich).layToanBoLoHangHoa().Cast<object>().ToArray(),
                    ["HOA_DON"] = new BUS_HOADON(giaoDich).LayDanhSach().Cast<object>().ToArray(),
                    ["CHI_TIET_HOA_DON"] = new BUS_CHITIETHOADON(giaoDich).LayDanhSach().Cast<object>().ToArray(),
                    ["THANH_TOAN"] = new BUS_THANHTOAN(giaoDich).LayDanhSach().Cast<object>().ToArray(),
                    ["PHIEU_XUAT"] = new BUS_PHIEUXUAT(giaoDich).LayDanhSach().Cast<object>().ToArray(),
                    ["CHI_TIET_PHIEU_XUAT"] = new BUS_CHITIETPHIEUXUAT(giaoDich).LayDanhSach().Cast<object>().ToArray(),
                };
                if (owns) Commit();
                return result;
            }
            catch { if (owns) Rollback(); throw; }
        }

        public Dictionary<string, object?> Save(string table, IDictionary<string, object?> values,
            string? id = null, IDictionary<string, object?>? expected = null, bool statusOnly = false)
        {
            var owns = !giaoDich.DangGiaoDich;
            if (owns) Begin();
            try
            {
                Dictionary<string, object?> saved;
                switch (table)
                {
                    case "BENH_NHAN":
                    {
                        var bus = new BUS_BENHNHAN(giaoDich);
                        saved = SaveEntity<ET_BENHNHAN>("MaBN", values, id, expected, statusOnly,
                            bus.themBenhNhan, bus.suaBenhNhan, bus.layBenhNhanTheoMa);
                        break;
                    }
                    case "HO_SO_BENH_AN":
                    {
                        var bus = new BUS_HOSOBENHAN(giaoDich);
                        saved = SaveEntity<ET_HOSOBENHAN>("MaHS", values, id, expected, statusOnly,
                            bus.themHoSoBenhAn, bus.suaHoSoBenhAn, bus.layHoSoBenhAnTheoMa);
                        break;
                    }
                    case "PHIEU_KHAM_BENH":
                    {
                        var bus = new BUS_PHIEUKHAMBENH(giaoDich);
                        saved = SaveEntity<ET_PHIEUKHAMBENH>("MaPhieu", values, id, expected, statusOnly,
                            bus.themPhieuKhamBenh, bus.suaPhieuKhamBenh, bus.layPhieuKhamBenhTheoMa);
                        break;
                    }
                    case "LICH_KHAM":
                    {
                        var bus = new BUS_LICHKHAM(giaoDich);
                        saved = SaveEntity<ET_LICHKHAM>("MaLich", values, id, expected, statusOnly,
                            bus.themLichKham, bus.suaLichKham, bus.layLichKhamTheoMa);
                        break;
                    }
                    case "LICH_SU_KHAM_BENH":
                    {
                        var bus = new BUS_LICHSUKHAMBENH(giaoDich);
                        saved = SaveEntity<ET_LICHSUKHAMBENH>("MaLS", values, id, expected, statusOnly,
                            bus.themLichSuKhamBenh, bus.suaLichSuKhamBenh, bus.layLichSuKhamBenhTheoMa);
                        break;
                    }
                    case "TINH_TRANG_BENH":
                    {
                        var bus = new BUS_TINHTRANGBENH(giaoDich);
                        saved = SaveEntity<ET_TINHTRANGBENH>("MaTTB", values, id, expected, statusOnly,
                            bus.themTinhTrangBenh, bus.suaTinhTrangBenh, bus.layTinhTrangBenhTheoMa);
                        break;
                    }
                    case "KE_HOACH_DIEU_TRI":
                    {
                        var bus = new BUS_KEHOACHDIEUTRI(giaoDich);
                        saved = SaveEntity<ET_KEHOACHDIEUTRI>("MaKHDT", values, id, expected, statusOnly,
                            bus.themKeHoachDieuTri, bus.suaKeHoachDieuTri, bus.layKeHoachDieuTriTheoMa);
                        break;
                    }
                    case "CHI_TIET_KE_HOACH":
                    {
                        var bus = new BUS_CHITIETKEHOACH(giaoDich);
                        saved = SaveEntity<ET_CHITIETKEHOACH>("MaCTKH", values, id, expected, statusOnly,
                            bus.themChiTietKeHoach, bus.suaChiTietKeHoach, bus.layChiTietKeHoachTheoMa);
                        break;
                    }
                    case "DICH_VU_THUC_HIEN":
                    {
                        var bus = new BUS_DICHVUTHUCHIEN(giaoDich);
                        saved = SaveEntity<ET_DICHVUTHUCHIEN>("MaDVThucHien", values, id, expected, statusOnly,
                            bus.themDichVuThucHien, bus.suaDichVuThucHien, bus.layDichVuThucHienTheoMa);
                        break;
                    }
                    case "DON_THUOC":
                    {
                        var bus = new BUS_DONTHUOC(giaoDich);
                        saved = SaveEntity<ET_DONTHUOC>("MaDT", values, id, expected, statusOnly,
                            bus.themDonThuoc, bus.suaDonThuoc, bus.layDonThuocTheoMa);
                        break;
                    }
                    case "CHI_TIET_DON_THUOC":
                    {
                        var bus = new BUS_CHITIETDONTHUOC(giaoDich);
                        saved = SaveEntity<ET_CHITIETDONTHUOC>("MaCTDT", values, id, expected, statusOnly,
                            bus.themChiTietDonThuoc, bus.suaChiTietDonThuoc, bus.layChiTietDonThuocTheoMa);
                        break;
                    }
                    default: throw new InvalidOperationException("Chứng từ này phải được xử lý qua BUS của bảng tương ứng.");
                }
                if (owns) Commit();
                return saved;
            }
            catch { if (owns) Rollback(); throw; }
        }

        private Dictionary<string, object?> SaveEntity<T>(string key, IDictionary<string, object?> values,
            string? id, IDictionary<string, object?>? expected, bool statusOnly,
            Func<T, bool> add, Func<T, T?, bool, bool> update, Func<string, T?> get) where T : class, new()
        {
            var merged = id is null ? new Dictionary<string,object?>() : DAL_GIAODICH.GiaTri(get(id)
                ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại."));
            foreach(var pair in values) merged[pair.Key]=pair.Value;
            var entity = giaoDich.ChuyenDoi<T>(merged);
            if (id is null) add(entity);
            else
            {
                System.ComponentModel.TypeDescriptor.GetProperties(entity)[key]!.SetValue(entity, id);
                update(entity, expected is null ? null : giaoDich.ChuyenDoi<T>(expected), statusOnly);
            }
            var savedId = Convert.ToString(DAL_GIAODICH.GiaTri(entity)[key])!;
            return DAL_GIAODICH.GiaTri(get(savedId) ?? throw new InvalidOperationException("Không đọc lại được bản ghi vừa lưu."));
        }

        public void Delete(string table, string id, IDictionary<string, object?>? expected = null)
        {
            switch (table)
            {
                case "BENH_NHAN":
                    new BUS_BENHNHAN(giaoDich).xoaBenhNhan(id, expected is null ? null : giaoDich.ChuyenDoi<ET_BENHNHAN>(expected)); break;
                case "HO_SO_BENH_AN":
                    new BUS_HOSOBENHAN(giaoDich).xoaHoSoBenhAn(id, expected is null ? null : giaoDich.ChuyenDoi<ET_HOSOBENHAN>(expected)); break;
                case "PHIEU_KHAM_BENH":
                    new BUS_PHIEUKHAMBENH(giaoDich).xoaPhieuKhamBenh(id, expected is null ? null : giaoDich.ChuyenDoi<ET_PHIEUKHAMBENH>(expected)); break;
                case "LICH_KHAM":
                    new BUS_LICHKHAM(giaoDich).xoaLichKham(id, expected is null ? null : giaoDich.ChuyenDoi<ET_LICHKHAM>(expected)); break;
                case "LICH_SU_KHAM_BENH":
                    new BUS_LICHSUKHAMBENH(giaoDich).xoaLichSuKhamBenh(id, expected is null ? null : giaoDich.ChuyenDoi<ET_LICHSUKHAMBENH>(expected)); break;
                case "TINH_TRANG_BENH":
                    new BUS_TINHTRANGBENH(giaoDich).xoaTinhTrangBenh(id, expected is null ? null : giaoDich.ChuyenDoi<ET_TINHTRANGBENH>(expected)); break;
                case "KE_HOACH_DIEU_TRI":
                    new BUS_KEHOACHDIEUTRI(giaoDich).xoaKeHoachDieuTri(id, expected is null ? null : giaoDich.ChuyenDoi<ET_KEHOACHDIEUTRI>(expected)); break;
                case "CHI_TIET_KE_HOACH":
                    new BUS_CHITIETKEHOACH(giaoDich).xoaChiTietKeHoach(id, expected is null ? null : giaoDich.ChuyenDoi<ET_CHITIETKEHOACH>(expected)); break;
                case "DICH_VU_THUC_HIEN":
                    new BUS_DICHVUTHUCHIEN(giaoDich).xoaDichVuThucHien(id, expected is null ? null : giaoDich.ChuyenDoi<ET_DICHVUTHUCHIEN>(expected)); break;
                case "DON_THUOC":
                    new BUS_DONTHUOC(giaoDich).xoaDonThuoc(id, expected is null ? null : giaoDich.ChuyenDoi<ET_DONTHUOC>(expected)); break;
                case "CHI_TIET_DON_THUOC":
                    new BUS_CHITIETDONTHUOC(giaoDich).xoaChiTietDonThuoc(id, expected is null ? null : giaoDich.ChuyenDoi<ET_CHITIETDONTHUOC>(expected)); break;
                default: throw new InvalidOperationException("Chứng từ này phải được xử lý qua BUS của bảng tương ứng.");
            }
        }
    }
}
