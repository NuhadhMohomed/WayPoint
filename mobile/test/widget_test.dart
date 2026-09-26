import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/main.dart';

void main() {
  testWidgets('WayPoint app smoke test', (WidgetTester tester) async {
    // Build our app and trigger a frame.
    await tester.pumpWidget(const WayPointApp());

    // Verify app bar title renders
    expect(find.text('WayPoint Transit'), findsOneWidget);
    expect(find.text('Booking & Ticketing Hub'), findsOneWidget);
  });
}
