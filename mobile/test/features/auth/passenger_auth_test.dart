import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:dio/dio.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:waypoint_mobile/core/network/api_client.dart';
import 'package:waypoint_mobile/core/storage/secure_storage_service.dart';
import 'package:waypoint_mobile/features/auth/bloc/auth_cubit.dart';
import 'package:waypoint_mobile/features/auth/bloc/auth_state.dart';
import 'package:waypoint_mobile/features/auth/models/auth_models.dart';
import 'package:waypoint_mobile/features/auth/screens/passenger_auth_screen.dart';
import 'package:waypoint_mobile/core/widgets/waypoint_logo.dart';

class MockApiClient extends Mock implements ApiClient {}
class MockSecureStorageService extends Mock implements SecureStorageService {}

void main() {
  late MockApiClient mockApiClient;
  late MockSecureStorageService mockStorage;
  late AuthCubit authCubit;

  setUp(() {
    mockApiClient = MockApiClient();
    mockStorage = MockSecureStorageService();
    authCubit = AuthCubit(apiClient: mockApiClient, storageService: mockStorage);
  });

  tearDown(() {
    authCubit.close();
  });

  group('AuthCubit Unit Tests', () {
    const testUser = UserModel(
      id: 'usr-1',
      fullName: 'Kasun Passenger',
      email: 'kasun@waypoint.lk',
      role: 'Passenger',
    );

    test('initial state is AuthInitial', () {
      expect(authCubit.state, equals(AuthInitial()));
    });

    test('login success emits [AuthLoading, Authenticated]', () async {
      when(() => mockApiClient.post(
            any(),
            data: any(named: 'data'),
            queryParameters: any(named: 'queryParameters'),
            options: any(named: 'options'),
          )).thenAnswer((_) async => Response(
            requestOptions: RequestOptions(path: '/auth/login'),
            statusCode: 200,
            data: {
              'token': 'mock-jwt-token',
              'refreshToken': 'mock-refresh-token',
              'user': testUser.toJson(),
            },
          ));

      when(() => mockStorage.saveToken(any())).thenAnswer((_) async {});
      when(() => mockStorage.saveRefreshToken(any())).thenAnswer((_) async {});
      when(() => mockStorage.saveUser(any())).thenAnswer((_) async {});

      final expectedStates = [
        AuthLoading(),
        const Authenticated(user: testUser, token: 'mock-jwt-token'),
      ];

      expectLater(authCubit.stream, emitsInOrder(expectedStates));

      await authCubit.login('kasun@waypoint.lk', 'Password123!');
      verify(() => mockStorage.saveToken('mock-jwt-token')).called(1);
    });

    test('login failure emits [AuthLoading, AuthError]', () async {
      when(() => mockApiClient.post(
            any(),
            data: any(named: 'data'),
            queryParameters: any(named: 'queryParameters'),
            options: any(named: 'options'),
          )).thenThrow(DioException(
            requestOptions: RequestOptions(path: '/auth/login'),
            message: 'Invalid credentials',
          ));

      final expectedStates = [
        AuthLoading(),
        const AuthError('Invalid credentials'),
      ];

      expectLater(authCubit.stream, emitsInOrder(expectedStates));

      await authCubit.login('bad@waypoint.lk', 'wrong');
    });

    test('logout emits [AuthLoading, Unauthenticated]', () async {
      when(() => mockStorage.clearAll()).thenAnswer((_) async {});

      final expectedStates = [
        AuthLoading(),
        Unauthenticated(),
      ];

      expectLater(authCubit.stream, emitsInOrder(expectedStates));

      await authCubit.logout();
      verify(() => mockStorage.clearAll()).called(1);
    });
  });

  group('PassengerAuthScreen Widget Tests', () {
    testWidgets('renders brand title, email field, password field, and submit button', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: BlocProvider<AuthCubit>.value(
            value: authCubit,
            child: const PassengerAuthScreen(),
          ),
        ),
      );

      expect(find.byType(WayPointLogo), findsOneWidget);
      expect(find.text('WayPoint'), findsOneWidget);
      expect(find.text('Sri Lanka Transit'), findsOneWidget);
      expect(find.byKey(const Key('login_email_field')), findsOneWidget);
      expect(find.byKey(const Key('login_password_field')), findsOneWidget);
      expect(find.byKey(const Key('login_submit_button')), findsOneWidget);
    });

    testWidgets('switches to Register tab and shows registration inputs', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: BlocProvider<AuthCubit>.value(
            value: authCubit,
            child: const PassengerAuthScreen(),
          ),
        ),
      );

      // Tap Register tab
      await tester.tap(find.text('Register'));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('reg_name_field')), findsOneWidget);
      expect(find.byKey(const Key('reg_email_field')), findsOneWidget);
      expect(find.byKey(const Key('reg_phone_field')), findsOneWidget);
      expect(find.byKey(const Key('reg_password_field')), findsOneWidget);
      expect(find.byKey(const Key('reg_submit_button')), findsOneWidget);
    });
  });
}
