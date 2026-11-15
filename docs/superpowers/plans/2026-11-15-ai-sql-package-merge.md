# AI SQL package merge implementation plan

> **For agentic workers:** Use superpowers:executing-plans to implement the tasks inline. The user has authorized updating the original installer; no deployment or application database update is requested.

**Goal:** Bring the necessary fixes from `Database/RHM_F5_AI_DONGGOI.sql` into the canonical installer without replacing newer Sprint3 workflows.

**Architecture:** Keep the existing 74-table installer and its current AI/payroll contracts. Recover installation when only recognized AI/configuration tables remain, preserving their rows. Supply the read-only doctor schedule procedure already called by the API.

**Tech Stack:** SQL Server 2016 SP1+, PowerShell, existing SQL integration checks.

**Spec:** User request in this conversation; `RangHamMat.Kiosk.Api/DoctorScheduleLookup.cs` defines the schedule procedure's parameters and two result sets.

## Global constraints

- Edit the original SQL file directly; preserve the supplied package and the newer snapshot/workflows.
- Only execute installation against new `RHM_Hospital_Test_` databases, never the application database.
- Preserve existing AI identity/configuration values on recovery and rerun.
- Doctor schedule results include active doctors/rooms/shifts, confirmed schedules, and no approved leave; return no private clinical/personnel details.

## Review focus

- Unknown user tables must prevent snapshot import.
- Partial-install recovery must preserve custom values and table object identities.
- Recovery must keep primary keys, trusted constraints, and all 74 expected tables.
- Schedule result sets must match the API names and order, including empty results.
- Schedule filters must handle invalid ranges and morning/afternoon/evening consistently without writing data.

### Task 1: Recover partial AI installation

**Files:** `tools/KiemTraDongGoiAI.ps1`, `Database/QLBenhVienRangHamMat.sql`.

- [x] Add and run an isolated partial-install regression with existing AI identity and custom annual-leave configuration; observe the missing-baseline failure.
- [x] Apply the packaged snapshot recovery rule to recognized dbo extension tables only; keep the fallback initializer consistent.
- [x] Verify successful installation and preservation of existing rows/object IDs.

### Task 2: Complete the existing API schedule contract

**Files:** `tools/KiemTraDongGoiAI.ps1`, `Database/QLBenhVienRangHamMat.sql`, `tools/MergeSqlChecks.sql`, `docs/database/HOP_NHAT_SPRINT3_AI.md`.

- [x] Run the real stored-procedure call against an installed database; observe the missing-procedure failure.
- [x] Add `AI_TraCuuLichBacSi(@TuNgay date,@DenNgay date,@TenBacSi nvarchar(100)=NULL,@Buoi nvarchar(20)=N'tat_ca')` with doctor names first, then the five shift fields consumed by the API.
- [x] Verify real result sets, name/period filters, inactive/unconfirmed/on-leave exclusions, parameter validation and rollback.
- [x] Run existing fresh-install/rerun, staff AI/payroll checks, SQL synchronization checks and solution build. Record evidence and update documentation.

## Execution record

- Inventory: all 141 named CREATE objects from the supplied package already exist in the canonical installer (186 named objects). Canonical definitions include newer clinical, fee, permission and payroll protections and must be retained.
- `TAI_KHOAN.MaTK NOT NULL` and dynamic `DonGiaCa` constraints are already present. Missing package fix: partial-install snapshot eligibility.
- API calls `AI_TraCuuLichBacSi`, absent from both SQL files; its missing definition is a concrete runtime database error.
- Task 1 complete: red failed with SQL 52270; green installed/reran with 74 tables and preserved custom values and object IDs. Unknown dbo AI table and recognized name in another schema both prevented snapshot import without changing their rows.
- Task 2 complete: missing-procedure red assertion verified; final schedule checks pass, including active-fixture wildcard checks. Existing SQL sync 32/32, clinical workflow 47/47, API integration and AI state checks pass; solution build has zero warnings/errors.
- Actual supplied package was installed then upgraded using the canonical SQL. Existing patient/record/payroll counts and sampled core/AI object IDs remained unchanged; all merge checks passed after upgrade.
- Independent read-only review found no critical/important issues; strengthened wildcard assertion placement and result-field assertions as suggested.
- User authorization covers direct edits to the requested SQL; test databases are isolated. No application database update, commit or publication was performed.
