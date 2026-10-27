# Đối chiếu bốn giao diện với DB_Nhom4_v7(3).docx

Ngày cập nhật: 08/10/2026. Các trường bên dưới đã được đối chiếu độc lập với bảng thuộc tính trong DOCX. Giao diện được thiết kế riêng cho bệnh viện; tên và liên kết dữ liệu dùng DB của nhóm.

## BenhNhan

| Bảng | Thuộc tính nguồn của giao diện |
|---|---|
| BENH_NHAN | MaBN, HoTenBN, GioiTinh, NgaySinh, SDT, DiaChi, NhomMau, TienSuBenh, DiUng, TrangThai |
| HO_SO_BENH_AN | MaHS, MaBN, NgayLap, LyDoKhamBanDau, GhiChu, TrangThai |

## LichKham

| Bảng | Thuộc tính nguồn của giao diện |
|---|---|
| LICH_KHAM | MaLich, MaBN, MaBS, MaCa, MaPhong, MaDV, NgayGioKham, ThoiLuongDuKien, UuTien, LyDoKham, LyDoThayDoi, TrangThai, GhiChu, NgayTao, NgayCapNhat |

## KhamDieuTri

| Bảng | Thuộc tính nguồn của giao diện |
|---|---|
| LICH_SU_KHAM_BENH | MaLS, MaPhieu, MaBS, MaPhong, ThoiGianKham, TrieuChung, ChanDoan, KeHoachXuLy, TrangThai, NgayXacNhan, GhiChu |
| TINH_TRANG_BENH | MaTTB, MaLS, LoaiTinhTrang, MoTaTinhTrang, MucDo, GhiChu |
| KE_HOACH_DIEU_TRI | MaKHDT, MaLS, MaBS, NgayLap, NgayBatDau, NgayDuKienKetThuc, TrangThai, GhiChu |
| CHI_TIET_KE_HOACH | MaCTKH, MaKHDT, MaDV, LanThu, SoLuongDuKien, DonGiaDuKien, TrangThai, GhiChu |
| DICH_VU_THUC_HIEN | MaDVThucHien, MaLS, MaCTKH, MaDV, MaNVThucHien, ThoiGianThucHien, SoLuong, DonGia, KetQua, TrangThai |

## DonThuoc

| Bảng | Thuộc tính nguồn của giao diện |
|---|---|
| DON_THUOC | MaDT, MaLS, MaBS, NgayKe, LyDoThayThe, TrangThai, GhiChu |
| CHI_TIET_DON_THUOC | MaCTDT, MaDT, MaThuoc, LieuDung, SoLuong, SoNgayDung, HuongDan |

## Những thay đổi cần lưu ý

- BENH_NHAN không còn MaBHYT. Thông tin sức khỏe dùng NhomMau, TienSuBenh và DiUng.
- LICH_KHAM có MaCa, thời lượng dự kiến, ưu tiên, lý do thay đổi, ngày tạo/cập nhật. Hẹn lại lịch đã xác nhận tạo bản mới trong phiên và lưu lý do; lịch cũ được giữ và chuyển Đã đổi.
- LICH_SU_KHAM_BENH dùng MaPhieu, MaBS và MaPhong; không lặp MaHS/MaLich. Bệnh nhân truy qua phiếu tiếp nhận → hồ sơ bệnh án.
- TINH_TRANG_BENH chỉ có loại, mô tả, mức độ và ghi chú; không có vị trí/số răng.
- KE_HOACH_DIEU_TRI và CHI_TIET_KE_HOACH dùng số lượng/đơn giá dự kiến. Không thêm ThanhTien hay MaBN lặp lại.
- DICH_VU_THUC_HIEN được đưa vào giao diện điều trị vì có trong DB mới; cho phép tham chiếu bước kế hoạch hoặc để trống khi ngoài kế hoạch.
- DON_THUOC không lưu ChanDoan; chẩn đoán hiển thị được tra từ LICH_SU_KHAM_BENH. Có LyDoThayThe và TrangThai, không có mã gốc hoặc số phiên bản.
- CHI_TIET_DON_THUOC dùng MaThuoc thay TenThuoc. Tên, hàm lượng và đơn vị hiển thị được tra qua THUOC → HANG_HOA; không tạo cột DonViTinh hoặc giá thuốc trong bảng chi tiết đơn.

Các manifest gồm 17 bảng để có đủ nguồn liên kết và lựa chọn khóa ngoại. Trong đó 10 bảng / 88 thuộc tính mô tả bốn phân hệ chính; form/thẻ chỉ hiện trường có ý nghĩa ở từng bước, khóa/trạng thái được sinh hoặc thao tác riêng. Phiếu tiếp nhận có thêm hộp thoại để hoàn thành liên kết bắt buộc trước khi lập lần khám; các bảng danh mục còn lại là nguồn tra cứu.

SQL cũ trong ZIP gốc được giữ nguyên. Manifest là hợp đồng dữ liệu giao diện, không phải script chuyển đổi database. Các phân hệ khác và ET vẫn theo source gốc.


## Luồng tiếp nhận bổ sung

PHIEU_KHAM_BENH: MaPhieu, MaHS, MaLich, MaPhong, MaBS, MaNVTiepNhan, MaDV, SoThuTu, ThoiGianTiepNhan, LyDoKham, TrangThai, GhiChu. Form này chỉ bổ sung mắt xích cần có cho luồng khám, không thay phân hệ tiếp nhận gốc. MaLich/MaBS/MaDV có thể để trống theo DB; hồ sơ/phòng/nhân viên tiếp nhận cần hợp lệ. HO_SO_BENH_AN.MaBN và LICH_SU_KHAM_BENH.MaPhieu được kiểm tra duy nhất khi lưu giao diện.

TINH_TRANG_BENH và CHI_TIET_DON_THUOC không có TrangThai; quyền sửa theo bản cha. Bản xác nhận không được sửa nội dung. Các tính năng dùng dữ liệu phiên, không có migration hoặc lưu SQL trong gói này.

## Quan hệ và cách tiếp nhận của bản cập nhật

| Nội dung | Khóa/liên kết theo DB | Cách dùng trên giao diện |
|---|---|---|
| Bệnh nhân → bệnh án | HO_SO_BENH_AN.MaBN, duy nhất | Tạo bệnh nhân có thể mở ngay form bệnh án |
| Trực tiếp → tiếp nhận | PHIEU_KHAM_BENH.MaHS; MaLich trống | Tiếp nhận trực tiếp từ hồ sơ |
| Điện thoại → tiếp nhận | LICH_KHAM.MaBN; PHIEU_KHAM_BENH.MaLich | Đặt hẹn trước, tiếp nhận theo lịch khi đến viện |
| Tiếp nhận → khám | LICH_SU_KHAM_BENH.MaPhieu, duy nhất | Một tiếp nhận chỉ có một lần khám |
| Khám → tình trạng/kế hoạch/đơn | MaLS của mỗi bảng con | Lịch sử chọn đúng lần khám; các tab lọc theo khóa |
| Kế hoạch → bước | CHI_TIET_KE_HOACH.MaKHDT | Chọn kế hoạch rồi xem/thêm bước |
| Bước → thực hiện | DICH_VU_THUC_HIEN.MaCTKH tùy chọn; MaLS là lần khám thực tế | Thực hiện có thể ở lần khám khác của cùng bệnh nhân; MaDV phải khớp bước |
| Khám → đơn → dòng thuốc | DON_THUOC.MaLS → CHI_TIET_DON_THUOC.MaDT | Đơn có liên kết ngược về bệnh nhân/lần khám |

Không thêm trường loại bệnh nhân/loại phiếu khám. Cách tiếp nhận được suy ra từ việc lượt tiếp nhận có liên kết lịch hẹn hay không. Không thêm khóa ngoại đơn thuốc → kế hoạch vì DB không có quan hệ đó. Các kế hoạch của cùng bệnh nhân được hiển thị với nguồn lần khám rõ ràng để có thể thực hiện tiếp ở lần khám sau.
