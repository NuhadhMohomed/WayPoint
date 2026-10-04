import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/booking/screens/ticket_wallet_screen.dart';

void main() {
  testWidgets('TicketWalletScreen renders dynamic QR pass and brightness booster button', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: TicketWalletScreen(),
      ),
    );
    expect(find.text('Digital Boarding Pass'), findsOneWidget);
    expect(find.byIcon(Icons.brightness_high), findsOneWidget);
  });
}
