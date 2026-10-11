using System.Data;
using DAL;
using ET;

namespace BUS;

public sealed class BUS_PHIEUXUAT
{
    public string CapDonThuoc(string maDT, string maKho, string maNV) => DispensePrescription(maDT, maKho, maNV);
    private readonly DAL_PHIEUXUAT data;
    public BUS_PHIEUXUAT(DAL_GIAODICH? giaoDich = null) { data = new(giaoDich); }
    public ET_PHIEUXUAT[] LayDanhSach() => data.LayDanhSach();
    public ET_PHIEUXUAT? LayTheoMa(string ma) => data.layTheoMa(ma);
    public DataTable GetStock(string warehouseId, string prescriptionId) => data.GetStock(warehouseId, prescriptionId);
    public string DispensePrescription(string prescriptionId, string warehouseId, string employeeId) => data.DispensePrescription(prescriptionId, warehouseId, employeeId);
}
