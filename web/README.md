# WayPoint Web (Operator & Manager Workspace)

The desktop and tablet web application for **WayPoint** (**SE3090 Assignment 1**), serving fleet operators, transit dispatchers, ticketing staff, and transport managers.

Built with **React 18**, **Vite**, **Tailwind CSS**, and synchronized with the **Google Stitch Design System** (`docs/design/DESIGN.md`).

---

## 1. Directory Structure

```text
web/
├── public/                      # Static assets & icons
├── src/
│   ├── components/
│   │   ├── layout/              # Header, Sidebar, Navigation
│   │   └── ui/                  # Authoritative Design System Primitives (Button, Card, TransitBadge)
│   ├── features/                # Domain-partitioned feature screens
│   │   ├── auth/                # Login & operator authentication
│   │   ├── fleet/               # Bus inventory & seat layout designer (Student 2)
│   │   ├── routes/              # Route network & timetable editor (Student 1)
│   │   ├── bookings/            # Passenger booking management (Student 3)
│   │   └── disruptions/         # Disruption management & AI approvals (Student 4)
│   ├── services/                # Axios / fetch HTTP API client modules
│   ├── App.jsx                  # Main route switch & auth guard
│   ├── index.css                # Tailwind directives & theme definitions
│   └── main.jsx                 # React root mount
├── tailwind.config.js           # Authoritative design tokens (Lanka Blue, Sunset Amber, etc.)
├── package.json                 # Dependencies & scripts
└── vite.config.js               # Vite build configuration
```

---

## 2. Design System & Tokens (`docs/design/DESIGN.md`)

The web UI strictly enforces the authoritative color tokens and typography defined in [`docs/design/DESIGN.md`](../docs/design/DESIGN.md):

- **Primary / Lanka Blue**: `#0056D2` (`bg-primary`, `text-primary`)
- **Accent / Sunset Amber**: `#FEB300` (`bg-accent`, `text-accent`)
- **Operational / Jungle Green**: `#005312` (`bg-success`, `text-success`)
- **Critical / Crimson**: `#BA1A1A` (`bg-error`, `text-error`)
- **Typography Pairing**:
  - **Plus Jakarta Sans** for page headers, card titles, and modal dialogs (`font-heading`).
  - **Inter** for tabular operational grids, transit logs, and body text (`font-sans`).

### Shared UI Primitives (`src/components/ui/`)
All new screens must compose from the shared primitives:
- `Button`: Primary, secondary, outline, ghost, and danger variants.
- `Card`: Surface container with standard elevation, padding, and borders.
- `TransitBadge`: Authoritative badge for operational statuses (`SCHEDULED`, `DELAYED`, `CANCELLED`, `CONFIRMED`).

---

## 3. Pre-Designed Stitch Screens Reference

Reference your assigned screens in [`docs/design/stitch-screens-index.md`](../docs/design/stitch-screens-index.md):

| Screen Code | Screen Name | Assigned Student | Stitch Screen ID |
| :--- | :--- | :--- | :--- |
| **WEB-01** | Fleet Overview & Bus Inventory | Student 2 (Nuhadh) | `bcbb3d59666c42958f293cf72b6a9829` |
| **WEB-02** | Interactive Seat Layout Designer | Student 2 (Nuhadh) | `eb54f3a7fa4a40879df6aa407238249a` |
| **WEB-03** | Route Administration & Timetable Grid | Student 1 (Sethum) | `e2a8cbf4a5df48598a44c77cbbda7ebf` |
| **WEB-04** | Disruption Incident Management & AI Approval | Student 4 (Dineth) | `9356efcf44cf4c7cb17c76899479b1df` |
| **WEB-05** | Booking Search, Verification & Passenger Manifest | Student 3 (Mithila) | `a84a6fe047f34c5dbbc496a798b3f23a` |

---

## 4. Setup & Running Locally

### Step 1: Install Dependencies
```bash
npm install
```

### Step 2: Configure Environment
The web client connects to the ASP.NET Core API at `http://localhost:5010/api/v1`. This is configured in the root `.env`:
```env
VITE_API_URL=http://localhost:5010/api/v1
```

### Step 3: Start Development Server
```bash
npm run dev
```
Open `http://localhost:5173` in your browser.

### Step 4: Build for Production
```bash
npm run build
```
The compiled SPA bundle will be generated in `web/dist/` ready for Vercel deployment.
