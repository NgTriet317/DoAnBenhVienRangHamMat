using ET;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.NativeCodec;
using SixLabors.ImageSharp;

namespace BUS;

/// <summary>Chuyển khung đầu của ảnh DICOM sang PNG để kho ảnh và WPF dùng cùng luồng ảnh thường.</summary>
internal static class ChuyenDoiAnhDicom
{
    // Khởi tạo một lần khi cần chuyển DICOM, kể cả khi nhiều cửa sổ tải ảnh đồng thời.
    private static readonly Lazy<bool> Ready=new(()=>
    {
        new DicomSetupBuilder().RegisterServices(services=>services.AddFellowOakDicom()
            .AddImageManager<ImageSharpImageManager>().AddTranscoderManager<NativeTranscoderManager>()).Build();
        return true;
    });

    /// <summary>Kiểm tra kích thước trước render; fo-dicom áp window/level, LUT và MONOCHROME1 của ảnh nguồn.</summary>
    internal static byte[] SangPng(byte[] bytes,long diemAnhToiDa)
    {
        try
        {
            _=Ready.Value;
            using var input=new MemoryStream(bytes,false);
            var file=DicomFile.Open(input,FileReadOption.ReadAll);
            var dataset=file.Dataset;
            if(file.IsPartial||!dataset.Contains(DicomTag.PixelData))
                throw new LoiAnhXquangException("AnhDicomHong","Tệp DICOM bị thiếu dữ liệu hoặc không chứa ảnh X-quang.");
            var width=dataset.GetSingleValue<ushort>(DicomTag.Columns);
            var height=dataset.GetSingleValue<ushort>(DicomTag.Rows);
            if(width<1||height<1||width>12000||height>12000||(long)width*height>diemAnhToiDa)
                throw new LoiAnhXquangException("QuaKichThuoc","Kích thước ảnh DICOM vượt giới hạn an toàn của kho ảnh.");
            var pixels=DicomPixelData.Create(dataset);
            if(pixels.NumberOfFrames<1)
                throw new LoiAnhXquangException("AnhDicomHong","Tệp DICOM không có khung ảnh để hiển thị.");
            if(pixels.SamplesPerPixel<1||pixels.UncompressedFrameSize<1)
                throw new LoiAnhXquangException("AnhDicomHong","Thông số điểm ảnh trong tệp DICOM không hợp lệ.");
            // Không cho bộ render tự bù dữ liệu khi khung không nén bị cắt thiếu byte.
            if(!dataset.InternalTransferSyntax.IsEncapsulated&&pixels.GetFrame(0).Size<pixels.UncompressedFrameSize)
                throw new LoiAnhXquangException("AnhDicomHong","Dữ liệu điểm ảnh trong tệp DICOM bị thiếu hoặc hỏng.");
            var image=new DicomImage(dataset){ShowOverlays=false,CacheMode=CacheType.None};
            using var rendered=image.RenderImage(0);
            using var png=new MemoryStream();
            rendered.AsSharpImage().SaveAsPng(png);
            return png.ToArray();
        }
        catch(Exception ex) when(ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {throw new LoiAnhXquangException("BoGiaiMaDicom","Chưa nạp được bộ giải mã DICOM nén. Kiểm tra bộ cài và Visual C++ Redistributable trên máy.",ex);}
        catch(Exception ex) when(ex is not LoiAnhXquangException && ex is DicomException or IOException or ArgumentException or InvalidOperationException or NotSupportedException or OverflowException or IndexOutOfRangeException)
        {throw new LoiAnhXquangException("AnhDicomHong","Không chuyển đổi được ảnh DICOM. Tệp có thể bị hỏng, thiếu dữ liệu ảnh hoặc dùng kiểu nén chưa được hỗ trợ.",ex);}
    }
}
