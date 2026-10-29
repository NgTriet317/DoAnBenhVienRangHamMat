# Danh mục thuốc Răng Hàm Mặt

Ngày đối chiếu nguồn: **29/10/2026**. Giai đoạn4 của `Database/QLBenhVienRangHamMat.sql` bổ sung **52 sản phẩm thuốc**, **20 nhóm**, và gán nhóm cho **10 thuốc hiện có** khi còn thiếu nhóm. Trong 52 sản phẩm có **40 giá tham khảo theo đơn vị** và **12 giá chưa thiết lập**. Không cần chạy fragment hoặc SQL bổ sung.

Đây là dữ liệu nhận diện và phân loại, không phải danh mục sử dụng đã được Hội đồng thuốc và điều trị phê duyệt. Tên thuốc, hoạt chất và hàm lượng được đối chiếu nguồn công khai; việc có tên trong nguồn không xác nhận giấy phép lưu hành còn hiệu lực tại thời điểm mua thuốc. Không tạo hướng dẫn liều, đơn mẫu, người duyệt, lô hàng, hạn dùng hay số lượng tồn.

## Nguồn và chính sách giá

- **VPMED**: [Danh mục giá thuốc của Bệnh viện VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/). Dùng đúng cột **Giá Yêu Cầu**, VND/đơn vị được ghi trên từng dòng. Trang không nêu ngày hiệu lực chung; ngày tra cứu là 29/10/2026. Không dùng cột Giá BHYT=0 làm giá bán.
- **Tâm Đức**: [Bảng giá thuốc nội trú tháng 02/2026](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf), lập ngày 02/02/2026, cột **Giá Bán tháng 02.2026**. Các giá chỉ phản ánh bảng giá công khai của bệnh viện nguồn tại kỳ đó.
- **Nhà sản xuất/Cục Quản lý Dược**: liên kết cụ thể ở từng dòng bên dưới để xác nhận tên/hàm lượng/dạng bào chế, không suy ra giá từ sản phẩm tương tự.
- Đối chiếu bổ sung: [Fluconazole STELLA 150 mg](https://stellapharm.com/products/fluconazole-stella-150-mg/), [Pantostad 40 CAP](https://stellapharm.com/products/pantostad-40-cap/), [EMLA 5% (5g), thông tin do chủ sở hữu cung cấp](https://www.medicines.org.uk/emc/product/871/smpc). EMLA 5% chứa lidocain 2,5% + prilocain 2,5%; Pantostad **CAP** được giữ riêng với dạng viên nén.

Giá khởi tạo là **giá tham khảo của cơ sở nguồn**, không được mô tả là giá bán do bệnh viện đang triển khai phần mềm phê duyệt. Nhân viên được chỉnh giá theo chính sách thực tế; chạy lại script không ghi đè bất kỳ giá đang có nào. Không sao chép giá mua sắm/trúng thầu thành giá bán.

Vì `THUOC.DonGiaBan` hiện bắt buộc NOT NULL, 12 sản phẩm thiếu nguồn giá dùng `0` làm trạng thái **Chưa thiết lập giá**, có ghi rõ trong `HANG_HOA.MoTa`. Số 0 này không có nghĩa miễn phí. Giao diện cần hiện nhãn đó, và lập hóa đơn phải từ chối thuốc có `DonGiaBan <= 0` cho đến khi được thiết lập giá. Cho phép xem danh mục, soạn/ký đơn theo yêu cầu nghiệp vụ.

## Nhóm danh mục

Nhóm nằm trực tiếp tại `THUOC.MaNhomThuoc`, là FK nullable đến `NHOM_THUOC_KE_DON`. Việc phân nhóm không cần tạo bản ghi ở `CAU_HINH_THUOC_KE_DON` và không phụ thuộc trạng thái phê duyệt hướng dẫn.

| Mã nhóm | Tên nhóm | Sản phẩm mới |
|---|---|---:|
| RHM_GIAM_DAU | Giảm đau - hạ sốt | 5 |
| RHM_NSAID | Kháng viêm không steroid (NSAID) | 5 |
| RHM_KS_PENICILLIN | Kháng sinh penicilin và phối hợp | 6 |
| RHM_KS_CEPHALOSPORIN | Kháng sinh cephalosporin | 2 |
| RHM_KS_MACROLID | Kháng sinh macrolid | 2 |
| RHM_KS_LINCOSAMID | Kháng sinh lincosamid | 1 |
| RHM_KS_NITROIMIDAZOL | Kháng khuẩn nitroimidazol | 2 |
| RHM_KS_SULFONAMID | Kháng khuẩn sulfonamid phối hợp | 1 |
| RHM_KHANG_NAM | Thuốc kháng nấm | 2 |
| RHM_KHANG_VIRUS | Thuốc kháng virus | 2 |
| RHM_SAT_KHUAN_MIENG | Sát khuẩn miệng - họng | 1 |
| RHM_SAT_KHUAN_NGOAI | Sát khuẩn dùng ngoài | 2 |
| RHM_KHANG_HISTAMIN | Chống dị ứng - kháng histamin H1 | 5 |
| RHM_TIEU_HOA | Thuốc hỗ trợ tiêu hóa - bảo vệ dạ dày | 4 |
| RHM_CORTICOID_TOAN_THAN | Corticosteroid toàn thân | 3 |
| RHM_CORTICOID_TAI_CHO | Corticosteroid tại chỗ | 0 |
| RHM_GAY_TE_TAI_CHO | Thuốc gây tê tại chỗ | 5 |
| RHM_DUNG_DICH_RUA | Dung dịch rửa | 1 |
| RHM_CAM_MAU | Thuốc cầm máu | 1 |
| RHM_CAP_CUU | Thuốc cấp cứu - hỗ trợ thủ thuật | 2 |

Nhóm corticoid tại chỗ được dùng cho TH006 hiện có. Các thuốc tiêm/cấp cứu được phân nhóm để quản lý danh mục bệnh viện; việc xuất hiện trong danh mục không tạo chỉ định ngoại trú hay cho phép bệnh nhân tự dùng. Nhóm hỗ trợ tiêu hóa không được tự động thêm vào đơn.

## Sản phẩm bổ sung

Đơn giá là VND theo đơn vị trong bảng. Mã tham chiếu nguồn VPMED/STT Tâm Đức được lưu riêng trong `HANG_HOA.MoTa`; không lấy thông tin lô/hạn dùng từ nguồn làm dữ liệu kho.

| Mã thuốc | Tên | Hoạt chất; hàm lượng | Dạng / đơn vị | Giá khởi tạo | Nguồn |
|---|---|---|---|---:|---|
| THRHM001 | Hapacol 250 | Paracetamol; 250 mg/gói | Dạng gói uống / Gói | 2,000 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD0201-000003; cột Giá Yêu Cầu |
| THRHM002 | Koact 625 | Amoxicilin + acid clavulanic; 500 mg + 125 mg/viên | Viên uống / Viên | 3,000 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); KOAC001; cột Giá Yêu Cầu |
| THRHM003 | Axuka | Amoxicilin + acid clavulanic; 1000 mg + 200 mg/lọ | Thuốc tiêm / Lọ | 42,000 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD29-000024; cột Giá Yêu Cầu |
| THRHM004 | Metronidazol 250 | Metronidazol; 250 mg/viên | Viên uống / Viên | 252 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD29-000017; cột Giá Yêu Cầu |
| THRHM005 | Lidocain hydroclorid 40mg/2ml | Lidocain hydroclorid; 40 mg/2 ml | Dung dịch tiêm, ống 2 ml / Ống | 495 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003796; cột Giá Yêu Cầu |
| THRHM006 | Lignospan standard | Lidocain + epinephrin; 36 mg + 18,13 microgam/1,8 ml | Thuốc tiêm, ống 1,8 ml / Ống | 20,895 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003487; cột Giá Yêu Cầu |
| THRHM007 | Aciclovir 5% | Aciclovir; 50 mg/g; tuýp 5 g | Thuốc dùng ngoài, tuýp 5 g / Tuýp | 3,700 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003857; cột Giá Yêu Cầu |
| THRHM008 | Agilecox 200 | Celecoxib; 200 mg/viên | Viên uống / Viên | 1,155 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003910; cột Giá Yêu Cầu |
| THRHM009 | Desloratadin ODT 5 | Desloratadin; 5 mg/viên | Viên uống ODT / Viên | 1,386 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003900; cột Giá Yêu Cầu |
| THRHM010 | Medlon 16 | Methylprednisolon; 16 mg/viên | Viên uống / Viên | 1,220 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003833; cột Giá Yêu Cầu |
| THRHM011 | Medsolu 4 mg | Methylprednisolon; 4 mg/viên | Viên uống / Viên | 900 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003318; cột Giá Yêu Cầu |
| THRHM012 | Dexamethasone | Dexamethason; 4 mg/1 ml | Thuốc tiêm, ống 1 ml / Ống | 690 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003735; cột Giá Yêu Cầu |
| THRHM013 | Diclofenac | Diclofenac; 75 mg/3 ml | Thuốc tiêm, ống 3 ml / Ống | 1,200 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003129; cột Giá Yêu Cầu |
| THRHM014 | Haxium 40 | Esomeprazol; 40 mg/viên | Viên uống / Viên | 840 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); HAXI001; cột Giá Yêu Cầu |
| THRHM015 | Adrenalin DV24 | Epinephrin; 1 mg/1 ml | Thuốc tiêm, ống 1 ml / Ống | 2,800 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003598; cột Giá Yêu Cầu |
| THRHM016 | Biseptol 480 | Sulfamethoxazol + trimethoprim; 400 mg + 80 mg/viên | Viên uống / Viên | 2,500 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003876; cột Giá Yêu Cầu |
| THRHM017 | Novocain 3% | Procain hydroclorid; 30 mg/ml; ống 2 ml | Thuốc tiêm, ống 2 ml / Ống | 495 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003798; cột Giá Yêu Cầu; khác TD30-003260 |
| THRHM018 | Atropin sulphat | Atropin sulfat; 0,25 mg/ml; ống 1 ml | Thuốc tiêm, ống 1 ml / Ống | 530 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003902; cột Giá Yêu Cầu |
| THRHM019 | Hadumedrol | Diphenhydramin; 10 mg/ml; ống 1 ml | Thuốc tiêm, ống 1 ml / Ống | 630 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD30-003806; cột Giá Yêu Cầu |
| THRHM020 | Nerusyn 1,5g | Ampicilin + sulbactam; 1 g + 0,5 g/lọ | Thuốc tiêm / Lọ | 41,000 | [VPMED](https://vpmed.vn/bang-gia/bang-gia-thuoc/); TD29-000019; cột Giá Yêu Cầu |
| THRHM021 | Betadine Antiseptic | Povidon iod; 10%; lọ 125 ml | Dung dịch sát khuẩn ngoài da / Lọ | 60,786 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 39, BeDT, trang 1; Giá Bán tháng 02.2026 |
| THRHM022 | Betadine Mouthwash Gargle | Povidon iod; 1% | Dung dịch súc miệng - họng / Lọ | 75,273 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 40, BeMT, trang 1; nguồn không ghi thể tích lọ |
| THRHM023 | Lorastad 10mg | Loratadin; 10 mg/viên | Viên uống / Viên | 1,062 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 321, LorT4, trang 7 |
| THRHM024 | Povidine 10% 90ml | Povidon iod; 10%; chai 90 ml | Dung dịch sát khuẩn ngoài da / Chai | 18,720 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 46, PovT3, trang 1 |
| THRHM025 | Emla 5% | Lidocain + prilocain; 2,5% + 2,5%; tuýp 5 g | Kem bôi ngoài da / Tuýp | 70,359 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 41, EmCT, trang 1 |
| THRHM026 | Xylocaine Jelly Oin 2% | Lidocain; 2%; tuýp 30 g | Gel dùng tại chỗ / Tuýp | 78,062 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 52, XyJT, trang 1 |
| THRHM027 | Augmentin 1g | Amoxicilin + acid clavulanic; 875 mg + 125 mg/viên | Viên nén / Viên | 19,516 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 54, AugT6, trang 2 |
| THRHM028 | Augmentin 250/31.25 mg | Amoxicilin + acid clavulanic; 250 mg + 31,25 mg/gói | Dạng gói uống / Gói | 12,484 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 55, AugT1, trang 2 |
| THRHM029 | Fluconazole STELLA 150mg | Fluconazol; 150 mg/viên | Viên nang cứng / Viên | 10,763 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 65, FlST1, trang 2; dạng bào chế đối chiếu STELLA |
| THRHM030 | Nystafar | Nystatin; 100.000 IU/1 g | Dạng gói dùng miệng / Gói | 1,890 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 74, NysT, trang 2 |
| THRHM031 | Acyclovir STELLA 800mg | Aciclovir; 800 mg/viên | Viên uống / Viên | 4,800 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 138, AcST, trang 3 |
| THRHM032 | Cetirizine STELLA 10mg | Cetirizin; 10 mg/viên | Viên uống / Viên | 551 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 190, CeST, trang 4 |
| THRHM033 | Clorpheniramin 4mg DHG | Clorpheniramin; 4 mg/viên | Viên nén / Viên | 250 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 193, CloT58, trang 4; Dược Hậu Giang |
| THRHM034 | Celebrex 200mg | Celecoxib; 200 mg/viên | Viên uống / Viên | 13,938 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 189, CelT3, trang 4 |
| THRHM035 | Paracetamol 10mg/ml Allomed | Paracetamol; 10 mg/ml; túi 100 ml | Dung dịch truyền / Túi | 10,296 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 29, ParT15, trang 1 |
| THRHM036 | Sodium chloride 0.9% Mekophar - rửa | Natri clorid; 0,9%; chai 500 ml | Dung dịch rửa / Chai | 12,402 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 50, SoCT42, trang 1; loại dùng rửa |
| THRHM037 | Medsamic | Acid tranexamic; 500 mg/5 ml | Thuốc tiêm, ống 5 ml / Ống | 25,155 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 333, MedT8, trang 7 |
| THRHM038 | Motilium M | Domperidon; 10 mg/viên | Viên nén / Viên | 2,574 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 344, MoMT, trang 7 |
| THRHM039 | MUCOSTA Tablets | Rebamipid; 100 mg/viên | Viên nén / Viên | 4,802 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 345, MucT13, trang 7 |
| THRHM040 | Pantostad CAP 40mg | Pantoprazol; 40 mg/viên | Viên nang / Viên | 2,160 | [Tâm Đức](https://tamduchearthospital.com/wp-content/uploads/2026/02/giathuocthang02.2026.pdf); STT 379, PaCT13, trang 8; CAP, không phải Pantostad dạng viên nén |
| THRHM041 | Hapacol 80 | Paracetamol; 80 mg/gói | Bột sủi bọt / Gói | Chưa thiết lập (0) | [Thông tin sản phẩm](https://hapacol.vn/san-pham/hapacol-80-2-2/) |
| THRHM042 | Hapacol 150 | Paracetamol; 150 mg/gói | Bột sủi bọt / Gói | Chưa thiết lập (0) | [Thông tin sản phẩm](https://dav.gov.vn/upload_images/files/722_QD_QLD%202025_signed.pdf) |
| THRHM043 | Hapacol 650 | Paracetamol; 650 mg/viên | Viên nén / Viên | Chưa thiết lập (0) | [Thông tin sản phẩm](https://hapacol.vn/san-pham/hapacol-650/) |
| THRHM044 | Ibuprofen STELLA 200 mg | Ibuprofen; 200 mg/viên | Viên nén bao phim / Viên | Chưa thiết lập (0) | [Thông tin sản phẩm](https://stellapharm.com/products/ibudolor-200/) |
| THRHM045 | Ibuprofen STELLA 400 mg | Ibuprofen; 400 mg/viên | Viên nén bao phim / Viên | Chưa thiết lập (0) | [Thông tin sản phẩm](https://stellapharm.com/products/ibuprofen-stella-400-mg/) |
| THRHM046 | Hagimox 250 | Amoxicilin; 250 mg/gói | Bột pha hỗn dịch uống / Gói | Chưa thiết lập (0) | [Thông tin sản phẩm](https://dhgpharma.com.vn/vi/san-pham/khang-sinh/hagimox-250) |
| THRHM047 | Haginat 250 | Cefuroxim; 250 mg/viên | Viên nén bao phim / Viên | Chưa thiết lập (0) | [Thông tin sản phẩm](https://dhgpharma.com.vn/vi/san-pham/khang-sinh/haginat-250) |
| THRHM048 | Haginat 500 | Cefuroxim; 500 mg/viên | Viên nén bao phim / Viên | Chưa thiết lập (0) | [Thông tin sản phẩm](https://dhgpharma.com.vn/vi/san-pham/khang-sinh/haginat-500) |
| THRHM049 | Clindamycin STELLA 300 mg | Clindamycin; 300 mg/viên | Viên nang cứng / Viên | Chưa thiết lập (0) | [Thông tin sản phẩm](https://stellapharm.com/products/clindamycin-stella-300-mg/) |
| THRHM050 | Metronidazole STELLA 400 mg | Metronidazol; 400 mg/viên | Viên nén / Viên | Chưa thiết lập (0) | [Thông tin sản phẩm](https://stellapharm.com/products/metronidazole-stella-400-mg/) |
| THRHM051 | Clabact 250 | Clarithromycin; 250 mg/viên | Viên nén bao phim / Viên | Chưa thiết lập (0) | [Thông tin sản phẩm](https://dhgpharma.com.vn/vi/san-pham/khang-sinh/clabact-250) |
| THRHM052 | Clabact 500 | Clarithromycin; 500 mg/viên | Viên nén bao phim / Viên | Chưa thiết lập (0) | [Thông tin sản phẩm](https://dhgpharma.com.vn/vi/san-pham/khang-sinh/clabact-500) |

Đơn vị chai/lọ/tuýp/gói/ống được giữ nguyên khi lấy giá. Riêng Betadine Mouthwash Gargle 1%: bảng giá nguồn không ghi thể tích lọ; script không suy đoán 125 ml hay 250 ml. Những dòng nguồn chỉ xác nhận dạng viên uống được giữ ở mức “Viên uống”, không tự gán viên nang/bao phim. Không hợp nhất các biệt dược theo hoạt chất đơn thuần.

## Gán nhóm cho thuốc đã có

Một truy vấn chỉ đọc đã xác nhận 10 mã dưới đây. Script yêu cầu đồng thời đúng mã, tên và hàm lượng, và chỉ cập nhật khi `MaNhomThuoc IS NULL`. Tên hoặc hàm lượng đã được người dùng sửa thì không được đoán nhóm theo mã cũ.

| Mã | Tên đã đối chiếu | Nhóm |
|---|---|---|
| TH001 | Paracetamol 500 mg | RHM_GIAM_DAU |
| TH002 | Amoxicillin 500 mg | RHM_KS_PENICILLIN |
| TH003 | Chlorhexidine 0,12% - chai 100 ml | RHM_SAT_KHUAN_MIENG |
| TH004 | Ibuprofen 200 mg | RHM_NSAID |
| TH005 | Cetirizine 10 mg | RHM_KHANG_HISTAMIN |
| TH006 | Triamcinolone acetonide 0,1% - tuýp 5 g | RHM_CORTICOID_TAI_CHO |
| TH007 | Cefalexin 500 mg | RHM_KS_CEPHALOSPORIN |
| TH008 | Natri clorid 0,9% - chai 500 ml | RHM_DUNG_DICH_RUA |
| TH009 | Povidone iodine 10% - chai 100 ml | RHM_SAT_KHUAN_NGOAI |
| TH010 | Loratadine 10 mg | RHM_KHANG_HISTAMIN |

Việc giữ NULL cho thuốc khác chưa nhận diện được là có chủ ý; quản trị cần phân loại sau khi kiểm tra nhãn thực tế.

## Hành vi của fragment và kiểm tra

- Danh mục đã ghép vào giai đoạn4 của SQL chính, sau phần tạo `THUOC`, `HANG_HOA`, `LOAI_HANG_HOA` và `NHOM_THUOC_KE_DON`. Chạy toàn bộ file SQL chính, không cần script riêng.
- Thêm cột nhóm nếu chưa có, tạo FK bằng WITH CHECK và bật kiểm tra để constraint được trusted; tạo index nhóm.
- Toàn bộ DDL và dữ liệu nằm trong một transaction; lỗi rollback. Batch động cho phép dùng cột vừa thêm mà không gặp lỗi bind ở cùng batch.
- Chỉ INSERT khi thiếu nhóm/thuốc/hàng hóa; không UPDATE tên, hoạt chất mô tả, hàm lượng, trạng thái, giá hay đơn vị của thuốc đã tồn tại. Các UPDATE chỉ điền nhóm còn NULL.
- Nếu nhận diện trùng chính xác tên + hàm lượng + đơn vị dưới mã khác, dùng bản ghi hiện hữu. Mã THRHM/HHRHM đang bị dùng với quan hệ hàng hóa khác gây lỗi rõ ràng, không gán lại danh tính kho.
- `MucTonToiThieu=0` cho thuốc mới nghĩa ngưỡng cảnh báo chưa cấu hình; không phải khai báo tồn kho bằng 0. Không có INSERT vào bảng lô, kho, nhập/xuất hoặc số dư.
- Không ghi `CAU_HINH_THUOC_KE_DON`, không thêm liều hoặc lời dặn, không tạo mẫu đơn.
- Đã kiểm SQL PARSEONLY, tích hợp/cài mới/chạy lặp bằng SQL chính, luồng kê đơn/hóa đơn/bàn giao và đồng bộ database hiện tại. Xem [KE_THUOC_TRONG_KHAM](KE_THUOC_TRONG_KHAM.md) và bằng chứng PrescriptionFlow1.

Nguồn có thể đổi sau ngày tra cứu. Khi đưa vào vận hành, dược sĩ cần đối chiếu sản phẩm/quy cách thật và giá bán nội bộ, đặc biệt các chai/tuýp mà nguồn chỉ ghi nồng độ.
