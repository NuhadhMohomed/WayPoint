import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/onboarding/screens/onboarding_screen.dart';

void main() {
  testWidgets('displays slide 1 content and Skip button', (tester) async {
    bool finished = false;
    await tester.pumpWidget(
      MaterialApp(
        home: OnboardingScreen(onFinish: () => finished = true),
      ),
    );

    expect(find.text('Explore Intercity Transit'), findsOneWidget);
    expect(find.text('Skip'), findsOneWidget);
    expect(find.text('Next'), findsOneWidget);

    await tester.tap(find.text('Skip'));
    await tester.pump();
    expect(finished, isTrue);
  });

  testWidgets('advances slides and shows Get Started on last slide', (tester) async {
    bool finished = false;
    await tester.pumpWidget(
      MaterialApp(
        home: OnboardingScreen(onFinish: () => finished = true),
      ),
    );

    // Slide 1 -> Slide 2
    await tester.tap(find.text('Next'));
    await tester.pumpAndSettle();
    expect(find.text('Pick Your Exact Seat'), findsOneWidget);

    // Slide 2 -> Slide 3
    await tester.tap(find.text('Next'));
    await tester.pumpAndSettle();
    expect(find.text('Board with Offline QR Tickets'), findsOneWidget);
    expect(find.text('Get Started'), findsOneWidget);

    await tester.tap(find.text('Get Started'));
    await tester.pump();
    expect(finished, isTrue);
  });
}
