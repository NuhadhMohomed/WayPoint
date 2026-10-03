import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/journey/models/journey_models.dart';
import 'package:waypoint_mobile/features/journey/screens/journey_comparison_screen.dart';
import 'package:waypoint_mobile/features/journey/screens/journey_search_screen.dart';

void main() {
  testWidgets('JourneySearchScreen (MOB-02) renders search inputs and corridor chips', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: JourneySearchScreen(),
        ),
      ),
    );

    // Verify title & badges
    expect(find.text('Intercity Journey Planner'), findsOneWidget);
    expect(find.text('Component 1'), findsOneWidget);
    expect(find.text('Sethum'), findsOneWidget);

    // Verify origin and destination selectors
    expect(find.text('FROM (ORIGIN)'), findsOneWidget);
    expect(find.text('TO (DESTINATION)'), findsOneWidget);

    // Verify popular corridor chips
    expect(find.text('Colombo → Ella'), findsOneWidget);
    expect(find.text('Colombo → Kandy'), findsOneWidget);

    // Verify Search CTA button
    expect(find.text('Search Journeys (MOB-02)'), findsOneWidget);
  });

  testWidgets('JourneyComparisonScreen (MOB-04) renders candidates and transfer buffer indicator', (WidgetTester tester) async {
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
}
