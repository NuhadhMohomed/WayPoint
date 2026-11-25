# Journey Planning Feature Module (`mobile/lib/features/journey/`)

> **Status: Active Development (WIP)**

Implements intercity journey search, route comparison, preference filtering, and AI journey recommendation UI:

- **Screens**: `JourneySearchScreen`, `JourneyComparisonScreen`, `PreferenceFilterSheet`
- **Widgets**: `AiJourneyPromptCard`, `AiInsightsBanner`, `FirstRunWelcomeCard`
- **Services & Models**: `JourneyApiService`, `journey_models.dart`

### Current WIP Notes
- Integrated with `/api/v1/journeys/search` and `/api/v1/journeys/ai-recommendation`; multi-leg transfer edge cases, saved searches, and AI recommendation UX refinements are ongoing.
