import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/journey/models/journey_models.dart';
import 'package:waypoint_mobile/features/journey/screens/journey_comparison_screen.dart';
import 'package:waypoint_mobile/features/journey/screens/journey_search_screen.dart';

void main() {
  testWidgets('JourneySearchScreen renders search inputs and scenic corridor chips', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: JourneySearchScreen(),
        ),
      ),
    );

    // Verify title and search elements
    expect(find.text('Explore Corridors'), findsOneWidget);
    expect(find.byIcon(Icons.swap_vert), findsOneWidget);
    expect(find.text('Search Buses'), findsOneWidget);
  });

  testWidgets('JourneyComparisonScreen renders candidates and transfer buffer indicator', (WidgetTester tester) async {
    final direct = JourneyCandidateModel.sampleColomboToEllaDirect();
    final connecting = JourneyCandidateModel.sampleConnectingViaKandy();

    await tester.pumpWidget(
      MaterialApp(
        home: JourneyComparisonScreen(
          originCity: 'Colombo',
          destinationCity: 'Ella',
          travelDate: DateTime.now().add(const Duration(days: 1)),
          candidates: [direct, connecting],
        ),
      ),
    );

    // Verify corridor header
    expect(find.text('Colombo → Ella'), findsOneWidget);

    // Verify candidate service codes & routes
    expect(find.text('EX-08'), findsOneWidget);
    expect(find.text('RT-01 + EX-08'), findsOneWidget);

    // Verify Direct vs Connecting badges
    expect(find.text('Direct Express'), findsOneWidget);
    expect(find.text('Connecting'), findsOneWidget);

    // Verify BR-TRANSFER-001 transfer window indicator
    expect(find.textContaining('Safe 35m transfer window'), findsOneWidget);

    // Verify CTA buttons
    expect(find.text('Pick Seats'), findsNWidgets(2));
  });

  testWidgets('JourneySearchScreen renders AI Trip Planner card with natural language prompts and presets', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: JourneySearchScreen(),
        ),
      ),
    );

    // Verify AI Trip Planner header
    expect(find.text('AI Trip Planner'), findsOneWidget);
    expect(find.text('Natural Language Search'), findsOneWidget);

    // Verify prompt presets
    expect(find.text('Scenic route to Ella'), findsOneWidget);
    expect(find.text('Fastest to Kandy'), findsOneWidget);
    expect(find.text('Luxury to Galle'), findsOneWidget);
    expect(find.text('Comfort & legroom'), findsOneWidget);

    // Verify Ask AI submit button
    expect(find.text('Ask AI'), findsOneWidget);
  });

  testWidgets('JourneyComparisonScreen renders AI Insights Banner with match scores and agent reasoning', (WidgetTester tester) async {
    final direct = JourneyCandidateModel.sampleColomboToEllaDirect();

    await tester.pumpWidget(
      MaterialApp(
        home: JourneyComparisonScreen(
          originCity: 'Colombo',
          destinationCity: 'Ella',
          travelDate: DateTime.now().add(const Duration(days: 1)),
          candidates: [direct],
          agentReasoning: 'Selected EX-08 Express due to superior comfort, morning departure, and 95% corridor match.',
          isAiFallback: false,
        ),
      ),
    );

    // Verify AI Insights Banner
    expect(find.text('AI Recommendation Insights'), findsOneWidget);
    expect(find.text('AI Verified'), findsOneWidget);
    expect(find.textContaining('Selected EX-08 Express due to superior comfort'), findsOneWidget);

    // Verify Match Score rendering
    expect(find.textContaining('% Match'), findsOneWidget);
  });

  testWidgets('JourneyComparisonScreen renders safe fallback banner when AI service is offline (BR-AIVAL-002)', (WidgetTester tester) async {
    final direct = JourneyCandidateModel.sampleColomboToEllaDirect();

    await tester.pumpWidget(
      MaterialApp(
        home: JourneyComparisonScreen(
          originCity: 'Colombo',
          destinationCity: 'Ella',
          travelDate: DateTime.now().add(const Duration(days: 1)),
          candidates: [direct],
          agentReasoning: '',
          isAiFallback: true,
        ),
      ),
    );

    // Verify Safe Fallback Banner
    expect(find.text('AI Offline — Safe Fallback'), findsOneWidget);
    expect(find.text('Fallback Mode'), findsOneWidget);
    expect(find.textContaining('Showing verified direct and connecting routes matching your request corridor'), findsOneWidget);
  });

  testWidgets('JourneyComparisonScreen renders when candidates is empty', (WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: JourneyComparisonScreen(
          originCity: 'Colombo',
          destinationCity: 'Ella',
          travelDate: DateTime.now().add(const Duration(days: 1)),
          candidates: const [],
          agentReasoning: 'AI Reasoning',
          isAiFallback: false,
        ),
      ),
    );
    expect(find.text('No Transit Options Found'), findsOneWidget);
  });

  testWidgets('JourneyComparisonScreen renders connecting candidate and multiple candidates', (WidgetTester tester) async {
    final conn = JourneyCandidateModel.sampleConnectingViaKandy();
    final kandy = JourneyCandidateModel.sampleColomboToKandyDirect();

    await tester.pumpWidget(
      MaterialApp(
        home: JourneyComparisonScreen(
          originCity: 'Colombo',
          destinationCity: 'Kandy',
          travelDate: DateTime.now().add(const Duration(days: 1)),
          candidates: [kandy, conn],
          agentReasoning: 'Showing fastest routes to Kandy',
          isAiFallback: false,
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Showing fastest routes to Kandy'), findsOneWidget);
    expect(find.text('Pick Seats'), findsNWidgets(2));
  });
}


