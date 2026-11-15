# Nguồn ảnh X-quang tham khảo

Bốn tệp là ảnh panorama công khai dùng minh họa phần xem ảnh của hồ sơ **giả lập** trong đồ án. Chúng không phải phim chụp của BN006/BN028/BN034/BN046 và không chứng minh chẩn đoán của các hồ sơ này. Không gán kết luận gãy xương/bình thường hoặc thay chẩn đoán theo ảnh tham khảo. Ngày tạo metadata là ngày nhập bổ sung, không phải ngày chụp. Người nhập là tài khoản quản trị NV001, không giả danh kỹ thuật viên đã chụp phim.

| Lượt / bệnh nhân / dịch vụ | Ảnh và tác giả | Giấy phép | Bản sử dụng |
|---|---|---|---|
| LS006 / BN006 / DVTH006 | [X-ray of all 32 human teeth — Ruhrfisch](https://commons.wikimedia.org/wiki/File:X-ray_of_all_32_human_teeth.jpg) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | Nguyên byte bản gốc |
| LS017 / BN028 / DVTH014 | [Medizinische Panoramaaufnahme — Richard Huber](https://commons.wikimedia.org/wiki/File:Medizinische_Panoramaaufnahme.jpg) | [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) | Bản 1280 pixel do Wikimedia cung cấp, không sửa thêm |
| LS023 / BN034 / DVTH024 | [Panoramic radiograph - Orthopantomogram — Dennis van Zuijlekom](https://commons.wikimedia.org/wiki/File:Panoramic_radiograph_-_Orthopantomogram.jpg) | [CC BY-SA 2.0](https://creativecommons.org/licenses/by-sa/2.0/) | Nguyên byte bản gốc |
| LS035 / BN046 / DVTH048 | [X-ray of teeth — Rakesh.5suthar](https://commons.wikimedia.org/wiki/File:X-ray_of_teeth.jpg) | [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) | Bản 1280 pixel do Wikimedia cung cấp, không sửa thêm |

Đổi tên thành khóa GUID theo quy tắc kho; nội dung tệp tải về được giữ nguyên. Các ảnh tiếp tục được phân phối theo giấy phép tương ứng ở trên, bao gồm yêu cầu ghi công và chia sẻ tương tự. Không hàm ý tác giả bảo trợ dự án. Nguồn, giấy phép, thay đổi kích thước của Wikimedia, dung lượng và SHA-256 được ghi trong `manifest.json`; ghi chú của từng ảnh trong SQL và giao diện cũng có ghi công và liên kết.

Khi build/publish, JPG được chép vào `DuLieu/AnhXquang` cạnh exe. Manifest và tài liệu này ở `DuLieu/NguonAnhXquang`, tách khỏi kho vận hành để công cụ đối soát không báo tệp ngoài metadata. Chép nguyên thư mục phát hành khi chuyển máy; cài database mới từ SQL chính. Với database đã có dữ liệu, dùng `tools/BoSungAnhXquang.ps1` (mặc định chỉ kiểm tra, `-ApDung` mới nhập). Kho mạng cần chép cùng bốn tệp vào kho được cấu hình trên mọi máy.
