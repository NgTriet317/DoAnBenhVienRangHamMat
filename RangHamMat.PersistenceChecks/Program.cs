// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.ComponentModel;
using BUS;
using ET;
using RangHamMat.Controls;
using System.Data;

try
{
    var entityTables=new[]{"BENHNHAN","HOSOBENHAN","PHIEUKHAMBENH","LICHSUKHAMBENH","TINHTRANGBENH","DICHVU","KEHOACHDIEUTRI","CHITIETKEHOACH","DICHVUTHUCHIEN","DONTHUOC","CHITIETDONTHUOC","LICHKHAM","PHONG","CALAM","THUOC","HANGHOA","KHO","LOHANGHOA","HOADON","CHITIETHOADON","THANHTOAN","PHIEUXUAT","CHITIETPHIEUXUAT","NHANVIEN"};
    foreach(var name in entityTables)
    {
        if(typeof(BUS_BENHNHAN).Assembly.GetType("BUS.BUS_"+name) is null || typeof(DAL.DAL_HOADON).Assembly.GetType("DAL.DAL_"+name) is null)
            throw new InvalidOperationException("Thiếu DAL/BUS riêng cho ET_"+name);
    }
    using var bus=new PersistenceFixture();
    var snapshot=bus.Load();
    // Đọc trường của bản ghi ET/dictionary để đối chiếu trong ca kiểm tra persistence.
    object? Value(object row,string field) => row is IDictionary<string,object?> dict ? (dict.TryGetValue(field,out var value)?value:null) : TypeDescriptor.GetProperties(row)[field]?.GetValue(row);
    // Lấy mã của dòng danh mục đầu tiên trong snapshot SQL dùng làm nguồn cho ca thử.
    string FirstId(string table,string key) => Convert.ToString(Value(snapshot[table].First(),key))!;
    var doctor=FirstId("NHAN_VIEN","MaNV"); var room=FirstId("PHONG","MaPhong"); var service=FirstId("DICH_VU","MaDV"); var medicine=FirstId("THUOC","MaThuoc");
    // Lấy mã khóa từ dictionary của bản ghi thử đã lưu.
    string Id(Dictionary<string,object?> row,string key)=>Convert.ToString(row[key])!;
    // Gộp các cặp tên cột/giá trị thành dictionary rồi gọi luồng lưu dùng chung của ca thử; nằm trong transaction sẽ rollback.
    Dictionary<string,object?> Save(string table,params (string,object?)[] entries)=>bus.Save(table,entries.ToDictionary(e=>e.Item1,e=>e.Item2));
    // Ném lỗi khi điều kiện kiểm tra không đạt để chương trình trả FAIL.
    void Require(bool condition,string message) {if(!condition)throw new InvalidOperationException(message);}
    // Yêu cầu thao tác phải bị chặn bằng InvalidOperationException; nếu thao tác thành công thì ca thử thất bại.
    void Reject(Action action,string message) {try {action();}catch(InvalidOperationException){return;}throw new InvalidOperationException(message);}
    var cases=0;
    // Mở giao dịch cho từng ca thử, chạy kiểm tra và ghi PASS; luôn rollback trong finally để không giữ bản ghi thử.
    void Check(string name,Action run)
    {
        bus.Begin();
        try {run();Console.WriteLine("PASS: "+name);cases++;}
        finally {bus.Rollback();}
    }
    // Tạo bệnh nhân thử với SĐT/BHYT/dấu nháy và các trường nullable để kiểm tra lưu đúng kiểu SQL.
    Dictionary<string,object?> Patient()=>Save("BENH_NHAN",("HoTenBN","Kiểm tra rollback O'Brien"),("SDT","0900123456"),("MaBHYT","BHYT-001"),("TrangThai","Đang theo dõi"));
    // Tạo chuỗi bệnh nhân → bệnh án → tiếp nhận → lần khám thử, giữ đúng mã cha và thứ tự thời gian.
    Dictionary<string,object?> Visit()
    {
        var patient=Patient();
        var history=Save("HO_SO_BENH_AN",("MaBN",patient["MaBN"]),("NgayLap",DateTime.Now.AddMinutes(-3)),("LyDoKhamBanDau","Kiểm tra"),("TrangThai","Đang theo dõi"));
        var receipt=Save("PHIEU_KHAM_BENH",("MaHS",history["MaHS"]),("MaPhong",room),("MaBS",doctor),("MaNVTiepNhan",doctor),("SoThuTu",1),("ThoiGianTiepNhan",DateTime.Now.AddMinutes(-2)),("LyDoKham","Kiểm tra"),("TrangThai","Chờ khám"));
        return Save("LICH_SU_KHAM_BENH",("MaPhieu",receipt["MaPhieu"]),("MaBS",doctor),("MaPhong",room),("ThoiGianKham",DateTime.Now.AddMinutes(-1)),("TrieuChung",""),("ChanDoan","Chẩn đoán kiểm tra"),("KeHoachXuLy",""),("TrangThai","Nháp"));
    }
    Check("lưu bệnh nhân vào SQL giữ số 0 đầu, BHYT, dấu nháy và nullable",()=>
    {
        var row=Patient();var id=Id(row,"MaBN");
        var loaded=bus.Load()["BENH_NHAN"].Single(p=>Convert.ToString(Value(p,"MaBN"))==id);
        Require(Convert.ToString(Value(loaded,"SDT"))=="0900123456","SDT phải là chuỗi.");
        Require(Convert.ToString(Value(loaded,"HoTenBN"))=="Kiểm tra rollback O'Brien","SQL parameters phải giữ dấu nháy.");
        Require(Value(loaded,"NgaySinh") is null && Convert.ToString(Value(loaded,"MaBHYT"))=="BHYT-001","Ngày trống/BHYT không khớp.");
    });
    Check("cột computed và tổng kế hoạch được tính ở database",()=>
    {
        var visit=Visit();
        var plan=Save("KE_HOACH_DIEU_TRI",("MaLS",visit["MaLS"]),("MaBS",doctor),("NgayLap",DateTime.Now),("TrangThai","Nháp"));
        var line=Save("CHI_TIET_KE_HOACH",("MaKHDT",plan["MaKHDT"]),("MaDV",service),("LanThu",1),("SoLuongDuKien",2.5m),("DonGiaDuKien",10000m),("TrangThai","Nháp"));
        Require(Convert.ToDecimal(line["ThanhTienDuKien"])==25000m,"Thành tiền sai.");
        var actual=bus.Load()["KE_HOACH_DIEU_TRI"].Single(p=>Convert.ToString(Value(p,"MaKHDT"))==Id(plan,"MaKHDT"));
        Require(Convert.ToDecimal(Value(actual,"TongTienDuKien"))==25000m,"Trigger tổng không chạy.");
    });
    Check("xác nhận lần khám cập nhật trạng thái và ngày cùng một lệnh",()=>
    {
        var visit=Visit();
        var confirmed=bus.Save("LICH_SU_KHAM_BENH",new Dictionary<string,object?>{{"TrangThai","Đã xác nhận"}},Id(visit,"MaLS"),visit,true);
        Require(confirmed["NgayXacNhan"] is DateTime && Convert.ToString(confirmed["TrangThai"])=="Đã xác nhận","Xác nhận chưa lưu ngày.");
        Reject(()=>bus.Save("LICH_SU_KHAM_BENH",new Dictionary<string,object?>{{"ChanDoan","Đổi"}},Id(visit,"MaLS")),"Không được sửa bản xác nhận.");
    });
    Check("đơn nhiều dòng lưu liên kết và khóa nội dung sau xác nhận",()=>
    {
        var visit=Visit();var order=Save("DON_THUOC",("MaLS",visit["MaLS"]),("MaBS",doctor),("NgayKe",DateTime.Now),("TrangThai","Nháp"));
        var line=Save("CHI_TIET_DON_THUOC",("MaDT",order["MaDT"]),("MaThuoc",medicine),("LieuDung","Theo chỉ định"),("SoLuong",2m),("SoNgayDung",1),("HuongDan","Kiểm tra"));
        bus.Save("DON_THUOC",new Dictionary<string,object?>{{"TrangThai","Đã xác nhận"}},Id(order,"MaDT"),order,true);
        Reject(()=>bus.Save("CHI_TIET_DON_THUOC",new Dictionary<string,object?>{{"SoLuong",3m}},Id(line,"MaCTDT")),"Không sửa thuốc trong đơn xác nhận.");
    });
    Check("phát hiện bản ghi cũ khi cửa sổ khác đã sửa",()=>
    {
        var patient=Patient();
        bus.Save("BENH_NHAN",new Dictionary<string,object?>{{"DiaChi","Địa chỉ mới"}},Id(patient,"MaBN"));
        Reject(()=>bus.Save("BENH_NHAN",new Dictionary<string,object?>{{"DiaChi","Địa chỉ cũ"}},Id(patient,"MaBN"),patient),"Phải chặn ghi đè thay đổi mới.");
    });
    Check("lỗi dòng thứ hai không để lại đơn hoặc thuốc khi rollback",()=>
    {
        var visit=Visit();var order=Save("DON_THUOC",("MaLS",visit["MaLS"]),("MaBS",doctor),("NgayKe",DateTime.Now),("TrangThai","Nháp"));
        Save("CHI_TIET_DON_THUOC",("MaDT",order["MaDT"]),("MaThuoc",medicine),("LieuDung","Theo chỉ định"),("SoLuong",2m),("SoNgayDung",1),("HuongDan","Kiểm tra"));
        Reject(()=>Save("CHI_TIET_DON_THUOC",("MaDT",order["MaDT"]),("MaThuoc",medicine),("LieuDung","Theo chỉ định"),("SoLuong",0m),("SoNgayDung",1),("HuongDan","Kiểm tra")),"Dòng thuốc sai phải bị database chặn.");
    });
    Check("chặn bảng/trường ngoài phạm vi và không tự làm tròn số lượng",()=>
    {
        Reject(()=>Save("BENH_NHAN;DROP TABLE BENH_NHAN",("HoTenBN","a")),"Phải whitelist bảng.");
        Reject(()=>Save("BENH_NHAN",("NotAColumn",1)),"Phải kiểm tra trường.");
        var visit=Visit();
        Reject(()=>Save("DICH_VU_THUC_HIEN",("MaLS",visit["MaLS"]),("MaDV",service),("MaNVThucHien",doctor),("ThoiGianThucHien",DateTime.Now),("SoLuong",1.1234m),("DonGia",100m),("TrangThai","Nháp")),"Không tự giảm chính xác số lượng.");
    });
    Check("số thập phân tiếng Việt không bị hiểu thành phân cách hàng nghìn",()=>
    {
        var visit=Visit();
        var row=Save("DICH_VU_THUC_HIEN",("MaLS",visit["MaLS"]),("MaDV",service),("MaNVThucHien",doctor),("ThoiGianThucHien",DateTime.Now),("SoLuong","2,5"),("DonGia","10000,50"),("TrangThai","Nháp"));
        Require(Convert.ToDecimal(row["SoLuong"])==2.5m && Convert.ToDecimal(row["ThanhTien"])==25001.25m,"Dấu phẩy phải là phần thập phân, không được tăng số lượng/tiền gấp 10 hoặc 100.");
    });
    Check("xóa với phiên bản cũ không được mất thay đổi mới",()=>
    {
        var patient=Patient();
        bus.Save("BENH_NHAN",new Dictionary<string,object?>{{"DiaChi","Địa chỉ mới"}},Id(patient,"MaBN"));
        Reject(()=>bus.Delete("BENH_NHAN",Id(patient,"MaBN"),patient),"Xóa stale phải bị chặn.");
    });
    Check("không sửa nội dung kế hoạch xác nhận qua cờ đổi trạng thái",()=>
    {
        var visit=Visit();var plan=Save("KE_HOACH_DIEU_TRI",("MaLS",visit["MaLS"]),("MaBS",doctor),("NgayLap",DateTime.Now),("TrangThai","Nháp"));
        var confirmed=bus.Save("KE_HOACH_DIEU_TRI",new Dictionary<string,object?>{{"TrangThai","Đã xác nhận"}},Id(plan,"MaKHDT"),plan,true);
        Reject(()=>bus.Save("KE_HOACH_DIEU_TRI",new Dictionary<string,object?>{{"GhiChu","Không được sửa"}},Id(plan,"MaKHDT"),confirmed,true),"Cờ đổi trạng thái không được mở khóa nội dung đã xác nhận.");
    });
    Check("CRUD typed không báo stale do SQL làm tròn thời gian",()=>
    {
        using var connection=new DAL.DAL_BENHNHAN().MoKetNoi(); using var transaction=connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var patients=new BUS_BENHNHAN(transaction);
            var patient=new ET_BENHNHAN { HoTenBN="Typed rollback",NgaySinh=new DateTime(2000,1,2,18,30,15),TrangThai="Đang theo dõi" };
            Require(patients.themBenhNhan(patient),"Typed thêm bệnh nhân.");
            var original=UiRecord.ChuyenDoi<ET_BENHNHAN>(DAL.dbconnect.GiaTri(patient));
            patient.DiaChi="Sau lần thêm";
            Require(patients.suaBenhNhan(patient,original),"DATE phải được chuẩn hóa trước so sánh phiên bản.");
            var histories=new BUS_HOSOBENHAN(transaction);
            var history=new ET_HOSOBENHAN {MaBN=patient.MaBN,NgayLap=DateTime.Today.AddHours(10).AddTicks(7654321),LyDoKhamBanDau="Kiểm tra",TrangThai="Đang theo dõi"};
            Require(histories.themHoSoBenhAn(history),"Typed thêm bệnh án.");
            var before=UiRecord.ChuyenDoi<ET_HOSOBENHAN>(DAL.dbconnect.GiaTri(history));
            history.GhiChu="Sau lần thêm";
            Require(histories.suaHoSoBenhAn(history,before),"DATETIME2(0) phải được chuẩn hóa trước so sánh phiên bản.");
            Require(histories.xoaHoSoBenhAn(history.MaHS,history),"Typed xóa bản vừa sửa.");
            Require(patients.xoaBenhNhan(patient.MaBN,patient),"Typed xóa bệnh nhân vừa sửa.");
        }
        finally {transaction.Rollback();}
    });
    Check("mã chữ đầu bảng với hậu tố tối thiểu ba chữ số",()=>
    {
        var format=typeof(DAL.dbconnect).GetMethod("DinhDangMa")??throw new InvalidOperationException("Thiếu quy tắc định dạng mã tự động.");
        foreach(var item in new[]{("BENH_NHAN",1L,"BN001"),("BENH_NHAN",9L,"BN009"),("BENH_NHAN",10L,"BN010"),("BENH_NHAN",100L,"BN100"),("HO_SO_BENH_AN",2L,"HSBA002"),("LICH_SU_KHAM_BENH",3L,"LSKB003"),("CHI_TIET_DON_THUOC",4L,"CTDT004")})
            Require(Convert.ToString(format.Invoke(null,new object[]{item.Item1,item.Item2}))==item.Item3,"Mã không đúng quy tắc: "+item.Item3);
        var first=Patient();var second=Patient();
        Require(Id(first,"MaBN").StartsWith("BN")&&Id(first,"MaBN")!=Id(second,"MaBN"),"Mã cấp thực tế phải duy nhất và được lưu SQL.");
    });
    Check("DAL phiếu khám tự chặn tiếp nhận lịch đã hủy",()=>
    {
        using var connection = new DAL.DAL_PHIEUKHAMBENH().MoKetNoi();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var patients = new BUS_BENHNHAN(transaction);
            var patient = new ET_BENHNHAN { HoTenBN = "Kiểm tra lịch hủy", TrangThai = "Đang theo dõi" };
            patients.themBenhNhan(patient);
            var history = new ET_HOSOBENHAN { MaBN = patient.MaBN, NgayLap = DateTime.Now, LyDoKhamBanDau = "Kiểm tra", TrangThai = "Đang theo dõi" };
            new BUS_HOSOBENHAN(transaction).themHoSoBenhAn(history);
            var appointment = new ET_LICHKHAM { MaBN = patient.MaBN, MaBS = doctor, MaPhong = room,
                MaCa = FirstId("CA_LAM", "MaCa"), NgayGioKham = DateTime.Today.AddDays(1).AddHours(9),
                ThoiLuongDuKien = 30, UuTien = "Bình thường", LyDoKham = "Kiểm tra", TrangThai = "Hủy",
                NgayTao = DateTime.Now, NgayCapNhat = DateTime.Now };
            new BUS_LICHKHAM(transaction).themLichKham(appointment);
            Reject(() => new BUS_PHIEUKHAMBENH(transaction).themPhieuKhamBenh(new ET_PHIEUKHAMBENH {
                MaHS = history.MaHS, MaLich = appointment.MaLich, MaPhong = room, MaBS = doctor,
                MaNVTiepNhan = doctor, SoThuTu = 1, ThoiGianTiepNhan = DateTime.Now,
                LyDoKham = "Kiểm tra", TrangThai = "Chờ khám" }), "DAL phải chặn lịch hủy khi gọi BUS trực tiếp.");
        }
        finally { transaction.Rollback(); }
    });
    Check("DAL/BUS danh mục bổ sung có CRUD typed và chống phiên bản cũ",()=>
    {
        using var connection = new DAL.DAL_VAITRO().MoKetNoi();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var roles = new BUS_VAITRO(transaction);
            var role = new ET_VAITRO { TenVaiTro = "Vai trò kiểm tra", MoTa = "Rollback" };
            Require(roles.themVaiTro(role), "Thêm vai trò typed.");
            var original = roles.layVaiTroTheoMa(role.MaVaiTro)!;
            Require(original.TenVaiTro == role.TenVaiTro && role.MaVaiTro.StartsWith("VT"), "Ánh xạ VAI_TRO và mã tự cấp.");
            role.MoTa = "Đã sửa";
            Require(roles.suaVaiTro(role, original), "Sửa vai trò typed.");
            Reject(() => roles.xoaVaiTro(role.MaVaiTro, original), "Phải chặn xóa vai trò với phiên bản cũ.");
            Require(roles.xoaVaiTro(role.MaVaiTro, role), "Xóa vai trò typed.");
            var providers = new BUS_NHACUNGCAP(transaction);
            var provider = new ET_NHACUNGCAP { TenNCC = "NCC kiểm tra", Email = "check@example.test", NguoiLienHe = "Kiểm tra", TrangThai = "Đang hoạt động" };
            Require(providers.themNhaCungCap(provider), "Thêm nhà cung cấp nullable.");
            Require(providers.layNhaCungCapTheoMa(provider.MaNCC)?.SDT is null, "Giữ số điện thoại nullable.");
            Require(providers.xoaNhaCungCap(provider.MaNCC, provider), "Xóa nhà cung cấp typed.");
        }
        finally { transaction.Rollback(); }
    });
    Check("39 ET nền đọc SQL qua đúng BUS; 6 entity lâm sàng dùng suite quyền chuyên dụng",()=>
    {
        using var connection = new DAL.DAL_BENHNHAN().MoKetNoi();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            foreach (var type in typeof(ET_BENHNHAN).Assembly.GetTypes().Where(type => System.Reflection.CustomAttributeExtensions.GetCustomAttribute<Dapper.Contrib.Extensions.TableAttribute>(type, false) is { } map && !new[]{"DM_TINH_TRANG_BENH","MAU_KE_HOACH_DIEU_TRI","CHI_TIET_MAU_KE_HOACH","NHOM_THUOC_KE_DON","CAU_HINH_THUOC_KE_DON","BAN_GIAO_THUOC"}.Contains(map.Name)))
            {
                var busType = typeof(BUS_BENHNHAN).Assembly.GetType("BUS.BUS_" + type.Name[3..])!;
                var entityBus = Activator.CreateInstance(busType, new object?[] { transaction })!;
                Console.WriteLine("P12 persistence entity: "+type.Name);
                var read = busType.GetMethods().First(method => method.GetParameters().Length == 0 &&
                    typeof(System.Collections.IEnumerable).IsAssignableFrom(method.ReturnType) && typeof(IEnumerable<>).MakeGenericType(type).IsAssignableFrom(method.ReturnType));
                var rows = (System.Collections.IEnumerable)read.Invoke(entityBus, null)!;
                foreach (var row in rows) Require(row.GetType() == type, busType.Name + " phải trả về đúng ET.");
            }
        }
        finally { transaction.Rollback(); }
    });
    Check("phiếu nhập giữ tổng do trigger tính khi thêm và sửa typed",()=>
    {
        using var connection = new DAL.DAL_PHIEUNHAP().MoKetNoi();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var provider = new BUS_NHACUNGCAP(transaction).layToanBoNhaCungCap().First();
            var invoice = new ET_HOADONNHAP { SoHoaDonNCC = "Kiểm tra tổng rollback", MaNCC = provider.MaNCC, MaNV = doctor,
                NgayHoaDon = DateTime.Now, TongTien = 1000, TrangThai = "Nháp" };
            new BUS_HOADONNHAP(transaction).themHoaDonNhap(invoice);
            var receipts = new BUS_PHIEUNHAP(transaction);
            var receipt = new ET_PHIEUNHAP { MaHDN = invoice.MaHDN, MaKho = FirstId("KHO", "MaKho"), MaNV = doctor,
                NgayNhap = DateTime.Now, TongTien = 999, TrangThai = "Nháp", LyDo = "Kiểm tra" };
            receipts.themPhieuNhap(receipt);
            Require(receipts.layPhieuNhapTheoMa(receipt.MaPN)!.TongTien == 0, "Phiếu mới phải dùng tổng mặc định SQL.");
            new BUS_CHITIETPHIEUNHAP(transaction).themChiTietPhieuNhap(new ET_CHITIETPHIEUNHAP {
                MaPN = receipt.MaPN, MaHH = FirstId("HANG_HOA", "MaHH"), SoLuong = 2.5m, DonGia = 100 });
            var actual = receipts.layPhieuNhapTheoMa(receipt.MaPN)!;
            Require(actual.TongTien == 250, "Trigger phải tính tổng phiếu nhập từ chi tiết.");
            actual.TongTien = 999; actual.GhiChu = "Sửa ghi chú";
            Require(receipts.suaPhieuNhap(actual), "Sửa phiếu nhập typed.");
            Require(receipts.layPhieuNhapTheoMa(receipt.MaPN)!.TongTien == 250, "Sửa không được ghi đè tổng SQL.");
        }
        finally { transaction.Rollback(); }
    });
    Require(bus.Load()["BENH_NHAN"].Length==snapshot["BENH_NHAN"].Length,"Rollback phải giữ nguyên số bệnh nhân.");
    Require(bus.Load()["DON_THUOC"].Length==snapshot["DON_THUOC"].Length && bus.Load()["CHI_TIET_DON_THUOC"].Length==snapshot["CHI_TIET_DON_THUOC"].Length,"Rollback phải giữ nguyên đơn/dòng thuốc.");
    Console.WriteLine($"SUCCESS: {cases} kiểm tra nghiệp vụ SQL; mọi dữ liệu kiểm tra đã rollback.");return 0;
}
catch(Exception ex) {Console.Error.WriteLine("FAIL: "+ex);return 1;}
