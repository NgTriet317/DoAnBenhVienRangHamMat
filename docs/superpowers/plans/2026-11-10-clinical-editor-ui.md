# Clinical editor interface upgrade

**Goal:** Improve the screen used after starting a patient examination: clear patient identity, readable clinical forms, recognizable steps and accessible actions.

**Approach:** Restyle the existing WPF editor and keep its seven tabs, indices, named controls, bindings and handlers. Use a scoped resource dictionary, numbered step tabs, a compact identity/safety header, responsive examination fields, consistent clinical cards and a persistent footer. A small native `ClinicalFieldsPanel` arranges fields side by side at 620 DIP or wider and stacks them below that width. Hide the queue filter bar while an examination occupies the detail host to give the editor more height.

**Alternatives considered:** Cosmetic colors alone would leave the dense header and long forms. Replacing the clinical workflow would add unnecessary behavioral risk. This change builds on the existing controls and Blue Care palette.

**Constraints:** Preserve all earlier uncommitted changes; edit the supplied workspace; no commits, external publishing, database changes or medical content changes. Keep allergy/history information visible, preserve read-only/dirty-state rules, and make narrow/short screens usable.

## Tasks

- [x] Add editor-scoped styles in `RangHamMat.Wpf/Themes/ClinicalEditorStyles.xaml`.
- [x] Rework `RangHamMat.Wpf/Views/KhamLamSang.xaml`, retaining every existing control name, binding and event.
- [x] Separate patient name and demographics in the current header population code; give the active editor more space by collapsing queue filters.
- [x] Add `Controls/ClinicalWorkflow/ClinicalFieldsPanel.cs` so examination inputs remain readable on narrow screens.
- [x] Compile Debug/Release and run the existing structure checks.
- [x] Render the production XAML in WPF at representative sizes, verify tab/field/footer layouts and preserve the original event/control inventory. If SQL is unavailable, report that clinical persistence was not exercised.
- [x] Review the scoped diff and screenshots; address any material findings.

## Review focus

- Long patient names, allergy/history notes and examination text wrap without hiding safety information.
- Step indices remain compatible with the existing execution, prescription and completion logic.
- Keyboard focus, editable tables, disabled states and inline errors remain functional.
- Footer actions stay reachable at narrow/short sizes; long tables can scroll.
- Switching back to the queue restores its filters.

## Verification

- Solution Debug/Release builds succeeded with zero errors. Release reports existing warnings in unrelated files; incremental Debug reports zero warnings.
- Existing structure check: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBanGiao.ps1 -ChiCauTruc` passed.
- Temporary `obj/ClinicalEditorUiPreview/Program.cs`: 55 layout checks passed at 1024 x 840, 650 x 620, 440 x 820 and in the full 1440 x 1000 queue workspace.
- Layout rendering parses production XAML with clinical event attributes removed and uses the compiled WPF styles/panel. It does not exercise clinical save operations. Exact comparisons with the turn-start snapshots confirm preservation of every original named screen control, event connection and clinical binding.
- Checked seven tab positions, editable keyboard focus, long-text wrapping/internal scroll, two-column and stacked fields, visible safety notes, fixed footer, empty-footer collapse, no-prescription reason visibility and validation feedback.
- The queue-filter checks instantiate the compiled `KhamDieuTri` with an isolated empty cache. Filters collapse on an occupied detail host, the editor recovers their height, and filters return when the host is cleared.
- The previous queue harness also passed all 27 runtime checks against the updated application.
- Independent scoped review found no Critical/Important issues. Its narrow-field usability observation was reproduced by a failing layout assertion and addressed by `ClinicalFieldsPanel`; all 55 checks then passed. No review findings remain outstanding.
- Visual inspection: `obj/ClinicalEditorUiPreview/screenshots/`, including `examination-workspace.png`, `examination-wide.png`, `examination-440.png` and per-step captures.
- SQL persistence was not exercised: a local SQL Server connection probe failed with an encryption/credential error. No database data was modified.
