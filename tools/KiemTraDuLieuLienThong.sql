/* Doi chieu du lieu voi cac dieu kien su dung hien tai cua ung dung.
   Chi doc du lieu; @KetQua la bien bang trong phien ket noi.
   Chay tren database da cai du 64 bang bang sqlcmd -d ... -E -C -b -f65001.
   CanDoiChieu khong co nghia la duoc phep tu suy dien/sua ho so da chot.
   CanBoSung la khoang trong du lieu san dung, khong phai vi pham rang buoc.
   SaiXacDinh la lien ket cheo/trang thai trai voi nguon chung tu hien tai.
   Don da ky nhung chua thu tien chua can BAN_GIAO_THUOC; khong tao giao thuoc gia.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @KetQua TABLE
(
    QuyTac nvarchar(80) NOT NULL,
    MucDo nvarchar(30) NOT NULL,
    Bang nvarchar(128) NOT NULL,
    Ma nvarchar(200) NOT NULL,
    LyDo nvarchar(1000) NOT NULL
);

INSERT @KetQua
SELECT N'DON_THUOC_THIEU_BAN_CHOT', N'CanDoiChieu', N'CHI_TIET_DON_THUOC', c.MaCTDT,
       N'Don '+d.MaDT+N' da ky nhung thieu ten/ham luong/don vi chot; API LayBanInDon se tu choi.'
FROM dbo.DON_THUOC d JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaDT=d.MaDT
WHERE d.TrangThai=N'Đã xác nhận' AND
 (NULLIF(LTRIM(RTRIM(c.TenThuocChot)),N'') IS NULL
  OR NULLIF(LTRIM(RTRIM(c.HamLuongChot)),N'') IS NULL
  OR NULLIF(LTRIM(RTRIM(c.DonViTinhChot)),N'') IS NULL);

INSERT @KetQua
SELECT N'DUONG_DUNG_LA_HAM_LUONG', N'CanDoiChieu', N'CHI_TIET_DON_THUOC', c.MaCTDT,
       N'Don '+c.MaDT+N': DuongDung='+c.DuongDung+N'; can doi chieu chi dinh goc.'
FROM dbo.CHI_TIET_DON_THUOC c JOIN dbo.DON_THUOC d ON d.MaDT=c.MaDT
WHERE d.TrangThai=N'Đã xác nhận'
 AND (c.DuongDung LIKE N'%mg%' OR c.DuongDung LIKE N'%microgam%'
      OR c.DuongDung LIKE N'%ml%' OR c.DuongDung LIKE N'%[%]%');

INSERT @KetQua
SELECT N'LUOT_KHONG_CO_DON_HIEU_LUC', N'CanDoiChieu', N'LICH_SU_KHAM_BENH', l.MaLS,
       N'Luot da ky, KhongKeThuoc=0 nhung khong co don hieu luc; can doi chieu ly do khong ke thuoc.'
FROM dbo.LICH_SU_KHAM_BENH l
WHERE l.TrangThai=N'Đã xác nhận' AND l.KhongKeThuoc=0
 AND NOT EXISTS(SELECT 1 FROM dbo.DON_THUOC d WHERE d.MaLS=l.MaLS AND d.TrangThai=N'Đã xác nhận');

INSERT @KetQua
SELECT N'PHIEU_TRANG_THAI_CU', N'CanDoiChieu', N'PHIEU_KHAM_BENH', p.MaPhieu,
       N'Luot '+l.MaLS+N' da ky nhung phieu van o trang thai Da kham; can doi chieu thu/giao truoc chuan hoa.'
FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=p.MaPhieu
WHERE l.TrangThai=N'Đã xác nhận' AND p.TrangThai=N'Đã khám';

INSERT @KetQua
SELECT N'DON_THUOC_KHONG_CO_BAN_GIAO', N'CanDoiChieu', N'DON_THUOC', d.MaDT,
       N'Don da ky, hoa don da xac nhan va thu du nhung chua co BAN_GIAO_THUOC; can doi chieu nguon xuat.'
FROM dbo.DON_THUOC d
WHERE d.TrangThai=N'Đã xác nhận'
 AND EXISTS
 (SELECT 1 FROM dbo.HOA_DON h
  OUTER APPLY(SELECT COALESCE(SUM(CASE WHEN t.LoaiGiaoDich=N'Thu' THEN t.SoTien ELSE -t.SoTien END),0) ThuRong
              FROM dbo.THANH_TOAN t WHERE t.MaHD=h.MaHD AND t.TrangThai=N'Đã xác nhận') tt
  WHERE h.MaLS=d.MaLS AND h.TrangThai=N'Đã xác nhận' AND tt.ThuRong=h.SoTienPhaiThu)
 AND NOT EXISTS(SELECT 1 FROM dbo.BAN_GIAO_THUOC b WHERE b.MaDT=d.MaDT);

INSERT @KetQua
SELECT N'HOA_DON_QUY_TRINH_CU', N'CanDoiChieu', N'HOA_DON', h.MaHD,
       N'Luot '+h.MaLS+N', phien ban '+CONVERT(nvarchar(10),h.PhienBanQuyTrinh)
       +N', trang thai '+h.TrangThai+N'; khong tu doi sang v2 vi v2 yeu cau mot lan Thu du.'
FROM dbo.HOA_DON h
WHERE h.PhienBanQuyTrinh<>2 AND h.TaoKhiHoanTat=0 AND h.TrangThai<>N'Hủy';

INSERT @KetQua
SELECT N'XUAT_THUOC_CHUA_DOI_CHIEU_THU_DU', N'CanDoiChieu', N'PHIEU_XUAT', x.MaPX,
       N'Luot '+x.MaLS+N' da xuat ghi so nhung chua co hoa don xac nhan du so thu.'
FROM dbo.PHIEU_XUAT x
WHERE x.TrangThai=N'Đã ghi sổ' AND x.MaLS IS NOT NULL
 AND EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_XUAT c WHERE c.MaPX=x.MaPX AND c.MaCTDT IS NOT NULL)
 AND NOT EXISTS
 (
   SELECT 1 FROM dbo.HOA_DON h
   OUTER APPLY(SELECT COALESCE(SUM(CASE WHEN t.LoaiGiaoDich=N'Thu' THEN t.SoTien ELSE -t.SoTien END),0) ThuRong
               FROM dbo.THANH_TOAN t WHERE t.MaHD=h.MaHD AND t.TrangThai=N'Đã xác nhận') tt
   WHERE h.MaLS=x.MaLS AND h.TrangThai=N'Đã xác nhận' AND tt.ThuRong=h.SoTienPhaiThu
 );

INSERT @KetQua
SELECT N'BUOC_HOAN_THANH_THIEU_THUC_HIEN', N'CanDoiChieu', N'CHI_TIET_KE_HOACH', c.MaCTKH,
       N'Ke hoach '+k.MaKHDT+N', luot '+k.MaLS+N', dich vu '+c.MaDV
       +N' da hoan thanh nhung chua lien ket du luong thuc hien qua MaCTKH.'
FROM dbo.CHI_TIET_KE_HOACH c JOIN dbo.KE_HOACH_DIEU_TRI k ON k.MaKHDT=c.MaKHDT
WHERE c.TrangThai=N'Đã hoàn thành' AND c.SoLuongDuKien>
 COALESCE((SELECT SUM(s.SoLuong) FROM dbo.DICH_VU_THUC_HIEN s
           WHERE s.MaCTKH=c.MaCTKH AND s.TrangThai=N'Đã xác nhận'),0);

INSERT @KetQua
SELECT N'LUOT_DA_KY_THIEU_DV_THUC_HIEN', N'CanDoiChieu', N'LICH_SU_KHAM_BENH', l.MaLS,
       N'Luot da ky nhung khong co dich vu thuc hien da xac nhan; khong the tu tao chi phi tu ke hoach.'
FROM dbo.LICH_SU_KHAM_BENH l
WHERE l.TrangThai=N'Đã xác nhận'
 AND NOT EXISTS(SELECT 1 FROM dbo.DICH_VU_THUC_HIEN s WHERE s.MaLS=l.MaLS AND s.TrangThai=N'Đã xác nhận');

INSERT @KetQua
SELECT N'DONG_THUOC_XUAT_CHUA_VAO_HOA_DON', N'CanDoiChieu', N'CHI_TIET_PHIEU_XUAT', c.MaCTPX,
       N'Phieu '+x.MaPX+N', luot '+x.MaLS+N': chua co dong hoa don thuoc; can doi chieu chung tu goc.'
FROM dbo.CHI_TIET_PHIEU_XUAT c JOIN dbo.PHIEU_XUAT x ON x.MaPX=c.MaPX
WHERE x.TrangThai=N'Đã ghi sổ' AND x.MaLS IS NOT NULL AND c.MaCTDT IS NOT NULL
 AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON i JOIN dbo.HOA_DON h ON h.MaHD=i.MaHD
                WHERE i.MaCTPX=c.MaCTPX AND h.TrangThai<>N'Hủy');

INSERT @KetQua
SELECT N'THUOC_CHUA_CO_LO_NHAP', N'CanBoSung', N'THUOC', t.MaThuoc,
       N'Mat hang '+t.MaHH+N' dang hoat dong nhung chua co lo/nguon nhap ghi so de chon cap thuoc.'
FROM dbo.THUOC t JOIN dbo.HANG_HOA h ON h.MaHH=t.MaHH
WHERE h.TrangThai=N'Đang hoạt động' AND NOT EXISTS
 (SELECT 1 FROM dbo.CHI_TIET_PHIEU_NHAP c JOIN dbo.PHIEU_NHAP p ON p.MaPN=c.MaPN
  WHERE c.MaHH=t.MaHH AND p.TrangThai=N'Đã ghi sổ');

INSERT @KetQua
SELECT N'DICH_VU_CHUA_VAO_HOA_DON', N'CanDoiChieu', N'DICH_VU_THUC_HIEN', s.MaDVThucHien,
       N'Luot '+s.MaLS+N' da co hoa don nhung dich vu xac nhan '+s.MaDV+N' chua co dong hoa don tuong ung.'
FROM dbo.DICH_VU_THUC_HIEN s
WHERE s.TrangThai=N'Đã xác nhận'
 AND EXISTS(SELECT 1 FROM dbo.HOA_DON h WHERE h.MaLS=s.MaLS AND h.TrangThai<>N'Hủy')
 AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c JOIN dbo.HOA_DON h ON h.MaHD=c.MaHD
                WHERE c.MaDVThucHien=s.MaDVThucHien AND h.MaLS=s.MaLS AND h.TrangThai<>N'Hủy');

INSERT @KetQua
SELECT N'VAT_TU_CHUA_CO_NGUON_NHAP', N'CanBoSung', N'VAT_TU', v.MaVT,
       N'Mat hang '+v.MaHH+N' chua co nguon nhap ghi so; danh muc co nhung chua co ton de xuat.'
FROM dbo.VAT_TU v JOIN dbo.HANG_HOA h ON h.MaHH=v.MaHH
WHERE h.TrangThai=N'Đang hoạt động' AND NOT EXISTS
 (SELECT 1 FROM dbo.CHI_TIET_PHIEU_NHAP c JOIN dbo.PHIEU_NHAP p ON p.MaPN=c.MaPN
  WHERE c.MaHH=v.MaHH AND p.TrangThai=N'Đã ghi sổ');

INSERT @KetQua
SELECT N'DICH_VU_THIEU_BAC_SI_CHUYEN_KHOA', N'CanBoSung', N'DICH_VU', d.MaDV,
       N'Dich vu thuoc '+d.MaCK+N' nhung chua co bac si dang lam viec, co tai khoan hoat dong cung chuyen khoa.'
FROM dbo.DICH_VU d
WHERE d.TrangThai=N'Đang hoạt động' AND NOT EXISTS
 (SELECT 1 FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN a ON a.MaNV=n.MaNV
  WHERE n.MaCK=d.MaCK AND n.MaChucVu=N'CV002' AND n.TrangThai=N'Đang làm việc' AND a.TrangThai=N'Đang hoạt động');

INSERT @KetQua
SELECT N'LICH_LAM_VIEC_HOM_NAY', N'CanBoSung', N'CHUYEN_KHOA', n.MaCK,
       N'Chuyen khoa co bac si nhung chua co lich da xac nhan/co mat hom nay; phieu dich vu co the khong chon duoc bac si.'
FROM dbo.NHAN_VIEN n
WHERE n.MaChucVu=N'CV002' AND n.TrangThai=N'Đang làm việc' AND n.MaCK IS NOT NULL
 AND NOT EXISTS
 (SELECT 1 FROM dbo.LICH_LAM_VIEC v JOIN dbo.NHAN_VIEN bs ON bs.MaNV=v.MaNV
  WHERE bs.MaCK=n.MaCK AND bs.MaChucVu=N'CV002' AND v.NgayLam=CONVERT(date,SYSDATETIME())
    AND v.TrangThai IN(N'Đã xác nhận',N'Đang hoạt động') AND v.TrangThaiHienDien=N'Có mặt')
GROUP BY n.MaCK;

INSERT @KetQua
SELECT N'THIET_BI_THIEU_NGUON_MUA', N'CanDoiChieu', N'THIET_BI', t.MaTB,
       N'Serial '+t.SoSerial+N' co ngay mua nhung mat hang chua co chi tiet nhap; co the la tai san truoc khi dung phan mem.'
FROM dbo.THIET_BI t
WHERE t.NgayMua IS NOT NULL AND NOT EXISTS
 (SELECT 1 FROM dbo.CHI_TIET_PHIEU_NHAP c WHERE c.MaHH=t.MaHH);

INSERT @KetQua
SELECT N'LOAI_HANG_TRUNG_CHUYEN_BIET', N'CanDoiChieu', N'HANG_HOA', h.MaHH,
       N'Mot mat hang dong thoi thuoc THUOC va VAT_TU/THIET_BI; can doi chieu loai hang.'
FROM dbo.HANG_HOA h
WHERE EXISTS(SELECT 1 FROM dbo.THUOC t WHERE t.MaHH=h.MaHH)
 AND (EXISTS(SELECT 1 FROM dbo.VAT_TU v WHERE v.MaHH=h.MaHH)
      OR EXISTS(SELECT 1 FROM dbo.THIET_BI b WHERE b.MaHH=h.MaHH));

-- Khoa ngoai chi bao dam ma ton tai, khong bao dam hai chung tu cung mot lan kham.
-- Doi chieu nguon ke hoach, dich vu, don, hoa don va ban giao theo dung duong di trong DAL/BUS.
INSERT @KetQua
SELECT N'KE_HOACH_SAI_NGUON_TINH_TRANG', N'SaiXacDinh', N'KE_HOACH_DIEU_TRI', k.MaKHDT,
       N'Tinh trang nguon '+k.MaTTBNguon+N' thuoc luot khac voi '+k.MaLS+N'.'
FROM dbo.KE_HOACH_DIEU_TRI k JOIN dbo.TINH_TRANG_BENH t ON t.MaTTB=k.MaTTBNguon
WHERE k.MaLS<>t.MaLS;

INSERT @KetQua
SELECT N'DICH_VU_SAI_NGUON_KE_HOACH',
       CASE WHEN hs.MaBN<>hk.MaBN OR s.MaDV<>c.MaDV THEN N'SaiXacDinh' ELSE N'CanDoiChieu' END,
       N'DICH_VU_THUC_HIEN', s.MaDVThucHien,
       N'Luot/dich vu thuc hien khac nguon buoc ke hoach '+s.MaCTKH+N'.'
FROM dbo.DICH_VU_THUC_HIEN s JOIN dbo.CHI_TIET_KE_HOACH c ON c.MaCTKH=s.MaCTKH
JOIN dbo.KE_HOACH_DIEU_TRI k ON k.MaKHDT=c.MaKHDT
JOIN dbo.LICH_SU_KHAM_BENH ls ON ls.MaLS=s.MaLS JOIN dbo.PHIEU_KHAM_BENH ps ON ps.MaPhieu=ls.MaPhieu
JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=ps.MaHS
JOIN dbo.LICH_SU_KHAM_BENH lk ON lk.MaLS=k.MaLS JOIN dbo.PHIEU_KHAM_BENH pk ON pk.MaPhieu=lk.MaPhieu
JOIN dbo.HO_SO_BENH_AN hk ON hk.MaHS=pk.MaHS
WHERE s.MaLS<>k.MaLS OR s.MaDV<>c.MaDV;

INSERT @KetQua
SELECT N'DON_THUOC_SAI_BAC_SI', N'SaiXacDinh', N'DON_THUOC', d.MaDT,
       N'Bac si ke don '+d.MaBS+N' khac bac si phu trach luot '+l.MaLS+N': '+l.MaBS+N'.'
FROM dbo.DON_THUOC d JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=d.MaLS
WHERE d.MaBS<>l.MaBS;

INSERT @KetQua
SELECT N'DON_THAY_THE_KHAC_LUOT', N'SaiXacDinh', N'DON_THUOC', d.MaDT,
       N'Don thay the '+d.MaDTThayThe+N' phai cung luot va khong duoc tu tham chieu.'
FROM dbo.DON_THUOC d JOIN dbo.DON_THUOC cu ON cu.MaDT=d.MaDTThayThe
WHERE d.MaLS<>cu.MaLS OR d.MaDT=cu.MaDT;

INSERT @KetQua
SELECT N'LUOT_DA_CHOT_THIEU_HOAC_TRUNG_HOA_DON',
       CASE WHEN EXISTS(SELECT 1 FROM dbo.HOA_DON h WHERE h.MaLS=l.MaLS AND h.PhienBanQuyTrinh<>2 AND h.TaoKhiHoanTat=0)
            THEN N'CanDoiChieu' ELSE N'SaiXacDinh' END, N'LICH_SU_KHAM_BENH', l.MaLS,
       N'Luot da chot can dung mot hoa don chua huy de tiep tuc thu tien/tra cuu.'
FROM dbo.LICH_SU_KHAM_BENH l
WHERE l.TrangThai=N'Đã xác nhận'
 AND (SELECT COUNT(*) FROM dbo.HOA_DON h WHERE h.MaLS=l.MaLS AND h.TrangThai<>N'Hủy')<>1;

INSERT @KetQua
SELECT N'TIEP_NHAN_SAI_BENH_NHAN_LICH_HEN', N'SaiXacDinh', N'PHIEU_KHAM_BENH', p.MaPhieu,
       N'Ho so tiep nhan va lich hen '+p.MaLich+N' thuoc hai benh nhan khac nhau.'
FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=p.MaHS
JOIN dbo.LICH_KHAM l ON l.MaLich=p.MaLich
WHERE h.MaBN<>l.MaBN;

-- Ban giao chi duoc tao sau thu du; MaDT/MaHD/MaPX phai cung luot va dung luong don.
INSERT @KetQua
SELECT N'BAN_GIAO_SAI_NGUON', N'SaiXacDinh', N'BAN_GIAO_THUOC', b.MaDT,
       N'Don, hoa don va phieu xuat khong cung luot/trang thai hieu luc.'
FROM dbo.BAN_GIAO_THUOC b JOIN dbo.DON_THUOC d ON d.MaDT=b.MaDT
JOIN dbo.HOA_DON h ON h.MaHD=b.MaHD JOIN dbo.PHIEU_XUAT x ON x.MaPX=b.MaPX
WHERE d.MaLS<>h.MaLS OR x.MaLS IS NULL OR x.MaLS<>d.MaLS
 OR d.TrangThai<>N'Đã xác nhận' OR h.TrangThai<>N'Đã xác nhận' OR x.TrangThai<>N'Đã ghi sổ';

INSERT @KetQua
SELECT N'BAN_GIAO_CHUA_THU_DU', N'SaiXacDinh', N'BAN_GIAO_THUOC', b.MaDT,
       N'Hoa don '+b.MaHD+N' chua thu rong du so tien phai thu.'
FROM dbo.BAN_GIAO_THUOC b JOIN dbo.HOA_DON h ON h.MaHD=b.MaHD
OUTER APPLY(SELECT COALESCE(SUM(CASE WHEN t.LoaiGiaoDich=N'Thu' THEN t.SoTien ELSE -t.SoTien END),0) ThuRong
            FROM dbo.THANH_TOAN t WHERE t.MaHD=h.MaHD AND t.TrangThai=N'Đã xác nhận') tt
WHERE tt.ThuRong<>h.SoTienPhaiThu;

INSERT @KetQua
SELECT N'BAN_GIAO_LECH_SO_LUONG_DON', N'SaiXacDinh', N'BAN_GIAO_THUOC', b.MaDT,
       N'Phieu '+b.MaPX+N' chua xuat dung so luong tung dong don hoac co dong thuoc ngoai don.'
FROM dbo.BAN_GIAO_THUOC b
WHERE EXISTS
 (SELECT 1 FROM dbo.CHI_TIET_DON_THUOC c WHERE c.MaDT=b.MaDT AND c.SoLuong<>
  COALESCE((SELECT SUM(x.SoLuong) FROM dbo.CHI_TIET_PHIEU_XUAT x WHERE x.MaPX=b.MaPX AND x.MaCTDT=c.MaCTDT),0))
 OR EXISTS
 (SELECT 1 FROM dbo.CHI_TIET_PHIEU_XUAT x LEFT JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaCTDT=x.MaCTDT
  WHERE x.MaPX=b.MaPX AND(c.MaDT IS NULL OR c.MaDT<>b.MaDT));

-- Trang thai phieu la ket qua cua thu/giao, khong tu doi thanh Hoan tat cho dep du lieu.
INSERT @KetQua
SELECT N'PHIEU_SAI_TRANG_THAI_THANH_TOAN', N'SaiXacDinh', N'PHIEU_KHAM_BENH', p.MaPhieu,
       N'Trang thai '+p.TrangThai+N' khong khop hoa don '+h.MaHD+N' va cac don can ban giao.'
FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=p.MaPhieu
JOIN dbo.HOA_DON h ON h.MaLS=l.MaLS AND h.TrangThai<>N'Hủy'
WHERE l.TrangThai=N'Đã xác nhận' AND(h.PhienBanQuyTrinh=2 OR h.TaoKhiHoanTat=1) AND
 (h.TrangThai=N'Nháp' AND p.TrangThai<>N'Chờ thanh toán'
  OR h.TrangThai=N'Đã xác nhận' AND
   (EXISTS(SELECT 1 FROM dbo.DON_THUOC d WHERE d.MaLS=l.MaLS AND d.TrangThai=N'Đã xác nhận'
      AND NOT EXISTS(SELECT 1 FROM dbo.BAN_GIAO_THUOC b WHERE b.MaDT=d.MaDT AND b.MaHD=h.MaHD AND b.TrangThai=N'Đã giao'))
     AND p.TrangThai<>N'Chờ nhận thuốc'
    OR NOT EXISTS(SELECT 1 FROM dbo.DON_THUOC d WHERE d.MaLS=l.MaLS AND d.TrangThai=N'Đã xác nhận'
      AND NOT EXISTS(SELECT 1 FROM dbo.BAN_GIAO_THUOC b WHERE b.MaDT=d.MaDT AND b.MaHD=h.MaHD AND b.TrangThai=N'Đã giao'))
     AND p.TrangThai<>N'Hoàn tất'));

-- Lo hang phai khop mat hang, kho va nha cung cap cua chung tu nhap; FK tung cot khong du kiem nay.
INSERT @KetQua
SELECT N'NHAP_SAI_NGUON_LO', N'SaiXacDinh', N'CHI_TIET_PHIEU_NHAP', c.MaCTPN,
       N'Lo '+COALESCE(c.MaLo,N'[thieu lo]')+N' khong khop mat hang/kho/nha cung cap cua phieu '+p.MaPN+N'.'
FROM dbo.CHI_TIET_PHIEU_NHAP c JOIN dbo.PHIEU_NHAP p ON p.MaPN=c.MaPN
LEFT JOIN dbo.LO_HANG_HOA l ON l.MaLo=c.MaLo JOIN dbo.HOA_DON_NHAP h ON h.MaHDN=p.MaHDN
WHERE p.TrangThai=N'Đã ghi sổ'
 AND(l.MaLo IS NULL OR c.MaHH<>l.MaHH OR p.MaKho<>l.MaKho OR h.MaNCC<>l.MaNCC);

INSERT @KetQua
SELECT N'XUAT_SAI_NGUON_LO', N'SaiXacDinh', N'CHI_TIET_PHIEU_XUAT', c.MaCTPX,
       N'Lo '+COALESCE(c.MaLo,N'[thieu lo]')+N' khong khop mat hang/kho cua phieu '+p.MaPX+N'.'
FROM dbo.CHI_TIET_PHIEU_XUAT c JOIN dbo.PHIEU_XUAT p ON p.MaPX=c.MaPX
LEFT JOIN dbo.LO_HANG_HOA l ON l.MaLo=c.MaLo
WHERE p.TrangThai=N'Đã ghi sổ' AND(l.MaLo IS NULL OR c.MaHH<>l.MaHH OR p.MaKho<>l.MaKho);

INSERT @KetQua
SELECT N'DON_DA_KY_KHONG_CO_CHI_TIET', N'SaiXacDinh', N'DON_THUOC', d.MaDT,
       N'Don da ky phai co it nhat mot dong thuoc de in va tinh phi.'
FROM dbo.DON_THUOC d
WHERE d.TrangThai=N'Đã xác nhận' AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_DON_THUOC c WHERE c.MaDT=d.MaDT);

-- Phien ban 2 thu tron goi mot lan; hoa don 0 dong khong tao giao dich thu tien.
-- Giu rieng lich su phien ban cu vi quy trinh cu cho phep thu tung phan/hoan tien.
INSERT @KetQua
SELECT N'HOA_DON_V2_SAI_GIAO_DICH_THU', N'SaiXacDinh', N'HOA_DON', h.MaHD,
       N'Hoa don v2 da xac nhan can dung mot lan Thu du (kem tien khach dua/tien thua); hoa don 0 khong co khoan thu.'
FROM dbo.HOA_DON h
WHERE h.PhienBanQuyTrinh=2 AND h.TrangThai=N'Đã xác nhận' AND
 (h.SoTienPhaiThu=0 AND EXISTS(SELECT 1 FROM dbo.THANH_TOAN t WHERE t.MaHD=h.MaHD)
  OR h.SoTienPhaiThu>0 AND
   ((SELECT COUNT(*) FROM dbo.THANH_TOAN t WHERE t.MaHD=h.MaHD)<>1
    OR NOT EXISTS(SELECT 1 FROM dbo.THANH_TOAN t WHERE t.MaHD=h.MaHD AND t.TrangThai=N'Đã xác nhận'
      AND t.LoaiGiaoDich=N'Thu' AND t.SoTien=h.SoTienPhaiThu AND t.SoTienKhachDua IS NOT NULL AND t.TienThua IS NOT NULL)));

INSERT @KetQua
SELECT N'HOA_DON_SAI_LUOT_DICH_VU', N'SaiXacDinh', N'CHI_TIET_HOA_DON', c.MaCTHD,
       N'Dich vu '+c.MaDVThucHien+N' khac luot/trang thai/thanh tien voi hoa don '+c.MaHD+N'.'
FROM dbo.CHI_TIET_HOA_DON c JOIN dbo.HOA_DON h ON h.MaHD=c.MaHD
JOIN dbo.DICH_VU_THUC_HIEN s ON s.MaDVThucHien=c.MaDVThucHien
WHERE h.TrangThai<>N'Hủy' AND(s.MaLS<>h.MaLS OR s.TrangThai<>N'Đã xác nhận' OR s.ThanhTien<>c.ThanhTien);

INSERT @KetQua
SELECT N'HOA_DON_SAI_LUOT_THUOC', N'SaiXacDinh', N'CHI_TIET_HOA_DON', c.MaCTHD,
       N'Nguon don/xuat thuoc khong khop luot hoa don '+c.MaHD+N'.'
FROM dbo.CHI_TIET_HOA_DON c JOIN dbo.HOA_DON h ON h.MaHD=c.MaHD
LEFT JOIN dbo.CHI_TIET_PHIEU_XUAT x ON x.MaCTPX=c.MaCTPX LEFT JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX
LEFT JOIN dbo.CHI_TIET_DON_THUOC r ON r.MaCTDT=COALESCE(c.MaCTDT,x.MaCTDT)
LEFT JOIN dbo.DON_THUOC d ON d.MaDT=r.MaDT LEFT JOIN dbo.THUOC t ON t.MaThuoc=r.MaThuoc
WHERE h.TrangThai<>N'Hủy' AND(c.MaCTDT IS NOT NULL OR c.MaCTPX IS NOT NULL)
 AND(r.MaCTDT IS NULL OR d.MaLS<>h.MaLS OR d.TrangThai<>N'Đã xác nhận'
     OR c.MaCTPX IS NOT NULL AND(p.MaLS IS NULL OR p.MaLS<>h.MaLS OR p.TrangThai<>N'Đã ghi sổ' OR x.MaHH<>t.MaHH));

INSERT @KetQua
SELECT N'HOA_DON_SAI_PHIEU_PHU_THU', N'SaiXacDinh', N'CHI_TIET_HOA_DON', c.MaCTHD,
       N'Phu thu phai thuoc dung phieu dich vu cua luot va giu muc 30000 dong.'
FROM dbo.CHI_TIET_HOA_DON c JOIN dbo.HOA_DON h ON h.MaHD=c.MaHD
JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=h.MaLS
JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=c.MaPhieuPhuThu
WHERE h.TrangThai<>N'Hủy' AND(p.MaPhieu<>l.MaPhieu OR p.LoaiKham<>N'Dịch vụ' OR c.ThanhTien<>30000);

SELECT QuyTac,MucDo,COUNT(*) AS SoMuc FROM @KetQua GROUP BY QuyTac,MucDo ORDER BY MucDo,QuyTac;
SELECT QuyTac,MucDo,Bang,Ma,LyDo FROM @KetQua ORDER BY MucDo,QuyTac,Ma;
SELECT N'Chi doc du lieu; khong thuc hien chuan hoa, thu tien, xuat kho hay ban giao.' AS PhamVi;
