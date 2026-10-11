using System.ComponentModel;
using BUS;
using ET;

try
{
    var entityTables=new[]{"BENHNHAN","HOSOBENHAN","PHIEUKHAMBENH","LICHSUKHAMBENH","TINHTRANGBENH","DICHVU","KEHOACHDIEUTRI","CHITIETKEHOACH","DICHVUTHUCHIEN","DONTHUOC","CHITIETDONTHUOC","LICHKHAM","PHONG","CALAM","THUOC","HANGHOA","KHO","LOHANGHOA","HOADON","CHITIETHOADON","THANHTOAN","PHIEUXUAT","CHITIETPHIEUXUAT","NHANVIEN"};
    foreach(var name in entityTables)
    {
        if(typeof(BUS_LAMSANG).Assembly.GetType("BUS.BUS_"+name) is null || typeof(DAL.DAL_HOADON).Assembly.GetType("DAL.DAL_"+name) is null)
            throw new InvalidOperationException("Thiếu DAL/BUS riêng cho ET_"+name);
    }
    using var bus=new BUS_LAMSANG();
    var snapshot=bus.Load();
    object? Value(object row,string field) => row is IDictionary<string,object?> dict ? (dict.TryGetValue(field,out var value)?value:null) : TypeDescriptor.GetProperties(row)[field]?.GetValue(row);
    string FirstId(string table,string key) => Convert.ToString(Value(snapshot[table].First(),key))!;
    var doctor=FirstId("NHAN_VIEN","MaNV"); var room=FirstId("PHONG","MaPhong"); var service=FirstId("DICH_VU","MaDV"); var medicine=FirstId("THUOC","MaThuoc");
    string Id(Dictionary<string,object?> row,string key)=>Convert.ToString(row[key])!;
    Dictionary<string,object?> Save(string table,params (string,object?)[] entries)=>bus.Save(table,entries.ToDictionary(e=>e.Item1,e=>e.Item2));
    void Require(bool condition,string message) {if(!condition)throw new InvalidOperationException(message);}
    void Reject(Action action,string message) {try {action();}catch(InvalidOperationException){return;}throw new InvalidOperationException(message);}
    var cases=0;
    void Check(string name,Action run)
    {
        bus.Begin();
        try {run();Console.WriteLine("PASS: "+name);cases++;}
        finally {bus.Rollback();}
    }
    Dictionary<string,object?> Patient()=>Save("BENH_NHAN",("HoTenBN","Kiểm tra rollback O'Brien"),("SDT","0900123456"),("MaBHYT","BHYT-001"),("TrangThai","Đang theo dõi"));
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
        using var transaction=new DAL.DAL_GIAODICH();transaction.Begin();
        try
        {
            var patients=new BUS_BENHNHAN(transaction);
            var patient=new ET_BENHNHAN { HoTenBN="Typed rollback",NgaySinh=new DateTime(2000,1,2,18,30,15),TrangThai="Đang theo dõi" };
            Require(patients.themBenhNhan(patient),"Typed thêm bệnh nhân.");
            var original=transaction.ChuyenDoi<ET_BENHNHAN>(DAL.DAL_GIAODICH.GiaTri(patient));
            patient.DiaChi="Sau lần thêm";
            Require(patients.suaBenhNhan(patient,original),"DATE phải được chuẩn hóa trước so sánh phiên bản.");
            var histories=new BUS_HOSOBENHAN(transaction);
            var history=new ET_HOSOBENHAN {MaBN=patient.MaBN,NgayLap=DateTime.Today.AddHours(10).AddTicks(7654321),LyDoKhamBanDau="Kiểm tra",TrangThai="Đang theo dõi"};
            Require(histories.themHoSoBenhAn(history),"Typed thêm bệnh án.");
            var before=transaction.ChuyenDoi<ET_HOSOBENHAN>(DAL.DAL_GIAODICH.GiaTri(history));
            history.GhiChu="Sau lần thêm";
            Require(histories.suaHoSoBenhAn(history,before),"DATETIME2(0) phải được chuẩn hóa trước so sánh phiên bản.");
            Require(histories.xoaHoSoBenhAn(history.MaHS,history),"Typed xóa bản vừa sửa.");
            Require(patients.xoaBenhNhan(patient.MaBN,patient),"Typed xóa bệnh nhân vừa sửa.");
        }
        finally {transaction.Rollback();}
    });
    Check("mã chữ đầu bảng với hậu tố tối thiểu hai chữ số",()=>
    {
        var format=typeof(DAL.DAL_GIAODICH).GetMethod("DinhDangMa")??throw new InvalidOperationException("Thiếu quy tắc định dạng mã tự động.");
        foreach(var item in new[]{("BENH_NHAN",1L,"BN01"),("BENH_NHAN",9L,"BN09"),("BENH_NHAN",10L,"BN10"),("BENH_NHAN",100L,"BN100"),("HO_SO_BENH_AN",2L,"HSBA02"),("LICH_SU_KHAM_BENH",3L,"LSKB03"),("CHI_TIET_DON_THUOC",4L,"CTDT04")})
            Require(Convert.ToString(format.Invoke(null,new object[]{item.Item1,item.Item2}))==item.Item3,"Mã không đúng quy tắc: "+item.Item3);
        var first=Patient();var second=Patient();
        Require(Id(first,"MaBN").StartsWith("BN")&&Id(first,"MaBN")!=Id(second,"MaBN"),"Mã cấp thực tế phải duy nhất và được lưu SQL.");
    });
    Require(bus.Load()["BENH_NHAN"].Length==snapshot["BENH_NHAN"].Length,"Rollback phải giữ nguyên số bệnh nhân.");
    Require(bus.Load()["DON_THUOC"].Length==snapshot["DON_THUOC"].Length && bus.Load()["CHI_TIET_DON_THUOC"].Length==snapshot["CHI_TIET_DON_THUOC"].Length,"Rollback phải giữ nguyên đơn/dòng thuốc.");
    Console.WriteLine($"SUCCESS: {cases} kiểm tra nghiệp vụ SQL; mọi dữ liệu kiểm tra đã rollback.");return 0;
}
catch(Exception ex) {Console.Error.WriteLine("FAIL: "+ex);return 1;}
