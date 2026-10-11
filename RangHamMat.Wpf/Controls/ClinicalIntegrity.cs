namespace RangHamMat.Controls;
// All entry points, including inline edits, share these identity/link checks.
public static class ClinicalIntegrity
{
    public static string OwnerField(string table) => table switch { "HO_SO_BENH_AN"=>"MaBN", "PHIEU_KHAM_BENH"=>"MaHS", "LICH_SU_KHAM_BENH"=>"MaPhieu", "TINH_TRANG_BENH" or "KE_HOACH_DIEU_TRI" or "DON_THUOC" or "DICH_VU_THUC_HIEN"=>"MaLS", "CHI_TIET_KE_HOACH"=>"MaKHDT", "CHI_TIET_DON_THUOC"=>"MaDT", _=>"" };
    public static void Validate(string table, IDictionary<string,object?> values, string existingId = "")
    {
        if(!ClinicalSchema.Tables.TryGetValue(table,out var fields)) throw new InvalidOperationException("Bảng dữ liệu không thuộc luồng hồ sơ.");
        foreach(var field in values.Keys) if(!fields.ContainsKey(field)) throw new InvalidOperationException($"Trường {field} không có trong database của {table}.");
        string Text(string field) => values.TryGetValue(field,out var value) ? Convert.ToString(value)?.Trim() ?? "" : "";
        foreach(var pair in values)
            if(pair.Key!=ClinicalData.Key(table) && ClinicalData.ForeignTables.TryGetValue(pair.Key,out var foreign) && !string.IsNullOrWhiteSpace(Convert.ToString(pair.Value)) && ClinicalData.Find(foreign,pair.Value) is null) throw new InvalidOperationException("Không tìm thấy " + ClinicalSchema.Label(table,pair.Key) + ". Hãy chọn lại từ danh sách.");
        var required=table switch {
            "BENH_NHAN"=>new[]{"HoTenBN","TrangThai"}, "HO_SO_BENH_AN"=>new[]{"MaBN","NgayLap"},
            "PHIEU_KHAM_BENH"=>new[]{"MaHS","MaPhong","MaNVTiepNhan","ThoiGianTiepNhan","LyDoKham"},
            "LICH_KHAM"=>new[]{"MaBN","MaBS","MaPhong","MaCa","NgayGioKham"},
            "LICH_SU_KHAM_BENH"=>new[]{"MaPhieu","MaBS","MaPhong","ThoiGianKham"},
            "TINH_TRANG_BENH"=>new[]{"MaLS","LoaiTinhTrang","MoTaTinhTrang"},
            "KE_HOACH_DIEU_TRI"=>new[]{"MaLS","MaBS","NgayLap"}, "CHI_TIET_KE_HOACH"=>new[]{"MaKHDT","MaDV"},
            "DICH_VU_THUC_HIEN"=>new[]{"MaLS","MaDV","MaNVThucHien","ThoiGianThucHien"},
            "DON_THUOC"=>new[]{"MaLS","MaBS","NgayKe"}, "CHI_TIET_DON_THUOC"=>new[]{"MaDT","MaThuoc","LieuDung","HuongDan"}, _=>Array.Empty<string>() };
        foreach(var field in required) if(Text(field).Length==0) throw new InvalidOperationException("Vui lòng nhập/chọn " + ClinicalSchema.Label(table,field) + ".");
        if(existingId.Length>0 && ClinicalData.Find(table,existingId) is object original)
        {
            // A saved clinical record keeps its owner; editing content must not move an entire history.
            var ownerField=OwnerField(table);
            if(ownerField.Length>0 && Text(ownerField)!=UiRecord.Text(original,ownerField)) throw new InvalidOperationException("Bản ghi đã lưu phải giữ liên kết hồ sơ/lần khám ban đầu. Hãy lập bản mới cho hồ sơ khác.");
            if(table=="CHI_TIET_KE_HOACH" && ClinicalData.Children("DICH_VU_THUC_HIEN","MaCTKH",existingId).Any(row=>UiRecord.Text(row,"MaDV")!=Text("MaDV"))) throw new InvalidOperationException("Bước kế hoạch đã có dịch vụ thực hiện. Không thể đổi sang dịch vụ khác.");
        }
        if(table=="HO_SO_BENH_AN" && ClinicalData.Children(table,"MaBN",Text("MaBN")).Any(row=>UiRecord.Text(row,"MaHS")!=existingId)) throw new InvalidOperationException("Bệnh nhân đã có hồ sơ bệnh án. Mở hồ sơ hiện có để xem.");
        if(table=="LICH_SU_KHAM_BENH" && ClinicalData.Children(table,"MaPhieu",Text("MaPhieu")).Any(row=>UiRecord.Text(row,"MaLS")!=existingId)) throw new InvalidOperationException("Lượt tiếp nhận đã có lần khám. Hãy tạo lượt tiếp nhận mới.");
        if(table=="PHIEU_KHAM_BENH")
        {
            if(!int.TryParse(Text("SoThuTu"),out var order)||order<=0) throw new InvalidOperationException("Số thứ tự phải là số nguyên lớn hơn 0.");
            if(Text("MaLich").Length>0)
            {
                var appointment=ClinicalData.Find("LICH_KHAM",Text("MaLich"))!;
                var medical=ClinicalData.Find("HO_SO_BENH_AN",Text("MaHS"))!;
                if(UiRecord.Text(appointment,"MaBN")!=UiRecord.Text(medical,"MaBN")) throw new InvalidOperationException("Lịch hẹn không thuộc bệnh nhân của hồ sơ này.");
                if(UiRecord.Text(appointment,"TrangThai") is "Hủy" or "Đã đổi") throw new InvalidOperationException("Lịch hẹn đã hủy hoặc đã đổi. Chọn lịch hẹn còn hiệu lực.");
                if(ClinicalData.Children(table,"MaLich",Text("MaLich")).Any(row=>UiRecord.Text(row,"MaPhieu")!=existingId && UiRecord.Text(row,"TrangThai")!="Hủy")) throw new InvalidOperationException("Lịch hẹn đã được tiếp nhận. Hãy mở lượt tiếp nhận hiện có.");
            }
        }
        if(table=="DICH_VU_THUC_HIEN" && Text("MaCTKH").Length>0)
        {
            var step=ClinicalData.Find("CHI_TIET_KE_HOACH",Text("MaCTKH"))!;
            var visit=ClinicalData.Find("LICH_SU_KHAM_BENH",Text("MaLS"))!;
            var plan=ClinicalData.Find("KE_HOACH_DIEU_TRI",UiRecord.Get(step,"MaKHDT"));
            if(UiRecord.Text(step,"TrangThai")=="Hủy" || plan is null || UiRecord.Text(plan,"TrangThai")=="Hủy") throw new InvalidOperationException("Kế hoạch hoặc bước điều trị đã hủy, không thể ghi nhận thực hiện.");
            if(ClinicalData.PatientId("CHI_TIET_KE_HOACH",step)!=ClinicalData.PatientId("LICH_SU_KHAM_BENH",visit)) throw new InvalidOperationException("Bước kế hoạch thuộc bệnh nhân khác.");
            if(UiRecord.Text(step,"MaDV")!=Text("MaDV")) throw new InvalidOperationException("Dịch vụ thực hiện phải khớp bước kế hoạch đã chọn.");
        }
        if(table=="CHI_TIET_DON_THUOC" && !ClinicalRules.TryPrescriptionNumbers(Text("SoLuong"),Text("SoNgayDung"),out _,out _)) throw new InvalidOperationException("Số lượng thuốc phải dương, số ngày dùng phải là số nguyên dương.");
        if(table is "CHI_TIET_KE_HOACH" or "DICH_VU_THUC_HIEN")
        {
            var quantity=table=="CHI_TIET_KE_HOACH"?"SoLuongDuKien":"SoLuong";
            var price=table=="CHI_TIET_KE_HOACH"?"DonGiaDuKien":"DonGia";
            if(!ClinicalRules.TryNonNegativeNumber(Text(quantity),out var count)||count<=0||!ClinicalRules.TryNonNegativeNumber(Text(price),out var amount)) throw new InvalidOperationException("Số lượng phải dương và đơn giá không âm; nhập một dấu phẩy hoặc chấm thập phân, không phân cách hàng nghìn.");
        }
    }
}
