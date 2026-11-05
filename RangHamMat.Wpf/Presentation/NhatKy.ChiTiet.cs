using System.Data;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using ET;

namespace RangHamMat.Views;

public partial class NhatKy
{
    private sealed record ThayDoiHienThi(string Ten,string Truoc,string Sau);
    private static readonly Dictionary<string,string> DetailLabels=new(StringComparer.Ordinal)
    {
        ["HoTenBN"]="Họ tên bệnh nhân",["HoTen"]="Họ và tên",["NgaySinh"]="Ngày sinh",["GioiTinh"]="Giới tính",
        ["SDT"]="Số điện thoại",["DiaChi"]="Địa chỉ",["TrangThai"]="Trạng thái",["TrangThaiDuyet"]="Trạng thái duyệt",
        ["MaNV"]="Nhân viên",["MaTK"]="Tài khoản",["MaVaiTro"]="Vai trò",["MaCK"]="Chuyên khoa",["MaKhoa"]="Khoa",
        ["MaLoaiHH"]="Loại hàng hóa",["MaNhomThuoc"]="Nhóm thuốc",["MaDV"]="Dịch vụ",["MaDMTT"]="Tình trạng bệnh",
        ["MaPhong"]="Phòng",["DonGia"]="Đơn giá",["ThoiGianDuKien"]="Thời gian dự kiến",["NgayApDung"]="Ngày áp dụng"
    };
    private static readonly Dictionary<string,string> DetailTables=new(StringComparer.Ordinal)
    {
        ["BENH_NHAN"]="Bệnh nhân",["NHAN_VIEN"]="Nhân viên",["TAI_KHOAN"]="Tài khoản",["VAI_TRO"]="Vai trò",
        ["PHIEN_DANG_NHAP"]="Phiên đăng nhập",["HO_SO_BENH_AN"]="Hồ sơ bệnh án",["LICH_SU_KHAM_BENH"]="Lần khám",
        ["PHIEU_KHAM_BENH"]="Phiếu khám bệnh",["LICH_KHAM"]="Lịch khám",["DON_THUOC"]="Đơn thuốc",
        ["HOA_DON"]="Hóa đơn",["KHO"]="Kho",["HANG_HOA"]="Hàng hóa",["BANG_LUONG"]="Bảng lương",
        ["THIET_BI"]="Thiết bị",["BAO_TRI_THIET_BI"]="Bảo trì thiết bị",["THANH_TOAN"]="Thanh toán"
    };
    private static string FieldLabel(string key)=>DetailLabels.GetValueOrDefault(key)
        ??DanhMucHeThong.TatCa.SelectMany(d=>d.Truong).FirstOrDefault(f=>f.Cot==key)?.Nhan??RangHamMat.Controls.TruongDuLieu.Label("",key);
    private static string TableLabel(string key)=>DetailTables.GetValueOrDefault(key)
        ??DanhMucHeThong.TatCa.FirstOrDefault(d=>d.Bang==key)?.Ten??key;
    private static string Read(DataRowView row,string key)=>row.Row.Table.Columns.Contains(key)&&row[key]!=DBNull.Value?Convert.ToString(row[key])??"":"";
    private static Dictionary<string,string> ReadChangeFields(string json)
    {
        var result=new Dictionary<string,string>();
        if(string.IsNullOrWhiteSpace(json))return result;
        try
        {
            using var doc=JsonDocument.Parse(json);
            if(doc.RootElement.ValueKind!=JsonValueKind.Object)return result;
            foreach(var field in doc.RootElement.EnumerateObject())
            {
                if(new[]{"MatKhau","Token","Secret","ApiKey","ConnectionString"}.Any(s=>field.Name.Contains(s,StringComparison.OrdinalIgnoreCase)))continue;
                result[field.Name]=field.Value.ValueKind switch
                {
                    JsonValueKind.Null=>"Chưa có thông tin",JsonValueKind.String=>field.Value.GetString()??"",
                    JsonValueKind.True=>"Có",JsonValueKind.False=>"Không",
                    JsonValueKind.Array=>string.Join(", ",field.Value.EnumerateArray().Select(x=>x.ToString())),_=>field.Value.ToString()
                };
            }
        }
        catch(JsonException){/* Nhật ký cũ không có payload cấu trúc chỉ hiển thị thông tin chung. */}
        return result;
    }
    private void ShowDetail(DataRowView? row)
    {
        if(DetailEmpty is null||DetailContent is null)return;
        DetailEmpty.Visibility=row is null?Visibility.Visible:Visibility.Collapsed;
        DetailContent.Visibility=row is null?Visibility.Collapsed:Visibility.Visible;
        DetailChanges.ItemsSource=null;
        if(row is null){DetailAction.Text="";Detail.Text="";return;}
        DetailContent.ScrollToTop();
        string action=Read(row,"HanhDong"),outcome=Read(row,"KetQua"),account=Read(row,"TenDangNhap");
        DetailAction.Text=HanhDongNhatKy.Nhan(action);
        DetailOutcome.Text=outcome.Length>0?outcome:"Chưa có kết quả";
        bool failed=outcome.Contains("Thất bại",StringComparison.OrdinalIgnoreCase)||outcome.Contains("Lỗi",StringComparison.OrdinalIgnoreCase);
        DetailStatus.Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(failed?"#FDE9E9":"#E2F3E8"));
        DetailOutcome.Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(failed?"#B33B3B":"#226B45"));
        DetailPerson.Text=Read(row,"HoTen") is {Length:>0} person?person:account.Length>0?account:"Không xác định tài khoản";
        DetailAccount.Text=account.Length>0?"Tài khoản: "+account:"Không có thông tin tài khoản";
        DetailTime.Text=Read(row,"ThoiGianHienThi") is {Length:>0} time?time:"Chưa có thời gian";
        string table=Read(row,"LoaiDoiTuong"),module=Read(row,"PhanHe"),id=Read(row,"MaDoiTuong");
        DetailModule.Text=TableLabel(module.Length>0?module:table);
        DetailObject.Text=TableLabel(table)+(id.Length>0&&table!="PHIEN_DANG_NHAP"?" · "+id:"");
        DetailId.Text=Read(row,"MaNK");DetailSession.Text=Read(row,"MaPhien") is {Length:>0} session?session:"Không có phiên liên kết";
        var before=ReadChangeFields(Read(row,"DuLieuTruoc"));var after=ReadChangeFields(Read(row,"DuLieuSau"));
        var changes=new List<ThayDoiHienThi>();
        foreach(string field in before.Keys.Union(after.Keys).Where(k=>k!="TruongDaDoi"&&!k.StartsWith("Ma",StringComparison.Ordinal)))
        {
            string oldValue=before.GetValueOrDefault(field)??"Chưa có thông tin",newValue=after.GetValueOrDefault(field)??"Đã bỏ thông tin";
            if(oldValue!=newValue)changes.Add(new(FieldLabel(field),oldValue,newValue));
        }
        DetailChanges.ItemsSource=changes;
        string changedNames=after.GetValueOrDefault("TruongDaDoi")??before.GetValueOrDefault("TruongDaDoi")??"";
        Detail.Text=changes.Count>0?$"{changes.Count} trường thông tin được thay đổi."
            :changedNames.Length>0?"Các trường được cập nhật: "+string.Join(", ",changedNames.Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Select(FieldLabel))+". Giá trị chi tiết không được lưu trong nhật ký."
            :action is "Đăng nhập thành công" or "Đăng nhập thất bại" or "Đăng xuất"?"Ghi nhận "+action.ToLowerInvariant()+" của tài khoản trong hệ thống."
            :"Thao tác đã được ghi nhận. Nhật ký này không lưu chi tiết giá trị thay đổi.";
    }
}
