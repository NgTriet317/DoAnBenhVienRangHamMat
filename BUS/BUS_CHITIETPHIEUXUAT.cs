using System.Data;
using DAL;
using ET;

namespace BUS;

public sealed class BUS_CHITIETPHIEUXUAT
{
    private readonly DAL_CHITIETPHIEUXUAT data;
    public BUS_CHITIETPHIEUXUAT(DAL_GIAODICH? giaoDich = null) { data = new(giaoDich); }
    public ET_CHITIETPHIEUXUAT[] LayDanhSach() => data.LayDanhSach();
    public ET_CHITIETPHIEUXUAT? LayTheoMa(string ma) => data.layTheoMa(ma);
    public IEnumerable<ET_CHITIETPHIEUXUAT> layTheoPhieu(string maPX) => data.layTheoPhieu(maPX);
}
