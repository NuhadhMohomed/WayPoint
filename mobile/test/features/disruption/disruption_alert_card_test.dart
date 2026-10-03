import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/disruption/widgets/disruption_alert_card.dart';

void main() {
  testWidgets('DisruptionAlertCard renders title, message and badges correctly', (WidgetTester tester) async {
    bool tapped = false;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: DisruptionAlertCard(
            title: 'Express Service Delayed',
            message: 'Heavy traffic along Southern Expressway.',
            serviceNumber: 'EX-01',
            delayNotice: 'Delayed by 25 mins',
            onTap: () {
              tapped = true;
            },
          ),
        ),
      ),
    );

    expect(find.text('Express Service Delayed'), findsOneWidget);
    expect(find.text('Heavy traffic along Southern Expressway.'), findsOneWidget);
    expect(find.text('EX-01'), findsOneWidget);
    expect(find.text('Delayed by 25 mins'), findsOneWidget);

    await tester.tap(find.byType(DisruptionAlertCard));
    await tester.pump();
    expect(tapped, isTrue);
  });
}
