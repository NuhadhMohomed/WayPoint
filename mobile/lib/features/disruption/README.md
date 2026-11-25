# Disruption & Rebooking Feature Module (`mobile/lib/features/disruption/`)

> **Status: Active Development (WIP)**

Implements passenger-facing disruption alerts and AI-assisted rebooking acceptance flows:

- **Screens & Widgets**: `DisruptionAlertScreen`, `DisruptionAlertCard`
- **Services & Models**: `DisruptionApiService`, `DisruptionService`, `disruption_models.dart`

### Current WIP Notes
- Rebooking proposal retrieval and passenger consent actions are wired to the backend rebooking API; real-time push notifications and live background refresh are still in progress.
