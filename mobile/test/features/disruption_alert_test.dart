import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/disruption/models/disruption_models.dart';
import 'package:waypoint_mobile/features/disruption/screens/disruption_alert_screen.dart';

void main() {
  testWidgets('DisruptionAlertScreen renders side-by-side rebooking card with 1-tap accept', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: DisruptionAlertScreen(
          disruption: DisruptionAlertModel.sampleColomboToElla(),
        ),
      ),
    );
    expect(find.text('Accept Replacement Bus'), findsOneWidget);
    expect(find.text('Request 100% Refund'), findsOneWidget);
  });
}
