using System.Data;
using DAL;
using ET;

namespace BUS;

public sealed class BUS_THANHTOAN
{
    public string GhiNhanThanhToan(string maHD, string maNV, decimal soTien, string loaiGiaoDich, string phuongThuc, string? lyDo = null)
        => RecordPayment(maHD, maNV, soTien, loaiGiaoDich, phuongThuc, lyDo);
    private readonly DAL_THANHTOAN data;
    public BUS_THANHTOAN(DAL_GIAODICH? giaoDich = null) { data = new(giaoDich); }
    public ET_THANHTOAN[] LayDanhSach() => data.LayDanhSach();
    public ET_THANHTOAN? LayTheoMa(string ma) => data.layTheoMa(ma);
    public DataTable GetPayments(string invoiceId) => data.GetPayments(invoiceId);
    public string RecordPayment(string invoiceId, string employeeId, decimal amount, string type, string method, string? reason = null) => data.RecordPayment(invoiceId, employeeId, amount, type, method, reason);
}
