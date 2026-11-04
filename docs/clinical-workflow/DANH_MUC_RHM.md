# Danh mục tình trạng và dịch vụ Răng Hàm Mặt tham khảo

Ngày đối chiếu: **29/10/2026**. Danh mục nằm trong giai đoạn3 của `Database/QLBenhVienRangHamMat.sql`; không chạy SQL bổ sung.

Bổ sung **41 tình trạng/nhóm tiếp nhận, 32 dịch vụ có giá công khai và 98 liên kết gợi ý**. Cộng với 6 tình trạng và 8 dịch vụ `REF28_*`/`REF_DV_*` đã có, dữ liệu tham khảo gồm 47 tình trạng và 40 dịch vụ. Đây là số lượng của bộ dữ liệu này, không phải giả định tổng số bản ghi của database.

## Phạm vi và cách sử dụng

Danh mục dành cho bác sĩ chọn nhanh tình trạng sau khám và tham khảo các dịch vụ có thể liên quan. Có cả bệnh lý xác định, biểu hiện cần đánh giá và lần theo dõi sau điều trị; tên hiển thị giữ rõ mức độ chắc chắn. Không biến biểu hiện “đau mặt”, “mảng trắng” hay “khối u” thành chẩn đoán tự động.

Các liên kết là **phương án riêng để cân nhắc**. Chọn một tình trạng không đồng nghĩa chỉ định tất cả dịch vụ của nó. Ví dụ: cắt nang dưới 2 cm và cắt nang 2–5 cm là hai lựa chọn theo kích thước; nội nha khác nhau theo nhóm răng; sỏi ống Wharton không đồng nghĩa mọi viêm tuyến nước bọt cần lấy sỏi. Bác sĩ xác nhận tổn thương, điều kiện, phạm vi và chỉ định trước khi lập kế hoạch. Không tạo thuốc, liều dùng, người bệnh, bác sĩ, lịch sử điều trị hoặc chứng từ.

Khe hở môi/vòm miệng, dính khớp, viêm xương hàm, nghi đau dây thần kinh và khối u xương hàm giữ gợi ý khám/đánh giá thích hợp; không thêm một phẫu thuật có giá nhưng sai bệnh chỉ để mọi tình trạng có thủ thuật. Hình ảnh toàn cảnh/cận chóp/khớp là phương tiện có thể được chỉ định, không thay thế mọi khảo sát cần thiết. Tình trạng cấp cứu có gợi ý khám cấp cứu riêng, không dùng lịch hẹn thường để thay quy trình cấp cứu.

## Nguồn chính

1. [Bảng giá BVĐK Bình Định, hiệu lực 15/6/2026](https://benhvienbinhdinh.com.vn/wp-content/uploads/2026/06/BANG-GIA-BENH-VIEN-hieu-luc-ngay-15.6.2026_W.pdf), theo Quyết định **177/QĐ-CTCPBVĐKBĐ ngày 09/6/2026**. Đơn giá dưới đây lấy cột **Giá dịch vụ**; không lấy cột BHYT hoặc khám nhanh. STT cho phép đối chiếu từng dòng. Đây là giá tham khảo công khai của cơ sở nguồn, không tự đại diện giá đã phê duyệt của mọi bệnh viện.
2. [Phác đồ điều trị BV Răng Hàm Mặt TP.HCM năm 2023](https://bvranghammat.com/wp-content/uploads/2023/04/PHAC-DO-DIEU-TRI-BV-RHM-2023.pdf), các phần Nha chu, Chữa răng–Nội nha, Phẫu thuật trong miệng, Phẫu thuật hàm mặt, Tạo hình và Điều trị đặc biệt. Dùng để xác nhận phạm vi bệnh viện RHM, thuật ngữ và nguyên tắc đánh giá; phần diễn giải trong ứng dụng được viết lại ngắn, không sao chép phác đồ thuốc.
3. [BVĐK tỉnh Quảng Ninh: Dính khớp thái dương hàm](https://www.benhviendakhoatinhquangninh.vn/phac-do-dieu-tri-rang-ham-mat/dinh-khop-thai-duong-ham.1852.html), nguồn chuyên môn bệnh viện về nhóm bệnh khớp và phục hồi chức năng.
4. [NHS: Loét miệng](https://www.nhs.uk/conditions/mouth-ulcers/), [bạch sản](https://www.nhs.uk/conditions/leukoplakia/), [lichen phẳng](https://www.nhs.uk/conditions/lichen-planus/), [sỏi tuyến nước bọt](https://www.nhs.uk/conditions/salivary-gland-stones/), [đau dây thần kinh sinh ba](https://www.nhs.uk/conditions/trigeminal-neuralgia/), [dính phanh lưỡi](https://www.nhs.uk/conditions/tongue-tie/), [nhổ răng khôn](https://www.nhs.uk/tests-and-treatments/wisdom-tooth-removal/), [điều trị tủy](https://www.nhs.uk/tests-and-treatments/root-canal-treatment/). Dùng cho các mô tả tiếp nhận/chẩn đoán phân biệt và theo dõi; không lấy giá nước ngoài.

Nguồn đầy đủ được ghi trong `MoTa` từng tình trạng và dịch vụ. Tên một số dịch vụ được chuẩn hóa khoảng trắng, viết tắt GIC hoặc chính tả; STT giữ nguyên để tìm đúng dòng nguồn. Dịch vụ dẫn lưu STT 4078 có lỗi in “viêm tẩy”, được hiển thị là “viêm tấy”. Không thay phạm vi kỹ thuật, đối tượng [Nhi], [phòng khám] hoặc giới hạn kích thước.

## Nhóm tình trạng mới

Mã tình trạng đều có tiền tố `RHM_TT_`; mã dịch vụ mới trong cột cuối có tiền tố `RHM_DV_`. Bảng liệt kê các gợi ý bổ sung; viêm nha chu/hoại tử tủy còn liên kết dịch vụ `REF_DV_*` trong cùng file SQL.

| Mã tình trạng | Tên hiển thị | Nhóm | Dịch vụ để bác sĩ cân nhắc |
|---|---|---|---|
| MANDIBLE_FX | Gãy xương hàm dưới | Chấn thương | EMERGENCY, FIX_FIRST, PANORAMA, JAW_PLATE |
| MAXILLA_FX | Gãy xương hàm trên | Chấn thương | EMERGENCY, MULTITRAUMA |
| ZYGOMA_FX | Gãy xương gò má - cung tiếp | Chấn thương | EMERGENCY, ZYGOMA |
| FACIAL_WOUND | Vết thương phần mềm vùng hàm mặt | Phần mềm hàm mặt | EMERGENCY, SOFT_DEFECT |
| FOREIGN_BODY | Dị vật vùng hàm mặt | Phần mềm hàm mặt | EXAM, FOREIGN_BODY |
| TOOTH_AVULSION | Răng vĩnh viễn bật khỏi ổ do chấn thương | Chấn thương | EMERGENCY, PERIAPICAL, REIMPLANT |
| TMD | Rối loạn chức năng khớp thái dương hàm | Khớp và cơ nhai | EXAM, REHAB |
| TMJ_DISLOCATION | Trật khớp thái dương hàm | Khớp và cơ nhai | EMERGENCY, TMJ_REDUCE |
| TMJ_ANKYLOSIS | Dính khớp thái dương hàm | Khớp và cơ nhai | EXAM, TMJ_XR, REHAB |
| TRISMUS | Hạn chế há miệng sau chấn thương hoặc phẫu thuật | Khớp và cơ nhai | REEXAM, REHAB |
| ORAL_ULCER | Loét niêm mạc miệng | Niêm mạc miệng | EXAM, REEXAM |
| LEUKOPLAKIA | Tổn thương trắng niêm mạc miệng nghi bạch sản | Niêm mạc miệng | EXAM, REEXAM |
| ORAL_LICHEN | Lichen phẳng niêm mạc miệng | Niêm mạc miệng | EXAM, REEXAM |
| RANULA | Nang nhái vùng sàn miệng | Tuyến nước bọt | EXAM, FLOOR_CYST |
| SALIVARY_STONE | Sỏi tuyến nước bọt dưới hàm | Tuyến nước bọt | EXAM, WHARTON |
| SIALADENITIS | Viêm tuyến nước bọt | Tuyến nước bọt | EXAM, REEXAM |
| SALIVARY_TUMOR | Khối u tuyến nước bọt dưới hàm | Tuyến nước bọt | EXAM, SALIVARY_TUMOR |
| APICAL_ABSCESS | Áp xe quanh chóp răng | Nhiễm trùng | EMERGENCY, PERIAPICAL |
| FACIAL_CELLULITIS | Viêm tấy lan tỏa vùng hàm mặt | Nhiễm trùng | EMERGENCY, CELLULITIS |
| JAW_OSTEOMYELITIS | Viêm xương hàm | Nhiễm trùng | EXAM, PANORAMA |
| RADICULAR_CYST | Nang quanh chân răng | Nang và u | EXAM, PERIAPICAL, JAW_CYST_S, JAW_CYST_M |
| DENTIGEROUS_CYST | Nang thân răng | Nang và u | EXAM, PANORAMA, JAW_CYST_S, JAW_CYST_M |
| JAW_TUMOR | Khối u xương hàm cần đánh giá | Nang và u | EXAM, PANORAMA |
| GINGIVAL_MASS | Khối u lợi | Nang và u | EXAM, GINGIVAL_MASS |
| LIP_TUMOR | U môi lành tính | Nang và u | EXAM, LIP_TUMOR |
| VASCULAR_LESION | Tổn thương u máu - bạch mạch vùng hàm mặt | Nang và u | EXAM, VASCULAR |
| CLEFT_LIP | Khe hở môi bẩm sinh | Dị tật bẩm sinh | EXAM, REEXAM |
| CLEFT_PALATE | Khe hở vòm miệng bẩm sinh | Dị tật bẩm sinh | EXAM, REHAB |
| TONGUE_TIE | Dính phanh lưỡi | Dị tật bẩm sinh | EXAM, TONGUE_TIE |
| TRIGEMINAL_PAIN | Đau vùng mặt nghi đau dây thần kinh sinh ba | Đau và thần kinh | EXAM |
| POSTOP_PARESTHESIA | Tê môi hoặc lưỡi sau thủ thuật răng hàm mặt | Đau và thần kinh | REEXAM |
| EXTRACTION_REVIEW | Theo dõi sau nhổ răng | Sau điều trị | REEXAM, SUTURE_REMOVE |
| FIXATION_REVIEW | Theo dõi sau kết hợp xương hàm mặt | Sau điều trị | REEXAM, PANORAMA, REHAB |
| FACIAL_SCAR | Sẹo vùng hàm mặt sau chấn thương | Sau điều trị | EXAM, REHAB |
| PERIODONTITIS | Viêm nha chu | Răng và nha chu | EXAM, PERIO_CURET |
| PRIMARY_CARIES | Sâu ngà răng sữa | Răng và nha chu | EXAM, PRIMARY_GIC |
| PULP_NECROSIS | Hoại tử tủy răng vĩnh viễn | Răng và nha chu | EXAM, PERIAPICAL |
| IMPACTED_TOOTH | Răng ngầm | Răng và nha chu | EXAM, PANORAMA, IMPACTED |
| PERICORONITIS | Viêm quanh thân răng khôn hàm dưới | Răng và nha chu | EXAM, PANORAMA, WISDOM_L |
| TOOTH_WEAR | Mòn cổ răng | Răng và nha chu | EXAM, CERVICAL_COMP |
| ENDO_REVIEW | Tổn thương răng đã điều trị tủy cần đánh giá lại | Răng và nha chu | EXAM, PERIAPICAL, ENDO_REDO |

## Dịch vụ mới và giá đối chiếu

Tất cả giá là **VNĐ**, cùng ngày hiệu lực **15/6/2026**. Các ghi chú loại trừ của dòng nguồn được lưu vào `MoTa`. Giá thủ thuật/phẫu thuật không được diễn giải là trọn gói ca bệnh; phải dự toán các khoản theo thực tế và bảng giá cơ sở.

| Mã (tiền tố RHM_DV_) | Dịch vụ | Chuyên khoa | Giá dịch vụ | STT nguồn | Ghi chú dòng nguồn |
|---|---|---|---:|---:|---|
| EXAM | Khám Răng hàm mặt | CK001 | 100.000 | 107 | — |
| EMERGENCY | Khám cấp cứu - RHM | CK001 | 160.000 | 108 | — |
| REEXAM | Khám Răng hàm mặt (Lần 2) | CK001 | 160.000 | 109 | — |
| REHAB | Khám Phục hồi chức năng | CK007 | 100.000 | 113 | — |
| PERIAPICAL | Chụp Xquang răng cận chóp (Periapical) [1 phim] | CK004 | 90.000 | 850 | — |
| PANORAMA | Chụp Xquang răng toàn cảnh [1 phim] | CK004 | 100.000 | 852 | — |
| TMJ_XR | Chụp Xquang khớp thái dương hàm [số hóa 1 phim] | CK004 | 130.000 | 849 | — |
| FIX_FIRST | Cố định tạm thời sơ cứu gãy xương hàm | CK003 | 1.020.000 | 1745 | — |
| TMJ_REDUCE | Nắn sai khớp thái dương hàm | CK003 | 650.000 | 1746 | — |
| JAW_PLATE | Phẫu thuật điều trị gãy xương hàm dưới bằng nẹp vít hợp kim | CK003 | 12.230.000 | 4057 | Chưa bao gồm nẹp, vít. |
| ZYGOMA | Phẫu thuật điều trị gãy xương gò má bằng nẹp vít hợp kim | CK003 | 9.610.000 | 4060 | — |
| MULTITRAUMA | Phẫu thuật điều trị đa chấn thương vùng hàm mặt | CK003 | 14.980.000 | 4071 | Chưa bao gồm nẹp, vít. |
| FOREIGN_BODY | Phẫu thuật lấy dị vật vùng hàm mặt | CK003 | 9.880.000 | 4072 | — |
| SOFT_DEFECT | Phẫu thuật điều trị vết thương phần mềm vùng hàm mặt có thiếu hổng tổ chức | CK003 | 10.040.000 | 4074 | — |
| WHARTON | Phẫu thuật lấy sỏi ống Wharton tuyến dưới hàm | CK003 | 8.110.000 | 4076 | — |
| CELLULITIS | Phẫu thuật rạch dẫn lưu viêm tấy lan tỏa vùng hàm mặt | CK003 | 9.510.000 | 4078 | Nguồn in: viêm tẩy; chuẩn hóa chính tả hiển thị thành viêm tấy. |
| FLOOR_CYST | Cắt nang vùng sàn miệng | CK003 | 10.260.000 | 3933 | — |
| JAW_CYST_S | Cắt bỏ nang xương hàm dưới 2 cm | CK003 | 6.790.000 | 3937 | — |
| JAW_CYST_M | Cắt bỏ nang xương hàm từ 2-5 cm | CK003 | 12.260.000 | 3938 | — |
| LIP_TUMOR | Cắt u môi lành tính có tạo hình | CK003 | 9.320.000 | 3940 | — |
| GINGIVAL_MASS | Cắt u lợi đường kính dưới hoặc bằng 2 cm [phòng khám] | CK003 | 1.160.000 | 3942 | — |
| SALIVARY_TUMOR | Cắt u tuyến nước bọt dưới hàm | CK003 | 12.410.000 | 3945 | Chưa bao gồm dao siêu âm. |
| VASCULAR | Cắt u máu - bạch mạch vùng hàm mặt | CK003 | 10.640.000 | 3931 | — |
| PERIO_CURET | Phẫu thuật nạo túi lợi | CK002 | 570.000 | 3962 | — |
| IMPACTED | Phẫu thuật nhổ răng ngầm [phòng khám] | CK003 | 2.890.000 | 4017 | — |
| WISDOM_L | Phẫu thuật nhổ răng khôn mọc lệch hàm dưới [phòng khám] | CK003 | 1.730.000 | 4021 | — |
| TONGUE_TIE | Phẫu thuật cắt phanh lưỡi [phòng khám] | CK003 | 2.900.000 | 4031 | — |
| REIMPLANT | Cấy lại răng bị bật khỏi ổ răng [phòng khám] | CK003 | 1.160.000 | 4037 | — |
| PRIMARY_GIC | Điều trị răng sữa sâu ngà phục hồi bằng GIC [Nhi] | CK002 | 330.000 | 1716 | — |
| CERVICAL_COMP | Phục hồi cổ răng bằng Composite | CK002 | 450.000 | 1733 | — |
| SUTURE_REMOVE | Cắt chỉ sau phẫu thuật [Áp dụng ngoại trú] | CK007 | 130.000 | 1323 | — |
| ENDO_REDO | Điều trị tủy lại | CK002 | 2.490.000 | 4012 | — |

## Liên kết chuyên khoa và phụ thuộc database

Dùng chuyên khoa có thật trong schema dự án, không tạo nhân sự hay đơn vị giả:

| Mã | Chuyên khoa đang có | Phạm vi dịch vụ bổ sung |
|---|---|---|
| CK001 | Khám răng hàm mặt tổng quát | Khám, khám cấp cứu, khám lần 2 |
| CK002 | Điều trị bảo tồn | Nha chu, phục hồi răng sữa/cổ răng, điều trị tủy lại |
| CK003 | Phẫu thuật hàm mặt | Chấn thương, phần mềm, tuyến nước bọt, nang/u, phẫu thuật trong miệng |
| CK004 | Hình ảnh y học | Phim cận chóp, toàn cảnh, khớp thái dương hàm |
| CK007 | Phục hồi chức năng | Khám phục hồi và chăm sóc sau phẫu thuật |

Việc gán chuyên khoa là phân loại vận hành theo đơn vị sẵn có của dự án, không phải tuyên bố tổ chức của bệnh viện nguồn. Không suy ra bác sĩ nào đủ phạm vi hành nghề chỉ từ các liên kết này.

Fragment yêu cầu các bảng `DM_TINH_TRANG_BENH`, `DICH_VU`, `DICH_VU_TINH_TRANG`, `CHUYEN_KHOA` đã có; 5 chuyên khoa trên và 6 dịch vụ cũ `REF_DV_ENDO123`, `REF_DV_ENDO45`, `REF_DV_ENDO67D`, `REF_DV_ENDO67T`, `REF_DV_SCALING1`, `REF_DV_SCALING2` đã được nạp. Bảng tình trạng có trigger audit nên caller phải đặt actor hợp lệ trong `SESSION_CONTEXT(N'RHM.MaTK')` trước khi chạy.

Nội dung chỉ dùng INSERT có `NOT EXISTS`, kiểm tra trùng mã khác tên/chuyên khoa và thiếu FK; không UPDATE, DELETE, DDL, GO, USE hoặc tự mở transaction. Caller cần chạy trong transaction để toàn bộ fragment cùng thành công hoặc rollback khi kiểm tra phụ thuộc thất bại. Chạy lại giữ nguyên dữ liệu đã được người dùng điều chỉnh; không ghi đè đơn giá, mô tả hay liên kết hiện hữu.

`ThoiGianDuKien` 15–180 phút là cấu hình đặt lịch ban đầu của ứng dụng vì schema bắt buộc giá trị dương. **Các mốc phút không được công bố bởi nguồn bảng giá và không phải thời lượng lâm sàng đã kiểm chứng.** Người quản trị cần cấu hình theo quy trình cơ sở; số buổi và thời gian điều trị do bác sĩ xác định. Riêng kế hoạch điều trị không tự suy ra thời gian hay số lần từ tên tình trạng.
