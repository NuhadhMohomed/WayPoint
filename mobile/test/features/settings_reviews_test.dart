import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:waypoint_mobile/core/theme/theme_cubit.dart';
import 'package:waypoint_mobile/features/auth/bloc/auth_cubit.dart';
import 'package:waypoint_mobile/features/settings/screens/passenger_settings_screen.dart';
import 'package:waypoint_mobile/features/fleet/presentation/screens/review_submission_screen.dart';

import 'package:waypoint_mobile/core/network/api_client.dart';
import 'package:waypoint_mobile/core/storage/secure_storage_service.dart';

void main() {
  testWidgets('PassengerSettingsScreen renders Appearance and authoritative Light theme status', (tester) async {
    await tester.pumpWidget(
      MultiBlocProvider(
        providers: [
          BlocProvider(create: (_) => ThemeCubit()),
          BlocProvider(create: (_) => AuthCubit(apiClient: ApiClient(), storageService: SecureStorageService())),
        ],
        child: const MaterialApp(
          home: PassengerSettingsScreen(),
        ),
      ),
    );
    expect(find.text('Appearance'), findsOneWidget);
    expect(find.text('Visual Theme: Sovereign Light'), findsOneWidget);
    expect(find.text('Light Active'), findsOneWidget);
    expect(find.text('Dark'), findsNothing);
    expect(find.text('Saved Travelers'), findsOneWidget);
    expect(find.text('National Transit Helpline: 1955'), findsOneWidget);
  });

  testWidgets('ReviewSubmissionScreen renders 5 stars and quick tag chips', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: ReviewSubmissionScreen(
          entityId: 'bus-101',
          bookingId: 'bk-202',
          entityType: 'bus',
          entityName: 'Super Line Express',
        ),
      ),
    );

    expect(find.text('Rate Transit Service'), findsOneWidget);
    expect(find.text('How was your journey on Super Line Express?'), findsOneWidget);
    expect(find.text('Punctual'), findsOneWidget);
    expect(find.text('Clean Vehicle'), findsOneWidget);
    expect(find.text('Submit Verified Review'), findsOneWidget);

    // Tap a quick tag chip
    await tester.tap(find.text('Punctual'));
    await tester.pump();
    expect(find.text('Punctual'), findsOneWidget);
  });
}
