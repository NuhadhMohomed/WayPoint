import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/core/widgets/waypoint_logo.dart';

void main() {
  testWidgets('renders iconOnly variant without title or subtitle', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: WayPointLogo(size: 32, variant: WayPointLogoVariant.iconOnly),
        ),
      ),
    );

    expect(find.byIcon(Icons.alt_route_rounded), findsOneWidget);
    expect(find.text('WayPoint'), findsNothing);
    expect(find.text('Sri Lanka Transit'), findsNothing);
  });

  testWidgets('renders horizontal variant with row layout', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: WayPointLogo(size: 40, variant: WayPointLogoVariant.horizontal),
        ),
      ),
    );

    expect(find.byIcon(Icons.alt_route_rounded), findsOneWidget);
    expect(find.text('WayPoint'), findsOneWidget);
    expect(find.text('Sri Lanka Transit'), findsOneWidget);
    expect(find.byType(Row), findsWidgets);
  });

  testWidgets('renders stacked variant with centered title and subtitle', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: WayPointLogo(size: 64, variant: WayPointLogoVariant.stacked),
        ),
      ),
    );

    expect(find.byIcon(Icons.alt_route_rounded), findsOneWidget);
    expect(find.text('WayPoint'), findsOneWidget);
    expect(find.text('Sri Lanka Transit'), findsOneWidget);
    expect(find.byType(Column), findsWidgets);
  });
}
