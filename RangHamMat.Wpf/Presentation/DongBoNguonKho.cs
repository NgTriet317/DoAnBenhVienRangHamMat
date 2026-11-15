using BUS;

namespace RangHamMat.Views;

/// <summary>Chuẩn bị nguồn đọc cho các màn hình chứng từ cũ; không truy vấn SQL trong bước điền giao diện.</summary>
internal static class DongBoNguonKho
{
    private static IReadOnlyDictionary<string,object>? current;
    internal static T Read<T>(string key,Func<T> fallback)
        =>current is not null&&current.TryGetValue(key,out var value)?(T)value:fallback();

    internal static Task<Action> Prepare(Action apply,CancellationToken token)
        =>DongBoChuanBi.Doc(ReadAll,data=>
        {
            var previous=current;current=data;
            try{apply();}finally{current=previous;}
        },token);

    private static IReadOnlyDictionary<string,object> ReadAll()=>new Dictionary<string,object>
    {
        ["Kho"]=new BUS_KHO().layToanBoKho(),
        ["NhanVien"]=new BUS_NHANVIEN().DocDanhSachKhongGhi(),
        ["NhaCungCap"]=new BUS_NHACUNGCAP().layToanBoNhaCungCap(),
        ["HangHoa"]=new BUS_HANGHOA().layToanBoHangHoa(),
        ["DanhMuc"]=new BUS_HANGHOA().layDanhMucChiTiet(),
        ["VatTu"]=new BUS_VATTU().layToanBoVatTu(),
        ["Thuoc"]=new BUS_THUOC().layToanBoThuoc(),
        ["Lo"]=new BUS_LOHANGHOA().layToanBoLoHangHoa(),
        ["Khoa"]=new BUS_KHOA().layToanBoKhoa(),
        ["Phong"]=new BUS_PHONG().layToanBoPhong(),
        ["HoaDonNhap"]=new BUS_HOADONNHAP().layToanBoHoaDonNhap(),
        ["PhieuNhap"]=new BUS_PHIEUNHAP().layToanBoPhieuNhap(),
        ["ChiTietNhap"]=new BUS_CHITIETPHIEUNHAP().layToanBoChiTietPhieuNhap(),
        ["PhieuXuat"]=new BUS_PHIEUXUAT().LayDanhSach(),
        ["ChiTietXuat"]=new BUS_CHITIETPHIEUXUAT().LayDanhSach()
    };
}
