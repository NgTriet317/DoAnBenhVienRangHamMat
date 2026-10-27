using System.Data;
using ET;
using DAL;

namespace BUS;

public sealed partial class BUS_KHAMDIEUTRI
{
    private readonly DAL_KHAMDIEUTRI dal;
    public DuLieuHoSoTheoQuyen LayDuLieuHoSo(NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => dal.LayDuLieuHoSo(nguoiDung));
    public DuLieuHoSoTheoQuyen LayDuLieuHoSoGiaoDien(NguCanhHoSo context,NguCanhNghiepVu actor)=>XuLyLoi.ThucHien(()=>dal.LayDuLieuHoSoGiaoDien(context,actor));
    public TrangHoSo<DongBenhNhanDanhSach> LayTrangBenhNhan(BoLocDanhSachHoSo filter,NguCanhNghiepVu actor)=>XuLyLoi.ThucHien(()=>dal.LayTrangBenhNhan(filter,actor));
    public TrangHoSo<DongLanKhamDanhSach> LayTrangLanKham(BoLocDanhSachHoSo filter,NguCanhNghiepVu actor)=>XuLyLoi.ThucHien(()=>dal.LayTrangLanKham(filter,actor));
    public TrangHoSo<DongLichSuDon> LayTrangLichSuDon(BoLocDanhSachHoSo filter,NguCanhNghiepVu actor)=>XuLyLoi.ThucHien(()=>dal.LayTrangLichSuDon(filter,actor));
    public IReadOnlyList<string> LayMaLuotHoaDon(string? maLS,NguCanhNghiepVu actor)=>XuLyLoi.ThucHien(()=>dal.LayMaLuotHoaDon(maLS,actor));

    public void KiemTraLienKet(string? maBN, string? maLS, string? maDT, NguCanhNghiepVu nguoiDung)
        =>XacMinhNguCanh(new(){MaBN=string.IsNullOrEmpty(maBN)?null:maBN,MaLS=string.IsNullOrEmpty(maLS)?null:maLS,MaDT=string.IsNullOrEmpty(maDT)?null:maDT},nguoiDung);
    public NguCanhHoSo XacMinhNguCanh(NguCanhHoSo context,NguCanhNghiepVu nguoiDung)
        =>XuLyLoi.ThucHien(()=>dal.XacMinhNguCanh(context,nguoiDung));
    public BUS_KHAMDIEUTRI(IDbTransaction? transaction = null) => dal = new(transaction);
    public IReadOnlyList<HangDoiKhamItem> LayHangDoi(BoLocHangDoi boLoc, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => dal.LayHangDoi(boLoc, nguoiDung));
    public LanKhamChiTiet LayChiTiet(string maLS, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => dal.LayChiTiet(maLS, nguoiDung));
}
