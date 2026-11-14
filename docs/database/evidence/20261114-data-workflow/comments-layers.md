Comment-only update in BUS/, DAL/, ET/ on 2026-11-14.

Reviewed `docs/HUONG_DAN_CHU_THICH_CODE.md` and inventoried all 216 authored C# files: BUS 64, DAL 80, ET 72. Generated bin/obj sources are excluded. Added Vietnamese XML summaries and selected contract, return and transaction notes consistent with the project. Existing explanatory comments and trivial forwarding helpers were retained without blanket annotation.

Changed 28 source files: BUS 9, DAL 12, ET 7. Added 143 documentation blocks / 530 comment lines. The list below is relative to `D:/Hoc Tap/ChuyenDe/Nhom4_QuanLyBenhVienRHM`.

| Changed path | Added blocks | Added lines |
| --- | ---: | ---: |
| `BUS/BUS_ANHXQUANG.cs` | 10 | 41 |
| `BUS/BUS_BAOCAO.cs` | 8 | 27 |
| `BUS/BUS_DANHMUC.cs` | 3 | 16 |
| `BUS/BUS_DOISOATDULIEU.cs` | 1 | 3 |
| `BUS/BUS_NHATKY.cs` | 8 | 28 |
| `BUS/BUS_TIEPNHAN.cs` | 2 | 6 |
| `BUS/CauHinhKhoAnh.cs` | 1 | 4 |
| `BUS/KhoLuuAnhXquang.cs` | 8 | 32 |
| `BUS/SoSanhThongKe.cs` | 3 | 16 |
| `DAL/DAL_ANHXQUANG.cs` | 18 | 68 |
| `DAL/DAL_ANHXQUANG.PhanCong.cs` | 3 | 10 |
| `DAL/DAL_BAOCAO.BenhLy.cs` | 3 | 14 |
| `DAL/DAL_BAOCAO.cs` | 2 | 7 |
| `DAL/DAL_BAOCAO.TaiChinh.cs` | 2 | 7 |
| `DAL/DAL_BAOCAO.TongHop.cs` | 11 | 39 |
| `DAL/DAL_DANHMUC.cs` | 4 | 20 |
| `DAL/DAL_DOISOATDULIEU.cs` | 4 | 16 |
| `DAL/DAL_DOISOATDULIEU.LamSang.cs` | 2 | 6 |
| `DAL/DAL_DOISOATDULIEU.TaiChinhKho.cs` | 3 | 10 |
| `DAL/DAL_NHATKY.cs` | 10 | 38 |
| `DAL/DAL_TIEPNHAN.cs` | 2 | 6 |
| `ET/ET_DOISOATDULIEU.cs` | 3 | 9 |
| `ET/ET_DULIEUANHXQUANG.cs` | 9 | 27 |
| `ET/ET_DULIEUDANHMUCVANHATKY.cs` | 7 | 24 |
| `ET/ET_PHANCONGANHXQUANG.cs` | 1 | 3 |
| `ET/ET_THONGKE.cs` | 11 | 35 |
| `ET/HanhDongNhatKy.cs` | 2 | 6 |
| `ET/QuyTacMaTinhTrang.cs` | 2 | 12 |

All 9 previously comment-free files now have purpose comments and relevant method documentation:

- `BUS/BUS_DANHMUC.cs`
- `BUS/BUS_NHATKY.cs`
- `BUS/SoSanhThongKe.cs`
- `DAL/DAL_ANHXQUANG.PhanCong.cs`
- `DAL/DAL_BAOCAO.TaiChinh.cs`
- `DAL/DAL_DOISOATDULIEU.LamSang.cs`
- `DAL/DAL_DOISOATDULIEU.TaiChinhKho.cs`
- `ET/ET_DULIEUANHXQUANG.cs`
- `ET/ET_PHANCONGANHXQUANG.cs`

After this update, 216/216 authored files have comments; 0 remain entirely uncommented. The inventory still lists declarations lacking XML summaries when existing ordinary comments already explain their purpose, or when the declaration is a trivial constructor/helper. This is intentional.

The baseline was captured from the live working tree during this task, before any source changes. It includes the user's existing uncommitted changes. It does not use the old Oct30 baseline.

- `comments-baseline/<BUS|DAL|ET>/<path>`: original bytes for all 216 authored C# files.
- `snapshot.json`: the same original bytes as base64.
- `comments-after/<BUS|DAL|ET>/<path>`: after bytes for the 28 edited files.
- `inventory-before.json`, `inventory.json`: before/after Roslyn inventories.
- `verification.json`, `verification.log`: per-file comment-line and token comparison evidence.
- `evidence.json`: coverage, source hashes, protected-file hashes and build result.
- `Program.cs`, `edits.json`, `CommentAudit.dll`: the local auditing helper and exact documentation manifest.

Verification passed with 0 failures. Roslyn compares each C# token's raw kind and exact text, including SQL and other string contents, against the task-specific baseline. All 18,648 tokens in the 28 edited files are unchanged. An independent line comparison requires every original line to survive verbatim and every inserted line to start with a comment marker. Strict UTF-8 validation, BOM checks, existing newline preservation, unchanged syntax diagnostics and absence of new XML-documentation syntax warnings also passed. The remaining 188 source files are byte-identical to the captured baseline.

The user's three protected files are byte-identical to this task's baseline:

| Path | SHA-256 before and after |
| --- | --- |
| `DAL/DAL_KHAMDIEUTRI.PhanTrang.cs` | `3E47A48E3D28E2FB93B083F5130549E7A35DFCFD147BDBDE173449B36AA03D2C` |
| `DAL/dbconnect.cs` | `D14B230CCE8A4FEB0AE32A0129787542B80AD180074039C74DE5B2DDFCB1BE0C` |
| `ET/ET_DULIEUHOSOBENHAN.cs` | `4750B134E8AE3173DB71CCC89CE0DE3FA27B4CABA64E42E59B7C48CEA60CE7E9` |

Commands completed successfully:

```powershell
dotnet BUS/obj/CommentAudit_20261114/CommentAudit.dll verify .
dotnet BUS/obj/CommentAudit_20261114/CommentAudit.dll inventory .
dotnet build BUS/BUS.csproj -c Release --no-restore -v minimal -p:NuGetAudit=false -p:WarningLevel=0
git diff --check -- BUS DAL ET
```

Release build passed for ET, DAL and BUS. No test suite was added or needed for this documentation-only update.

No source files outside BUS/, DAL/, ET/ were edited. XAML semantic and PowerShell token comparisons are not applicable to this work because no XAML or PowerShell files were changed. Parent-owned `tools/KiemTraDuLieuLienThong.sql` was not edited. No out-of-scope findings were identified during the scoped review; unrelated working-tree changes belong to the user or concurrent agents. No commits were created.
