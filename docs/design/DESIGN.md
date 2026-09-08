---
name: WayPoint Design System
colors:
  surface: '#f8f9fa'
  surface-dim: '#d9dadb'
  surface-bright: '#f8f9fa'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f3f4f5'
  surface-container: '#edeeef'
  surface-container-high: '#e7e8e9'
  surface-container-highest: '#e1e3e4'
  on-surface: '#191c1d'
  on-surface-variant: '#424654'
  inverse-surface: '#2e3132'
  inverse-on-surface: '#f0f1f2'
  outline: '#737785'
  outline-variant: '#c3c6d6'
  surface-tint: '#0056d2'
  primary: '#0040a1'
  on-primary: '#ffffff'
  primary-container: '#0056d2'
  on-primary-container: '#ccd8ff'
  inverse-primary: '#b2c5ff'
  secondary: '#7e5700'
  on-secondary: '#ffffff'
  secondary-container: '#feb300'
  on-secondary-container: '#6a4800'
  tertiary: '#005312'
  on-tertiary: '#ffffff'
  tertiary-container: '#1c6d24'
  on-tertiary-container: '#9aec93'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#dae2ff'
  primary-fixed-dim: '#b2c5ff'
  on-primary-fixed: '#001847'
  on-primary-fixed-variant: '#0040a1'
  secondary-fixed: '#ffdeac'
  secondary-fixed-dim: '#ffba38'
  on-secondary-fixed: '#281900'
  on-secondary-fixed-variant: '#604100'
  tertiary-fixed: '#a3f69c'
  tertiary-fixed-dim: '#88d982'
  on-tertiary-fixed: '#002204'
  on-tertiary-fixed-variant: '#005312'
  background: '#f8f9fa'
  on-background: '#191c1d'
  surface-variant: '#e1e3e4'
typography:
  headline-xl:
    fontFamily: Plus Jakarta Sans
    fontSize: 40px
    fontWeight: '700'
    lineHeight: 48px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 40px
    letterSpacing: -0.02em
  headline-lg-mobile:
    fontFamily: Plus Jakarta Sans
    fontSize: 24px
    fontWeight: '700'
    lineHeight: 32px
  headline-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 28px
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  label-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
    letterSpacing: 0.01em
  label-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 8px
  xs: 4px
  sm: 12px
  md: 16px
  lg: 24px
  xl: 32px
  gutter: 16px
  margin-mobile: 20px
  margin-desktop: 40px
---

# WayPoint Design System Specification (DESIGN.md)

This document is the authoritative design contract for **WayPoint** frontend applications (React Web Operator Workspace and Flutter Passenger Mobile Client), synchronised with the Stitch Design System (`assets/0a9e5af03d7d4795a3ce2e1cd7f5d6f9`).

---

## 1. Brand Identity & Personality

The design system is engineered to evoke reliability, efficiency, and a sense of premium hospitality for intercity travel in Sri Lanka. The brand archetype is **"The Sophisticated Navigator"** — authoritative yet welcoming, stripping away the chaos of transit to provide a calm, structured journey planning and booking experience.

The visual style follows a **Modern Corporate** aesthetic with **Vibrant Accent** influences. It prioritises high legibility and spaciousness, ensuring that even in high-stress travel environments or on moving buses, the interface remains grounded, accessible, and fast to parse.

---

## 2. Color Palette & Semantic Roles

| Token Name | Hex Code | Semantic Role | Usage Guidance |
| :--- | :--- | :--- | :--- |
| **`primary`** (Lanka Blue) | `#0040A1` / `#0056D2` | Brand Trust & Focus | Navigation headers, primary CTA buttons, active tab markers, search action buttons. |
| **`secondary-container`** (Sunset Amber) | `#FEB300` / `#FFB300` | Energy & High Visibility | 10-minute hold countdown bar, pending status, promo fares, urgent notices. |
| **`tertiary`** (Jungle Green) | `#005312` / `#1C6D24` | Safety & Success | Confirmed bookings, available seats, on-time schedules, active bus routes. |
| **`error`** | `#BA1A1A` / `#FFDAD6` | Error & Alert | Disrupted services, failed payments, expired holds, cancelled trips. |
| **`surface` / `background`** | `#F8F9FA` | Main Canvas | Clean canvas background across web and mobile. |
| **`surface-container-lowest`** | `#FFFFFF` | Card & Panel Surfaces | Elevation level 1 cards, dialogs, bottom sheets, form input containers. |
| **`on-surface`** | `#191C1D` | Primary Text | High-contrast body copy and headings. |
| **`on-surface-variant`** | `#424654` | Muted Secondary Text | Subtitles, timestamps, intermediate stop names, bus license plates. |
| **`outline`** | `#737785` | Subtle Borders | 1px clean strokes for input borders and card dividers. |

---

## 3. Typography Hierarchy

The system uses a strict dual-sans pairing:
- **Headings & Display**: **Plus Jakarta Sans** (clean, contemporary, geometric soft curves).
- **Body & Tabular Data**: **Inter** (neutral, high x-height, clear numbers for fares and timetables).

### Type Scales

| Scale | Family | Size | Weight | Line Height | Tracking | Usage |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`headline-xl`** | Plus Jakarta Sans | 40px | 700 (Bold) | 48px | -0.02em | Hero headers, marketing banners |
| **`headline-lg`** | Plus Jakarta Sans | 32px | 700 (Bold) | 40px | -0.02em | Web dashboard view titles, main modals |
| **`headline-lg-mobile`** | Plus Jakarta Sans | 24px | 700 (Bold) | 32px | Normal | Mobile screen titles, top bar headings |
| **`headline-md`** | Plus Jakarta Sans | 24px | 600 (SemiBold) | 32px | Normal | Card headings, destination pair titles |
| **`body-lg`** | Inter | 18px | 400 (Regular) | 28px | Normal | Lead paragraphs, departure/arrival stop labels |
| **`body-md`** | Inter | 16px | 400 (Regular) | 24px | Normal | Default body copy, descriptions, form input values |
| **`label-md`** | Inter | 14px | 600 (SemiBold) | 20px | +0.01em | Table headers, button labels, badge text |
| **`label-sm`** | Inter | 12px | 500 (Medium) | 16px | Normal | Form field labels, micro-timestamps, seat numbers |

---

## 4. Spacing & Grid System

- **Base Rhythm**: Strict **8px** grid system (`xs: 4px`, `sm: 12px`, `md: 16px`, `lg: 24px`, `xl: 32px`).
- **Touch Targets**: Minimum **48x48px** touch target size enforced for all mobile tap elements.
- **Desktop Grid**: 12 columns, max content width 1200px, 40px outer margins, 16px gutters.
- **Mobile Grid**: 4 columns, 20px outer margins, 16px gutters.

---

## 5. Elevation & Shadows

1. **Level 0 (Canvas)**: `#F8F9FA` base surface with no shadow.
2. **Level 1 (Cards & Modals)**: Pure white `#FFFFFF` surface with diffused shadow:
   `box-shadow: 0 4px 20px rgba(0, 0, 0, 0.05);`
3. **Level 2 (Interactive Floating Elements / CTAs)**: Elevated buttons, FABs, and active tooltips:
   `box-shadow: 0 8px 24px rgba(0, 86, 210, 0.15);`

---

## 6. Component Guidelines

### A. Buttons
- **Primary**: Lanka Blue background (`#0056D2`), white text, 8px radius, height 48px, medium/semibold Inter typography. Hover state transitions to `#0040A1`.
- **Secondary / Action**: Sunset Amber background (`#FEB300`), dark text (`#191C1D`), 8px radius.
- **Outline / Ghost**: 1px subtle stroke (`#C3C6D6`), Lanka Blue text, transparent background.

### B. Cards & Containers
- White `#FFFFFF` surface, 16px (`1rem`) border radius, soft Level 1 diffused shadow.
- Avoid harsh 1px borders; use subtle tonal contrast against the `#F8F9FA` canvas.

### C. Transit Status Chips / Badges
- 8px border radius or pill (`full: 9999px`).
- **Available**: Soft green tint (`#E8F5E9`) with dark green text (`#005312`).
- **Held**: Soft amber tint (`#FFF8E1`) with amber-brown text (`#7E5700`).
- **Booked / Full**: Soft slate tint (`#ECEFF1`) with slate text (`#455A64`).
- **Disrupted**: Soft red tint (`#FFEBEE`) with crimson text (`#BA1A1A`).

### D. Input Fields
- White `#FFFFFF` background, 8px corner radius, 1px border (`#C3C6D6`).
- On Focus: 2px solid Lanka Blue (`#0056D2`) with subtle outer glow (`rgba(0, 86, 210, 0.1)`).
- Persistent `label-sm` text placed 6px above the field.

### E. 10-Minute Seat Reservation Bar (`FR-BOOKING-001`)
- Horizontal progress bar with Sunset Amber (`#FEB300`) filling line and countdown clock.

---

## 7. Developer & Agent Implementation Rules

1. **Never Hardcode Ad-Hoc Hex Colors**: Always reference theme tokens or Tailwind color classes (`bg-waypoint-blue`, `text-waypoint-amber`, etc.).
2. **Preserve Plus Jakarta Sans + Inter Pairing**: Do not substitute generic system fonts when rendering styled headings or schedule prices.
3. **Follow Stitch Master Screen Index**: All screens must implement the layout corresponding to their entry in [`docs/design/stitch-screens-index.md`](docs/design/stitch-screens-index.md).

