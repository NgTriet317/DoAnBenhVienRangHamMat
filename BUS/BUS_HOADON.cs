using System.Data;
using DAL;
using ET;

namespace BUS;

public sealed class BUS_HOADON
{
    public string LapHoaDon(string maLS, string maNV, decimal giamGia = 0) => CreateInvoice(maLS, maNV, giamGia);
    public void XacNhanHoaDon(string maHD) => ConfirmInvoice(maHD);
    public void HuyHoaDonNhap(string maHD) => CancelInvoice(maHD);
    private readonly DAL_HOADON data;
    private readonly BUS_CHITIETHOADON details;
    private readonly BUS_THANHTOAN payments;
    private readonly BUS_PHIEUXUAT exports;
    public BUS_HOADON(DAL_GIAODICH? giaoDich = null)
    { data = new(giaoDich); details = new(giaoDich); payments = new(giaoDich); exports = new(giaoDich); }
    public ET_HOADON[] LayDanhSach() => data.LayDanhSach();
    public ET_HOADON? LayTheoMa(string ma) => data.layTheoMa(ma);
    public DataTable GetInvoices(string? visitId = null) => data.GetInvoices(visitId);
    public DataTable GetInvoiceDetails(string invoiceId) => details.GetInvoiceDetails(invoiceId);
    public DataTable GetPayments(string invoiceId) => payments.GetPayments(invoiceId);
    public DataTable GetEligibleCharges(string visitId) => details.GetEligibleCharges(visitId);
    public DataTable GetStock(string warehouseId, string prescriptionId) => exports.GetStock(warehouseId, prescriptionId);
    public string CreateInvoice(string visitId, string employeeId, decimal discount = 0)
    {
        RequireIds(visitId, employeeId);
        if (discount < 0 || decimal.Round(discount, 2) != discount) throw new InvalidOperationException("Giảm giá phải không âm và có tối đa 2 chữ số thập phân.");
        return data.CreateInvoice(visitId, employeeId, discount);
    }
    public void ConfirmInvoice(string invoiceId) { RequireIds(invoiceId); data.ConfirmInvoice(invoiceId); }
    public void CancelInvoice(string invoiceId) { RequireIds(invoiceId); data.CancelInvoice(invoiceId); }
    public string RecordPayment(string invoiceId, string employeeId, decimal amount, string type, string method, string? reason = null)
    { RequireIds(invoiceId, employeeId); return payments.RecordPayment(invoiceId, employeeId, amount, type, method, reason); }
    public string DispensePrescription(string prescriptionId, string warehouseId, string employeeId)
    { RequireIds(prescriptionId, warehouseId, employeeId); return exports.DispensePrescription(prescriptionId, warehouseId, employeeId); }
    private static void RequireIds(params string[] ids)
    { if (ids.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 50)) throw new InvalidOperationException("Chọn đầy đủ chứng từ, nhân viên và liên kết cần thiết."); }
}
