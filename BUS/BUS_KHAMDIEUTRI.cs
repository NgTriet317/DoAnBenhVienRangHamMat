using System.Data;
using ET;
using DAL;

namespace BUS;

public sealed partial class BUS_KHAMDIEUTRI
{
    private readonly DAL_KHAMDIEUTRI dal;
    public DuLieuHoSoTheoQuyen LayDuLieuHoSo(NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => dal.LayDuLieuHoSo(nguoiDung));

    public void KiemTraLienKet(string? maBN, string? maLS, string? maDT, NguCanhNghiepVu nguoiDung)
    {
        var data = LayDuLieuHoSo(nguoiDung).CacBang;
        string PatientOfVisit(string visit)
        {
            var l = data["LICH_SU_KHAM_BENH"].Cast<ET_LICHSUKHAMBENH>().SingleOrDefault(x => x.MaLS == visit) ?? throw new TuChoiQuyenNghiepVuException();
            var p = data["PHIEU_KHAM_BENH"].Cast<ET_PHIEUKHAMBENH>().Single(x => x.MaPhieu == l.MaPhieu);
            return data["HO_SO_BENH_AN"].Cast<ET_HOSOBENHAN>().Single(x => x.MaHS == p.MaHS).MaBN;
        }
        if (!string.IsNullOrEmpty(maBN) && !data["BENH_NHAN"].Cast<ET_BENHNHAN>().Any(x => x.MaBN == maBN)) throw new TuChoiQuyenNghiepVuException();
        if (!string.IsNullOrEmpty(maLS) && !string.IsNullOrEmpty(maBN) && PatientOfVisit(maLS) != maBN) throw new TuChoiQuyenNghiepVuException();
        if (!string.IsNullOrEmpty(maLS)) _ = PatientOfVisit(maLS);
        if (!string.IsNullOrEmpty(maDT))
        {
            var order = data["DON_THUOC"].Cast<ET_DONTHUOC>().SingleOrDefault(d => d.MaDT == maDT) ?? throw new TuChoiQuyenNghiepVuException();
            if ((!string.IsNullOrEmpty(maLS) && order.MaLS != maLS) || (!string.IsNullOrEmpty(maBN) && PatientOfVisit(order.MaLS) != maBN)) throw new TuChoiQuyenNghiepVuException();
        }
    }
    public BUS_KHAMDIEUTRI(IDbTransaction? transaction = null) => dal = new(transaction);
    public IReadOnlyList<HangDoiKhamItem> LayHangDoi(BoLocHangDoi boLoc, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => dal.LayHangDoi(boLoc, nguoiDung));
    public LanKhamChiTiet LayChiTiet(string maLS, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => dal.LayChiTiet(maLS, nguoiDung));
}
