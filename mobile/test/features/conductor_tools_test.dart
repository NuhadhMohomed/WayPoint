import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/fleet/screens/conductor_manifest_screen.dart';
import 'package:waypoint_mobile/features/fleet/screens/conductor_scanner_screen.dart';

void main() {
  testWidgets('ConductorManifestScreen renders passenger roster with filter pills', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: ConductorManifestScreen(),
      ),
    );
    // Settle the delayed future in mock bloc
    await tester.pumpAndSettle(const Duration(milliseconds: 1000));

    expect(find.text('Passenger Manifest'), findsOneWidget);
    expect(find.text('All'), findsOneWidget);
    expect(find.text('Boarded'), findsWidgets);
    expect(find.text('Pending'), findsWidgets);
  });

  testWidgets('ConductorScannerScreen renders scanner UI and manual entry button', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: ConductorScannerScreen(),
      ),
    );
    expect(find.text('Boarding Pass Scanner'), findsOneWidget);
    expect(find.text('Enter Booking Ref Manually'), findsOneWidget);
  });
}
