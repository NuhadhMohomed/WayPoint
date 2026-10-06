import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/journey/widgets/first_run_welcome_card.dart';

void main() {
  testWidgets('renders corridor chips and invokes callback on tap', (tester) async {
    String selectedOrigin = '';
    String selectedDest = '';
    bool dismissed = false;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: FirstRunWelcomeCard(
            onSelectCorridor: (orig, dest) {
              selectedOrigin = orig;
              selectedDest = dest;
            },
            onDismiss: () => dismissed = true,
          ),
        ),
      ),
    );

    expect(find.text('Welcome to WayPoint! 🚌'), findsOneWidget);
    expect(find.text('Colombo ⇄ Galle'), findsOneWidget);
    expect(find.text('Colombo ⇄ Kandy'), findsOneWidget);
    expect(find.text('Colombo ⇄ Jaffna'), findsOneWidget);

    await tester.tap(find.text('Colombo ⇄ Galle'));
    expect(selectedOrigin, 'Colombo');
    expect(selectedDest, 'Galle');

    await tester.tap(find.byIcon(Icons.close_rounded));
    expect(dismissed, isTrue);
  });
}
