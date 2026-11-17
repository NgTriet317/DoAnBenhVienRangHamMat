using System.Data;
using System.Windows.Controls;
using ET;
using BUS;
using RangHamMat.Services;

namespace RangHamMat.Views;

public partial class Kho
{
    private sealed record AnhChupKho(List<ET_KHO> Kho, List<ET_NHANVIEN> NhanVien, DataTable Ton, DataTable Lo);
    private AnhChupKho? dongBoKho;

    private Task<Action> ChuanBiDongBo(CancellationToken token)
    {
        string? maKho = cboKho.SelectedValue as string;
        return DongBoChuanBi.Doc(() =>
        {
            var bus = new BUS_KHO();
            var kho = bus.layToanBoKho().ToList();
            var selected = kho.FirstOrDefault(x => x.MaKho == maKho);
            return new AnhChupKho(kho, new BUS_NHANVIEN().DocDanhSachKhongGhi().ToList(),
                selected is null ? new DataTable() : bus.layTonKhoTheoKho(selected.MaKho),
                selected is null ? new DataTable() : bus.layLoTonTheoKho(selected.MaKho));
        }, data =>
        {
            dongBoKho = data;
            try { DongBoLuaChon.GiuDong(gridTonKho, ["MaHH"], hienThiDuLieu); }
            finally { dongBoKho = null; }
        }, token);
    }

    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["KHO", "NHAN_VIEN", "HANG_HOA", "LOAI_HANG_HOA", "VAT_TU", "THUOC", "LO_HANG_HOA", "NHA_CUNG_CAP",
         "PHIEU_NHAP", "CHI_TIET_PHIEU_NHAP", "PHIEU_XUAT", "CHI_TIET_PHIEU_XUAT"],
        ChuanBiDongBo, () => dangTai);
}

public partial class DanhMucHangHoa
{
    private (DataTable HangHoa, List<ET_LOAIHANGHOA> Loai)? dongBoDanhMuc;
    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["HANG_HOA", "LOAI_HANG_HOA", "VAT_TU", "THUOC", "THIET_BI", "LO_HANG_HOA", "KHO",
         "PHIEU_NHAP", "CHI_TIET_PHIEU_NHAP", "PHIEU_XUAT", "CHI_TIET_PHIEU_XUAT"],
        token => DongBoChuanBi.Doc(() => (HangHoa: new BUS_HANGHOA().layDanhMucChiTiet(),
            Loai: new BUS_LOAIHANGHOA().layToanBoLoaiHangHoa().ToList()), data =>
        {
            dongBoDanhMuc = data;
            try { DongBoLamMoi(); } finally { dongBoDanhMuc = null; }
        }, token), () => dangThem || dangSua);

    private void DongBoLamMoi() => DongBoLuaChon.GiuDong(Grid_HANG_HOA, ["MaHH"], () =>
        {
            string? maLoai = DongBoLuaChon.GiaTriCombo(cboLoaiLoc);
            string? trangThai = cboTrangThaiLoc.SelectedItem as string;
            string? maHang = (Grid_HANG_HOA.SelectedItem as DataRowView)?["MaHH"].ToString();
            LoadDuLieu(maHang);
            cboLoaiLoc.SelectedValue = maLoai;
            DongBoLuaChon.ChonNhan(cboTrangThaiLoc, trangThai);
        });
}

public partial class ThietBi
{
    private sealed record AnhChupThietBi(List<ET_THIETBI> ThietBi, List<ET_HANGHOA> HangHoa,
        List<ET_PHONG> Phong, List<ET_NHACUNGCAP> NhaCungCap, List<ET_LOAIHANGHOA> Loai);
    private AnhChupThietBi? dongBoThietBi;
    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["THIET_BI", "BAO_TRI_THIET_BI", "THANH_LY_THIET_BI", "HANG_HOA", "LOAI_HANG_HOA", "PHONG", "KHOA", "NHA_CUNG_CAP"],
        token => DongBoChuanBi.Doc(() => new AnhChupThietBi(new BUS_THIETBI().layToanBoThietBi().ToList(),
            new BUS_HANGHOA().layToanBoHangHoa().ToList(), new BUS_PHONG().layToanBoPhong().ToList(),
            new BUS_NHACUNGCAP().layToanBoNhaCungCap().ToList(), new BUS_LOAIHANGHOA().layToanBoLoaiHangHoa().ToList()), data =>
        {
            dongBoThietBi = data;
            try { DongBoLamMoi(); } finally { dongBoThietBi = null; }
        }, token), () => dangSua);

    private void DongBoLamMoi() => DongBoLuaChon.Giu<ThietBiDong>(dgvThietBi, x => x.DuLieu.MaTB, () =>
        {
            string? phong = DongBoLuaChon.GiaTriCombo(cboPhongLoc);
            string? trangThai = cboTrangThaiLoc.SelectedItem as string;
            LoadDuLieuThietBi(thietBiDangChon?.MaTB);
            cboPhongLoc.SelectedValue = phong;
            DongBoLuaChon.ChonNhan(cboTrangThaiLoc, trangThai);
            ApDungBoLocThietBi(thietBiDangChon?.MaTB);
        });
}

public partial class NhapXuatVatTu
{
    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["KHO", "NHAN_VIEN", "HANG_HOA", "LO_HANG_HOA", "KHOA", "PHONG", "NHA_CUNG_CAP", "HOA_DON_NHAP",
         "PHIEU_NHAP", "CHI_TIET_PHIEU_NHAP", "PHIEU_XUAT", "CHI_TIET_PHIEU_XUAT"],
        token => DongBoNguonKho.Prepare(()=>DongBoLuaChon.Giu<SoKhoPhieu>(lstPhieu, x => x.Loai + ":" + x.SoPhieu, TaiDuLieu),token), () => dangTai);
}

public partial class NhapKho
{
    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["KHO", "NHAN_VIEN", "NHA_CUNG_CAP", "HANG_HOA", "LOAI_HANG_HOA", "VAT_TU", "THUOC", "LO_HANG_HOA",
         "HOA_DON_NHAP", "PHIEU_NHAP", "CHI_TIET_PHIEU_NHAP", "CHI_TIET_PHIEU_XUAT"],
        token => DongBoNguonKho.Prepare(()=>DongBoLuaChon.Giu<PhieuNhapHienThi>(lstPhieu, x => x.MaPhieu, () =>
        {
            TaiDuLieu();
            if (lstPhieu.SelectedItem is PhieuNhapHienThi selected) HienPhieu(selected);
        }),token), () => dangTai || formTaoMoi.IsVisible);
}

public partial class XuatKho
{
    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["KHO", "NHAN_VIEN", "KHOA", "PHONG", "HANG_HOA", "LO_HANG_HOA", "PHIEU_NHAP", "CHI_TIET_PHIEU_NHAP",
         "PHIEU_XUAT", "CHI_TIET_PHIEU_XUAT", "DON_THUOC", "CHI_TIET_DON_THUOC"],
        token => DongBoNguonKho.Prepare(()=>DongBoLuaChon.Giu<PhieuXuatHienThi>(lstPhieuXuat, x => x.MaPhieu, () => TaiDuLieu(maDangSua)),token),
        () => dangTai || duocSua);
}

public partial class HoaDonNhaCungCap
{
    private void DangKyDongBoManHinh() => DongBoManHinh.RegisterPrepared(this,
        ["HOA_DON_NHAP", "NHA_CUNG_CAP", "NHAN_VIEN", "PHIEU_NHAP", "CHI_TIET_PHIEU_NHAP"],
        token => DongBoNguonKho.Prepare(()=>DongBoLuaChon.Giu<HoaDonNccDong>(lstHoaDon, x => x.DuLieu.MaHDN, () => TaiDuLieu(dangChon?.DuLieu.MaHDN ?? "")),token),
        () => dangTai || dangTaoMoi || CoThayDoiChuaLuu());
}
