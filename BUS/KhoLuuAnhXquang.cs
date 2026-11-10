using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace BUS;

/// <summary>Hạ tầng tệp; các đầu mối giao diện phải đi qua BUS_ANHXQUANG để kiểm quyền SQL.</summary>
public sealed class KhoLuuAnhXquang(CauHinhKhoAnh config)
{
    private static readonly Regex StorageKey=new(@"\A[0-9a-fA-F]{32}\.(png|jpg|jpeg)\z",RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
    private long MaxBytes=>config.DungLuongToiDaMB*1024L*1024L;

    private void ValidateLimits()
    {
        if(config.DungLuongToiDaMB is <1 or >20 || config.DiemAnhToiDa is <1 or >40000000)
            throw Error("CauHinh","Giới hạn kho ảnh chưa hợp lệ: tối đa 20 MiB và 40 triệu điểm ảnh.");
    }
    public string ThuMucGoc()
    {
        ValidateLimits();var value=config.ThuMucDungChung;
        if(string.IsNullOrWhiteSpace(value))throw Error("KhoChuaCauHinh","Kho ảnh chưa được cấu hình. Liên hệ quản trị viên.");
        if(!Path.IsPathFullyQualified(value)||value.StartsWith(@"\\?\",StringComparison.Ordinal)||value.StartsWith(@"\\.\",StringComparison.Ordinal)||(!config.ChoPhepDuongDanCucBo&&!value.StartsWith(@"\\",StringComparison.Ordinal)))
            throw Error("DuongDanKhongHopLe","Kho ảnh phải là thư mục dùng chung đã được quản trị viên cấu hình.");
        try
        {
            var full=Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar);
            if(!Directory.Exists(full))throw Error("KhoKhongTonTai","Không truy cập được kho ảnh. Kiểm tra kết nối hoặc liên hệ quản trị viên.");
            for(DirectoryInfo? directory=new(full);directory is not null;directory=directory.Parent)
                if((directory.Attributes&FileAttributes.ReparsePoint)!=0)throw Error("DuongDanKhongHopLe","Kho ảnh không được đi qua thư mục liên kết.");
            return full;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException)
        {throw Error("KhoKhongSanSang","Không truy cập được kho ảnh. Kiểm tra kết nối và quyền thư mục.",ex);}
    }
    private string Resolve(string key)
    {
        if(string.IsNullOrEmpty(key)||!StorageKey.IsMatch(key))throw Error("DuongDanKhongHopLe","Khóa tệp ảnh không hợp lệ.");
        var root=ThuMucGoc();var path=Path.Combine(root,key);
        if(File.Exists(path)&&(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)
            throw Error("DuongDanKhongHopLe","Không mở tệp liên kết ngoài kho ảnh.");
        return path;
    }
    public AnhDaKiemTra KiemTraTep(Stream content,string originalName)
    {
        ValidateLimits();
        if(string.IsNullOrWhiteSpace(originalName)||originalName.Length>255||originalName.IndexOfAny(['\\','/',':','\0'])>=0)
            throw Error("TenTepKhongHopLe","Tên ảnh không hợp lệ; chọn lại tệp ảnh gốc.");
        var extension=Path.GetExtension(originalName).ToLowerInvariant();
        if(extension is not(".png" or ".jpg" or ".jpeg"))throw Error("DinhDangKhongHoTro","Chỉ nhận ảnh JPG, JPEG hoặc PNG. DICOM cần bộ xem chuyên dụng.");
        byte[] bytes;
        using(var output=new MemoryStream())
        {
            var buffer=new byte[81920];int length;
            while((length=content.Read(buffer,0,buffer.Length))>0)
            {
                if(output.Length+length>MaxBytes)throw Error("QuaDungLuong","Ảnh vượt giới hạn dung lượng đã cấu hình.");
                output.Write(buffer,0,length);
            }
            bytes=output.ToArray();
        }
        if(bytes.Length==0)throw Error("AnhHong","Tệp ảnh rỗng hoặc không đọc được.");
        try
        {
            var format=Image.DetectFormat(bytes);var mime=format.DefaultMimeType;
            if((extension==".png"&&mime!="image/png")||(extension!=".png"&&mime!="image/jpeg"))
                throw Error("SaiDinhDang","Nội dung ảnh không khớp đuôi tệp đã chọn.");
            var info=Image.Identify(bytes);
            if(info.Width<1||info.Height<1||info.Width>12000||info.Height>12000||(long)info.Width*info.Height>config.DiemAnhToiDa)
                throw Error("QuaKichThuoc","Kích thước ảnh vượt giới hạn an toàn của kho ảnh.");
            using var image=Image.Load(new DecoderOptions{MaxFrames=2},bytes);
            if(image.Frames.Count!=1)throw Error("DinhDangKhongHoTro","Chỉ nhận ảnh tĩnh một khung hình.");
            return new(bytes,mime,extension,Convert.ToHexString(SHA256.HashData(bytes)),info.Width,info.Height);
        }
        catch(Exception ex) when(ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException or ArgumentException)
        {throw Error("AnhHong","Tệp ảnh hỏng hoặc không đúng định dạng hỗ trợ.",ex);}
    }
    public string LuuTep(AnhDaKiemTra image)
    {
        var root=ThuMucGoc();var key=Guid.NewGuid().ToString("N")+image.PhanMoRong;var target=Resolve(key);
        var pending=Path.Combine(root,".pending");Directory.CreateDirectory(pending);
        if((File.GetAttributes(pending)&FileAttributes.ReparsePoint)!=0)throw Error("DuongDanKhongHopLe","Thư mục tạm của kho ảnh không được là liên kết.");
        var temp=Path.Combine(pending,Guid.NewGuid().ToString("N")+".part");
        try
        {
            using(var file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {file.Write(image.NoiDung);file.Flush(true);}
            File.Move(temp,target,false);return key;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
        {throw Error("GhiTepThatBai","Chưa lưu được ảnh vào kho. Kiểm tra kết nối hoặc quyền thư mục.",ex);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    internal void XoaTepChuaLienKet(string key)
    {var path=Resolve(key);if(File.Exists(path))File.Delete(path);}

    public byte[] DocTep(ET_ANHXQUANGLANKHAM image)
    {
        if(image.DuongDanTuongDoi!=image.TenTepLuu)throw Error("DuongDanKhongHopLe","Metadata ảnh có đường dẫn không hợp lệ.");
        var path=Resolve(image.DuongDanTuongDoi);
        try
        {
            using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
            if(file.Length!=image.KichThuocBytes||file.Length>MaxBytes)throw Error("SaiToanVen","Dung lượng tệp ảnh không khớp hồ sơ lưu trữ.");
            var data=KiemTraTep(file,image.TenTepGoc);
            if(!string.Equals(data.Sha256,image.Sha256,StringComparison.OrdinalIgnoreCase)||data.MimeType!=image.MimeType)
                throw Error("SaiToanVen","Tệp ảnh không khớp dấu kiểm tra toàn vẹn. Liên hệ quản trị viên để khôi phục.");
            return data.NoiDung;
        }
        catch(Exception ex) when(ex is FileNotFoundException or DirectoryNotFoundException)
        {throw Error("TepKhongTonTai","Không tìm thấy tệp ảnh trong kho. Metadata vẫn được giữ; cần khôi phục tệp từ bản sao lưu.",ex);}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
        {throw Error("DocTepThatBai","Không đọc được ảnh từ kho dùng chung. Kiểm tra kết nối hoặc quyền truy cập.",ex);}
    }
    public IReadOnlyList<SuCoKhoAnh> KiemTra(IEnumerable<ET_ANHXQUANGLANKHAM> images)
    {
        var root=ThuMucGoc();var known=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var issues=new List<SuCoKhoAnh>();
        foreach(var image in images)
        {
            known.Add(image.DuongDanTuongDoi);
            try{DocTep(image);}catch(LoiAnhXquangException ex){issues.Add(new(ex.MaLoi,image.DuongDanTuongDoi,image.MaAnh));}
        }
        foreach(var file in Directory.EnumerateFiles(root))
            if(!known.Contains(Path.GetFileName(file)))issues.Add(new("TepKhongDangKy",Path.GetFileName(file),null));
        var pending=Path.Combine(root,".pending");
        if(Directory.Exists(pending))
        {
            if((File.GetAttributes(pending)&FileAttributes.ReparsePoint)!=0)issues.Add(new("DuongDanKhongHopLe",".pending",null));
            else foreach(var file in Directory.EnumerateFiles(pending))issues.Add(new("TepTam",Path.GetFileName(file),null));
        }
        return issues;
    }
    private static LoiAnhXquangException Error(string code,string message,Exception? inner=null)=>new(code,message,inner);
}
