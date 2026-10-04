import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/journey/screens/journey_search_screen.dart';

void main() {
  testWidgets('JourneySearchScreen contains corridors carousel and quick swap button', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: JourneySearchScreen()),
      ),
    );
    expect(find.text('Explore Corridors'), findsOneWidget);
    expect(find.byIcon(Icons.swap_vert), findsOneWidget);
    expect(find.text('Search Buses'), findsOneWidget);
  });
}
