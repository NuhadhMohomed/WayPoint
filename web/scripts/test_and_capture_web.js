import { chromium } from 'playwright';
import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const projectRoot = path.resolve(__dirname, '../..');
const screenshotRoot = path.join(projectRoot, 'screenshots');

const dirs = [
  '01_auth_and_overview',
  '02_component_1_journey_and_routes',
  '03_component_2_fleet_and_resources',
  '04_component_3_booking_and_operator',
  '05_component_4_disruptions_and_ai',
  '06_mobile_passenger_screens'
];

dirs.forEach(d => {
  const dirPath = path.join(screenshotRoot, d);
  if (!fs.existsSync(dirPath)) {
    fs.mkdirSync(dirPath, { recursive: true });
  }
});

async function run() {
  console.log('--- Starting Playwright Full System Integration & Screenshot Capture ---');
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 2
  });
  const page = await context.newPage();

  // ----------------------------------------------------
  // FOLDER 1: 01_auth_and_overview
  // ----------------------------------------------------
  console.log('Testing 01_auth_and_overview...');
  await page.goto('http://localhost:5173/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);
  await page.screenshot({ path: path.join(screenshotRoot, '01_auth_and_overview', '01_login_screen.png') });
  console.log('Saved 01_login_screen.png');

  // Trigger login error state
  await page.fill('input[type="email"]', 'admin@waypoint.lk');
  await page.fill('input[type="password"]', 'WrongPassword999!');
  await page.click('button[type="submit"]');
  await page.waitForTimeout(1200);
  await page.screenshot({ path: path.join(screenshotRoot, '01_auth_and_overview', '02_login_error_state.png') });
  console.log('Saved 02_login_error_state.png');

  // Register Screen
  await page.goto('http://localhost:5173/register', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);
  await page.screenshot({ path: path.join(screenshotRoot, '01_auth_and_overview', '03_register_screen.png') });
  console.log('Saved 03_register_screen.png');

  // Successful Login
  await page.goto('http://localhost:5173/login', { waitUntil: 'networkidle' });
  await page.fill('input[type="email"]', 'admin@waypoint.lk');
  await page.fill('input[type="password"]', 'Password123!');
  await page.click('button[type="submit"]');
  await page.waitForURL('http://localhost:5173/', { timeout: 10000 });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '01_auth_and_overview', '04_overview_dashboard_admin.png') });
  console.log('Saved 04_overview_dashboard_admin.png');

  // ----------------------------------------------------
  // FOLDER 2: 02_component_1_journey_and_routes (Sethum)
  // ----------------------------------------------------
  console.log('Testing 02_component_1_journey_and_routes...');
  await page.goto('http://localhost:5173/routes/catalog', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '02_component_1_journey_and_routes', '01_route_catalog_manager.png') });
  console.log('Saved 01_route_catalog_manager.png');

  // If there's an add route button / modal
  const addRouteBtn = await page.$('button:has-text("Add Route"), button:has-text("New Route"), button:has-text("Create Route")');
  if (addRouteBtn) {
    await addRouteBtn.click();
    await page.waitForTimeout(800);
    await page.screenshot({ path: path.join(screenshotRoot, '02_component_1_journey_and_routes', '02_route_add_modal.png') });
    console.log('Saved 02_route_add_modal.png');
    // close modal if needed
    const closeBtn = await page.$('button:has-text("Cancel"), button[aria-label="Close"]');
    if (closeBtn) await closeBtn.click();
  } else {
    // If no modal, capture catalog filter state
    await page.screenshot({ path: path.join(screenshotRoot, '02_component_1_journey_and_routes', '02_route_filter_view.png') });
  }

  await page.goto('http://localhost:5173/routes/scheduler', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '02_component_1_journey_and_routes', '03_service_scheduler.png') });
  console.log('Saved 03_service_scheduler.png');

  await page.goto('http://localhost:5173/routes/corridors', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '02_component_1_journey_and_routes', '04_tourist_corridors_buffer.png') });
  console.log('Saved 04_tourist_corridors_buffer.png');

  // ----------------------------------------------------
  // FOLDER 3: 03_component_2_fleet_and_resources (Nuhadh)
  // ----------------------------------------------------
  console.log('Testing 03_component_2_fleet_and_resources...');
  await page.goto('http://localhost:5173/fleet/buses', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '03_component_2_fleet_and_resources', '01_fleet_matrix_builder.png') });
  console.log('Saved 01_fleet_matrix_builder.png');

  await page.goto('http://localhost:5173/fleet/layouts', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '03_component_2_fleet_and_resources', '02_seat_layout_designer_interactive.png') });
  console.log('Saved 02_seat_layout_designer_interactive.png');

  await page.goto('http://localhost:5173/fleet/drivers', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '03_component_2_fleet_and_resources', '03_driver_rostering_rest_rules.png') });
  console.log('Saved 03_driver_rostering_rest_rules.png');

  await page.goto('http://localhost:5173/fleet/reviews', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '03_component_2_fleet_and_resources', '04_fleet_reviews_dashboard.png') });
  console.log('Saved 04_fleet_reviews_dashboard.png');

  // ----------------------------------------------------
  // FOLDER 4: 04_component_3_booking_and_operator (Mithila)
  // ----------------------------------------------------
  console.log('Testing 04_component_3_booking_and_operator...');
  await page.goto('http://localhost:5173/operator', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '04_component_3_booking_and_operator', '01_operator_dashboard_terminal.png') });
  console.log('Saved 01_operator_dashboard_terminal.png');

  await page.goto('http://localhost:5173/manifest', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '04_component_3_booking_and_operator', '02_booking_manifest_monitor.png') });
  console.log('Saved 02_booking_manifest_monitor.png');

  // Test search in manifest
  const searchInput = await page.$('input[placeholder*="Search"], input[type="search"], input[type="text"]');
  if (searchInput) {
    await searchInput.fill('Nimal');
    await page.waitForTimeout(600);
    await page.screenshot({ path: path.join(screenshotRoot, '04_component_3_booking_and_operator', '03_manifest_search_filter.png') });
    console.log('Saved 03_manifest_search_filter.png');
  }

  // Check sandbox preset cards if available
  const sandboxCard = await page.$('.bg-amber-950, .border-amber-500, button:has-text("4000 0000 0000 0001")');
  if (sandboxCard) {
    await page.screenshot({ path: path.join(screenshotRoot, '04_component_3_booking_and_operator', '04_payment_sandbox_controls.png') });
    console.log('Saved 04_payment_sandbox_controls.png');
  }

  // ----------------------------------------------------
  // FOLDER 5: 05_component_4_disruptions_and_ai (Dineth)
  // ----------------------------------------------------
  console.log('Testing 05_component_4_disruptions_and_ai...');
  await page.goto('http://localhost:5173/disruptions/intake', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '05_component_4_disruptions_and_ai', '01_disruption_intake_form.png') });
  console.log('Saved 01_disruption_intake_form.png');

  await page.goto('http://localhost:5173/disruptions/approvals', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '05_component_4_disruptions_and_ai', '02_manager_approval_workbench.png') });
  console.log('Saved 02_manager_approval_workbench.png');

  await page.goto('http://localhost:5173/disruptions/alerts', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '05_component_4_disruptions_and_ai', '03_service_alert_broadcast.png') });
  console.log('Saved 03_service_alert_broadcast.png');

  await page.goto('http://localhost:5173/disruptions/ai-traces', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '05_component_4_disruptions_and_ai', '04_ai_observability_traces.png') });
  console.log('Saved 04_ai_observability_traces.png');

  await page.goto('http://localhost:5173/disruptions/admin', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(screenshotRoot, '05_component_4_disruptions_and_ai', '05_admin_console_audit_integrity.png') });
  console.log('Saved 05_admin_console_audit_integrity.png');

  await browser.close();
  console.log('--- Web Frontend Testing & Screenshot Capture Completed Successfully! ---');
}

run().catch(err => {
  console.error('Error during capture:', err);
  process.exit(1);
});
