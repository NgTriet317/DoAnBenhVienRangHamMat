using BUS;
using ET;

namespace RangHamMat.ClinicalChecks;

internal static class PhamViGiaoDienChecks
{
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    private static void Deny(Action run)
    {
        try { run(); } catch (TuChoiQuyenNghiepVuException) { return; }
        throw new InvalidOperationException("Thao tác ngoài quyền không bị chặn.");
    }

    public static IEnumerable<(string Name, Action<Fixture> Run)> Cases()
    {
        yield return ("Snapshot UI bác sĩ loại BN ngoài phân công và nháp bác sĩ khác", f =>
        {
            var data = new BUS_KHAMDIEUTRI(f.Transaction).LayDuLieuHoSo(f.User("A")).CacBang;
            var patients = data["BENH_NHAN"].Cast<ET_BENHNHAN>().ToArray();
            var visits = data["LICH_SU_KHAM_BENH"].Cast<ET_LICHSUKHAMBENH>().ToArray();
            Require(patients.Length == 1 && patients[0].MaBN == f.Id("BN1"), "SQL phải chọn BN theo mã/phân công.");
            Require(visits.Select(v => v.MaLS).Order().SequenceEqual(new[] { f.Id("LSA"), f.Id("LSH") }.Order()), "Nháp B hoặc lịch sử BN khác bị lộ.");
            var orders = data["DON_THUOC"].Cast<ET_DONTHUOC>().ToArray();
            Require(orders.Length == 2 && orders.All(d => d.MaDT != f.Id("DTB") && d.MaDT != f.Id("DTU")), "Đơn nháp bác sĩ khác bị lộ.");
            Require(data["CHI_TIET_DON_THUOC"].Cast<ET_CHITIETDONTHUOC>().All(c => orders.Any(d => d.MaDT == c.MaDT)), "Lộ dòng đơn ngoài tập cha có quyền.");
        });
        yield return ("Snapshot dược chỉ có lượt và đơn đã ký, không triệu chứng tự do", f =>
        {
            var data = new BUS_KHAMDIEUTRI(f.Transaction).LayDuLieuHoSo(f.User("P")).CacBang;
            Require(data["DON_THUOC"].Cast<ET_DONTHUOC>().All(d => d.TrangThai != "Nháp"), "Dược không đọc đơn nháp.");
            Require(data["LICH_SU_KHAM_BENH"].Cast<ET_LICHSUKHAMBENH>().All(v => v.TrangThai == "Đã xác nhận" && v.TrieuChung == "" && v.KeHoachXuLy == ""), "Chỉ cấp nội dung cần giao thuốc.");
        });
        yield return ("Tiếp nhận được ghi BN, bác sĩ và Admin tra cứu không được sửa BN", f =>
        {
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            var values = new Dictionary<string, object?> { ["HoTenBN"] = "FIXTURE" };
            rights.KiemTraGhiHoSo("BENH_NHAN", values, f.Id("BN1"), false, f.User("R"));
            Deny(() => rights.KiemTraGhiHoSo("BENH_NHAN", values, f.Id("BN1"), false, f.User("A")));
            Deny(() => rights.KiemTraGhiHoSo("BENH_NHAN", values, f.Id("BN1"), false, f.User("X")));
        });
        yield return ("Guard dòng thuốc truy ngược cả đơn/lượt/phiếu, không tin MaDT thay thế", f =>
        {
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            var user = f.User("A");
            var values = new Dictionary<string, object?> { ["MaDT"] = f.Id("DTA"), ["SoLuong"] = 3m };
            rights.KiemTraGhiHoSo("CHI_TIET_DON_THUOC", values, f.Id("CTA"), false, user);
            Deny(() => rights.KiemTraGhiHoSo("CHI_TIET_DON_THUOC", values, f.Id("CTB"), false, user));
            Deny(() => rights.KiemTraGhiHoSo("CHI_TIET_DON_THUOC", new Dictionary<string, object?> { ["MaDT"] = f.Id("DTB") }, f.Id("CTA"), false, user));
            Deny(() => rights.KiemTraGhiHoSo("CHI_TIET_DON_THUOC", new Dictionary<string, object?> { ["MaDT"] = f.Id("DTB") }, null, false, user));
        });
        yield return ("Guard ký/hủy/xóa đơn bác sĩ khác và thay người ký bị chặn", f =>
        {
            var user = f.User("A");
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            Deny(() => rights.KiemTraGhiHoSo("DON_THUOC", new Dictionary<string, object?> { ["TrangThai"] = "Đã xác nhận" }, f.Id("DTB"), false, user));
            Deny(() => rights.KiemTraGhiHoSo("DON_THUOC", new Dictionary<string, object?>(), f.Id("DTB"), true, user));
            Deny(() => rights.KiemTraGhiHoSo("DON_THUOC", new Dictionary<string, object?> { ["MaBS"] = f.Id("B") }, f.Id("DTA"), false, user));
        });
        yield return ("Quyền đổi khi form mở làm guard mọi bảng từ chối", f =>
        {
            var user = f.User("A");
            f.Execute("UPDATE dbo.NHAN_VIEN SET MaChucVu=N'CV007' WHERE MaNV=@id", new { id = user.MaNV });
            Deny(() => new BUS_QUYENNGHIEPVU(f.Transaction).KiemTraGhiHoSo("CHI_TIET_DON_THUOC", new Dictionary<string, object?>(), f.Id("CTA"), false, user));
        });
        yield return ("Điều dưỡng chỉ sửa kết quả/thời gian dòng được giao", f =>
        {
            var service = f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU ORDER BY MaDV");
            f.Execute("""
                INSERT dbo.DICH_VU_THUC_HIEN(MaDVThucHien,MaLS,MaDV,MaNVThucHien,ThoiGianThucHien,SoLuong,DonGia,KetQua,TrangThai)
                VALUES(@id,@visit,@service,@nurse,SYSDATETIME(),1,100,N'FIXTURE',N'Nháp')
                """, new { id = f.Id("DVN"), visit = f.Id("LSA"), service, nurse = f.Id("N") });
            var user = f.User("N");
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            rights.KiemTraGhiHoSo("DICH_VU_THUC_HIEN", new Dictionary<string, object?> { ["KetQua"] = "FIXTURE MỚI" }, f.Id("DVN"), false, user);
            rights.KiemTraGhiHoSo("DICH_VU_THUC_HIEN", new Dictionary<string, object?>
            {
                ["MaDVThucHien"]=f.Id("DVN"),["MaLS"]=f.Id("LSA"),["MaDV"]=service,["MaNVThucHien"]=f.Id("N"),
                ["MaCTKH"]="",["SoLuong"]="1",["DonGia"]="100",["TrangThai"]="Nháp",["KetQua"]="CHỈ SỬA KẾT QUẢ"
            }, f.Id("DVN"), false, user);
            Deny(() => rights.KiemTraGhiHoSo("DICH_VU_THUC_HIEN", new Dictionary<string, object?> { ["DonGia"] = 1000m }, f.Id("DVN"), false, user));
            Deny(() => rights.KiemTraGhiHoSo("DICH_VU_THUC_HIEN", new Dictionary<string, object?> { ["TrangThai"] = "Đã xác nhận" }, f.Id("DVN"), false, user));
            Deny(() => rights.KiemTraGhiHoSo("DICH_VU_THUC_HIEN", new Dictionary<string, object?>(), f.Id("DVN"), true, user));
            Deny(() => rights.KiemTraGhiHoSo("DICH_VU_THUC_HIEN", new Dictionary<string, object?> { ["MaLS"] = f.Id("LSA") }, null, false, user));
            var data = new BUS_KHAMDIEUTRI(f.Transaction).LayDuLieuHoSo(user).CacBang;
            Require(data["DICH_VU_THUC_HIEN"].Cast<ET_DICHVUTHUCHIEN>().All(v => v.MaNVThucHien == user.MaNV), "Danh sách điều dưỡng chỉ có dòng được giao.");
            f.Execute("UPDATE dbo.DICH_VU_THUC_HIEN SET MaNVThucHien=@doctor WHERE MaDVThucHien=@id", new { doctor = f.Id("B"), id = f.Id("DVN") });
            Deny(() => rights.KiemTraGhiHoSo("DICH_VU_THUC_HIEN", new Dictionary<string, object?> { ["KetQua"] = "Đổi" }, f.Id("DVN"), false, user));
        });
        yield return ("Dòng giao điều dưỡng chỉ kèm định danh lượt, không nội dung khám nháp", f =>
        {
            var service=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU ORDER BY MaDV");
            f.Execute("""
                INSERT dbo.DICH_VU_THUC_HIEN(MaDVThucHien,MaLS,MaDV,MaNVThucHien,ThoiGianThucHien,SoLuong,DonGia,TrangThai)
                VALUES(@id,@visit,@service,@nurse,SYSDATETIME(),1,100,N'Nháp')
                """,new{id=f.Id("DVN"),visit=f.Id("LSA"),service,nurse=f.Id("N")});
            var data=new BUS_KHAMDIEUTRI(f.Transaction).LayDuLieuHoSo(f.User("N")).CacBang;
            var parent=data["LICH_SU_KHAM_BENH"].Cast<ET_LICHSUKHAMBENH>().Single(v=>v.MaLS==f.Id("LSA"));
            Require(parent.TrieuChung=="" && parent.ChanDoan=="" && parent.KeHoachXuLy=="" && parent.GhiChu is null,"Ngữ cảnh dòng được giao không được lộ nội dung khám nháp.");
        });
        yield return ("Thu ngân chỉ nhận nguồn tính phí, không đọc kết quả dịch vụ/ghi chú đơn", f =>
        {
            var service = f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU ORDER BY MaDV");
            f.Execute("""
                INSERT dbo.DICH_VU_THUC_HIEN(MaDVThucHien,MaLS,MaDV,MaNVThucHien,ThoiGianThucHien,SoLuong,DonGia,KetQua,TrangThai)
                VALUES(@id,@visit,@service,@doctor,SYSDATETIME(),1,100,N'FIXTURE NỘI DUNG RIÊNG',N'Đã xác nhận')
                """, new { id=f.Id("DVC"), visit=f.Id("LSH"), service,doctor=f.Id("B") });
            var data = new BUS_KHAMDIEUTRI(f.Transaction).LayDuLieuHoSo(f.User("C")).CacBang;
            Require(data["BENH_NHAN"].Cast<ET_BENHNHAN>().All(b=>b.NhomMau is null && b.TienSuBenh is null && b.DiUng is null),"Dữ liệu thu không chứa nhóm máu/tiền sử/dị ứng.");
            var actual = data["DICH_VU_THUC_HIEN"].Cast<ET_DICHVUTHUCHIEN>().Single(d=>d.MaDVThucHien==f.Id("DVC"));
            Require(actual.KetQua is null && actual.DonGia==100m && actual.SoLuong==1m,"Phải giữ lượng/giá và loại nội dung chuyên môn khỏi dữ liệu thu.");
            var order=data["DON_THUOC"].Cast<ET_DONTHUOC>().Single(d=>d.MaDT==f.Id("DTH"));
            Require(order.GhiChu is null && order.LyDoThayThe is null,"Không đưa ghi chú đơn sang snapshot thu ngân.");
        });
    }
}
