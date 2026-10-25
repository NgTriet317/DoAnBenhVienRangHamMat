using System.Data;
using ET;
using DAL;

namespace BUS;

public sealed class BUS_KHAMDIEUTRI
{
    private readonly DAL_KHAMDIEUTRI dal;
    public BUS_KHAMDIEUTRI(IDbTransaction? transaction = null) => dal = new(transaction);
    public IReadOnlyList<HangDoiKhamItem> LayHangDoi(BoLocHangDoi boLoc, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => dal.LayHangDoi(boLoc, nguoiDung));
    public LanKhamChiTiet LayChiTiet(string maLS, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => dal.LayChiTiet(maLS, nguoiDung));
}
