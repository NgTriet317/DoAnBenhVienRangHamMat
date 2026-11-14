Independent workflow audit, 2026-11-14

Audited isolated clone `RHM_Hospital_Test_Links_20261114_7b2433bd` on `lpc:DESKTOP-C9VCE8A`.
Read product sources and clone data only. The SQL diagnostics use SELECT and session-local table variables; no database data, schema, product file, or other agent output was changed.

Result: **0 confirmed cross-table mismatches** from 25 mismatch predicates, plus one historical payroll blocker predicate. Reproducible SQL: `independent-invariants.sql`; complete successful output: `independent-invariants.log` (sqlcmd exit 0, server timestamp 2026-11-14 12:33).

1. Historical payroll blocks the October V9 workflow; preserve historical values.

   IDs: `BL001` through `BL010`, all `PhienBanTinhLuong=8`, period `2026-10-01` through `2026-10-10`. `BL003` is closed; the other nine are drafts. The installed `RHM_TinhLuongKy` contains the V8-overlap guard, confirmed from `sys.sql_modules`. Source `Database/QLBenhVienRangHamMat.sql:5020` checks V8 periods overlapping the requested month whose boundaries differ, then throws 52333. Therefore creating a full October period is blocked. This is an explicit historical compatibility safeguard, not proof that historical pay or attendance is wrong.

   Reproduction predicate (10 rows):

   ```sql
   SELECT MaBL,MaNV,TuNgay,DenNgay,TrangThai
   FROM dbo.BANG_LUONG
   WHERE PhienBanTinhLuong=8
     AND TuNgay<='20261031' AND DenNgay>='20261001'
     AND (TuNgay<>'20261001' OR DenNgay<>'20261031');
   ```

   Action for the expanded check: report `HistoricalBlocker`/informational separately from `SaiXacDinh`; do not rewrite old periods, recalculate `BL003`, or fabricate modern attendance/commission links.

2. Schema reference coverage gap, with no violating records.

   `XAC_NHAN_DOI_LICH_KHAM` has no declared FK for `MaBSCu`, `MaBSMoi` (NHAN_VIEN), `MaPhongCu`, `MaPhongMoi` (PHONG), `MaLLVThayThe` (LICH_LAM_VIEC), or `MaCa` (CA_LAM). The existing DAL's FK enumeration cannot inspect these undeclared references. The table currently has **0 rows**, and explicit NOT EXISTS predicates for all six references return **0 orphans**. Snapshot semantics should be considered before proposing constraints.

   Action: retain explicit diagnostic predicates for these six columns; absence of the FK alone is not a data violation. Other non-PK `Ma*` columns without an FK were business/snapshot identifiers (`MaBHYT`, `MaYeuCau`, `MaDoiTuong`, `MaSuKien`, `MaThamChieu`) or `MatKhauHash`, not demonstrated missing relational references.

Verified consistent:

- All 16 appointments: doctor/service specialty, doctor specialty/room department, expected duration within the shift, and active resource overlap. Current/future active appointments have matching confirmed roster shifts. No active appointment collides with approved leave or invalid backup assignments.
- All 1,102 roster rows: employee overlap, doctor room overlap, doctor specialty/room department, day/shift membership, approved leave conflicts, clock dates, employment dates, and current reference status predicates return zero.
- Confirmed `CHAM_CONG` row `CC52bcac88432e44ba9bfddf9d1cd2d6b4` -> `LLV011`: 2026-11-10 13:00–17:00, 0 late minutes, 0.5 workday, approved 17:15 by `NV001`; coherent. Confirmed replacements `PCTT006`–`PCTT010` match approved `DN001`–`DN005`, compatible substitute `NV003`, and `LLV006`–`LLV010`. Earlier canceled `PCTT001`–`PCTT005` are valid history.
- All 18 assets have source receipts matching the purchase date and supplier (listed individually in the log). Open `BTTB011` -> `TB016` correctly corresponds to `Đang thực hiện` / `Đang bảo trì`, with both start timestamps. `TB015` is disposed, unassigned from its room, has complete disposal snapshots and no open maintenance; its 2022 purchase matches `PN015`/`CTPN015`/`HDN015`.

Do not classify these as inconsistencies:

- 354 completed clock-in/out pairs await modern attendance approval; these are pending workflow, not automatically payable workdays. `LLV006`–`LLV010` belong to the preserved V8 payroll history.
- 14 shifts on 2026-11-12 have clock-in without clock-out: `LLV036`, `LLV114`, `LLV192`, `LLV270`, `LLV348`, `LLV425`, `LLV503`, `LLV581`, `LLV659`, `LLV737`, `LLV815`, `LLV893`, `LLV971`, `LLV1049`. They need attendance follow-up, not synthesized clock-out times.
- Old completed `BT001`–`BT010` lack modern confirmation/execution timestamps. `Database/QLBenhVienRangHamMat.sql:5521` explicitly preserves historical dates without inventing previously unrecorded timestamps. Their completion dates and asset states are coherent.
- Empty overtime, payroll detail, commission detail, and appointment-change confirmation tables are not errors by themselves. No V9 closed payroll exists, so modern closed-payroll source checks currently have no applicable records.
