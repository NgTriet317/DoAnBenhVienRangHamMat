# Examination workspace UI implementation plan

**Goal:** Improve the patient preview and the examination workspace, removing the large gap before the examination action.

**Architecture:** Keep the existing WPF controls, event handlers, access checks and adaptive two-pane layout. Restyle only `KhamDieuTri`, and bind its preview to the currently selected receipt. The preview is content-sized with a single scrollable document; its action follows the information.

**Tech stack:** .NET 8, WPF XAML, existing Blue Care resources.

## Design and constraints

- Preserve the blue/white design, Vietnamese text, keyboard focus and current clinical workflow.
- Group patient identity, receipt/time, doctor/room, service and reason with readable labels.
- Give queue selection, waiting and examination statuses distinct visual states.
- Keep filters and history available, including pagination and automatic refresh.
- Keep existing narrow-window adaptation and wrap long text.
- Work in the supplied workspace to preserve the user's existing uncommitted changes; do not commit or publish.

## Approaches

1. Change only row heights: removes the gap but leaves the information difficult to scan.
2. Restyle the existing workspace and group the preview information: selected implementation, aligned with the user's requested visual scope and the existing WPF architecture.
3. Replace the clinical workspace: unnecessary workflow and regression risk for a presentation change.

## Tasks

- [x] Restyle `RangHamMat.Wpf/Views/KhamDieuTri.xaml`: page header, filters, queue/history lists, empty state and preview.
- [x] Update `RangHamMat.Wpf/Presentation/KhamDieuTri.HangCho.cs`: provide the selected receipt as preview data, format the existing text fields and handle absent values.
- [x] Build Debug/Release; run the existing structure/UI checks appropriate to this presentation change.
- [x] Render the actual WPF controls with representative data at wide/narrow sizes, short heights and long text; check the button follows the content, selection is visible and scrolling works.

## Review focus

- Correct identity when switching between waiting and in-progress receipts.
- Missing date, doctor/room/service/reason values remain understandable.
- Long patient names and clinical text wrap without horizontal clipping.
- A short window can scroll to the examination action.
- Existing history, filters, access checks and clinical editor navigation retain their handlers.

## Verification record

- Debug and Release solution builds: succeeded, zero errors. The initial full builds report existing warnings in unchanged files; the final incremental Debug build reports zero warnings.
- `tools/KiemTraBanGiao.ps1 -ChiCauTruc`: passed.
- Temporary harness `obj/ExaminationUiPreview/Program.cs`: 27 runtime checks passed using the compiled WPF controls and an isolated in-memory cache, without SQL access or persisted changes.
- Covered 1440 x 1000, 1440 x 620 and 760 x 900 windows, patient switching, waiting/in-progress actions, restricted access, long names/reasons, absent data, scrolling, refresh position, adaptive tabs and history context.
- Visual inspection caught centered list content (192 of 308 DIP used); a runtime assertion reproduced the problem, and stretching the selected tab content fixed it (308 of 308 DIP used).
- Preview action follows the reason with a 42-DIP gap, with no expanding spacer; tall-window preview sizes to content.
- Rendered screenshots: `obj/ExaminationUiPreview/screenshots/` (`wide.png`, `empty.png`, `narrow-long.png`, `narrow-missing.png`).
- `git diff --check` for all changed presentation files: passed.
- Independent scoped code review found no Critical/Important issues. Its missing-name wording observation was reproduced with a failing runtime check and corrected for doctor, room and service identifiers; all 27 checks then passed. The verification record is updated and no review findings remain outstanding.
