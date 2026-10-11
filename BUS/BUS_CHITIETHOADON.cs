using System.Data;
using DAL;
using ET;

namespace BUS;

public sealed class BUS_CHITIETHOADON
{
    private readonly DAL_CHITIETHOADON data;
    public BUS_CHITIETHOADON(DAL_GIAODICH? giaoDich = null) { data = new(giaoDich); }
    public ET_CHITIETHOADON[] LayDanhSach() => data.LayDanhSach();
    public ET_CHITIETHOADON? LayTheoMa(string ma) => data.layTheoMa(ma);
    public DataTable GetInvoiceDetails(string invoiceId) => data.GetInvoiceDetails(invoiceId);
    public DataTable GetEligibleCharges(string visitId) => data.GetEligibleCharges(visitId);
}
