using System.Data;
using System.Windows;
using System.Windows.Controls;
using ET;
using BUS;
using RangHamMat.Services;

namespace RangHamMat.Views;

public partial class NhanSu
{
    private sealed record AnhChupNhanSu(List<ET_NHANVIEN> NhanVien, List<ET_CHUCVU> ChucVu, List<ET_CHUYENKHOA> ChuyenKhoa);
    private AnhChupNhanSu? dongBoNhanSu;

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["NHAN_VIEN", "CHUC_VU", "CHUYEN_KHOA", "DON_XIN_NGHI"], token => DongBoChuanBi.Doc(
        () => new AnhChupNhanSu(new BUS_NHANVIEN().DocDanhSachKhongGhi().ToList(),
            new BUS_CHUCVU().layToanBoChucVu().ToList(), new BUS_CHUYENKHOA().layToanBoChuyenKhoa().ToList()), data =>
        {
            dongBoNhanSu = data;
            try { DongBoLamMoi(); } finally { dongBoNhanSu = null; }
        }, token), DongBoDangSua);

    private void DongBoLamMoi() =>
        DongBoLuaChon.Giu<ET_NHANVIEN>(dgvNhanVien, x => x.MaNV, () =>
        {
            DongBoLuaChon.Giu<ET_CHUCVU>(dgvChucVu, x => x.MaChucVu, LoadDuLieuChucVu);
            LoadDuLieuChuyenKhoa();
            LoadDuLieuNhanVien();
        });

    private string? dongBoMocNhanVien,dongBoMocChucVu;
    private string DongBoGiaTriNhanVien()=>string.Join("\u001f",txtHoTen.Text,txtSDT.Text,txtDiaChi.Text,txtBangCap.Text,
        dpNgaySinh.Text,dpNgayVaoLam.Text,cboChucVu.SelectedValue,cboChuyenKhoa.SelectedValue,
        cboGioiTinh.SelectedIndex,cboTrangThai.SelectedIndex);
    private string DongBoGiaTriChucVu()=>string.Join("\u001f",txtTenChucVu.Text,txtMoTaChucVu.Text);
    private void DongBoGhiNhanForm(){dongBoMocNhanVien=DongBoGiaTriNhanVien();dongBoMocChucVu=DongBoGiaTriChucVu();}
    private bool DongBoDangSua()=>dongBoMocNhanVien!=DongBoGiaTriNhanVien()||dongBoMocChucVu!=DongBoGiaTriChucVu();
}

public partial class ChungChiNhanVien
{
    private (List<ET_NHANVIEN> NhanVien, List<ET_CHUNGCHINHANVIEN> ChungChi)? dongBoChungChi;

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["CHUNG_CHI_NHAN_VIEN", "NHAN_VIEN", "CHUC_VU", "CHUYEN_KHOA"], token => DongBoChuanBi.Doc(
        () => (NhanVien: new BUS_NHANVIEN().DocDanhSachKhongGhi().ToList(),
            ChungChi: new BUS_CHUNGCHINHANVIEN().layToanBoChungChiNhanVien().ToList()), data =>
        {
            dongBoChungChi = data;
            try { DongBoLamMoi(); } finally { dongBoChungChi = null; }
        }, token));

    private void DongBoLamMoi() =>
        DongBoLuaChon.Giu<ET_CHUNGCHINHANVIEN>(dgvChungChiNhanVien, x => x.MaCC, () =>
        {
            DongBoLuaChon.Giu<ET_NHANVIEN>(lstNhanVien, x => x.MaNV, LoadDanhSachNhanVien);
            LoadDanhSachChungChi();
        });
}

public partial class CaLamViec
{
    private sealed record AnhChupCa(List<ET_CALAM> Ca, List<ET_NHANVIEN> NhanVien,
        List<ET_LICHLAMVIEC> Lich, List<ET_CA_THEO_NGAY> CaiDat, List<ET_PHONG> Phong, int DonCho);
    private AnhChupCa? dongBoCa;

    private Task<Action> ChuanBiDongBo(CancellationToken token)
    {
        var tuan = dauTuan;
        bool quanLy = IsManager;
        return DongBoChuanBi.Doc(() => new AnhChupCa(new BUS_CALAM().layToanBoCaLam().ToList(),
            new BUS_NHANVIEN().DocDanhSachKhongGhi().ToList(), new BUS_LICHLAMVIEC().layLichTheoTuan(tuan),
            new BUS_CA_THEO_NGAY().layCaTheoTuan(tuan), new BUS_PHONG().layToanBoPhong().ToList(),
            quanLy ? new BUS_DON_XIN_NGHI().demDonChoDuyet() : 0), data =>
        {
            dongBoCa = data;
            try { LoadDuLieu(); CapNhatThongBaoDonXinNghi(); } finally { dongBoCa = null; }
        }, token);
    }

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["CA_LAM", "CA_THEO_NGAY", "LICH_LAM_VIEC", "NHAN_VIEN", "PHONG", "CHUC_VU", "CHUYEN_KHOA",
         "DON_XIN_NGHI", "PHAN_CONG_THAY_THE", "XAC_NHAN_DOI_LICH_KHAM", "LICH_KHAM"], ChuanBiDongBo,
        () => ApproveOverlay.IsVisible || RejectOverlay.IsVisible || CreateOverlay.IsVisible ||
            SetShiftOverlay.IsVisible || DonXinNghiOverlay.IsVisible);
}

public partial class ChamCong
{
    private DataTable? dongBoChamCong;
    private string? dongBoMocForm;
    private string DongBoGiaTriForm() => string.Join("\u001f", txtGioVao.Text, txtGioRa.Text, txtLyDo.Text, cboKetQua.SelectedIndex);
    private void DongBoGhiNhanForm() => dongBoMocForm = DongBoGiaTriForm();
    private Task<Action> ChuanBiDongBo(CancellationToken token)
    {
        int thang = cboThang.SelectedIndex + 1;
        int nam = int.Parse(txtNam.Text);
        return DongBoChuanBi.Doc(() => new BUS_CHAMCONG().layChamCongTheoThang(thang, nam), data =>
        {
            dongBoChamCong = data;
            try { DongBoLuaChon.GiuDong(gridChamCong, ["MaLLV"], hienThiDuLieu); }
            finally { dongBoChamCong = null; }
        }, token);
    }

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["CHAM_CONG", "NHAT_KY_DUYET_CHAM_CONG", "LICH_LAM_VIEC", "NHAN_VIEN", "CHUC_VU", "CA_LAM", "DON_XIN_NGHI", "BANG_LUONG"],
        ChuanBiDongBo, () => dangTai || !PhienDangNhap.IsAdmin || cboThang.SelectedIndex < 0 ||
            !int.TryParse(txtNam.Text, out int y) || y is < 2000 or > 2100 ||
            gridChamCong.SelectedItem is not null && dongBoMocForm != DongBoGiaTriForm());
}

public partial class BangLuong
{
    private sealed record AnhChupLuong(bool V9, bool DaCai, DateTime Ngay, DataTable Luong, int CongCho, DataTable DoiSoat);
    private AnhChupLuong? dongBoLuong;

    private Task<Action> ChuanBiDongBo(CancellationToken token)
    {
        var tu = new DateTime(int.Parse(txtNam.Text), cboThang.SelectedIndex + 1, 1);
        var den = tu.AddMonths(1).AddDays(-1);
        return DongBoChuanBi.Doc(() =>
        {
            var bus = new BUS_BANGLUONG();
            bool v9 = bus.suDungSchemaV9();
            return new AnhChupLuong(v9, v9 && bus.daCaiTinhLuong(), bus.layNgayMayChu(),
                bus.layBangLuongTheoKy(tu, den), bus.demCongChoDoiSoat(tu, den),
                v9 ? bus.layDoiSoatTheoKy(tu, den) : new DataTable());
        }, data =>
        {
            dongBoLuong = data;
            try { DongBoLamMoi(); } finally { dongBoLuong = null; }
        }, token);
    }

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["BANG_LUONG", "CHI_TIET_LUONG", "CHI_TIET_HOA_HONG_LUONG", "CHAM_CONG", "LICH_LAM_VIEC", "NHAN_VIEN",
         "CHUC_VU", "CHINH_SACH_LUONG", "DON_XIN_NGHI", "RHM_NGAY_PHEP_NAM", "HOA_DON", "CHI_TIET_HOA_DON", "THANH_TOAN", "DICH_VU_THUC_HIEN"],
        ChuanBiDongBo, () => dangTai || PhieuLuongOverlay.IsVisible || !PhienDangNhap.IsAdmin ||
            cboThang.SelectedIndex < 0 || !int.TryParse(txtNam.Text, out int y) || y is < 2000 or > 2100);

    private void DongBoLamMoi() => DongBoLuaChon.GiuDong(gridBangLuong, ["MaBL"], () =>
        {
            string? chucVu = (cboChucVu.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string? trangThai = (cboTrangThai.SelectedItem as ComboBoxItem)?.Content?.ToString();
            hienThiDuLieu();
            DongBoLuaChon.ChonNhan(cboChucVu, chucVu);
            DongBoLuaChon.ChonNhan(cboTrangThai, trangThai);
            locDuLieu();
        });
}

public partial class ChinhSachLuong
{
    private (DataTable ChinhSach, DataTable ChucVu)? dongBoChinhSach;
    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["CHINH_SACH_LUONG", "CHUC_VU"],
        token => DongBoChuanBi.Doc(() => (ChinhSach: new BUS_CHINHSACHLUONG().layDanhSachChinhSach(),
            ChucVu: new BUS_CHINHSACHLUONG().layDanhSachChucVu()), data =>
        {
            dongBoChinhSach = data;
            try { DongBoLuaChon.GiuDong(gridChinhSach, ["MaCSL"], () => hienThiDuLieu(maChinhSach)); }
            finally { dongBoChinhSach = null; }
        }, token),
        () => dangTai || btnLuuNhap.IsEnabled);
}

public partial class DonXinNghiQuanLy
{
    private List<ET_DON_XIN_NGHI_HIENTHI>? dongBoDonNghi;
    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["DON_XIN_NGHI", "NHAN_VIEN", "CHUYEN_KHOA", "CHUC_VU", "PHONG", "CA_LAM", "LICH_LAM_VIEC",
         "LICH_KHAM", "PHAN_CONG_THAY_THE", "XAC_NHAN_DOI_LICH_KHAM", "RHM_NGAY_PHEP_NAM", "RHM_CAU_HINH_QUY_PHEP", "NGAY_NGHI_LE"],
        token => DongBoChuanBi.Doc(() => new BUS_DON_XIN_NGHI().layDanhSachDon(), data =>
        {
            dongBoDonNghi = data;
            try { DongBoLuaChon.Giu<ET_DON_XIN_NGHI_HIENTHI>(dgvDonXinNghi, x => x.MaDonNghi, TaiDuLieu); }
            finally { dongBoDonNghi = null; }
        }, token),
        () => !PhienDangNhap.IsAdmin || ChiTietOverlay.IsVisible || KhanCapOverlay.IsVisible);
}

public partial class QuyPhepNhanSu
{
    private sealed record AnhChupQuyPhep(DataTable DanhSach, DataTable CauHinh, DataTable NgayPhep);
    private AnhChupQuyPhep? dongBoQuyPhep;
    private Task<Action> ChuanBiDongBo(CancellationToken token)
    {
        int year = nam;
        string? maNV = (gridQuyPhep.SelectedItem as DataRowView)?["MaNV"].ToString();
        return DongBoChuanBi.Doc(() =>
        {
            var bus = new BUS_QUYPHEP();
            return new AnhChupQuyPhep(bus.layDanhSachQuyPhep(year), bus.layCauHinh(year),
                maNV is null ? new DataTable() : bus.layNgayPhepNhanVien(maNV, year));
        }, data =>
        {
            dongBoQuyPhep = data;
            try { DongBoLuaChon.GiuDong(gridQuyPhep, ["MaNV"], TaiDuLieu); }
            finally { dongBoQuyPhep = null; }
        }, token);
    }

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["RHM_NGAY_PHEP_NAM", "RHM_CAU_HINH_QUY_PHEP", "DON_XIN_NGHI", "NHAN_VIEN", "LICH_LAM_VIEC", "NGAY_NGHI_LE"],
        ChuanBiDongBo, () => dangTai || !PhienDangNhap.IsAdmin);
}

public partial class XinNghiViec
{
    private sealed record AnhChupPhepCaNhan(Dictionary<int, DataTable> Quy, int NgayDuKien);
    private AnhChupPhepCaNhan? dongBoPhepCaNhan;
    private Task<Action> ChuanBiDongBo(CancellationToken token)
    {
        string maNV = PhienDangNhap.MaNV;
        int nam = int.TryParse(txtNamQuyPhep.Text, out int value) ? value : DateTime.Today.Year;
        DateTime? tu = dpTuNgay.SelectedDate, den = dpDenNgay.SelectedDate;
        bool duKien = (cboLoaiNghi.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Nghỉ phép năm" &&
            tu.HasValue && den.HasValue && den.Value >= tu.Value && den.Value.Year == tu.Value.Year;
        return DongBoChuanBi.Doc(() =>
        {
            var quy = new Dictionary<int, DataTable>();
            if (string.IsNullOrWhiteSpace(maNV)) return new AnhChupPhepCaNhan(quy, 0);
            var bus = new BUS_QUYPHEP();
            quy[nam] = bus.layQuyPhep(maNV, nam);
            if (duKien && tu!.Value.Year != nam) quy[tu.Value.Year] = bus.layQuyPhep(maNV, tu.Value.Year);
            return new AnhChupPhepCaNhan(quy, duKien ? bus.demNgayDuKien(maNV, tu!.Value, den!.Value) : 0);
        }, data =>
        {
            dongBoPhepCaNhan = data;
            try { TaiQuyPhep(nam); CapNhatDuKienPhep(); }
            finally { dongBoPhepCaNhan = null; }
        }, token);
    }

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["RHM_NGAY_PHEP_NAM", "RHM_CAU_HINH_QUY_PHEP", "DON_XIN_NGHI", "NHAN_VIEN", "LICH_LAM_VIEC", "NGAY_NGHI_LE"],
        ChuanBiDongBo); // Chỉ cập nhật số dư/ước tính; không đổi ngày, lý do, bàn giao hoặc trạng thái bản nháp.
}
