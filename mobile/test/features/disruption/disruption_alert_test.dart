import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/disruption/models/disruption_models.dart';
import 'package:waypoint_mobile/features/disruption/screens/disruption_alert_screen.dart';

void main() {
  group('DisruptionAlertModel Unit Tests', () {
    test('sampleColomboToElla creates valid model with correct initial data', () {
      final model = DisruptionAlertModel.sampleColomboToElla();

      expect(model.bookingReference, equals('WP-8F29K1'));
      expect(model.passengerName, equals('Nimal Silva'));
      expect(model.originalBusPlate, equals('Bus ND-8821'));
      expect(model.replacementBusPlate, equals('Bus WP-CAD-4120'));
      expect(model.assignedSeats, containsAll(['1A', '2A']));
      expect(model.fareDifference, equals(0.0));
      expect(model.remainingSeconds, greaterThan(0));
      expect(model.isExpired, isFalse);
    });

    test('remainingSeconds returns 0 when hold expires in the past', () {
      final expiredModel = DisruptionAlertModel(
        bookingReference: 'WP-TEST-EXPIRED',
        passengerName: 'Kamal Perera',
        disruptionTitle: 'Service Cancelled',
        disruptionReason: 'Engine overheating',
        compensationNotice: 'Full refund applicable',
        originalBusPlate: 'Bus ND-1000',
        originalBusClass: 'Standard AC',
        originalDepartureTime: '06:00 AM',
        originalOriginStop: 'Colombo Fort',
        originalArrivalTime: '10:00 AM',
        originalDestinationStop: 'Kandy',
        originalFarePaid: 2500.0,
        replacementBusPlate: 'Bus ND-2000',
        replacementBusClass: 'Luxury AC',
        replacementDepartureTime: '06:45 AM',
        replacementOriginStop: 'Colombo Fort',
        replacementArrivalTime: '10:30 AM',
        replacementDestinationStop: 'Kandy',
        assignedSeats: const ['3B'],
        fareDifference: 0.0,
        fareDifferenceLabel: 'Rs. 0.00',
        holdExpiresAt: DateTime.now().subtract(const Duration(minutes: 5)),
      );

      expect(expiredModel.remainingSeconds, equals(0));
      expect(expiredModel.isExpired, isTrue);
    });

    test('equatable props equality comparison works', () {
      final expiry = DateTime.now().add(const Duration(minutes: 10));
      final m1 = DisruptionAlertModel(
        bookingReference: 'WP-EQ-1',
        passengerName: 'Sunil',
        disruptionTitle: 'Title',
        disruptionReason: 'Reason',
        compensationNotice: 'Notice',
        originalBusPlate: 'Bus 1',
        originalBusClass: 'Std',
        originalDepartureTime: '08:00 AM',
        originalOriginStop: 'A',
        originalArrivalTime: '10:00 AM',
        originalDestinationStop: 'B',
        originalFarePaid: 1000.0,
        replacementBusPlate: 'Bus 2',
        replacementBusClass: 'Lux',
        replacementDepartureTime: '08:30 AM',
        replacementOriginStop: 'A',
        replacementArrivalTime: '10:30 AM',
        replacementDestinationStop: 'B',
        assignedSeats: const ['1A'],
        fareDifference: 0.0,
        fareDifferenceLabel: 'Free',
        holdExpiresAt: expiry,
      );

      final m2 = DisruptionAlertModel(
        bookingReference: 'WP-EQ-1',
        passengerName: 'Sunil',
        disruptionTitle: 'Title',
        disruptionReason: 'Reason',
        compensationNotice: 'Notice',
        originalBusPlate: 'Bus 1',
        originalBusClass: 'Std',
        originalDepartureTime: '08:00 AM',
        originalOriginStop: 'A',
        originalArrivalTime: '10:00 AM',
        originalDestinationStop: 'B',
        originalFarePaid: 1000.0,
        replacementBusPlate: 'Bus 2',
        replacementBusClass: 'Lux',
        replacementDepartureTime: '08:30 AM',
        replacementOriginStop: 'A',
        replacementArrivalTime: '10:30 AM',
        replacementDestinationStop: 'B',
        assignedSeats: const ['1A'],
        fareDifference: 0.0,
        fareDifferenceLabel: 'Free',
        holdExpiresAt: expiry,
      );

      expect(m1, equals(m2));
    });
  });

  group('DisruptionAlertScreen (MOB-09) Widget Tests', () {
    testWidgets('renders all critical UI cards, badges and buttons', (WidgetTester tester) async {
      final sample = DisruptionAlertModel.sampleColomboToElla();

      await tester.pumpWidget(
        MaterialApp(
          theme: ThemeData.dark(),
          home: Scaffold(
            body: DisruptionAlertScreen(disruption: sample),
          ),
        ),
      );

      // Verify incident header
      expect(find.text('DISRUPTION NOTIFICATION'), findsOneWidget);
      expect(find.text(sample.disruptionTitle), findsOneWidget);
      expect(find.textContaining('WP-8F29K1'), findsWidgets);

      // Verify original service details
      expect(find.text('Bus ND-8821'), findsOneWidget);
      expect(find.text('Standard AC'), findsOneWidget);

      // Verify AI replacement service details
      expect(find.text('Bus WP-CAD-4120'), findsOneWidget);
      expect(find.text('Super Line Luxury Coach'), findsOneWidget);

      // Verify action buttons
      expect(find.text('Accept Replacement Seat'), findsOneWidget);
      expect(find.text('Decline & Request 100% Refund'), findsOneWidget);
    });

    testWidgets('invokes onAccepted callback when Accept button is pressed', (WidgetTester tester) async {
      bool acceptedTriggered = false;
      final sample = DisruptionAlertModel.sampleColomboToElla();

      await tester.pumpWidget(
        MaterialApp(
          theme: ThemeData.dark(),
          home: Scaffold(
            body: DisruptionAlertScreen(
              disruption: sample,
              onAccepted: () {
                acceptedTriggered = true;
              },
            ),
          ),
        ),
      );

      // Find accept button and tap
      final acceptBtn = find.text('Accept Replacement Seat');
      expect(acceptBtn, findsOneWidget);
      await tester.tap(acceptBtn);
      await tester.pumpAndSettle();

      expect(acceptedTriggered, isTrue);
    });

    testWidgets('shows decline dialog when Decline button is pressed', (WidgetTester tester) async {
      final sample = DisruptionAlertModel.sampleColomboToElla();

      await tester.pumpWidget(
        MaterialApp(
          theme: ThemeData.dark(),
          home: Scaffold(
            body: DisruptionAlertScreen(disruption: sample),
          ),
        ),
      );

      final declineBtn = find.text('Decline & Request 100% Refund');
      expect(declineBtn, findsOneWidget);
      await tester.tap(declineBtn);
      await tester.pumpAndSettle();

      // Verify confirm refund dialog opens
      expect(find.text('Confirm 100% Refund'), findsOneWidget);
      expect(find.text('Cancel'), findsOneWidget);
      expect(find.text('Confirm Refund'), findsOneWidget);
    });
  });
}
