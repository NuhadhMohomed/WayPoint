import { chromium } from 'playwright';
import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const projectRoot = path.resolve(__dirname, '../..');
const mobileScreenshotDir = path.join(projectRoot, 'screenshots', '06_mobile_passenger_screens');

if (!fs.existsSync(mobileScreenshotDir)) {
  fs.mkdirSync(mobileScreenshotDir, { recursive: true });
}

const screens = [
  {
    name: '01_mobile_passenger_auth.png',
    url: 'http://localhost:8080/#/auth',
    description: 'MOB-01: Passenger Auth (Login & Register)'
  },
  {
    name: '02_mobile_journey_search.png',
    url: 'http://localhost:8080/#/journey-search',
    description: 'MOB-02/03: Journey Planner Search & Corridors'
  },
  {
    name: '03_mobile_journey_comparison.png',
    url: 'http://localhost:8080/#/journey-compare',
    description: 'MOB-04: Journey Comparison & Safe Transfer Buffer'
  },
  {
    name: '04_mobile_seat_picker.png',
    url: 'http://localhost:8080/#/seat-picker',
    description: 'MOB-05: Interactive 2D Seat Picker Matrix'
  },
  {
    name: '05_mobile_payment_checkout.png',
    url: 'http://localhost:8080/#/checkout',
    description: 'MOB-06: Payment Checkout & Card Sandbox Presets'
  },
  {
    name: '06_mobile_ticket_wallet.png',
    url: 'http://localhost:8080/#/wallet',
    description: 'MOB-07: Digital Ticket Wallet & QR Boarding Pass'
  },
  {
    name: '07_mobile_booking_history.png',
    url: 'http://localhost:8080/#/booking-history',
    description: 'MOB-08: Booking History & Refund Management'
  },
  {
    name: '08_mobile_disruption_alert_screen.png',
    url: 'http://localhost:8080/#/disruption-alert',
    description: 'MOB-09: Push Disruption Alert & AI Remedy'
  },
  {
    name: '09_mobile_conductor_scanner.png',
    url: 'http://localhost:8080/#/conductor-scanner',
    description: 'MOB-10: Conductor QR Boarding Scanner'
  },
  {
    name: '10_mobile_conductor_manifest.png',
    url: 'http://localhost:8080/#/conductor-manifest',
    description: 'MOB-11: Conductor Passenger Manifest Roster'
  },
  {
    name: '11_mobile_review_submission.png',
    url: 'http://localhost:8080/#/review-bus',
    description: 'SCR-FLEET-100: Post-Journey Bus Review Submission'
  },
  {
    name: '12_mobile_main_hub_navigation.png',
    url: 'http://localhost:8080/#/',
    description: 'Mobile Bottom Navigation Shell'
  }
];

async function captureMobile() {
  console.log('--- Starting Playwright Flutter Mobile Client Capture ---');
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 393, height: 852 },
    deviceScaleFactor: 2,
    isMobile: true,
    hasTouch: true
  });
  const page = await context.newPage();

  // First visit to allow Flutter engine to fully initialize
  console.log('Loading Flutter app on http://localhost:8080/#/ ...');
  await page.goto('http://localhost:8080/#/', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(4000);

  for (const s of screens) {
    console.log(`Navigating to ${s.description} -> ${s.url} ...`);
    await page.goto(s.url, { waitUntil: 'domcontentloaded' });
    // Allow animation & rendering to settle
    await page.waitForTimeout(2000);

    const outPath = path.join(mobileScreenshotDir, s.name);
    await page.screenshot({ path: outPath });
    console.log(`Saved ${s.name} (${s.description})`);
  }

  await browser.close();
  console.log('--- Flutter Mobile Screens Capture Completed Successfully! ---');
}

captureMobile().catch(err => {
  console.error('Error during mobile capture:', err);
  process.exit(1);
});
