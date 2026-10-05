import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/booking/models/booking_models.dart';
import 'package:waypoint_mobile/features/booking/screens/payment_checkout_screen.dart';

void main() {
  testWidgets('PaymentCheckoutScreen renders fare accordion and sandbox simulation buttons', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: PaymentCheckoutScreen(
          holdInfo: SeatHoldInfo.sampleColomboToElla(),
        ),
      ),
    );
    expect(find.text('Fare Breakdown'), findsOneWidget);
    expect(find.text('Simulate Sandbox Payment'), findsOneWidget);
  });
}
