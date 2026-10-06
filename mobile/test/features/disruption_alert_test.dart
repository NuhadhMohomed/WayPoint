import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:waypoint_mobile/features/disruption/models/disruption_models.dart';
import 'package:waypoint_mobile/features/disruption/screens/disruption_alert_screen.dart';
import 'package:waypoint_mobile/features/disruption/services/disruption_api_service.dart';

class MockDisruptionApiService extends Mock implements DisruptionApiService {}

void main() {
  testWidgets('DisruptionAlertScreen renders side-by-side rebooking card with 1-tap accept', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: DisruptionAlertScreen(
          disruption: DisruptionAlertModel.sampleColomboToElla(),
        ),
      ),
    );
    expect(find.text('Accept Replacement Bus'), findsOneWidget);
    expect(find.text('Request 100% Refund'), findsOneWidget);
  });

  testWidgets('Accepting replacement in PendingManagerApproval shows reservation sheet and status pill', (tester) async {
    tester.view.physicalSize = const Size(800, 1600);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final mockService = MockDisruptionApiService();
    when(() => mockService.executeRebooking(
          proposalId: any(named: 'proposalId'),
          proposalStatus: any(named: 'proposalStatus'),
        )).thenAnswer((_) async => const RebookingConsentResult(
          status: RebookingConsentStatus.pendingManagerApproval,
          message: 'Alternative preference recorded. Held pending Transport Manager operational sign-off.',
        ));

    final disruption = DisruptionAlertModel.sampleColomboToElla();

    await tester.pumpWidget(
      MaterialApp(
        home: DisruptionAlertScreen(
          disruption: DisruptionAlertModel(
            proposalId: 'prop-test-01',
            proposalStatus: 'PendingManagerApproval',
            bookingReference: disruption.bookingReference,
            passengerName: disruption.passengerName,
            disruptionTitle: disruption.disruptionTitle,
            disruptionReason: disruption.disruptionReason,
            compensationNotice: disruption.compensationNotice,
            originalBusPlate: disruption.originalBusPlate,
            originalBusClass: disruption.originalBusClass,
            originalDepartureTime: disruption.originalDepartureTime,
            originalOriginStop: disruption.originalOriginStop,
            originalArrivalTime: disruption.originalArrivalTime,
            originalDestinationStop: disruption.originalDestinationStop,
            originalFarePaid: disruption.originalFarePaid,
            replacementBusPlate: disruption.replacementBusPlate,
            replacementBusClass: disruption.replacementBusClass,
            replacementDepartureTime: disruption.replacementDepartureTime,
            replacementOriginStop: disruption.replacementOriginStop,
            replacementArrivalTime: disruption.replacementArrivalTime,
            replacementDestinationStop: disruption.replacementDestinationStop,
            assignedSeats: disruption.assignedSeats,
            fareDifference: disruption.fareDifference,
            fareDifferenceLabel: disruption.fareDifferenceLabel,
            holdExpiresAt: disruption.holdExpiresAt,
          ),
          apiService: mockService,
        ),
      ),
    );

    await tester.tap(find.text('Accept Replacement Bus'));
    await tester.pumpAndSettle();

    expect(find.text('Alternative Preference Reserved!'), findsOneWidget);
    expect(find.text('Keep My Seat Lock'), findsOneWidget);
  });

  testWidgets('Failed rebooking shows error snackbar and does not mark accepted', (tester) async {
    tester.view.physicalSize = const Size(800, 1600);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final mockService = MockDisruptionApiService();
    when(() => mockService.executeRebooking(
          proposalId: any(named: 'proposalId'),
          proposalStatus: any(named: 'proposalStatus'),
        )).thenAnswer((_) async => const RebookingConsentResult(
          status: RebookingConsentStatus.failed,
          message: 'Network connection failed.',
        ));

    final disruption = DisruptionAlertModel.sampleColomboToElla();

    await tester.pumpWidget(
      MaterialApp(
        home: DisruptionAlertScreen(
          disruption: DisruptionAlertModel(
            proposalId: 'prop-fail-01',
            proposalStatus: 'PendingManagerApproval',
            bookingReference: disruption.bookingReference,
            passengerName: disruption.passengerName,
            disruptionTitle: disruption.disruptionTitle,
            disruptionReason: disruption.disruptionReason,
            compensationNotice: disruption.compensationNotice,
            originalBusPlate: disruption.originalBusPlate,
            originalBusClass: disruption.originalBusClass,
            originalDepartureTime: disruption.originalDepartureTime,
            originalOriginStop: disruption.originalOriginStop,
            originalArrivalTime: disruption.originalArrivalTime,
            originalDestinationStop: disruption.originalDestinationStop,
            originalFarePaid: disruption.originalFarePaid,
            replacementBusPlate: disruption.replacementBusPlate,
            replacementBusClass: disruption.replacementBusClass,
            replacementDepartureTime: disruption.replacementDepartureTime,
            replacementOriginStop: disruption.replacementOriginStop,
            replacementArrivalTime: disruption.replacementArrivalTime,
            replacementDestinationStop: disruption.replacementDestinationStop,
            assignedSeats: disruption.assignedSeats,
            fareDifference: disruption.fareDifference,
            fareDifferenceLabel: disruption.fareDifferenceLabel,
            holdExpiresAt: disruption.holdExpiresAt,
          ),
          apiService: mockService,
        ),
      ),
    );

    await tester.tap(find.text('Accept Replacement Bus'));
    await tester.pumpAndSettle();

    // Verify error snackbar appears
    expect(find.text('Network connection failed.'), findsOneWidget);
    // Verify accept button is still visible and not marked accepted
    expect(find.text('Accept Replacement Bus'), findsOneWidget);
    expect(find.text('Alternative Preference Reserved!'), findsNothing);
  });
}
