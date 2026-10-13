-- MaCa là NVARCHAR, không phải IDENTITY.
-- Chỉ chạy nếu CA_THEO_NGAY chưa tồn tại.
-- Đổi độ dài 50 nếu CA_LAM.MaCa của bạn dùng độ dài khác.
IF OBJECT_ID('dbo.CA_THEO_NGAY', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CA_THEO_NGAY
    (
        NgayLam DATE NOT NULL,
        MaCa NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_CA_THEO_NGAY PRIMARY KEY (NgayLam, MaCa),
        CONSTRAINT FK_CA_THEO_NGAY_CA_LAM
            FOREIGN KEY (MaCa) REFERENCES dbo.CA_LAM(MaCa)
    );
END;

-- Nếu đã có lịch phân công trước đây, thêm ca đó vào danh sách cài đặt.
INSERT INTO dbo.CA_THEO_NGAY (NgayLam, MaCa)
SELECT DISTINCT CAST(l.NgayLam AS DATE), l.MaCa
FROM dbo.LICH_LAM_VIEC l
INNER JOIN dbo.CA_LAM c ON c.MaCa = l.MaCa
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.CA_THEO_NGAY x
    WHERE x.NgayLam = CAST(l.NgayLam AS DATE) AND x.MaCa = l.MaCa
);
