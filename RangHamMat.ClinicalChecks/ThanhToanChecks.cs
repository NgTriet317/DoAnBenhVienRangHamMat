using BUS;
using ET;

namespace RangHamMat.ClinicalChecks;

internal static class ThanhToanChecks
{
    private static void Need(bool ok, string text) => KhamVaKeHoachChecks.Need(ok, text);
    private static void Throws<T>(Action run) where T : Exception
    { try { run(); } catch (T) { return; } throw new Exception("Phải chặn bằng " + typeof(T).Name); }
    private static string Warehouse(Fixture f) => f.Scalar<string>("SELECT TOP(1) MaKho FROM dbo.KHO WHERE TrangThai=N'Đang hoạt động' ORDER BY MaKho");
    internal static string Prepare(Fixture f, bool drugs = true, bool second = false, bool single = false, decimal quantity = 10, decimal servicePrice = 100000)
    {
        f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id; DELETE dbo.DON_THUOC WHERE MaDT=@id", new { id = f.Id("DTA") });
        var a = f.User("A");
        var services = new BUS_DICHVUTHUCHIEN(f.Transaction);
        var s = services.LuuThucHien(DichVuThucHienChecks.Request(f) with { DonGia = servicePrice }, a);
        services.XacNhanThucHien(s.DichVu.MaDVThucHien, s.DauVanTay, a);
        if (drugs)
        {
            var b = new BUS_DONTHUOC(f.Transaction);
            var r = DonThuocChecks.Request(f, single ? 1 : 2);
            foreach (var d in r.CacDong) d.SoLuong = quantity;
            // Giá fixture để phép cộng độc lập dữ liệu danh mục baseline.
            f.Execute("UPDATE dbo.THUOC SET DonGiaBan=CASE WHEN MaThuoc=@first THEN 2000 ELSE 3000 END WHERE MaThuoc IN(@first,@second)", new { first = r.CacDong[0].MaThuoc, second = r.CacDong.Last().MaThuoc });
            var rx = b.LuuDon(r, a); b.XacNhanDon(rx.Don.MaDT, rx.DauVanTay, a);
            if (second)
            { var next = b.LuuDon(DonThuocChecks.Request(f, 1) with { GhiChu = "FIXTURE bổ sung" }, a); b.XacNhanDon(next.Don.MaDT, next.DauVanTay, a); }
        }
        var clinical = new BUS_KHAMDIEUTRI(f.Transaction);
        var saved = clinical.LuuKham(KhamVaKeHoachChecks.Request(clinical.LayBanNhap(f.Id("LSA"), a)) with { KhongKeThuoc = !drugs, LyDoKhongKeThuoc = drugs ? null : "FIXTURE" }, a);
        clinical.HoanTatKham(f.Id("LSA"), saved.DauVanTay, a);
        return Warehouse(f);
    }
    private static BangTinhPhi Read(Fixture f, string? warehouse) => new BUS_THANHTOANTRONGOI(f.Transaction).LapBangTinh(f.Id("LSA"), warehouse, f.User("C"));
    private static string Counts(Fixture f) => f.Scalar<string>("SELECT CONCAT((SELECT COUNT(*) FROM dbo.HOA_DON),':',(SELECT COUNT(*) FROM dbo.PHIEU_XUAT),':',(SELECT COUNT(*) FROM dbo.THANH_TOAN),':',(SELECT SUM(SoLuongTon) FROM dbo.VW_TON_KHO))");
    private static string ThreeLots(Fixture f, decimal each = .005m)
    {
        f.Execute("""
            DECLARE @drug nvarchar(50)=(SELECT TOP(1) c.MaThuoc FROM dbo.CHI_TIET_DON_THUOC c JOIN dbo.DON_THUOC d ON d.MaDT=c.MaDT WHERE d.MaLS=@ls ORDER BY c.MaCTDT);
            DECLARE @hh nvarchar(50)=(SELECT MaHH FROM dbo.THUOC WHERE MaThuoc=@drug);
            DECLARE @ncc nvarchar(50)=(SELECT TOP(1) MaNCC FROM dbo.NHA_CUNG_CAP ORDER BY MaNCC);
            INSERT dbo.KHO(MaKho,TenKho,MaNV,LoaiKho,TrangThai) VALUES(@p+'W',N'FIXTURE 3 LÔ',@nv,N'Kho thuốc',N'Đang hoạt động');
            INSERT dbo.HOA_DON_NHAP(MaHDN,SoHoaDonNCC,MaNCC,MaNV,NgayHoaDon,TongTien,TrangThai)
            VALUES(@p+'HDN',@p,@ncc,@nv,DATEADD(day,-5,SYSDATETIME()),0,N'Nháp');
            INSERT dbo.PHIEU_NHAP(MaPN,MaHDN,MaKho,MaNV,NgayNhap,TrangThai,LyDo)
            VALUES(@p+'PN',@p+'HDN',@p+'W',@nv,DATEADD(day,-4,SYSDATETIME()),N'Nháp',N'FIXTURE');
            INSERT dbo.LO_HANG_HOA(MaLo,MaHH,MaNCC,MaKho,SoLo,NgayNhap,HanSuDung,TrangThai)
            SELECT @p+'L'+CONVERT(nvarchar(2),n),@hh,@ncc,@p+'W',CONCAT(N'FIXTURE-',n),DATEADD(day,n-5,SYSDATETIME()),DATEADD(day,100-n,SYSDATETIME()),N'Còn sử dụng'
            FROM (VALUES(1),(2),(3)) v(n);
            INSERT dbo.CHI_TIET_PHIEU_NHAP(MaCTPN,MaPN,MaHH,MaLo,SoLuong,DonGia)
            SELECT @p+'CTPN'+CONVERT(nvarchar(2),n),@p+'PN',@hh,@p+'L'+CONVERT(nvarchar(2),n),@each,1 FROM (VALUES(1),(2),(3)) v(n);
            UPDATE dbo.PHIEU_NHAP SET TrangThai=N'Đã ghi sổ' WHERE MaPN=@p+'PN';
            UPDATE dbo.THUOC SET DonGiaBan=1 WHERE MaThuoc=@drug;
            """, new { p = f.Prefix, nv = f.Id("P"), ls = f.Id("LSA"), each });
        return f.Id("W");
    }
    internal static IEnumerable<(string, Action<Fixture>)> Cases()
    {
        yield return ("P08: Ba lô lượng lẻ làm tròn từng lô đúng trigger và FIFO", f =>
        {
            Prepare(f, single: true, quantity: .015m); var w = ThreeLots(f); var before = Counts(f); var s = Read(f, w);
            Need(s.TienThuoc == .03m && s.TongTien == 100000.03m && s.Thuoc.Single().CacLo.Count == 3 && !s.DuocThanhToan, "Đã làm tròn một lần hoặc cho thu lẻ đồng");
            Need(s.Thuoc.Single().CacLo.Select(l => l.MaLo).SequenceEqual(new[] { f.Id("L1"), f.Id("L2"), f.Id("L3") }), "Đổi FIFO sang chỉ ưu tiên hạn dùng");
            Need(Counts(f) == before, "Preview trừ tồn");
            // API legacy so với giờ máy chính xác hơn giây SQL đã làm tròn; đợi đơn thực sự đến giờ.
            // Ca này so phân lô/trigger, không thay quy tắc thời gian của entry point cũ.
            var dated=f.Scalar<DateTime>("SELECT NgayKe FROM dbo.DON_THUOC WHERE MaDT=@id",new{id=s.Thuoc.Single().MaDT})-DateTime.Now;
            if(dated>TimeSpan.Zero)Thread.Sleep(dated+TimeSpan.FromMilliseconds(20));
            var legacy = new BUS_HOADON(f.Transaction); legacy.DispensePrescription(s.Thuoc.Single().MaDT, w, f.Id("P"));
            var invoice = legacy.CreateInvoice(f.Id("LSA"), f.Id("C"));
            Need(f.Scalar<decimal>("SELECT TongTien FROM dbo.HOA_DON WHERE MaHD=@invoice", new { invoice }) == s.TongTien, "Preview khác tổng trigger");
            Need(legacy.GetInvoiceDetails(invoice).Rows.Count == 4, "Mất ba mã chi tiết xuất khi lập hóa đơn");
        });
        yield return ("P08: Đơn thứ hai không được dùng lại tồn đã phân cho đơn thứ nhất", f =>
        { Prepare(f, second: true, single: true, quantity: 10); var w = ThreeLots(f, 4); var before = Counts(f); Throws<LoiKhamException>(() => Read(f, w)); Need(Counts(f) == before, "Thiếu tồn đã ghi dở"); });
        yield return ("P08: Đổi lượng bằng đơn thay thế đổi token; đơn ngừng/hủy không cộng", f =>
        {
            var w = Prepare(f); var s = Read(f, w); var a = f.User("A"); var b = new BUS_DONTHUOC(f.Transaction);
            var old = b.LayBanNhapDon(s.Thuoc[0].MaDT, a); var r = DonThuocChecks.Request(f, 1) with { MaDTThayThe = old.Don.MaDT, LyDoThayThe = "FIXTURE thay lượng" };
            var pending = b.LuuDon(r, a); Throws<LoiKhamException>(() => Read(f, w)); f.ActAs("A"); b.XacNhanDon(pending.Don.MaDT, pending.DauVanTay, a);
            var next = Read(f, w); Need(next.DauVanTay != s.DauVanTay && next.TongTien == 106000 && next.Thuoc.Count == 1 && next.Thuoc[0].MaDT != old.Don.MaDT, "Cộng đơn đã ngừng hoặc bỏ sót lượng thay");
            f.ActAs("A"); var canceled = b.LuuDon(r with { MaDTThayThe = pending.Don.MaDT }, a); b.HuyDonNhap(canceled.Don.MaDT, canceled.DauVanTay, a); Need(Read(f, w).DauVanTay == next.DauVanTay, "Đơn hủy làm đổi nguồn thu");
        });
        yield return ("P08: Xuất của lượt khác làm đổi tồn/lô và token bảng tính", f =>
        {
            Prepare(f, single: true, quantity: 5); var w = ThreeLots(f, 5); var s = Read(f, w);
            new BUS_HOADON(f.Transaction).DispensePrescription(f.Id("DTH"), w, f.Id("P")); var next = Read(f, w);
            Need(next.DauVanTay != s.DauVanTay && next.Thuoc.Single().CacLo.Count == 2, "Không tái phân lô sau thay đổi tồn");
        });
        yield return ("P08: Hóa đơn cũ thu một phần giữ nguyên số lẻ và số còn phải thu", f =>
        {
            Prepare(f, false, servicePrice: 100000.25m); var b = new BUS_HOADON(f.Transaction); var id = b.CreateInvoice(f.Id("LSA"), f.Id("C")); b.ConfirmInvoice(id);
            b.RecordPayment(id, f.Id("C"), 40000.10m, "Thu", "Tiền mặt"); var before = Counts(f); var s = Read(f, null); var h = s.HoaDonCu.Single();
            Need(!s.DuocThanhToan && s.DauVanTay is null && s.DichVu.Count == 0 && h.PhaiThu == 100000.25m && h.DaThu == 40000.10m && h.ConPhaiThu == 60000.15m, "Đánh dấu đã thu đủ/đã làm tròn dữ liệu cũ");
            Need(Counts(f) == before && !s.ThongBao.StartsWith("Đã thanh toán"), "Đã thu lại/đánh dấu sai");
        });
        yield return ("P08: Phiếu xuất nháp cũ cũng bắt đối chiếu, không tái lập phí", f =>
        { var w = Prepare(f); f.Execute("INSERT dbo.PHIEU_XUAT(MaPX,MaKho,MaNV,MaLS,NgayXuat,LyDo,TrangThai) VALUES(@id,@w,@nv,@ls,SYSDATETIME(),N'FIXTURE',N'Nháp')", new { id = f.Id("PX08"), w, nv = f.Id("P"), ls = f.Id("LSA") }); var s = Read(f, null); Need(!s.DuocThanhToan && s.DauVanTay is null && s.Thuoc.Count == 0, "Tính thu trên chứng từ cũ"); });
        yield return ("P08: Dịch vụ 100000 và hai thuốc 50000; xem không ghi tài chính/tồn", f =>
        { var w = Prepare(f); var before = Counts(f); var s = Read(f, w); Need(s.TongTien == 150000 && s.TienDichVu == 100000 && s.TienThuoc == 50000 && s.Thuoc.Count == 2 && s.DuocThanhToan, "Sai tổng/nguồn"); Need(Counts(f) == before, "Xem phí đã ghi chứng từ/trừ tồn"); });
        yield return ("P08: Tất cả đơn hiệu lực được cộng; dùng chung tồn", f =>
        { var w = Prepare(f, second: true); var s = Read(f, w); Need(s.TongTien == 156000 && s.Thuoc.Select(d => d.MaDT).Distinct().Count() == 2, "Bỏ sót đơn bổ sung"); });
        yield return ("P08: Token ổn định và đổi khi đổi giá thuốc", f =>
        { var w = Prepare(f); var s = Read(f, w); Need(s.DauVanTay?.Length == 64 && Read(f, w).DauVanTay == s.DauVanTay, "Token theo thời điểm render"); f.Execute("UPDATE dbo.THUOC SET DonGiaBan=DonGiaBan+1 WHERE MaThuoc=@id", new { id = s.Thuoc[0].MaThuoc }); var next = Read(f, w); Need(next.DauVanTay != s.DauVanTay && next.TongTien == s.TongTien + 10, "Giá đổi chưa tái tính"); });
        yield return ("P08: Có thuốc bắt buộc kho đang hoạt động", f =>
        { var w = Prepare(f); Throws<LoiKhamException>(() => Read(f, null)); f.Execute("UPDATE dbo.KHO SET TrangThai=N'Ngừng hoạt động' WHERE MaKho=@w", new { w }); Throws<LoiKhamException>(() => Read(f, w)); });
        yield return ("P08: Dịch vụ không kê thuốc tính được không cần kho", f =>
        { Prepare(f, false); var s = Read(f, null); Need(s.TongTien == 100000 && s.Thuoc.Count == 0 && s.DuocThanhToan, "Bắt kho cho lượt không thuốc"); });
        yield return ("P08: Quyền SQL và đúng bác sĩ; không lộ dữ liệu chuyên môn", f =>
        { var w = Prepare(f); var b = new BUS_THANHTOANTRONGOI(f.Transaction); foreach (var role in new[] { "N", "P", "B" }) { var u = f.User(role) with { Quyen = QuyenLamViec.ThuTien }; Throws<TuChoiQuyenNghiepVuException>(() => b.LapBangTinh(f.Id("LSA"), w, u)); } var s = b.LapBangTinh(f.Id("LSA"), w, f.User("A")); Need(s.TongTien == 150000 && !System.Text.Json.JsonSerializer.Serialize(s).Contains("FIXTURE kết quả"), "Lộ kết quả khám"); });
        yield return ("P08: Chưa hoàn tất không được tính phí", f =>
        { Throws<LoiKhamException>(() => Read(f, Warehouse(f))); });
        yield return ("P08: Admin tra cứu có nhật ký", f =>
        { var w = Prepare(f); var x = f.User("X"); new BUS_THANHTOANTRONGOI(f.Transaction).LapBangTinh(f.Id("LSA"), w, x); Need(f.Scalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaTK=@MaTK AND MaDoiTuong=@id", new { x.MaTK, id = f.Id("LSA") }) > 0, "Thiếu audit"); });
    }
}
