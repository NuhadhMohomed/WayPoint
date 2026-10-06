# Mobile Customer AI Journey Recommendations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enable passengers to submit natural language journey queries on Flutter mobile with AI-ranked recommendations and safe-failure resilience via an authoritative ASP.NET Core gateway.

**Architecture:** ASP.NET Core acts as the authoritative gateway exposing `POST /api/v1/journeys/ai-recommendation`, forwarding to the Python LangGraph microservice (`POST /api/ai/journey-recommendation`) with a 10s timeout, and falling back automatically to deterministic `JourneyPlanningService.SearchAsync`. Flutter mobile introduces an `AiJourneyPromptCard` with preset chips on the Explore screen and renders an `AiInsightsBanner` with match scores on the comparison screen.

**Tech Stack:** ASP.NET Core 9, C# 13, Flutter 3 (Dart 3), Python 3.11 (FastAPI/LangGraph), xUnit, Flutter Test.

**Spec:** [docs/superpowers/specs/2026-10-06-mobile-customer-ai-journey-recommendations-design.md](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/superpowers/specs/2026-10-06-mobile-customer-ai-journey-recommendations-design.md)

## Global Constraints

- Flutter clients must communicate exclusively through ASP.NET Core (`http://localhost:5010/api/v1`); direct calls to port 8000 or PostgreSQL are strictly prohibited (`AGENTS.md`).
- 10-second maximum timeout on Python AI microservice calls (`AI_EXECUTION_TIMEOUT_SECONDS`).
- Deterministic Safe Failure (`BR-AIVAL-002`): If the AI microservice encounters timeout or connection error, return verified deterministic candidates with `IsAiFallback: true`.
- Objective input length capped at 500 characters, whitespace-trimmed, and sanitized.
- Every task must follow strict TDD: failing test first, verification, minimal code, passing test, commit.

## Review Focus

1. Empty or whitespace-only objective string: Expect `400 Bad Request` ProblemDetails without invoking AI.
2. AI microservice unreachable (`HttpRequestException`): Expect `200 OK` with `IsAiFallback: true` and candidates populated from deterministic search.
3. AI microservice timeout ($>10$s `TaskCanceledException`): Expect `200 OK` with `IsAiFallback: true` and non-null candidates list.
4. Python microservice returns 0 candidates: Expect fallback to deterministic search for default corridor rather than an empty response screen.
5. Mobile network offline: Catch network exceptions in `JourneyApiService`, surface a non-blocking SnackBar, and fall back to local curated candidates.

---

### Task 1: Backend DTOs & Microservice Client Gateway

**Files:**
- Create: `backend/WayPoint.Application/Features/JourneyPlanning/DTOs/AiJourneyRecommendationDtos.cs`
- Create: `backend/WayPoint.Application/Features/JourneyPlanning/IAiRecommendationClient.cs`
- Create: `backend/WayPoint.Infrastructure/Services/AiRecommendationClient.cs`
- Modify: `backend/WayPoint.Infrastructure/DependencyInjection.cs:30-40`
- Test: `backend/WayPoint.Tests/AiRecommendationClientTests.cs`

**Interfaces:**
- Consumes: `IHttpClientFactory`, `IConfiguration`
- Produces: `IAiRecommendationClient.GetJourneyRecommendationAsync(string objective, CancellationToken cancellationToken) -> Task<AiWorkflowResultDto>`

- [ ] **Step 1: Write the failing unit tests for `AiRecommendationClient`**

Create `backend/WayPoint.Tests/AiRecommendationClientTests.cs`:
```csharp
[Fact]
public async Task GetJourneyRecommendationAsync_Success_ReturnsMappedWorkflowResult()
{
    var mockHandler = new MockHttpMessageHandler(@"{
        ""workflow_id"": ""e0f7f3a2-71c1-4b1f-9b2f-2d7c588e1a12"",
        ""workflow_type"": ""journey_recommendation"",
        ""status"": ""Completed"",
        ""steps_completed"": 3,
        ""candidate_routes"": [
            {
                ""service_id"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
                ""service_code"": ""SRV-COL-ELLA-0800"",
                ""route_number"": ""EX-08"",
                ""origin"": ""Colombo"",
                ""destination"": ""Ella"",
                ""departure_time"": ""2026-10-01T08:00:00"",
                ""arrival_time"": ""2026-10-01T14:30:00"",
                ""total_fare"": 2500.0,
                ""duration_minutes"": 390,
                ""is_connecting"": false,
                ""match_score"": 0.95,
                ""available_seats"": 12,
                ""bus_class"": ""SemiLuxury""
            }
        ],
        ""agent_reasoning"": ""Selected EX-08 for shortest travel time and guaranteed AC seats.""
    }");
    var client = new AiRecommendationClient(new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") });

    var result = await client.GetJourneyRecommendationAsync("Fastest bus to Ella with AC", CancellationToken.None);

    Assert.NotNull(result);
    Assert.Equal("Completed", result.Status);
    Assert.Single(result.CandidateRoutes);
    Assert.Equal("EX-08", result.CandidateRoutes[0].RouteNumber);
    Assert.Equal(0.95m, result.CandidateRoutes[0].MatchScore);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/WayPoint.Tests --filter FullyQualifiedName~AiRecommendationClientTests`
Expected: FAIL with compilation error (missing types).

- [ ] **Step 3: Implement DTOs, interface, and `AiRecommendationClient`**

1. Create `backend/WayPoint.Application/Features/JourneyPlanning/DTOs/AiJourneyRecommendationDtos.cs` with `AiJourneyRecommendationRequestDto`, `AiJourneyRecommendationResponseDto`, `AiWorkflowResultDto`, and `AiCandidateRouteDto`.
2. Create `backend/WayPoint.Application/Features/JourneyPlanning/IAiRecommendationClient.cs`.
3. Create `backend/WayPoint.Infrastructure/Services/AiRecommendationClient.cs` using `HttpClient.PostAsJsonAsync("/api/ai/journey-recommendation", new { objective })` with a 10s timeout.
4. Register typed client in `backend/WayPoint.Infrastructure/DependencyInjection.cs`:
   `services.AddHttpClient<IAiRecommendationClient, AiRecommendationClient>(client => { client.BaseAddress = new Uri(aiUrl); client.Timeout = TimeSpan.FromSeconds(10); });`

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/WayPoint.Tests --filter FullyQualifiedName~AiRecommendationClientTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add backend/WayPoint.Application/Features/JourneyPlanning/ backend/WayPoint.Infrastructure/Services/AiRecommendationClient.cs backend/WayPoint.Infrastructure/DependencyInjection.cs backend/WayPoint.Tests/AiRecommendationClientTests.cs
git commit -m "feat(backend): implement AI recommendation DTOs and microservice client"
```

---

### Task 2: Backend Controller Endpoint & Safe-Failure Fallback

**Files:**
- Modify: `backend/WayPoint.API/Controllers/JourneySearchController.cs:1-31`
- Test: `backend/WayPoint.Tests/JourneySearchControllerTests.cs`

**Interfaces:**
- Consumes: `IAiRecommendationClient`, `IJourneyPlanningService`
- Produces: `POST /api/v1/journeys/ai-recommendation` -> `ActionResult<AiJourneyRecommendationResponseDto>`

- [ ] **Step 1: Write the failing controller tests for AI recommendation & Safe Failure**

In `backend/WayPoint.Tests/JourneySearchControllerTests.cs`, add:
```csharp
[Fact]
public async Task GetAiRecommendations_ValidObjective_ReturnsAiRecommendations()
{
    var mockAi = new Mock<IAiRecommendationClient>();
    mockAi.Setup(x => x.GetJourneyRecommendationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new AiWorkflowResultDto { Status = "Completed", CandidateRoutes = new List<AiCandidateRouteDto> { new() { ServiceCode = "SRV-COL-ELLA", TotalFare = 2500 } }, AgentReasoning = "Scenic AC match" });

    var mockService = new Mock<IJourneyPlanningService>();
    var controller = new JourneySearchController(mockService.Object, mockAi.Object);

    var actionResult = await controller.GetAiRecommendations(new AiJourneyRecommendationRequestDto { Objective = "Bus to Ella" }, CancellationToken.None);

    var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
    var response = Assert.IsType<AiJourneyRecommendationResponseDto>(okResult.Value);
    Assert.False(response.IsAiFallback);
    Assert.Equal("Scenic AC match", response.AgentReasoning);
}

[Fact]
public async Task GetAiRecommendations_AiServiceThrowsException_TriggersSafeFailureFallback()
{
    var mockAi = new Mock<IAiRecommendationClient>();
    mockAi.Setup(x => x.GetJourneyRecommendationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
          .ThrowsAsync(new HttpRequestException("AI service unavailable"));

    var mockService = new Mock<IJourneyPlanningService>();
    mockService.Setup(x => x.SearchAsync(It.IsAny<JourneySearchRequestDto>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new JourneySearchResponseDto { Candidates = new List<JourneyCandidateDto> { new() { RouteNumber = "EX-08" } } });

    var controller = new JourneySearchController(mockService.Object, mockAi.Object);

    var actionResult = await controller.GetAiRecommendations(new AiJourneyRecommendationRequestDto { Objective = "Bus to Ella" }, CancellationToken.None);

    var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
    var response = Assert.IsType<AiJourneyRecommendationResponseDto>(okResult.Value);
    Assert.True(response.IsAiFallback);
    Assert.Single(response.Candidates);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/WayPoint.Tests --filter FullyQualifiedName~JourneySearchControllerTests`
Expected: FAIL with compilation error (missing method `GetAiRecommendations`).

- [ ] **Step 3: Implement `GetAiRecommendations` in `JourneySearchController.cs`**

1. Inject `IAiRecommendationClient aiClient` into `JourneySearchController`.
2. Add `[Authorize] [HttpPost("ai-recommendation")]`:
   - Validate `string.IsNullOrWhiteSpace(request.Objective)` -> `400 Bad Request`.
   - Wrap `aiClient.GetJourneyRecommendationAsync` in try-catch for `HttpRequestException`, `TaskCanceledException`, and `Exception`.
   - In catch block (Safe Failure): log warning, parse origin/destination heuristic or default corridor ("Colombo", "Ella"), invoke `journeyPlanningService.SearchAsync()`, and return `200 OK` with `IsAiFallback = true`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/WayPoint.Tests --filter FullyQualifiedName~JourneySearchControllerTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add backend/WayPoint.API/Controllers/JourneySearchController.cs backend/WayPoint.Tests/JourneySearchControllerTests.cs
git commit -m "feat(backend): add /api/v1/journeys/ai-recommendation with safe-failure fallback"
```

---

### Task 3: Mobile Service & Data Models

**Files:**
- Modify: `mobile/lib/features/journey/models/journey_models.dart:105-150`
- Modify: `mobile/lib/features/journey/services/journey_api_service.dart:1-65`
- Test: `mobile/test/features/journey/journey_api_service_test.dart`

**Interfaces:**
- Consumes: `MobileApiClient` (`Dio`)
- Produces: `JourneyApiService.getAiRecommendations({required String objective, int passengerCount}) -> Future<AiJourneyRecommendationModel>`

- [ ] **Step 1: Write failing test for `JourneyApiService.getAiRecommendations`**

Create `mobile/test/features/journey/journey_api_service_test.dart`:
```dart
test('getAiRecommendations parses response and handles fallback flag', () async {
  final dioAdapter = DioAdapterMock();
  dioAdapter.onPost('/journeys/ai-recommendation', (server) => server.reply(200, {
    'workflowId': 'e0f7f3a2-71c1-4b1f-9b2f-2d7c588e1a12',
    'status': 'Completed',
    'agentReasoning': 'Evaluated 3 routes. Selected EX-08.',
    'isAiFallback': false,
    'candidates': [
      {
        'serviceId': 'srv-1',
        'serviceCode': 'SRV-01',
        'routeNumber': 'EX-08',
        'origin': 'Colombo',
        'destination': 'Ella',
        'departureTime': '2026-10-01T08:00:00Z',
        'arrivalTime': '2026-10-01T14:30:00Z',
        'totalFare': 2500.0,
        'durationMinutes': 390,
        'isConnecting': false,
        'matchScore': 0.95,
        'availableSeats': 12,
        'busClass': 'SemiLuxury',
      }
    ]
  }));

  final service = JourneyApiService(client: MobileApiClient(dio: dioAdapter.dio));
  final result = await service.getAiRecommendations(objective: 'Fastest bus to Ella');

  expect(result.isAiFallback, isFalse);
  expect(result.agentReasoning, contains('Selected EX-08'));
  expect(result.candidates.length, equals(1));
  expect(result.candidates.first.matchScore, equals(0.95));
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `flutter test test/features/journey/journey_api_service_test.dart`
Expected: FAIL with missing method `getAiRecommendations`.

- [ ] **Step 3: Implement `AiJourneyRecommendationModel` and `getAiRecommendations`**

1. In `mobile/lib/features/journey/models/journey_models.dart`, define `AiJourneyRecommendationModel`:
   `class AiJourneyRecommendationModel { final String workflowId; final String status; final String agentReasoning; final bool isAiFallback; final List<JourneyCandidateModel> candidates; ... }`
2. In `mobile/lib/features/journey/services/journey_api_service.dart`, implement `getAiRecommendations({required String objective, int passengerCount = 1, DateTime? travelDate})`:
   Post to `/journeys/ai-recommendation`. On network exception, return offline demo candidates flagged with `isAiFallback: true`.

- [ ] **Step 4: Run test to verify it passes**

Run: `flutter test test/features/journey/journey_api_service_test.dart`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/journey/models/journey_models.dart mobile/lib/features/journey/services/journey_api_service.dart mobile/test/features/journey/journey_api_service_test.dart
git commit -m "feat(mobile): add AI journey recommendation model and service method"
```

---

### Task 4: Mobile UI — AI Prompt Card & Preset Chips on Explore Screen

**Files:**
- Create: `mobile/lib/features/journey/widgets/ai_journey_prompt_card.dart`
- Modify: `mobile/lib/features/journey/screens/journey_search_screen.dart`
- Test: `mobile/test/features/journey/ai_journey_prompt_card_test.dart`

**Interfaces:**
- Consumes: `JourneyApiService.getAiRecommendations()`
- Produces: `AiJourneyPromptCard` widget rendered above city pickers; navigates to `JourneyComparisonScreen` with AI result.

- [ ] **Step 1: Write failing widget test for `AiJourneyPromptCard`**

Create `mobile/test/features/journey/ai_journey_prompt_card_test.dart`:
```dart
testWidgets('AiJourneyPromptCard renders textfield, preset chips, and triggers search', (tester) async {
  String? submittedObjective;
  await tester.pumpWidget(MaterialApp(
    home: Scaffold(
      body: AiJourneyPromptCard(
        isSearching: false,
        onSubmit: (objective) => submittedObjective = objective,
      ),
    ),
  ));

  expect(find.text('AI Trip Planner'), findsOneWidget);
  expect(find.byType(TextField), findsOneWidget);
  expect(find.text('Scenic route to Ella'), findsOneWidget);

  // Tap preset chip
  await tester.tap(find.text('Scenic route to Ella'));
  await tester.pump();

  // Verify text field populated
  expect(find.text('Scenic route to Ella with AC'), findsOneWidget);

  // Tap Ask AI button
  await tester.tap(find.text('Ask AI'));
  await tester.pump();

  expect(submittedObjective, equals('Scenic route to Ella with AC'));
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `flutter test test/features/journey/ai_journey_prompt_card_test.dart`
Expected: FAIL (widget does not exist).

- [ ] **Step 3: Implement `AiJourneyPromptCard` and embed in `JourneySearchScreen`**

1. Create `mobile/lib/features/journey/widgets/ai_journey_prompt_card.dart`:
   - Gradient container with accent badge `✨ AI Trip Planner`.
   - `TextEditingController` connected to prompt input.
   - Horizontal `ListView` of preset chips (*"Scenic route to Ella"*, *"Fastest to Kandy"*, *"Luxury AC to Galle"*).
   - "Ask AI" button with sparkle icon and loading indicator when `isSearching == true`.
2. In `mobile/lib/features/journey/screens/journey_search_screen.dart`:
   - Add `_executeAiSearch(String objective)` calling `_apiService.getAiRecommendations(objective: objective)`.
   - Embed `AiJourneyPromptCard` right above the city selectors.
   - On completion, navigate to `JourneyComparisonScreen` passing candidates, `agentReasoning`, and `isAiFallback`.

- [ ] **Step 4: Run test to verify it passes**

Run: `flutter test test/features/journey/ai_journey_prompt_card_test.dart`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/journey/widgets/ai_journey_prompt_card.dart mobile/lib/features/journey/screens/journey_search_screen.dart mobile/test/features/journey/ai_journey_prompt_card_test.dart
git commit -m "feat(mobile): add AI journey prompt card and preset chips to explore screen"
```

---

### Task 5: Mobile UI — AI Insights Banner & Comparison Screen Integration

**Files:**
- Create: `mobile/lib/features/journey/widgets/ai_insights_banner.dart`
- Modify: `mobile/lib/features/journey/screens/journey_comparison_screen.dart`
- Test: `mobile/test/features/journey/ai_insights_banner_test.dart`

**Interfaces:**
- Consumes: `agentReasoning`, `isAiFallback`, `matchScore`
- Produces: `AiInsightsBanner` widget displayed at top of `JourneyComparisonScreen`.

- [ ] **Step 1: Write failing widget test for `AiInsightsBanner`**

Create `mobile/test/features/journey/ai_insights_banner_test.dart`:
```dart
testWidgets('AiInsightsBanner displays agent reasoning and match badge when active', (tester) async {
  await tester.pumpWidget(MaterialApp(
    home: Scaffold(
      body: AiInsightsBanner(
        agentReasoning: 'Selected EX-08 for shortest travel time and guaranteed AC seats.',
        isAiFallback: false,
      ),
    ),
  ));

  expect(find.text('AI Recommendation Reasoning'), findsOneWidget);
  expect(find.textContaining('Selected EX-08 for shortest travel time'), findsOneWidget);
  expect(find.text('AI Verified'), findsOneWidget);
});

testWidgets('AiInsightsBanner displays fallback notice when isAiFallback is true', (tester) async {
  await tester.pumpWidget(MaterialApp(
    home: Scaffold(
      body: AiInsightsBanner(
        agentReasoning: '',
        isAiFallback: true,
      ),
    ),
  ));

  expect(find.textContaining('Showing verified direct and connecting routes (AI offline)'), findsOneWidget);
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `flutter test test/features/journey/ai_insights_banner_test.dart`
Expected: FAIL (widget does not exist).

- [ ] **Step 3: Implement `AiInsightsBanner` and integrate into `JourneyComparisonScreen`**

1. Create `mobile/lib/features/journey/widgets/ai_insights_banner.dart`:
   - Surface card with primary/secondary border accent.
   - Shows sparkle icon, title, and formatted `agentReasoning`.
   - If `isAiFallback == true`, shows amber warning badge and clear fallback description.
2. In `mobile/lib/features/journey/screens/journey_comparison_screen.dart`:
   - Add optional parameters `agentReasoning` and `isAiFallback`.
   - Render `AiInsightsBanner` at the top of the candidate list when `agentReasoning != null || isAiFallback == true`.
   - Ensure `JourneyCandidateCard` displays match score badge (e.g. `95% Match`) prominently.

- [ ] **Step 4: Run test to verify it passes**

Run: `flutter test test/features/journey/ai_insights_banner_test.dart`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/journey/widgets/ai_insights_banner.dart mobile/lib/features/journey/screens/journey_comparison_screen.dart mobile/test/features/journey/ai_insights_banner_test.dart
git commit -m "feat(mobile): add AI insights banner and match scoring to comparison screen"
```

---

### Task 6: End-to-End Verification & Documentation

**Files:**
- Modify: `docs/requirements/traceability-matrix.md` (Update mapping for mobile AI recommendations)
- Test: Full backend & mobile test suites

- [ ] **Step 1: Run full backend test suite**

Run: `dotnet test backend/WayPoint.Tests`
Expected: All tests pass with zero failures.

- [ ] **Step 2: Run full mobile test suite**

Run: `flutter test mobile/test`
Expected: All tests pass with zero failures.

- [ ] **Step 3: Update traceability matrix documentation**

Update `docs/requirements/traceability-matrix.md` to link `FR-JOURNEY-004` and `FR-AI-001` to `JourneySearchController.GetAiRecommendations` and mobile `AiJourneyPromptCard`.

- [ ] **Step 4: Final commit**

```bash
git add docs/requirements/traceability-matrix.md
git commit -m "docs: update traceability matrix for customer mobile AI recommendations"
```
