# AI Intake Official Receipt Implementation Plan

> Superseded on 2026-11-14 by the draft-only workflow: AI saves a staff-review draft and never creates an official receipt. Do not execute this historical plan.

> **For agentic workers:** Use `superpowers:executing-plans` task-by-task. No branch changes or commits are part of this plan.

**Goal:** Let the WPF intake chat request a real, idempotent appointment receipt through an authenticated backend operation and show only server-confirmed results.

**Architecture:** Preserve the local WPF chat, prompts, JSON flow, session memory and current staff intake. Add review/edit/submit state to WPF, a typed client, authenticated API routes, and a dedicated SQL service-identity/audit/idempotency path that reuses the existing intake business operation. Do not issue a staff identity or SQL access to WPF/model.

**Tech Stack:** .NET 8 WPF, ASP.NET Core minimal API, BUS/DAL/ET, Dapper, SQL Server.

**Spec:** User request, 2026-11-14, “AI tiếp nhận được tạo phiếu khám chính thức”.

## Global Constraints

- Preserve the current `AI` branch and all pre-existing uncommitted changes.
- Keep SQL connection strings and long-lived credentials out of WPF and published JSON.
- Use the existing `PHIEU_KHAM_BENH`, intake BUS/DAL, audit and A5 view; never claim success from model output.
- Only return `created` after SQL commit; retries with the same confirmation key return the same receipt.
- Build .NET 8 projects and run SQL integration cases only when a test SQL Server is available; do not report unrun tests as passing.

## Review Focus

- A timeout after SQL commit must return the same receipt on lookup/retry.
- A race across kiosk devices must not duplicate a confirmation or queue number.
- Emergency markers and ambiguous patient matches must stop automatic creation.
- A changed review value invalidates the prior confirmation key and requires confirmation again.
- Expired or unregistered backend credentials must fail closed without exposing SQL details.

## File Map

- WPF chat: `RangHamMat.Wpf/Views/TroLyAI.xaml(.cs)`.
- WPF API client and contracts: `RangHamMat.Wpf/AI/`.
- Flow and prompt: `RangHamMat.Wpf/AI/Config/LocalIntakeFlow.json`, `PhieuCreation.json`, `RangHamMat.Wpf/AI/Prompts/TaoPhieuKham.md`.
- Backend endpoints and domain service: `RangHamMat.Kiosk.Api/`.
- Shared intake contract and business/data authorization: `ET/`, `BUS/`, `DAL/`.
- Additive SQL migration and main SQL integration: `SQL/15_AI_ServiceIntake.sql`, `Database/RHM_F5_Khong_GhiNhoPhien.sql`.
- Staff intake/A5 continuity: `RangHamMat.Wpf/Views/TiepNhanAI.xaml(.cs)` and existing A5 views.

## Tasks

### Task 1: Review and confirmation state

- Add a structured intake review model, stable confirmation key, emergency lock, edit-one-field behavior and server-result state to the WPF chat.
- Add actual confirm/edit/staff controls and receipt/A5 action. No model message can invoke the API.
- Extend flow/config/prompt without putting patient data in files.
- Build WPF and verify config files copy to output.

### Task 2: Typed authenticated API client

- Add request/response contracts and `AiIntakeApiClient` for submit, idempotency lookup, doctors and services.
- Keep endpoint-only settings in `PhieuCreation.json`; obtain short-lived backend credentials from an approved device/session mechanism, never from Groq or a checked-in secret.
- Treat timeout as unknown and look up by the same key before offering retry.
- Verify with focused tests for HTTP status mapping, key reuse and timeout lookup.

### Task 3: Service identity and transaction path

- Add a separately registered backend service identity with only `AI_TAO_PHIEU`; do not derive it from `PhienDangNhap.MaNV` or an employee account.
- Add an idempotent SQL migration for identity, confirmation mapping and AI audit fields/constraints; merge it into the main SQL build script.
- Implement a dedicated BUS/DAL entry point that rechecks the service permission, patient identity, urgency, current schedule/service/room, and writes patient/history/receipt/mapping/audit in one SQL transaction.
- Preserve ordinary staff authorization and behavior.
- Run SQL migration twice and integration cases AI-P01–P16 against a test SQL Server.

### Task 4: API routes and operational integration

- Add authenticated submit, idempotency lookup, available-doctor and service routes with bounded request schema, rate limits and safe error mapping.
- Return only committed DB-generated receipt fields.
- Ensure staff queues show AI-origin receipts and existing A5 view opens by `MaPhieu`.
- Verify API authorization, conflict and concurrency behavior against SQL Server.

### Task 5: Build and report

- Build the solution and report actual build output, executed SQL cases, unexecuted cases, changed files, migration/API/security setup and remaining limits.
- Keep all pre-existing user changes intact and leave the branch uncommitted.

## Execution record — 2026-11-14

- Added the review/edit/confirm/staff/A5 flow to the existing WPF chat and retained its session memory, token controls, Groq configuration and local intake flow. The API client uses HTTPS and a device certificate from the Windows certificate store; creation remains disabled by default.
- Added a dedicated API plus `BUS_AI_TIEPNHAN` / `DAL_AI_TIEPNHAN` operation. It uses a SQL service identity with only `AI_TAO_PHIEU`; staff `BUS_TIEPNHAN` remains employee-authorized and is not impersonated. The procedure makes patient/history/receipt/mapping/audit writes under one serializable transaction and returns the DB receipt only after commit.
- Added idempotency lookup and conflict handling, live doctor/service queries, urgency and patient-match restrictions, staff queue visibility including visible official `MaPhieu`/`SoThuTu`, existing A5 receipt lookup, and the 30,000 VND AI service surcharge in the current invoice path. Added SQL 15 and synchronized it into the canonical F5 file.
- SQL integration cases AI-P01–P16 remain unrun. A local SQL Server service is running, but integrated-auth connection attempts failed with `Cannot generate SSPI context` / target-principal errors, and no known test database credentials were supplied. The production-named database was not modified.
- Verification: `dotnet build BenhVienRangHamMat.sln --no-restore -v:quiet` succeeded with 126 warnings and 0 errors; focused `AIIntakeStateChecks` passed; changed JSON parsed, SQL migration source parity matched, and `git diff --check` passed (Git also reported expected LF-to-CRLF normalization notices). SQL migration/F5 execution, SQL schema validation, and SQL-backed receipt/concurrency tests are not verified.
