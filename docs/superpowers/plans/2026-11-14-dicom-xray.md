# DICOM X-quang

**Yêu cầu:** Bác sĩ tải DICOM trong tab ảnh X-quang và xem ảnh như JPG/PNG hiện tại.

**Cách thực hiện:** Dùng fo-dicom 5.2.6 và ImageSharp đã có để render khung đầu tiên thành PNG. Giữ tên tệp gốc trong metadata; SHA-256, MIME, dung lượng và khóa kho mô tả PNG. Kiểm tra ảnh lưu theo tên tệp trong kho khi mở lại. Thêm codec tương thích fo-dicom 5 để đọc JPEG/JPEG-LS/JPEG 2000 nén. Không thay đổi schema SQL hay quyền tải/xem ảnh.

**Giới hạn:** Giữ tối đa 20 MiB cho đầu vào và PNG đầu ra, 40 triệu điểm ảnh, mỗi chiều tối đa 12.000. Tệp hỏng/không có PixelData phải báo lỗi nghiệp vụ. DICOM nhiều khung dùng khung đầu tiên, được ghi rõ trên giao diện.

- [x] Viết và chạy bộ kiểm độc lập SQL: DICOM 8/16 bit, MONOCHROME1/2, nhiều khung, ảnh màu, ảnh hỏng, giả đuôi, quá giới hạn, PNG/JPEG cũ, lưu/mở lại và WPF viewer.
- [x] Bổ sung chuyển đổi trong BUS/KhoLuuAnhXquang.cs và bộ chuyển đổi riêng BUS/ChuyenDoiAnhDicom.cs; xử lý lỗi bộ giải mã bằng thông báo tiếng Việt.
- [x] Cập nhật hộp chọn tệp, hướng dẫn và trạng thái tải DICOM trong AnhXquangLanKham; cập nhật README.
- [x] Chạy kiểm chuyển đổi và build solution; chạy bộ kiểm X-quang SQL/WPF trên database thử cách ly, gồm upload DICOM, retry, quyền và toàn vẹn.

**Kiểm cần chú ý:** SHA-256 phải tính từ PNG; tên gốc .dcm vẫn mở lại được; PNG giả đuôi .dcm phải bị chặn; 16-bit phải áp window/level; nén JPEG 2000 phải giải mã thực tế.

**Kết quả ngày 14/11/2026:**

- Trước triển khai: bộ kiểm mới có 17 ca lỗi vì DICOM bị chặn, 3 ca cũ đạt.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraDicomXquang.ps1`: 27/27 đạt, gồm chuyển đổi thực tế RLE, JPEG Baseline, JPEG Lossless, JPEG-LS và JPEG 2000; kiểm cả giới hạn PNG đầu ra và thông số SamplesPerPixel=0.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraAnhXquang.ps1 -TaoDatabaseKiemThu -Database RHM_Hospital_Test_Dicom_20261114_02`: 34/34 đạt ở lần kiểm cuối sau sửa lỗi dữ liệu DICOM hỏng. Dùng database thử mới; không cập nhật dữ liệu bệnh viện. Lần chạy đầu trong sandbox không kết nối được xác thực Windows, các lần chạy có quyền truy cập Windows hoàn tất.
- `dotnet build BenhVienRangHamMat.sln --no-restore -p:NuGetAudit=false -v minimal`: thành công. Lần biên dịch đầu có cảnh báo nullable và ẩn thuộc tính từ các tệp sẵn có; không có lỗi biên dịch.
- Minh chứng cục bộ: `obj/DicomXrayChecks/conversion.log`, `obj/DicomXrayChecks/sql-xray-final.log`, `obj/RhmFinal/xray-dicom.png`. Thư viện `runtimes/win-x64/native/Dicom.Native.dll` có trong output ứng dụng.

**Lựa chọn triển khai:** Giữ kho lưu ảnh PNG và tên DICOM gốc trong metadata, không lưu bản DICOM nguồn. Dùng khung đầu tiên như hướng dẫn trên giao diện; không xây dựng bộ xem chuỗi ảnh DICOM.

**Rà soát độc lập:** Phát hiện SamplesPerPixel=0 có thể gây AggregateException bên trong fo-dicom; đã thêm kiểm tra thông số trước render và kiểm hồi quy (thấy lỗi trước sửa, đạt sau sửa). Thay thông báo “tệp gốc” ở thao tác ẩn thành “ảnh trong kho” để mô tả đúng dữ liệu được giữ.
