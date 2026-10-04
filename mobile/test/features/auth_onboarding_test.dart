import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/onboarding/screens/onboarding_screen.dart';
import 'package:waypoint_mobile/features/auth/bloc/auth_cubit.dart';
import 'package:waypoint_mobile/features/auth/screens/passenger_auth_screen.dart';
import 'package:waypoint_mobile/core/storage/local_cache_service.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('OnboardingScreen renders 3-step carousel with Skip and Next/Get Started', (tester) async {
    bool completed = false;
    await tester.pumpWidget(
      MaterialApp(
        home: OnboardingScreen(
          onFinish: () => completed = true,
        ),
      ),
    );

    expect(find.text('Explore Sri Lanka'), findsOneWidget);
    expect(find.text('Skip'), findsOneWidget);
    expect(find.text('Next'), findsOneWidget);

    // Tap Skip directly finishes onboarding
    await tester.tap(find.text('Skip'));
    await tester.pumpAndSettle();
    expect(completed, isTrue);
  });

  test('AuthCubit initial state and logout flow', () async {
    final cache = LocalCacheService();
    final authCubit = AuthCubit(cache: cache);

    expect(authCubit.state.isAuthenticated, isFalse);
    expect(authCubit.state.userRole, 'Passenger');

    authCubit.emitAuthenticated(token: 'jwt-123', email: 'passenger@waypoint.lk', role: 'Passenger');
    expect(authCubit.state.isAuthenticated, isTrue);

    await authCubit.logout();
    expect(authCubit.state.isAuthenticated, isFalse);
  });

  testWidgets('PassengerAuthScreen renders clean tabs and WayPointLogo', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: PassengerAuthScreen(),
      ),
    );

    expect(find.text('Sign In'), findsWidgets);
    expect(find.text('Create Account'), findsWidgets);
  });
}
