using BUS;
using ET;

namespace RangHamMat.ClinicalChecks;

internal static class QuyenVaTiepNhanChecks
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (TuChoiQuyenNghiepVuException) { return; }
        throw new InvalidOperationException("Thao tác ngoài quyền đã không bị từ chối.");
    }

    private static void RejectInput(Action action)
    {
        try { action(); }
        catch (InvalidOperationException ex) when (ex.GetType() == typeof(InvalidOperationException) && ex.InnerException is null) { return; }
        throw new InvalidOperationException("Đầu vào không hợp lệ đã không bị từ chối.");
    }

    public static IEnumerable<(string Name, Action<Fixture> Run)> Cases()
    {
        yield return ("CV004 được tiếp nhận và thu theo cấu hình", f =>
        {
            var user = f.User("R");
            Require(user.CoQuyen(QuyenLamViec.TiepNhan | QuyenLamViec.ThuTien), "Thiếu quyền quầy tiếp nhận.");
            Require(!user.CoQuyen(QuyenLamViec.Kham), "Tiếp nhận không được ký chuyên môn.");
        });
        yield return ("CV003 không tự được cấp quyền thu", f =>
        {
            var user = f.User("N");
            Require(user.CoQuyen(QuyenLamViec.TiepNhan), "Điều dưỡng có quyền tiếp nhận.");
            Require(!user.CoQuyen(QuyenLamViec.ThuTien), "Điều dưỡng cần cấu hình thu riêng.");
        });
        yield return ("Thu ngân và dược sĩ có quyền riêng", f =>
        {
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            var cashier = f.User("C");
            var pharmacist = f.User("P");
            Require(cashier.CoQuyen(QuyenLamViec.ThuTien) && !cashier.CoQuyen(QuyenLamViec.GiaoThuoc), "Sai quyền thu ngân.");
            Require(pharmacist.CoQuyen(QuyenLamViec.GiaoThuoc) && !pharmacist.CoQuyen(QuyenLamViec.ThuTien), "Sai quyền dược sĩ.");
        });
        yield return ("Admin không có chức vụ bác sĩ không được ký", f =>
        {
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            var admin = f.User("X");
            Require(admin.LaAdmin && !admin.CoQuyen(QuyenLamViec.Kham), "Không được suy quyền bác sĩ từ Admin.");
            Reject(() => rights.KiemTraQuyenGhiLanKham(f.Id("LSA"), admin));
        });
        yield return ("Hàng đợi SQL chỉ chứa phiếu của bác sĩ đăng nhập", f =>
        {
            var user = f.User("A");
            var rows = new BUS_KHAMDIEUTRI(f.Transaction).LayHangDoi(new BoLocHangDoi(), user);
            Require(rows.Count == 1 && rows[0].MaPhieu == f.Id("PHA") && rows[0].MaBS == f.Id("A"), "Hàng đợi lộ phiếu bác sĩ khác.");
        });
        yield return ("Bỏ bộ lọc không mở rộng sang bác sĩ khác", f =>
        {
            var user = f.User("A");
            var rows = new BUS_KHAMDIEUTRI(f.Transaction).LayHangDoi(new BoLocHangDoi { TuNgay = DateTime.Today.AddYears(-10), DenNgay = DateTime.Today.AddYears(1) }, user);
            Require(rows.Count == 1 && rows.All(r => r.MaBS == f.Id("A")), "Không được dùng bộ lọc đổi bác sĩ.");
        });
        yield return ("Từ khóa là tham số SQL, không vượt phạm vi", f =>
        {
            var user = f.User("A");
            var rows = new BUS_KHAMDIEUTRI(f.Transaction).LayHangDoi(new BoLocHangDoi { TuKhoa = "' OR 1=1 --" }, user);
            Require(rows.Count == 0, "Từ khóa SQL không được thay điều kiện quyền.");
        });
        yield return ("Chặn trang vượt 100 dòng", f =>
        {
            var user = f.User("A");
            RejectInput(() => new BUS_KHAMDIEUTRI(f.Transaction).LayHangDoi(new BoLocHangDoi { KichThuocTrang = 101 }, user));
        });
        yield return ("Bác sĩ đọc được nháp của mình", f =>
        {
            var user = f.User("A");
            var detail = new BUS_KHAMDIEUTRI(f.Transaction).LayChiTiet(f.Id("LSA"), user);
            Require(detail.LanKham.MaLS == f.Id("LSA") && detail.MaBN == f.Id("BN1") && detail.DuocSua, "Nháp đúng bác sĩ phải mở được.");
        });
        yield return ("MaLS trực tiếp của bác sĩ khác không lộ nháp dù cùng bệnh nhân", f =>
        {
            var user = f.User("A");
            Reject(() => new BUS_KHAMDIEUTRI(f.Transaction).LayChiTiet(f.Id("LSB"), user));
        });
        yield return ("Bác sĩ đọc lịch sử đã xác nhận của bệnh nhân được phân", f =>
        {
            var user = f.User("A");
            var detail = new BUS_KHAMDIEUTRI(f.Transaction).LayChiTiet(f.Id("LSH"), user);
            Require(detail.MaBN == f.Id("BN1") && !detail.DuocSua, "Lịch sử bác sĩ khác chỉ đọc.");
        });
        yield return ("Bệnh nhân trùng tên không được mở rộng quyền lịch sử", f =>
        {
            var user = f.User("A");
            Reject(() => new BUS_KHAMDIEUTRI(f.Transaction).LayChiTiet(f.Id("LSU"), user));
        });
        yield return ("Kiểm tra quyền ghi từ chối lượt của bác sĩ khác", f =>
        {
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            var user = f.User("A");
            rights.KiemTraQuyenGhiLanKham(f.Id("LSA"), user);
            Reject(() => rights.KiemTraQuyenGhiLanKham(f.Id("LSB"), user));
            Reject(() => rights.KiemTraQuyenGhiLanKham(f.Id("LSH"), user));
        });
        yield return ("Không sửa nội dung lượt đã ký của chính bác sĩ", f =>
        {
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            var user = f.User("B");
            Reject(() => rights.KiemTraQuyenGhiLanKham(f.Id("LSH"), user));
        });
        yield return ("MaDT trực tiếp không được đọc nháp của bác sĩ khác", f =>
        {
            var user = f.User("A");
            var bus = new BUS_DONTHUOC(f.Transaction);
            var own = bus.LayChiTietCoQuyen(f.Id("DTA"), user);
            Require(own.DonThuoc.MaLS == f.Id("LSA") && own.CacDong.Count == 1, "Đơn của mình phải đủ chi tiết.");
            Reject(() => bus.LayChiTietCoQuyen(f.Id("DTB"), user));
        });
        yield return ("Đọc đơn lịch sử đã ký, không đọc đơn BN khác cùng tên", f =>
        {
            var user = f.User("A");
            var bus = new BUS_DONTHUOC(f.Transaction);
            Require(bus.LayChiTietCoQuyen(f.Id("DTH"), user).DonThuoc.MaLS == f.Id("LSH"), "Phải xem được đơn lịch sử phù hợp.");
            Reject(() => bus.LayChiTietCoQuyen(f.Id("DTU"), user));
        });
        yield return ("Khóa tài khoản sau mở form làm mất quyền cả đọc và ghi", f =>
        {
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            var user = f.User("A");
            f.Execute("UPDATE dbo.TAI_KHOAN SET TrangThai=N'Khóa' WHERE MaNV=@id", new { id = user.MaNV });
            Reject(() => new BUS_KHAMDIEUTRI(f.Transaction).LayChiTiet(f.Id("LSA"), user));
            Reject(() => rights.KiemTraQuyenGhiLanKham(f.Id("LSA"), user));
            Reject(() => new BUS_DONTHUOC(f.Transaction).LayChiTietCoQuyen(f.Id("DTA"), user));
        });
        yield return ("Đổi chức vụ sau mở form không giữ quyền ký từ ngữ cảnh cũ", f =>
        {
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            var user = f.User("A");
            f.Execute("UPDATE dbo.NHAN_VIEN SET MaChucVu=N'CV003' WHERE MaNV=@id", new { id = user.MaNV });
            Reject(() => rights.KiemTraQuyenGhiLanKham(f.Id("LSA"), user));
        });
        yield return ("Nhân viên ngừng làm việc không dùng được ngữ cảnh cũ", f =>
        {
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            var user = f.User("A");
            f.Execute("UPDATE dbo.NHAN_VIEN SET TrangThai=N'Đã nghỉ việc' WHERE MaNV=@id", new { id = user.MaNV });
            Reject(() => rights.KiemTraQuyenGhiLanKham(f.Id("LSA"), user));
        });
        yield return ("Cờ quyền tự dựng ở UI không cấp quyền bác sĩ", f =>
        {
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            var fake = f.User("R") with { Quyen = QuyenLamViec.Kham, LaAdmin = true, MaChucVu = "CV002" };
            Reject(() => rights.KiemTraQuyenGhiLanKham(f.Id("LSA"), fake));
        });
        yield return ("Mã không tồn tại bị từ chối, không chọn bản đầu", f =>
        {
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            var user = f.User("A");
            Reject(() => new BUS_KHAMDIEUTRI(f.Transaction).LayChiTiet(f.Id("missing"), user));
            Reject(() => new BUS_DONTHUOC(f.Transaction).LayChiTietCoQuyen(f.Id("missing"), user));
            Reject(() => rights.LayNguCanh(f.Id("missing")));
        });
        yield return ("Admin tra cứu có nhật ký mã nguồn và không chép bệnh án", f =>
        {
            var admin = f.User("X");
            new BUS_KHAMDIEUTRI(f.Transaction).LayChiTiet(f.Id("LSH"), admin);
            var count = f.Scalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaTK=@tk AND MaDoiTuong=@id AND DuLieuTruoc IS NULL AND DuLieuSau IS NULL", new { tk = admin.MaTK, id = f.Id("LSH") });
            Require(count == 1, "Tra cứu Admin phải có audit cùng transaction, không lưu nội dung y tế.");
        });
        yield return ("Không tạo ngữ cảnh B khi phiên xác thực là A", f =>
        {
            f.ActAs("A");
            var rights = new BUS_QUYENNGHIEPVU(f.Transaction);
            f.User("A");
            Reject(() => rights.LayNguCanh(f.Id("B")));
        });
        yield return ("Đổi toàn bộ định danh trong DTO không mạo danh phiên khác", f =>
        {
            var user = f.User("A");
            var fake = user with { MaNV = f.Id("B"), MaTK = f.Id("TKB") };
            Reject(() => new BUS_KHAMDIEUTRI(f.Transaction).LayChiTiet(f.Id("LSB"), fake));
            Reject(() => new DAL.DAL_KHAMDIEUTRI(f.Transaction).LayChiTiet(f.Id("LSB"), fake));
            Reject(() => new BUS_QUYENNGHIEPVU(f.Transaction).KiemTraQuyenGhiLanKham(f.Id("LSB"), fake));
        });
        yield return ("Ngữ cảnh phiên trước mất hiệu lực khi đổi người đăng nhập", f =>
        {
            var old = f.User("A");
            f.ActAs("B");
            Reject(() => new BUS_KHAMDIEUTRI(f.Transaction).LayChiTiet(f.Id("LSA"), old));
        });
        yield return ("Không có phiên ứng dụng đã xác thực thì không đọc hồ sơ", f =>
        {
            var user = f.User("A");
            Thread.CurrentPrincipal = new System.Security.Claims.ClaimsPrincipal();
            Reject(() => new BUS_KHAMDIEUTRI(f.Transaction).LayChiTiet(f.Id("LSA"), user));
            Reject(() => new BUS_QUYENNGHIEPVU(f.Transaction).LayNguCanh(f.Id("A")));
        });
    }
}
