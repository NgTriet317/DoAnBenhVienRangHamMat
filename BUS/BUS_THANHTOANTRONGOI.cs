using System.Data;
using DAL;
using ET;
namespace BUS;

public sealed class BUS_THANHTOANTRONGOI
{
    private readonly DAL_THANHTOANTRONGOI dal;
    public BUS_THANHTOANTRONGOI(IDbTransaction? transaction = null) => dal = new(transaction);
    public BangTinhPhi LapBangTinh(string maLS, string? maKhoCapThuoc, NguCanhNghiepVu nguoiDung)
        => dal.LapBangTinh(maLS, maKhoCapThuoc, nguoiDung);
}
