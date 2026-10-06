import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/navigation/screens/branded_splash_screen.dart';
import 'package:waypoint_mobile/core/widgets/waypoint_logo.dart';

void main() {
  testWidgets('renders WayPointLogo, tagline, version and progress line', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: BrandedSplashScreen(),
      ),
    );

    expect(find.byType(WayPointLogo), findsOneWidget);
    expect(find.text('Sri Lanka Intercity Express Network'), findsOneWidget);
    expect(find.text('v0.1.0 • WayPoint'), findsOneWidget);
    expect(find.byType(LinearProgressIndicator), findsOneWidget);
  });
}
