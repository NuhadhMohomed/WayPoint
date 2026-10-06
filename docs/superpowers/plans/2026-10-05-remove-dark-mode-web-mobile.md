# Remove Dark Mode from Web and Mobile Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cleanly remove the dark mode feature and all associated theme-switching controls across the React web app and Flutter mobile client, establishing an authoritative, sovereign light theme while preserving 100% test suite health.

**Architecture:** Lock `useThemeStore` (Web) and `ThemeCubit` (Mobile) to Light mode with self-healing initialization that purges stale `dark` preferences. Strip Tailwind `darkMode: 'class'`, remove theme toggles from layouts, command palettes, navigation shells, and settings screens, and pin `MaterialApp` to `ThemeMode.light` with `AppTheme.lightTheme`.

**Tech Stack:** React 18, Zustand, Tailwind CSS, Lucide React, Flutter 3.x, flutter_bloc, Material 3.

**Spec:** User directive: "Plan and remove the dark mode from both web and mobile apps".

## Global Constraints

- Never break existing component prop signatures or crash when external state references `ThemeCubit` or `useThemeStore`.
- Sanitize localStorage (`waypoint_theme`) and mobile secure storage (`waypoint_theme_mode`) so returning users with dark mode cached are safely migrated to light mode.
- Remove Tailwind CSS `darkMode: 'class'` from `web/tailwind.config.js`.
- Retain 100% green test passing bar: 36 Vitest tests in `web/` and 35 Flutter tests in `mobile/`.
- UI must remain visually cohesive with the Sri Lanka National Transit Operation Room and Sovereign Passenger design languages.

## Review Focus

1. Stale LocalStorage/SecureStorage: User previously selected dark mode -> Initialization cleans storage and removes `.dark` class from `document.documentElement`.
2. Missing Toggle Calls: Legacy code or shortcut calling `toggleDarkMode()` or `ThemeCubit.toggleTheme()` -> Safe no-op without error.
3. Command Palette: User searching for "dark" or "theme" in Ctrl+K -> Command list does not offer defunct theme switcher.
4. Settings & Shell Navigation: Passenger profile settings screen -> Shows Sovereign Light Theme without broken toggle states.
5. Production Bundle: Web Vite build and Flutter test compilation -> Clean builds with no unresolved imports or unused dark theme assets.

---

### Task 1: Web Tier — Sanitize `themeStore.js` & Tailwind Configuration

**Files:**
- Modify: `web/src/store/themeStore.js`
- Modify: `web/tailwind.config.js`

**Interfaces:**
- Consumes: Zustand `create`, DOM `document.documentElement`
- Produces: `useThemeStore` exporting `{ isDarkMode: false, toggleDarkMode: () => {}, sidebarCollapsed, toggleSidebar, soundEnabled, toggleSound }`

- [x] **Step 1: Sanitize `web/src/store/themeStore.js`**
  - Lock `isDarkMode` to `false`.
  - At module load / store creation, execute self-healing:
    ```javascript
    if (typeof document !== 'undefined') {
      document.documentElement.classList.remove('dark');
    }
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem('waypoint_theme');
    }
    ```
  - Convert `toggleDarkMode` to a safe no-op function.

- [x] **Step 2: Update `web/tailwind.config.js`**
  - Remove `darkMode: 'class'` property so Tailwind stops generating dark mode selector variants.

- [x] **Step 3: Verify Web Test Suite**
  Run: `npm test -- --run` in `web/`
  Expected: PASS (all 36 tests pass)

---

### Task 2: Web Tier — Remove Theme Toggles from UI Controls

**Files:**
- Modify: `web/src/layouts/DashboardLayout.jsx`
- Modify: `web/src/components/ui/CommandPalette.jsx`

**Interfaces:**
- Consumes: `useThemeStore` from `web/src/store/themeStore.js`
- Produces: Clean header actions without Moon/Sun toggle and Command Palette without `act-theme`

- [x] **Step 1: Clean Header in `web/src/layouts/DashboardLayout.jsx`**
  - Remove `Moon` and `Sun` icon imports from `lucide-react`.
  - Remove `isDarkMode` and `toggleDarkMode` destructuring from `useThemeStore`.
  - Remove the theme toggle `<button>` from the header navigation bar (lines 217-225).

- [x] **Step 2: Clean `web/src/components/ui/CommandPalette.jsx`**
  - Remove `{ id: 'act-theme', title: 'Toggle Dark / Light Theme', ... }` from `COMMAND_ITEMS`.
  - Remove `toggleDarkMode` from `useThemeStore` destructuring.
  - Remove `item.action === 'toggle-theme'` handling from `handleSelect`.
  - Update search placeholder example text to avoid suggesting "Theme".

- [x] **Step 3: Verify Web Test Suite and Production Build**
  Run: `npm test -- --run` in `web/`
  Run: `npm run build` in `web/`
  Expected: PASS (36 tests pass, Vite build succeeds with zero errors)

---

### Task 3: Mobile Tier — Lock MaterialApp and Sanitize `ThemeCubit` & `AppTheme`

**Files:**
- Modify: `mobile/lib/core/theme/theme_cubit.dart`
- Modify: `mobile/lib/core/theme/app_theme.dart`
- Modify: `mobile/lib/main.dart`

**Interfaces:**
- Consumes: Flutter `MaterialApp`, `ThemeCubit`, `AppTheme`
- Produces: `ThemeCubit` locked to `ThemeMode.light`, `MaterialApp` configured strictly for `ThemeMode.light` with `AppTheme.lightTheme`

- [x] **Step 1: Sanitize `mobile/lib/core/theme/theme_cubit.dart`**
  - Ensure initial state is `ThemeMode.light`.
  - In `_loadTheme()`, delete any stale `waypoint_theme_mode` key from `_storage` and emit `ThemeMode.light`.
  - Make `setTheme(ThemeMode mode)` and `toggleTheme()` enforce `emit(ThemeMode.light)`.

- [x] **Step 2: Update `mobile/lib/core/theme/app_theme.dart`**
  - Keep `lightTheme` as authoritative.
  - Make `darkTheme` return `lightTheme` (or keep as deprecated fallback to avoid breaking external imports).

- [x] **Step 3: Update `mobile/lib/main.dart`**
  - In `WayPointApp.build`, remove `darkTheme: AppTheme.darkTheme`.
  - Explicitly set `themeMode: ThemeMode.light`.

- [x] **Step 4: Verify Mobile Core Widgets**
  Run: `flutter test test/core/widgets_test.dart` in `mobile/`
  Expected: PASS

---

### Task 4: Mobile Tier — Remove Dark Mode Controls from Navigation Shell & Settings

**Files:**
- Modify: `mobile/lib/features/navigation/screens/passenger_navigation_shell.dart`
- Modify: `mobile/lib/features/settings/screens/passenger_settings_screen.dart`
- Modify: `mobile/test/features/settings_reviews_test.dart`

**Interfaces:**
- Consumes: `PassengerNavigationShell`, `PassengerSettingsScreen`
- Produces: Passenger settings UI focused on Sovereign Light Theme without defunct dark toggles

- [x] **Step 1: Remove Dark Mode Switch in `mobile/lib/features/navigation/screens/passenger_navigation_shell.dart`**
  - Remove `isDark` parameter from `_buildProfileTab(BuildContext context)`.
  - Remove the "Dark Mode" `ListTile` with `Switch` from `_buildProfileTab`.

- [x] **Step 2: Update `mobile/lib/features/settings/screens/passenger_settings_screen.dart`**
  - Replace the 3-segment button (System/Light/Dark) in `Appearance` section with an informative "Visual Theme" tile:
    - Title: "Visual Theme"
    - Subtitle: "Sovereign Transit Light Theme (Authoritative)"
    - Icon: `Icons.wb_sunny_outlined`
  - Remove dark-mode modal background branching (use `Colors.white`).

- [x] **Step 3: Update Widget Test Expectations in `mobile/test/features/settings_reviews_test.dart`**
  - Update `PassengerSettingsScreen` test: expect `find.text('Appearance')` and `find.text('Visual Theme')`.
  - Verify that `find.text('Dark')` is no longer present (`findsNothing`).

- [x] **Step 4: Verify Mobile Test Suite**
  Run: `flutter test` in `mobile/`
  Expected: PASS (all 35 tests pass)

---

### Task 5: Final Full-Stack Verification & Git Integration

**Files:**
- Repository-wide verification across `web/` and `mobile/`

- [x] **Step 1: Run Full Web Verification**
  Run: `npm test -- --run` in `web/`
  Run: `npm run build` in `web/`
  Expected: All 36 tests pass, bundle builds cleanly.

- [x] **Step 2: Run Full Mobile Verification**
  Run: `flutter test` in `mobile/`
  Expected: All 35 tests pass.

- [x] **Step 3: Git Commit**
  Stage modified files and commit with a clean, descriptive message:
  ```bash
  git add web/src/store/themeStore.js web/tailwind.config.js web/src/layouts/DashboardLayout.jsx web/src/components/ui/CommandPalette.jsx mobile/lib/core/theme/theme_cubit.dart mobile/lib/core/theme/app_theme.dart mobile/lib/main.dart mobile/lib/features/navigation/screens/passenger_navigation_shell.dart mobile/lib/features/settings/screens/passenger_settings_screen.dart mobile/test/features/settings_reviews_test.dart docs/superpowers/plans/2026-10-05-remove-dark-mode-web-mobile.md
  git commit -m "feat(ui): cleanly remove dark mode feature across web and mobile tiers"
  ```
