import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/core/widgets/waypoint_button.dart';
import 'package:waypoint_mobile/core/widgets/waypoint_card.dart';
import 'package:waypoint_mobile/core/widgets/transit_badge.dart';
import 'package:waypoint_mobile/core/widgets/shimmer_loading.dart';
import 'package:waypoint_mobile/core/widgets/empty_state_view.dart';

void main() {
  testWidgets('WayPointButton shows spinner when isLoading is true and suppresses tap', (tester) async {
    bool tapped = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: WayPointButton(
            text: 'Reserve Seats',
            isLoading: true,
            onPressed: () => tapped = true,
          ),
        ),
      ),
    );
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    await tester.tap(find.byType(WayPointButton));
    expect(tapped, isFalse);
  });

  testWidgets('WayPointCard renders content and handles tap callback', (tester) async {
    bool tapped = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: WayPointCard(
            onTap: () => tapped = true,
            child: const Text('Card Content'),
          ),
        ),
      ),
    );
    expect(find.text('Card Content'), findsOneWidget);
    await tester.tap(find.text('Card Content'));
    expect(tapped, isTrue);
  });

  testWidgets('EmptyStateView renders title, description, and action button', (tester) async {
    bool actionClicked = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: EmptyStateView(
            title: 'No Trips Found',
            description: 'Try adjusting your filters.',
            actionLabel: 'Search Again',
            onAction: () => actionClicked = true,
          ),
        ),
      ),
    );
    expect(find.text('No Trips Found'), findsOneWidget);
    expect(find.text('Try adjusting your filters.'), findsOneWidget);
    expect(find.text('Search Again'), findsOneWidget);
    await tester.tap(find.text('Search Again'));
    expect(actionClicked, isTrue);
  });

  testWidgets('ShimmerLoadingCard renders placeholder box', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: ShimmerLoadingCard(height: 120),
        ),
      ),
    );
    expect(find.byType(ShimmerLoadingCard), findsOneWidget);
  });

  testWidgets('TransitBadge renders custom label and status', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: TransitBadge(
            status: TransitStatus.available,
            customLabel: 'Seat 12A Available',
          ),
        ),
      ),
    );
    expect(find.text('Seat 12A Available'), findsOneWidget);
  });
}
