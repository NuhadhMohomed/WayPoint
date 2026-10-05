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
}
