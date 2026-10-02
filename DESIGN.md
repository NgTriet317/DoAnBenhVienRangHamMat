---
name: Răng Hàm Mặt — Blue Care
description: Friendly native WPF workspace for everyday hospital tasks.
colors:
  accent: "#2468CD"
  accent-dark: "#163C70"
  accent-soft: "#EAF2FF"
  canvas: "#F5F7FA"
  ink: "#21344B"
  muted: "#64758A"
  line: "#E2E8F0"
typography:
  home-heading: {fontFamily: Segoe UI, fontSize: 30px, fontWeight: 600}
  title: {fontFamily: Segoe UI, fontSize: 28px, fontWeight: 600}
  section: {fontFamily: Segoe UI, fontSize: 18px, fontWeight: 600}
  body: {fontFamily: Segoe UI, fontSize: 14px, fontWeight: 400}
rounded:
  button: 9px
  card: 14px
  input: 8px
  icon: 14px
spacing:
  control: 8px
  section: 20px
  card: 24px
  workspace: 32px
components:
  app-tile: {width: 180px, height: 156px, backgroundColor: White}
  text-input: {minHeight: 42px}
  table: {rowHeight: 50px, headerHeight: 48px}
---

# Design System: Blue Care

The desktop launcher stays central. The redesign replaces the dense database-sheet presentation with a welcoming Home, focused lists and separate grouped editors. Blue remains the approved accent; no purple. Native Segoe UI supports Vietnamese and familiar Windows interaction. Numeric dimensions are WPF device-independent units.

## Home

White hospital header; quiet role selector; a friendly greeting and search; wrapping category controls and 19 white app tiles. Line icons reuse the source Paths (28 DIP, 1.6 stroke, 52-DIP pale blue backplate). Tiles retain their automation names and tooltips. Search, category and role combine. The footer has a visible exit action and honest demo status.

## Lists and editors

Lists initially show around six columns. An explicit button expands or collapses secondary columns. Empty states describe what will appear and link to the correct form tab. No invented records or summary metrics.

Editors preserve every database attribute and split them into main/personal, clinical/content and management sections. Fields wrap at 270 DIP with 20-DIP gaps; cards use 24-DIP padding. The form scrolls vertically; back/Home actions stay outside the scroll area. Identity fields are read-only, foreign keys are unpopulated selectors, datetime fields include date and time, passwords only use PasswordBox. Save is disabled because no backend was requested.

## Navigation and adaptation

After login, one borderless, maximized, non-resizable window hosts every app and form. Home returns to launcher, back/Escape returns to the app list. Main Window restores to maximized; OS app switching remains possible. Login fits the work area through a down-only Viewbox. DataGrid height follows available space rather than forcing a fixed minimum. Home and editors use vertical ScrollViewers.

## Tokens and states

Named colors map to Accent, AccentDark, AccentSoft, Canvas, Ink, Muted and Line in Theme.xaml. White is a literal surface. Buttons use 42-DIP minimum height and 16,10 padding; hover, pressed, disabled and keyboard focus remain explicit. Text/Password inputs use rounded borders with an accent focus border; ComboBox and DatePicker retain platform interactions. Selected tabs have pale blue surfaces and deep blue text. No gradients, shadows or decorative animations are needed.

## Evidence

Source/XML/schema checks and C# contract compilation completed. WPF cannot run in this Linux environment, and full WPF build is not confirmed. XemTruoc/index.html is an illustrative HTML layout, not a WPF screenshot. Actual Windows/DPI inspection remains required.

## Revision: schedule, charts and contextual details

Approved changes preserve the existing blue visual system. Segmented ViewSwitch radio controls expose table/week schedule on shifts and appointments, and table/charts on reports. Calendar columns are days, rows are hours; day headers stay visible on vertical scroll. Report charts keep hospital revenue separate from warehouse receipts/payments. Empty chart states have no invented values.

RecordDetails opens only on a double-clicked data row and displays related child rows filtered to the parent key. Separate creation forms retain contextual Add actions, with parent key locked. Back/Escape follow history. Medicine is a dedicated material-type view sharing the existing schema. Audit categories are limited to access, permissions and information changes. Warehouse financial pages have explicit receipt/payment tabs.

Source verification only; native Windows rendering remains required. The previous HTML Home mockup is retained as reference and explicitly excludes these new WPF behaviors.
