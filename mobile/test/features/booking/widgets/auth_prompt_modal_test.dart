import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/booking/widgets/auth_prompt_modal.dart';
import 'package:waypoint_mobile/core/widgets/waypoint_logo.dart';

void main() {
  testWidgets('renders WayPointLogo, auth message, and action buttons', (tester) async {
    bool signInTapped = false;
    bool cancelTapped = false;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: AuthPromptModal(
            onSignIn: () => signInTapped = true,
            onCancel: () => cancelTapped = true,
          ),
        ),
      ),
    );

    expect(find.byType(WayPointLogo), findsOneWidget);
    expect(find.text('Sign In to Reserve'), findsOneWidget);
    expect(find.text('Sign In / Register'), findsOneWidget);
    expect(find.text('Continue Browsing'), findsOneWidget);

    await tester.tap(find.text('Sign In / Register'));
    expect(signInTapped, isTrue);

    await tester.tap(find.text('Continue Browsing'));
    expect(cancelTapped, isTrue);
  });
}
