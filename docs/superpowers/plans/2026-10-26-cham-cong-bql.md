# BQL Attendance Approval Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** BQL approves scheduled attendance for payroll with traceable review and edit history.
**Architecture:** WPF `ChamCong` code-behind -> `BUS_CHAMCONG` -> `DAL_CHAMCONG` -> SQL V9; DataTable/DataView like `Kho.xaml.cs`. SQL migrations add an audit log and closed-period guards; payroll remains draft-only until separate calculation is developed.
**Tech Stack:** WPF .NET 8 / C#, Microsoft.Data.SqlClient, SQL Server.
**Spec:** User-approved design on 2026-10-26: BQL enters/checks hours and approves; no employee clock in/out; no payroll closing.

## Global Constraints
- Reuse existing project conventions and login (`PhienDangNhap.IsAdmin`, `MaNV`).
- No MVVM or dictionaries for display.
- Do not auto-confirm hours or grant pay based only on schedule.
- Do not change locked payroll; trace all edits and approvals.
- Respect CA_LAM time and approved leave.

## Review Focus
1. Incomplete four-hour historical shifts remain pending/invalid, never auto-complete.
2. Future date cannot be approved; approved leave cannot be co-approved.
3. Two BQL approvals on same row cannot silently overwrite.
4. Closed-payroll month cannot have attendance or schedule edits.
5. Bulk approvals rollback as a group if any entry invalid.

### Task 1: SQL constraints and audit
**Files:** `SQL/BO_SUNG_DUYET_CHAM_CONG_V9.sql`
- [ ] Write validation for required V9 schema and verify refusal on old DB.
- [ ] Implement history table and triggers for closed-payroll updates.
- [ ] Verify SQL script parser and object references.

### Task 2: DAL/BUS attendance
**Files:** `DAL/DAL_CHAMCONG.cs`, `BUS/BUS_CHAMCONG.cs`
- [ ] Implement monthly attendance read joined schedule, role and leave.
- [ ] Implement transactional approval with SQL locks and app lock, audit, payroll-chot guard.
- [ ] Implement bulk approval revalidation, rejection reasons, and no implicit paid leave addition.
- [ ] Verify method signatures, SQL parameters, month boundaries and calculations.

### Task 3: WPF interface and navigation
**Files:** `RangHamMat.Wpf/Views/ChamCong.xaml(.cs)`, `RangHamMat.Wpf/MainWindow.xaml(.cs)`, `RangHamMat.Wpf/Views/BangLuong.xaml(.cs)`
- [ ] Build night-ink/cream attendance review XAML with month filters, attendance grid, details.
- [ ] Hook direct events in code-behind, select and approve one or many; user role checks.
- [ ] Register navigation and link from payroll screen.
- [ ] Verify XAML event names and XML, static project references.

### Task 4: packaging and handoff
**Files:** project zip, SQL migration, test notes.
- [ ] Verify all expected files, inspect zip integrity and report unavailable native WPF/SQL run if tools absent.
