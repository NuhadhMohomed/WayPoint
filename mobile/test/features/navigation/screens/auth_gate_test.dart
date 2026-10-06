import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:waypoint_mobile/core/storage/secure_storage_service.dart';
import 'package:waypoint_mobile/features/auth/bloc/auth_cubit.dart';
import 'package:waypoint_mobile/features/auth/bloc/auth_state.dart';
import 'package:waypoint_mobile/features/auth/models/auth_models.dart';
import 'package:waypoint_mobile/features/auth/screens/passenger_auth_screen.dart';
import 'package:waypoint_mobile/features/navigation/screens/auth_gate.dart';
import 'package:waypoint_mobile/features/navigation/screens/passenger_navigation_shell.dart';
import 'package:waypoint_mobile/features/onboarding/screens/onboarding_screen.dart';

class MockSecureStorageService extends Mock implements SecureStorageService {}
class MockAuthCubit extends Mock implements AuthCubit {}

void main() {
  late MockSecureStorageService mockStorage;
  late MockAuthCubit mockAuthCubit;

  setUp(() {
    mockStorage = MockSecureStorageService();
    mockAuthCubit = MockAuthCubit();
  });

  testWidgets('shows OnboardingScreen when onboarding is not completed', (tester) async {
    when(() => mockStorage.isOnboardingCompleted()).thenAnswer((_) async => false);
    when(() => mockAuthCubit.state).thenReturn(Unauthenticated());
    when(() => mockAuthCubit.stream).thenAnswer((_) => Stream.value(Unauthenticated()));
    when(() => mockAuthCubit.checkAuthStatus()).thenAnswer((_) async {});

    await tester.pumpWidget(
      MaterialApp(
        home: BlocProvider<AuthCubit>.value(
          value: mockAuthCubit,
          child: AuthGate(storageService: mockStorage),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(OnboardingScreen), findsOneWidget);
  });

  testWidgets('shows PassengerAuthScreen asking to sign in or register when onboarding is completed and unauthenticated', (tester) async {
    when(() => mockStorage.isOnboardingCompleted()).thenAnswer((_) async => true);
    when(() => mockAuthCubit.state).thenReturn(Unauthenticated());
    when(() => mockAuthCubit.stream).thenAnswer((_) => Stream.value(Unauthenticated()));
    when(() => mockAuthCubit.checkAuthStatus()).thenAnswer((_) async {});

    await tester.pumpWidget(
      MaterialApp(
        home: BlocProvider<AuthCubit>.value(
          value: mockAuthCubit,
          child: AuthGate(storageService: mockStorage),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(PassengerAuthScreen), findsOneWidget);
    expect(find.text('Sign In'), findsOneWidget);
    expect(find.text('Register'), findsOneWidget);
    expect(find.byKey(const Key('continue_as_guest_button')), findsOneWidget);
  });

  testWidgets('transitions from PassengerAuthScreen to PassengerNavigationShell when user taps Continue as Guest', (tester) async {
    when(() => mockStorage.isOnboardingCompleted()).thenAnswer((_) async => true);
    when(() => mockAuthCubit.state).thenReturn(Unauthenticated());
    when(() => mockAuthCubit.stream).thenAnswer((_) => Stream.value(Unauthenticated()));
    when(() => mockAuthCubit.checkAuthStatus()).thenAnswer((_) async {});

    await tester.pumpWidget(
      MaterialApp(
        home: BlocProvider<AuthCubit>.value(
          value: mockAuthCubit,
          child: AuthGate(storageService: mockStorage),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final guestButton = find.byKey(const Key('continue_as_guest_button'));
    expect(guestButton, findsOneWidget);

    await tester.ensureVisible(guestButton);
    await tester.pumpAndSettle();
    await tester.tap(guestButton);
    await tester.pumpAndSettle();

    expect(find.byType(PassengerAuthScreen), findsNothing);
    expect(find.byType(PassengerNavigationShell), findsOneWidget);
    expect(find.text('Explore'), findsOneWidget);
  });

  testWidgets('shows PassengerAuthScreen immediately after onboarding screen completion', (tester) async {
    when(() => mockStorage.isOnboardingCompleted()).thenAnswer((_) async => false);
    when(() => mockStorage.setOnboardingCompleted(completed: true)).thenAnswer((_) async {});
    when(() => mockAuthCubit.state).thenReturn(Unauthenticated());
    when(() => mockAuthCubit.stream).thenAnswer((_) => Stream.value(Unauthenticated()));
    when(() => mockAuthCubit.checkAuthStatus()).thenAnswer((_) async {});

    await tester.pumpWidget(
      MaterialApp(
        home: BlocProvider<AuthCubit>.value(
          value: mockAuthCubit,
          child: AuthGate(storageService: mockStorage),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(OnboardingScreen), findsOneWidget);

    // Tap Skip on OnboardingScreen
    await tester.tap(find.text('Skip'));
    await tester.pumpAndSettle();

    // Verify user is immediately asked to sign in or register
    expect(find.byType(PassengerAuthScreen), findsOneWidget);
    expect(find.text('Sign In'), findsOneWidget);
    expect(find.text('Register'), findsOneWidget);
  });

  testWidgets('shows PassengerNavigationShell with user when authenticated', (tester) async {
    const user = UserModel(
      id: 'usr-1',
      fullName: 'Nuhadh Mohomed',
      email: 'nuhadh@example.com',
      role: 'Passenger',
    );
    when(() => mockStorage.isOnboardingCompleted()).thenAnswer((_) async => true);
    when(() => mockAuthCubit.state).thenReturn(const Authenticated(user: user, token: 'token-123'));
    when(() => mockAuthCubit.stream).thenAnswer((_) => Stream.value(const Authenticated(user: user, token: 'token-123')));
    when(() => mockAuthCubit.checkAuthStatus()).thenAnswer((_) async {});

    await tester.pumpWidget(
      MaterialApp(
        home: BlocProvider<AuthCubit>.value(
          value: mockAuthCubit,
          child: AuthGate(storageService: mockStorage),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(PassengerNavigationShell), findsOneWidget);
    expect(find.text('Explore'), findsOneWidget);
  });
}
