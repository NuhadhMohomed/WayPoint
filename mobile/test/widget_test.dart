import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/main.dart';

void main() {
  testWidgets('WayPoint app smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(const WayPointApp());
    await tester.pump();

    // Verify WayPointApp renders MaterialApp with title
    final materialApp = tester.widget<MaterialApp>(find.byType(MaterialApp));
    expect(materialApp.title, equals('WayPoint Transit'));
  });
}
