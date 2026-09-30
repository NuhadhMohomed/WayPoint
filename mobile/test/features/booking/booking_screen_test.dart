import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/booking/models/booking_models.dart';
import 'package:waypoint_mobile/features/booking/screens/payment_checkout_screen.dart';
import 'package:waypoint_mobile/features/booking/screens/ticket_wallet_screen.dart';
import 'package:waypoint_mobile/features/booking/screens/booking_history_screen.dart';

void main() {
  group('Component 3: Booking Models Unit Tests (Mithila)', () {
    test(
        'SeatHoldInfo.sampleColomboToElla initializes with correct sample data',
        () {
      final hold = SeatHoldInfo.sampleColomboToElla();

      expect(hold.serviceCode, equals('SRV-COL-ELLA-0800'));
      expect(hold.seatNumbers, containsAll(['4A', '4B']));
      expect(hold.farePerSeat, equals(2850.0));
      expect(hold.subtotal, equals(5700.0));
      expect(hold.totalAmount, equals(5850.0));
      expect(hold.remainingSeconds, greaterThan(0));
      expect(hold.isExpired, isFalse);
    });

    test('PaymentSandboxCard presets have required test card numbers', () {
      const success = PaymentSandboxCard.successCard;
      const declined = PaymentSandboxCard.declinedCard;
      const timeout = PaymentSandboxCard.timeoutCard;

      expect(success.cardNumber.endsWith('0001'), isTrue);
      expect(declined.cardNumber.endsWith('0002'), isTrue);
      expect(timeout.cardNumber.endsWith('0003'), isTrue);
    });

    test('DigitalTicketPass.sampleColomboToElla generates valid signed pass',
        () {
      final pass = DigitalTicketPass.sampleColomboToElla();

      expect(pass.bookingReference, equals('WP-7B92K1'));
      expect(pass.passengerName, equals('Nimal Silva'));
      expect(pass.seatNumbers, contains('4A'));
      expect(pass.totalFare, equals(5700.0));
      expect(pass.qrCodePayload, contains('HMAC:'));
    });

    test(
        'HistoricalBookingItem calculates tiered refund eligibility accurately',
        () {
      // Tier 1: > 24 hours -> 90% refund
      final itemTier1 = HistoricalBookingItem(
        bookingId: 'b-1',
        bookingReference: 'WP-TEST-1',
        serviceCode: 'SRV-TEST',
        routeTitle: 'Colombo - Ella Highland Scenic Corridor',
        originCity: 'Colombo',
        destinationCity: 'Ella',
        departureTime: DateTime.now().add(const Duration(hours: 30)),
        arrivalTime: DateTime.now().add(const Duration(hours: 35)),
        seatNumbers: const ['1A'],
        totalPaid: 3000.0,
        status: 'Confirmed',
        bookedAt: DateTime.now(),
      );
      expect(itemTier1.refundTierPercentage, equals(0.90));
      expect(itemTier1.refundTierAmount, equals(2700.0));

      // Tier 2: 12 to 24 hours -> 50% refund
      final itemTier2 = HistoricalBookingItem(
        bookingId: 'b-2',
        bookingReference: 'WP-TEST-2',
        serviceCode: 'SRV-TEST',
        routeTitle: 'Colombo - Kandy Intercity Express',
        originCity: 'Colombo',
        destinationCity: 'Kandy',
        departureTime: DateTime.now().add(const Duration(hours: 18)),
        arrivalTime: DateTime.now().add(const Duration(hours: 21)),
        seatNumbers: const ['2A'],
        totalPaid: 2000.0,
        status: 'Confirmed',
        bookedAt: DateTime.now(),
      );
      expect(itemTier2.refundTierPercentage, equals(0.50));
      expect(itemTier2.refundTierAmount, equals(1000.0));

      // Tier 3: < 12 hours -> 0% refund
      final itemTier3 = HistoricalBookingItem(
        bookingId: 'b-3',
        bookingReference: 'WP-TEST-3',
        serviceCode: 'SRV-TEST',
        routeTitle: 'Colombo - Galle Southern Expressway Direct',
        originCity: 'Colombo',
        destinationCity: 'Galle',
        departureTime: DateTime.now().add(const Duration(hours: 6)),
        arrivalTime: DateTime.now().add(const Duration(hours: 8)),
        seatNumbers: const ['3A'],
        totalPaid: 1500.0,
        status: 'Confirmed',
        bookedAt: DateTime.now(),
      );
      expect(itemTier3.refundTierPercentage, equals(0.0));
      expect(itemTier3.refundTierAmount, equals(0.0));
    });
  });

  group('Component 3: Booking Screen Widget Tests (Mithila)', () {
    testWidgets('PaymentCheckoutScreen renders hold bar and preset test chips',
        (tester) async {
      final hold = SeatHoldInfo.sampleColomboToElla();

      await tester.pumpWidget(
        MaterialApp(
          home: PaymentCheckoutScreen(holdInfo: hold),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('Secure Checkout'), findsOneWidget);
      expect(find.text('Payment Sandbox Presets'), findsOneWidget);
      expect(find.text('Instant Success'), findsOneWidget);
      expect(find.text('Card Declined'), findsOneWidget);
      expect(find.text('Gateway Timeout'), findsOneWidget);
    });

    testWidgets('TicketWalletScreen renders tabbed passes view',
        (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: TicketWalletScreen(),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.textContaining('Active Passes'), findsOneWidget);
      expect(find.textContaining('Past Trips'), findsOneWidget);
    });

    testWidgets('BookingHistoryScreen renders filter tabs and booking items',
        (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: BookingHistoryScreen(
            initialBookings: HistoricalBookingItem.sampleBookings(),
          ),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.textContaining('All'), findsWidgets);
      expect(find.textContaining('Upcoming'), findsWidgets);
      expect(find.textContaining('Cancelled'), findsWidgets);
      expect(find.textContaining('Completed'), findsWidgets);
    });
  });
}
