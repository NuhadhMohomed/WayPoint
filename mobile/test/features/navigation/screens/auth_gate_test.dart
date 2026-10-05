import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:waypoint_mobile/core/storage/secure_storage_service.dart';
import 'package:waypoint_mobile/features/auth/bloc/auth_cubit.dart';
import 'package:waypoint_mobile/features/auth/bloc/auth_state.dart';
import 'package:waypoint_mobile/features/auth/models/auth_models.dart';
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

  testWidgets('shows PassengerNavigationShell as guest when onboarding is completed and unauthenticated', (tester) async {
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

    expect(find.byType(PassengerNavigationShell), findsOneWidget);
    expect(find.text('Explore'), findsOneWidget);
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
