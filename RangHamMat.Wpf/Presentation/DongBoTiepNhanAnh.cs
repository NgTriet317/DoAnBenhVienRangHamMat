using System.Data;
using System.Windows;
using ET;
using BUS;
using RangHamMat.Controls;
using RangHamMat.Services;

namespace RangHamMat.Views;

public partial class PhieuHuongDan
{
    private DataTable? dongBoPhieu;
    private Task<Action> ChuanBiDongBo(CancellationToken token)
    {
        string maNV = PhienDangNhap.MaNV;
        bool admin = PhienDangNhap.IsAdmin;
        return DongBoChuanBi.Doc(() => new BUS_PHIEUHUONGDAN().layDanhSachPhieu(maNV, admin), data =>
        {
            dongBoPhieu = data;
            try { DongBoLamMoi(); } finally { dongBoPhieu = null; }
        }, token);
    }

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["PHIEU_KHAM_BENH", "BENH_NHAN", "HO_SO_BENH_AN", "LICH_SU_KHAM_BENH", "LICH_KHAM", "NHAN_VIEN", "PHONG", "KHOA", "DICH_VU", "AI_KET_QUA_TAO_PHIEU"],
        ChuanBiDongBo, () => dangTai);

    private void DongBoLamMoi() => DongBoLuaChon.GiuDong(gridPhieu, ["MaPhieu"], () =>
        {
            string search = txtTimKiem.Text;
            bool? today = chkHomNay.IsChecked;
            string? state = cboTrangThai.SelectedItem as string;
            hienThiDuLieu();
            txtTimKiem.Text = search;
            chkHomNay.IsChecked = today;
            DongBoLuaChon.ChonNhan(cboTrangThai, state);
            locDuLieu();
        });
}

public partial class TiepNhanKham
{
    private void DangKyDongBoManHinh() => DongBoManHinh.Register(this,
        ["BENH_NHAN", "PHIEU_KHAM_BENH", "LICH_KHAM", "NHAN_VIEN", "LICH_LAM_VIEC", "PHONG", "DICH_VU"],
        () => Form_Loaded(this, new RoutedEventArgs()),
        () => true); // Form tiếp nhận đang mở: giữ toàn bộ lựa chọn và bản gốc đến khi đóng.
}

public partial class AnhXquangLanKham
{
    private sealed record AnhChupXquang(DanhSachAnhXquang DanhSach, IReadOnlyList<NhanVienChupAnh> KyThuatVien);
    private AnhChupXquang? dongBoAnh;

    internal bool DongBoDangNhap => busy || loading || !string.IsNullOrWhiteSpace(ImageNote.Text) ||
        !string.IsNullOrWhiteSpace(TechnicianPicker.SelectedValue as string);

    private Task<Action> ChuanBiDongBo(CancellationToken token)
    {
        bool hienAn = ShowHidden.IsChecked == true;
        return DongBoChuanBi.Doc(() =>
        {
            var data = bus.LayDanhSach(visitId, hienAn, actor);
            var technicians = data.DuocPhanCong ? bus.LayKyThuatVien(visitId, actor) : Array.Empty<NhanVienChupAnh>();
            new KhoLuuAnhXquang(config ?? CauHinhKhoAnh.Doc()).ThuMucGoc();
            return new AnhChupXquang(data, technicians);
        }, data =>
        {
            dongBoAnh = data;
            try
            {
                // TaiLai giữ nguyên dòng nếu còn; không gọi MoAnh hoặc sự kiện chọn sau khi loading=false.
                TaiLai((ImageList.SelectedItem as AnhXquangHienThi)?.Anh.MaAnh, xemTruoc: false);
            }
            finally { dongBoAnh = null; }
        }, token);
    }

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["ANH_XQUANG_LAN_KHAM", "PHAN_CONG_ANH_XQUANG", "DICH_VU_THUC_HIEN", "LICH_SU_KHAM_BENH", "PHIEU_KHAM_BENH", "BENH_NHAN", "NHAN_VIEN", "DICH_VU"],
        ChuanBiDongBo,
        () => DongBoDangNhap);
}

public partial class AnhXquangDuocGiao
{
    private bool dongBoDangTai;

    private Task<Action> ChuanBiDongBo(CancellationToken token)
        => DongBoChuanBi.Doc(() => new BUS_ANHXQUANG(transaction, config).LayLuotDuocGiao(actor), rows =>
        {
            string? selected = VisitPicker.SelectedValue as string;
            dongBoDangTai = true;
            try
            {
                VisitPicker.ItemsSource = rows.Select(x => new LuaChonDanhMuc(x.MaLS,
                    $"{x.ThoiGianKham:dd/MM HH:mm} · {x.HoTenBN} ({x.MaBN}) · {x.MaLS}")).ToArray();
                VisitPicker.SelectedValue = selected;
                if (VisitPicker.SelectedItem is null) ImageHost.Content = null;
                StatusText.Text = $"{rows.Count} lượt chụp đang mở trong phạm vi được phép.";
            }
            finally { dongBoDangTai = false; }
        }, token);

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["PHAN_CONG_ANH_XQUANG", "DICH_VU_THUC_HIEN", "LICH_SU_KHAM_BENH", "PHIEU_KHAM_BENH", "BENH_NHAN", "NHAN_VIEN", "DICH_VU"],
        ChuanBiDongBo, () => ImageHost.Content is AnhXquangLanKham image && image.DongBoDangNhap);
}
