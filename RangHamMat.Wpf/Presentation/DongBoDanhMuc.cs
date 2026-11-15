using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Services;

namespace RangHamMat.Views;

public partial class DichVu
{
    private string? dongBoMocForm;
    private sealed record AnhChupDichVu(IReadOnlyList<Dictionary<string, object?>> Dong,
        Dictionary<string, IReadOnlyList<Dictionary<string, object?>>> Cha);
    private AnhChupDichVu? dongBoDichVu;

    private string DongBoGiaTriForm() => string.Join("\u001f", Values().OrderBy(x => x.Key)
        .Select(x => x.Key + "=" + Convert.ToString(x.Value, System.Globalization.CultureInfo.InvariantCulture)));

    private void DongBoGhiNhanForm() => dongBoMocForm = DongBoGiaTriForm();

    private bool DongBoDangSua()
    {
        try { return dongBoMocForm is not null && dongBoMocForm != DongBoGiaTriForm(); }
        catch { return true; } // Ngày/số gõ dở vẫn là nội dung phải giữ.
    }

    private Task<Action> ChuanBiDongBo(CancellationToken token)
    {
        var definition = Current!;
        string search = SearchBox.Text.Trim();
        string status = StatusFilter.SelectedIndex switch { 0 => definition.DangDung, 1 => definition.NgungDung, _ => "" };
        string maNV = PhienDangNhap.MaNV;
        return DongBoChuanBi.Doc(() =>
        {
            var actor = new BUS_QUYENNGHIEPVU().LayNguCanh(maNV);
            var bus = new BUS_DANHMUC();
            var parents = definition.Truong.Where(f => f.BangCha is not null).Select(f => f.BangCha!).Distinct()
                .ToDictionary(x => x, x => bus.Lay(x, "", DanhMucHeThong.Lay(x).DangDung, actor));
            return new AnhChupDichVu(bus.Lay(definition.Bang, search, status, actor), parents);
        }, data =>
        {
            dongBoDichVu = data;
            try { DongBoLuaChon.GiuDong(CatalogGrid, definition.Khoa, LoadRows); }
            finally { dongBoDichVu = null; }
        }, token);
    }

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        DanhMucHeThong.TatCa.Select(x => x.Bang).ToArray(), ChuanBiDongBo,
        () => Current is null || StatusFilter.SelectedIndex is < 0 or > 2 || DongBoDangSua());
}

public partial class NhatKy
{
    private sealed record AnhChupNhatKy(NguCanhNghiepVu Actor, (int DangNhap, int DangNhapThatBai, int ThayDoi) ThongKe, TrangNhatKy Trang);
    private AnhChupNhatKy? dongBoNhatKy;

    private Task<Action> ChuanBiDongBo(CancellationToken token)
    {
        var f = applied!;
        var filter = new BoLocNhatKy { TuUtc = f.TuUtc, DenUtc = f.DenUtc, TaiKhoan = f.TaiKhoan,
            PhanHe = f.PhanHe, HanhDong = f.HanhDong, KetQua = f.KetQua, MaDoiTuong = f.MaDoiTuong,
            Trang = page, KichThuocTrang = int.Parse(Convert.ToString((PageSize.SelectedItem as ComboBoxItem)?.Content) ?? "50") };
        string maNV = PhienDangNhap.MaNV;
        return DongBoChuanBi.Doc(() =>
        {
            var actor = new BUS_QUYENNGHIEPVU().LayNguCanh(maNV);
            var bus = new BUS_NHATKY();
            return new AnhChupNhatKy(actor, bus.ThongKe(actor), bus.LayTrang(filter, actor));
        }, data =>
        {
            dongBoNhatKy = data;
            try { DongBoLuaChon.GiuDong(AuditGrid, ["MaNK"], () => Load()); }
            finally { dongBoNhatKy = null; }
        }, token);
    }

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["NHAT_KY_THAO_TAC", "NHAT_KY_PHIEN", "PHIEN_DANG_NHAP", "TAI_KHOAN", "NHAN_VIEN"], ChuanBiDongBo,
        () => !ready || applied is null);
}

internal sealed record AnhChupTaiKhoan(List<ET_TAIKHOAN> TaiKhoan, List<ET_VAITRO> VaiTro,
    List<ET_NHANVIEN> NhanVien, List<ET_KHOA> Khoa, List<ET_PHONG> Phong, Dictionary<string, HashSet<string>> Quyen);

public partial class TaiKhoan
{
    private AnhChupTaiKhoan? dongBoTaiKhoan;
    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["TAI_KHOAN", "VAI_TRO", "PHAN_QUYEN_VAI_TRO", "NHAN_VIEN", "KHOA", "PHONG"],
        token => DongBoChuanBi.Doc(() =>
        {
            var roles = new BUS_VAITRO().layToanBoVaiTro().ToList();
            var permissions = new BUS_PHANQUYENVAITRO();
            return new AnhChupTaiKhoan(new BUS_TAIKHOAN().layToanBoTaiKhoan().ToList(), roles,
                new BUS_NHANVIEN().DocDanhSachKhongGhi().ToList(), new BUS_KHOA().layToanBoKhoa().ToList(),
                new BUS_PHONG().layToanBoPhong().ToList(),
                roles.ToDictionary(x => x.MaVaiTro, x => permissions.layQuyenTheoVaiTro(x.MaVaiTro).ToHashSet(StringComparer.Ordinal)));
        }, data =>
        {
            dongBoTaiKhoan = editorTaiKhoan.DongBoNguon = editorVaiTro.DongBoNguon = data;
            try { DongBoLamMoi(); }
            finally { dongBoTaiKhoan = editorTaiKhoan.DongBoNguon = editorVaiTro.DongBoNguon = null; }
        }, token), () => dangTai || dangThemTaiKhoan || dangThemVaiTro ||
            editorTaiKhoan.DongBoDangSua || editorVaiTro.DongBoDangSua || danhSachPhanQuyen.GetChanges() is not null);

    private void DongBoLamMoi() =>
        DongBoLuaChon.GiuDong(Grid_TAI_KHOAN, ["MaTK"], () =>
        DongBoLuaChon.GiuDong(Grid_VAI_TRO, ["MaVaiTro"], () =>
        {
            string? role = (cboVaiTro.SelectedItem as ComboBoxItem)?.Tag as string;
            hienThiDuLieu();
            cboVaiTro.SelectedItem = cboVaiTro.Items.OfType<ComboBoxItem>().FirstOrDefault(x => Equals(x.Tag, role));
            locTaiKhoan();
        }));
}

public partial class CuaSoTaiKhoan
{
    internal AnhChupTaiKhoan? DongBoNguon;
    private string? dongBoMocForm;

    private string DongBoGiaTriForm() => pnlTaiKhoan.Visibility == Visibility.Visible
        ? string.Join("\u001f", txtTenDangNhap.Text, pwdMatKhau.Password, pwdXacNhan.Password,
            (cboNhanVien.SelectedItem as ComboBoxItem)?.Tag, (cboVaiTro.SelectedItem as ComboBoxItem)?.Tag,
            rbTheoKhoa.IsChecked, rbTheoPhong.IsChecked, (cboKhoa.SelectedItem as ComboBoxItem)?.Tag,
            (cboPhong.SelectedItem as ComboBoxItem)?.Tag)
        : string.Join("\u001f", txtTenVaiTro.Text, txtMoTa.Text);

    private void DongBoGhiNhanForm() => dongBoMocForm = DongBoGiaTriForm();
    internal bool DongBoDangSua => dongBoMocForm is not null && dongBoMocForm != DongBoGiaTriForm();
}
