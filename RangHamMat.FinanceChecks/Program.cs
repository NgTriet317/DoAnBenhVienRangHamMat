using System.Data;
using System.Transactions;
using BUS;
using DAL;
using Dapper;
using Microsoft.Data.SqlClient;

var finance = new BUS_HOADON();
var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + message);
    checks++;
}
void Reject(Action operation, string message)
{
    try { operation(); }
    catch (InvalidOperationException) { checks++; return; }
    throw new InvalidOperationException("FAIL: " + message);
}
void RollbackCheck(Action<Fixture> operation, bool insufficient = false)
{
    using var scope = new TransactionScope(TransactionScopeOption.RequiresNew,
        new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromMinutes(2) });
    var fixture = Sql(db => db.QuerySingle<Fixture>(Fixture.Script, new { insufficient }));
    operation(fixture);
    // Intentionally never Complete: all fixture and BUS writes roll back.
}

var before = Sql(db => db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.HOA_DON"));
// A local shared transaction must read its own pending finance writes. A short
// timeout makes opening a competing connection fail promptly instead of hanging.
var previousTimeout = SqlMapper.Settings.CommandTimeout;
SqlMapper.Settings.CommandTimeout = 2;
try
{
    using var shared = new DAL_GIAODICH();
    shared.Begin();
    try
    {
        var fixture = shared.ThucHien((db, tx) => db.QuerySingle<Fixture>(Fixture.Script, new { insufficient = false }, tx));
        var invoiceBus = new BUS_HOADON(shared);
        var paymentBus = new BUS_THANHTOAN(shared);
        var exportBus = new BUS_PHIEUXUAT(shared);
        var detailBus = new BUS_CHITIETHOADON(shared);
        var exportDetailBus = new BUS_CHITIETPHIEUXUAT(shared);
        var export = exportBus.DispensePrescription(fixture.Prescription, fixture.Warehouse, fixture.Employee);
        var invoice = invoiceBus.CreateInvoice(fixture.Visit, fixture.Employee);
        invoiceBus.ConfirmInvoice(invoice);
        var payment = paymentBus.RecordPayment(invoice, fixture.Employee, 100, "Thu", "Tiền mặt");
        var lineId = shared.ThucHien((db, tx) => db.ExecuteScalar<string>("SELECT TOP(1) MaCTHD FROM dbo.CHI_TIET_HOA_DON WHERE MaHD=@invoice", new { invoice }, tx));
        var exportLineId = shared.ThucHien((db, tx) => db.ExecuteScalar<string>("SELECT TOP(1) MaCTPX FROM dbo.CHI_TIET_PHIEU_XUAT WHERE MaPX=@export", new { export }, tx));
        Check(invoiceBus.LayTheoMa(invoice)?.MaLS == fixture.Visit, "shared invoice read sees its own pending header");
        Check(paymentBus.LayTheoMa(payment)?.MaHD == invoice, "shared payment read sees its own pending payment");
        Check(exportBus.LayTheoMa(export)?.MaLS == fixture.Visit, "shared export read sees its own pending export");
        Check(detailBus.LayTheoMa(lineId)?.MaHD == invoice, "shared invoice detail read sees its own pending line");
        Check(exportDetailBus.LayTheoMa(exportLineId)?.MaPX == export, "shared export detail read sees its own pending line");
        Check(new DAL_HOADON(shared).layToanBo().Any(row => row.MaHD == invoice), "shared invoice read-all sees pending writes");
        Check(new DAL_THANHTOAN(shared).layToanBo().Any(row => row.MaTT == payment), "shared payment read-all sees pending writes");
        Check(new DAL_PHIEUXUAT(shared).layToanBo().Any(row => row.MaPX == export), "shared export read-all sees pending writes");
        Check(new DAL_CHITIETHOADON(shared).layToanBo().Count(row => row.MaHD == invoice) == 4, "shared invoice detail read-all sees pending writes");
        Check(new DAL_CHITIETPHIEUXUAT(shared).layToanBo().Count(row => row.MaPX == export) == 3, "shared export detail read-all sees pending writes");
        Check(exportDetailBus.layTheoPhieu(export).Count() == 3, "shared export detail by-document read sees pending writes");
        var header = invoiceBus.GetInvoices(fixture.Visit).Rows.Cast<DataRow>().Single(row => Convert.ToString(row["MaHD"]) == invoice);
        Check(Convert.ToDecimal(header["DaThu"]) == 100, "shared invoice table query sees pending payment balance");
        Check(detailBus.GetInvoiceDetails(invoice).Rows.Count == 4, "shared invoice detail table query sees pending writes");
        Check(detailBus.GetEligibleCharges(fixture.Visit).Rows.Count == 0, "shared eligible charge query excludes pending invoiced sources");
        Check(paymentBus.GetPayments(invoice).Rows.Count == 1, "shared payment table query sees pending payment");
        Check(!exportBus.GetStock(fixture.Warehouse, fixture.Prescription).Rows.Cast<DataRow>().Any(row => Convert.ToString(row["MaLo"]) == fixture.OldLot),
            "shared stock table query reflects pending dispensing");
        Check(shared.DangGiaoDich, "finance reads preserve the caller-owned transaction");
    }
    finally { shared.Rollback(); }
}
finally { SqlMapper.Settings.CommandTimeout = previousTimeout; }
RollbackCheck(f =>
{
    var eligible = finance.GetEligibleCharges(f.Visit);
    Check(eligible.Rows.Count == 1 && Convert.ToDecimal(eligible.Rows[0]["ThanhTien"]) == 250000m,
        "only the confirmed performed service is chargeable before dispensing");
    Reject(() => finance.CreateInvoice(f.Visit, f.Employee, 250001), "discount cannot exceed actual charges");
    var draft = finance.CreateInvoice(f.Visit, f.Employee, 10);
    Check(draft.StartsWith("HD") && long.TryParse(draft[2..], out _), "invoice gets its generated string code");
    Check(finance.GetInvoiceDetails(draft).Rows.Count == 1, "draft persists service lines");
    Reject(() => finance.CreateInvoice(f.Visit, f.Employee), "a source must not appear on two invoices");
    finance.CancelInvoice(draft);
    Check(finance.GetInvoiceDetails(draft).Rows.Count == 0, "cancel releases draft sources");
    Check(finance.GetEligibleCharges(f.Visit).Rows.Count == 1, "canceled source becomes available");
    Reject(() => finance.RecordPayment(draft, f.Employee, 1, "Thu", "Tiền mặt"), "canceled invoice is not payable");

    var export = finance.DispensePrescription(f.Prescription, f.Warehouse, f.Employee);
    Check(export.StartsWith("PX") && long.TryParse(export[2..], out _), "export gets its generated string code");
    Check(Sql(db => db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.CHI_TIET_PHIEU_XUAT WHERE MaPX=@export", new { export })) == 3,
        "full prescription is split across FIFO lots");
    Check(Sql(db => db.ExecuteScalar<decimal>("SELECT SUM(SoLuong) FROM dbo.CHI_TIET_PHIEU_XUAT WHERE MaPX=@export AND MaLo=@OldLot", new { export, f.OldLot })) == 2,
        "oldest eligible lot is used first");
    Check(!finance.GetStock(f.Warehouse, f.Prescription).Rows.Cast<DataRow>().Any(row => Convert.ToString(row["MaLo"]) == f.OldLot),
        "used-up FIFO lot disappears from stock");
    Reject(() => finance.DispensePrescription(f.Prescription, f.Warehouse, f.Employee), "prescription cannot be dispensed twice");
    var invoice = finance.CreateInvoice(f.Visit, f.Employee, 20);
    var detail = finance.GetInvoiceDetails(invoice);
    Check(detail.Rows.Count == 4 && detail.Rows.Cast<DataRow>().Sum(row => Convert.ToDecimal(row["ThanhTien"])) == 250900m,
        "actual service and all posted prescription lots form the invoice, forecast and draft service excluded");
    Sql(db => db.Execute("UPDATE dbo.THUOC SET DonGiaBan=99999 WHERE MaThuoc=@Drug", new { f.Drug }));
    Check(finance.GetInvoiceDetails(invoice).Rows.Cast<DataRow>().Where(row => row["MaCTPX"] != DBNull.Value)
        .Sum(row => Convert.ToDecimal(row["ThanhTien"])) == 900m, "drug price snapshot survives later price change");
    finance.ConfirmInvoice(invoice);
    Reject(() => finance.CancelInvoice(invoice), "confirmed invoice cannot be canceled");
    Reject(() => finance.ConfirmInvoice(invoice), "confirmed invoice cannot be rewritten");
    Reject(() => finance.RecordPayment(invoice, f.Employee, 250881, "Thu", "Tiền mặt"), "overpayment rejected");
    Reject(() => finance.RecordPayment(invoice, f.Employee, 0, "Thu", "Tiền mặt"), "zero payment rejected");
    Reject(() => finance.RecordPayment(invoice, f.Employee, 0.001m, "Thu", "Tiền mặt"), "sub-cent amount cannot silently round");
    Reject(() => finance.RecordPayment(invoice, f.Employee, 1, "Hoàn", "Tiền mặt", "Hoàn"), "refund before collection rejected");
    var payment = finance.RecordPayment(invoice, f.Employee, 1000, "Thu", "Tiền mặt");
    Check(payment.StartsWith("TT") && long.TryParse(payment[2..], out _), "payment gets its generated string code");
    Check(new BUS_THANHTOAN().LayTheoMa(payment)?.MaHD == invoice, "payment has its own typed BUS/DAL read path");
    Check(new BUS_PHIEUXUAT().LayTheoMa(export)?.MaLS == f.Visit, "export has its own typed BUS/DAL read path");
    Check(new BUS_CHITIETHOADON().LayDanhSach().Count(line => line.MaHD == invoice) == 4,
        "invoice detail has its own typed BUS/DAL read path");
    Check(new BUS_CHITIETPHIEUXUAT().layTheoPhieu(export).Count() == 3,
        "export detail has its own typed BUS/DAL read path");
    using (var shared = new DAL_GIAODICH())
        Check(new BUS_HOADON(shared).LayDanhSach().Single(row => row.MaHD == invoice).TrangThai == "Đã xác nhận",
            "invoice supports the shared clinical loading transaction");
    Reject(() => finance.RecordPayment(invoice, f.Employee, 100, "Hoàn", "Tiền mặt"), "refund reason required");
    Reject(() => finance.RecordPayment(invoice, f.Employee, 1001, "Hoàn", "Tiền mặt", "Hoàn"), "refund cannot exceed net collection");
    finance.RecordPayment(invoice, f.Employee, 100, "Hoàn", "Chuyển khoản", "Điều chỉnh thu");
    var header = finance.GetInvoices(f.Visit).Rows.Cast<DataRow>().Single(row => Convert.ToString(row["MaHD"]) == invoice);
    Check(Convert.ToDecimal(header["DaThu"]) == 900 && Convert.ToDecimal(header["ConPhaiThu"]) == 249980,
        "collection minus refund produces the remaining balance");
    finance.RecordPayment(invoice, f.Employee, 249980, "Thu", "Chuyển khoản");
    Reject(() => finance.RecordPayment(invoice, f.Employee, 1, "Thu", "Tiền mặt"), "settled invoice cannot be overpaid");
    Check(finance.GetPayments(invoice).Rows.Count == 3, "payment ledger persists separate positive collection/refund records");
});

RollbackCheck(f =>
{
    var exportsBefore = Sql(db => db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.PHIEU_XUAT"));
    Reject(() => finance.DispensePrescription(f.Prescription, f.Warehouse, f.Employee), "insufficient stock must reject the whole prescription");
    Check(Sql(db => db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.PHIEU_XUAT")) == exportsBefore,
        "failed dispensing leaves no partial export or inventory movement");
}, insufficient: true);
Check(Sql(db => db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.HOA_DON")) == before, "rollback checks leave no invoices committed");

// Use an existing committed header, so contention proves the BUS row lock rather
// than the insert lock on a new fixture. Neither collector writes any live data.
var existing = Sql(db => db.QuerySingle<(string Invoice, string Employee)>("SELECT TOP(1) MaHD Invoice,MaNV Employee FROM dbo.HOA_DON WHERE TrangThai=N'Đã xác nhận' ORDER BY MaHD"));
using (var lockScope = new TransactionScope(TransactionScopeOption.RequiresNew,
    new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(30) }))
{
    Reject(() => finance.RecordPayment(existing.Invoice, existing.Employee, 0.001m, "Thu", "Tiền mặt"), "first collector validates while holding the invoice lock");
    using var suppressed = new TransactionScope(TransactionScopeOption.Suppress);
    var blocked = false;
    try { Sql(db => db.ExecuteScalar<string>("SET LOCK_TIMEOUT 1000; SELECT MaHD FROM dbo.HOA_DON WITH(UPDLOCK,HOLDLOCK) WHERE MaHD=@Invoice", new { existing.Invoice })); }
    catch (SqlException ex) when (ex.Number == 1222) { blocked = true; }
    Check(blocked, "a second collector cannot enter the same invoice while its transaction holds the row lock");
}
Console.WriteLine($"SUCCESS: {checks} finance transaction checks; all SQL changes rolled back.");

static T Sql<T>(Func<IDbConnection, T> action)
{
    using var db = new CheckConnection().Open();
    return action(db);
}

sealed class CheckConnection : dbconnect
{
    public IDbConnection Open() { var db = CreateConnection(); db.Open(); return db; }
}
sealed class Fixture
{
    public string Visit { get; set; } = ""; public string Prescription { get; set; } = ""; public string Employee { get; set; } = "";
    public string Warehouse { get; set; } = ""; public string OldLot { get; set; } = ""; public string Drug { get; set; } = ""; public string Goods { get; set; } = "";
    public const string Script = """
    DECLARE @employee nvarchar(50)=(SELECT MIN(MaNV) FROM dbo.NHAN_VIEN WHERE TrangThai=N'Đang làm việc');
    DECLARE @warehouse nvarchar(50)=(SELECT MIN(MaKho) FROM dbo.KHO WHERE TrangThai=N'Đang hoạt động');
    DECLARE @source nvarchar(50)=(SELECT MIN(MaPhieu) FROM dbo.PHIEU_KHAM_BENH);
    DECLARE @ticket nvarchar(50)=N'T-MaPhieu-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    DECLARE @visit nvarchar(50)=N'T-MaLS-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),@service nvarchar(50)=(SELECT MIN(MaDV) FROM dbo.DICH_VU);
    DECLARE @plan nvarchar(50)=N'T-MaKHDT-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    DECLARE @type nvarchar(50)=(SELECT MIN(MaLoaiHH) FROM dbo.LOAI_HANG_HOA);
    DECLARE @goodsA nvarchar(50)=N'T-MaHH-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    DECLARE @drugA nvarchar(50)=N'T-MaThuoc-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    DECLARE @goodsB nvarchar(50)=N'T-MaHH-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    DECLARE @drugB nvarchar(50)=N'T-MaThuoc-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    DECLARE @rx nvarchar(50)=N'T-MaDT-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    DECLARE @supplier nvarchar(50)=(SELECT MIN(MaNCC) FROM dbo.NHA_CUNG_CAP);
    DECLARE @importInvoice nvarchar(50)=N'T-MaHDN-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    DECLARE @import nvarchar(50)=N'T-MaPN-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    DECLARE @old nvarchar(50)=N'T-MaLo-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    DECLARE @new nvarchar(50)=N'T-MaLo-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    DECLARE @lotB nvarchar(50)=N'T-MaLo-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    DECLARE @expired nvarchar(50)=N'T-MaLo-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
    INSERT dbo.PHIEU_KHAM_BENH(MaPhieu,MaHS,MaPhong,MaBS,MaNVTiepNhan,SoThuTu,ThoiGianTiepNhan,LyDoKham,TrangThai)
      SELECT @ticket,MaHS,MaPhong,MaBS,@employee,999,DATEADD(day,-1,SYSDATETIME()),N'Rollback finance check',N'Chờ khám' FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@source;
    INSERT dbo.LICH_SU_KHAM_BENH(MaLS,MaPhieu,MaBS,MaPhong,ThoiGianKham,TrieuChung,ChanDoan,KeHoachXuLy,TrangThai,NgayXacNhan)
      SELECT @visit,@ticket,COALESCE(MaBS,@employee),MaPhong,DATEADD(hour,-12,SYSDATETIME()),N'Check',N'Check',N'Check',N'Đã xác nhận',DATEADD(hour,-11,SYSDATETIME()) FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@ticket;
    INSERT dbo.DICH_VU_THUC_HIEN(MaDVThucHien,MaLS,MaDV,MaNVThucHien,ThoiGianThucHien,SoLuong,DonGia,TrangThai)
      VALUES(N'T-MaDVThucHien-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),@visit,@service,@employee,DATEADD(hour,-10,SYSDATETIME()),2,125000,N'Đã xác nhận'),(N'T-MaDVThucHien-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),@visit,@service,@employee,SYSDATETIME(),1,888888,N'Nháp');
    INSERT dbo.KE_HOACH_DIEU_TRI(MaKHDT,MaLS,MaBS,NgayLap,TrangThai) VALUES(@plan,@visit,@employee,SYSDATETIME(),N'Nháp');
    INSERT dbo.CHI_TIET_KE_HOACH(MaCTKH,MaKHDT,MaDV,LanThu,SoLuongDuKien,DonGiaDuKien,TrangThai) VALUES(N'T-MaCTKH-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),@plan,@service,1,10,99999,N'Nháp');
    INSERT dbo.HANG_HOA(MaHH,TenHH,MaLoaiHH,DonViTinh,MoTa,TrangThai) VALUES(@goodsA,N'Rollback drug A',@type,N'Viên',N'Check',N'Đang sử dụng');
    INSERT dbo.THUOC(MaThuoc,MaHH,HamLuong,MucTonToiThieu,DonGiaBan) VALUES(@drugA,@goodsA,N'10 mg',0,100);
    INSERT dbo.HANG_HOA(MaHH,TenHH,MaLoaiHH,DonViTinh,MoTa,TrangThai) VALUES(@goodsB,N'Rollback drug B',@type,N'Viên',N'Check',N'Đang sử dụng');
    INSERT dbo.THUOC(MaThuoc,MaHH,HamLuong,MucTonToiThieu,DonGiaBan) VALUES(@drugB,@goodsB,N'20 mg',0,150);
    INSERT dbo.DON_THUOC(MaDT,MaLS,MaBS,NgayKe,TrangThai) VALUES(@rx,@visit,@employee,DATEADD(hour,-9,SYSDATETIME()),N'Nháp');
    INSERT dbo.CHI_TIET_DON_THUOC(MaCTDT,MaDT,MaThuoc,LieuDung,SoLuong,SoNgayDung,HuongDan)
      VALUES(N'T-MaCTDT-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),@rx,@drugA,N'1 viên',3,3,N'Sau ăn'),(N'T-MaCTDT-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),@rx,@drugB,N'2 viên',CASE WHEN @insufficient=1 THEN 9999 ELSE 4 END,2,N'Sau ăn');
    UPDATE dbo.DON_THUOC SET TrangThai=N'Đã xác nhận' WHERE MaDT=@rx;
    INSERT dbo.HOA_DON_NHAP(MaHDN,SoHoaDonNCC,MaNCC,MaNV,NgayHoaDon,TongTien,TrangThai) VALUES(@importInvoice,N'ROLLBACK-CHECK',@supplier,@employee,DATEADD(day,-4,SYSDATETIME()),0,N'Nháp');
    INSERT dbo.PHIEU_NHAP(MaPN,MaHDN,MaKho,MaNV,NgayNhap,TrangThai,LyDo) VALUES(@import,@importInvoice,@warehouse,@employee,DATEADD(day,-3,SYSDATETIME()),N'Nháp',N'Rollback check');
    INSERT dbo.LO_HANG_HOA(MaLo,MaHH,MaNCC,MaKho,SoLo,NgayNhap,HanSuDung,TrangThai) VALUES(@old,@goodsA,@supplier,@warehouse,N'Old',DATEADD(day,-3,SYSDATETIME()),DATEADD(day,30,SYSDATETIME()),N'Còn sử dụng');
    INSERT dbo.CHI_TIET_PHIEU_NHAP(MaCTPN,MaPN,MaHH,MaLo,SoLuong,DonGia) VALUES(N'T-MaCTPN-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),@import,@goodsA,@old,2,10);
    INSERT dbo.LO_HANG_HOA(MaLo,MaHH,MaNCC,MaKho,SoLo,NgayNhap,HanSuDung,TrangThai) VALUES(@new,@goodsA,@supplier,@warehouse,N'New',DATEADD(day,-2,SYSDATETIME()),NULL,N'Còn sử dụng');
    INSERT dbo.CHI_TIET_PHIEU_NHAP(MaCTPN,MaPN,MaHH,MaLo,SoLuong,DonGia) VALUES(N'T-MaCTPN-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),@import,@goodsA,@new,5,10);
    INSERT dbo.LO_HANG_HOA(MaLo,MaHH,MaNCC,MaKho,SoLo,NgayNhap,HanSuDung,TrangThai) VALUES(@lotB,@goodsB,@supplier,@warehouse,N'B',DATEADD(day,-3,SYSDATETIME()),DATEADD(day,30,SYSDATETIME()),N'Còn sử dụng');
    INSERT dbo.CHI_TIET_PHIEU_NHAP(MaCTPN,MaPN,MaHH,MaLo,SoLuong,DonGia) VALUES(N'T-MaCTPN-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),@import,@goodsB,@lotB,10,10);
    INSERT dbo.LO_HANG_HOA(MaLo,MaHH,MaNCC,MaKho,SoLo,NgayNhap,HanSuDung,TrangThai) VALUES(@expired,@goodsA,@supplier,@warehouse,N'Expired',DATEADD(day,-10,SYSDATETIME()),DATEADD(day,-1,SYSDATETIME()),N'Còn sử dụng');
    INSERT dbo.CHI_TIET_PHIEU_NHAP(MaCTPN,MaPN,MaHH,MaLo,SoLuong,DonGia) VALUES(N'T-MaCTPN-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),@import,@goodsA,@expired,100,10);
    UPDATE dbo.PHIEU_NHAP SET TrangThai=N'Đã ghi sổ' WHERE MaPN=@import;
    SELECT @visit Visit,@rx Prescription,@employee Employee,@warehouse Warehouse,@old OldLot,@drugA Drug,@goodsA Goods;
    """;
}
