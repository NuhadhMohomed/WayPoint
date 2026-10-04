import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/core/widgets/seat_reservation_bar.dart';

void main() {
  testWidgets('SeatReservationBar displays remaining hold time and selected seats', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          bottomNavigationBar: SeatReservationBar(
            remainingSeconds: 580,
            selectedSeats: const ['12A', '12B'],
            totalAmount: 4800,
            onContinue: () {},
          ),
        ),
      ),
    );
    expect(find.textContaining('Seats 12A, 12B'), findsOneWidget);
    expect(find.textContaining('09:40'), findsOneWidget);
  });
}
