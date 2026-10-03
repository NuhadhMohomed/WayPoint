import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:waypoint_mobile/features/auth/models/auth_models.dart';
import 'package:waypoint_mobile/features/auth/screens/passenger_auth_screen.dart';
import 'package:waypoint_mobile/features/auth/services/auth_service.dart';

void main() {
  testWidgets('PassengerAuthScreen (MOB-01) renders Login and Register tabs', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: PassengerAuthScreen(),
      ),
    );

    // Verify Title & Tabs
    expect(find.text('Welcome to WayPoint'), findsOneWidget);
    expect(find.widgetWithText(Tab, 'Login'), findsOneWidget);
    expect(find.widgetWithText(Tab, 'Register'), findsOneWidget);

    // Initial tab is Login
    expect(find.text('Sign In'), findsOneWidget);
    expect(find.text('Email'), findsOneWidget);
    expect(find.text('Password'), findsOneWidget);

    // Switch to Register tab
    await tester.tap(find.text('Register'));
    await tester.pumpAndSettle();

    expect(find.text('Create Account'), findsOneWidget);
    expect(find.text('Full Name'), findsOneWidget);
    expect(find.text('NIC or Passport Number'), findsOneWidget);
  });

  test('AuthService login executes successfully with valid response', () async {
    final mockClient = MockClient((request) async {
      expect(request.url.path, contains('/auth/login'));
      final body = jsonDecode(request.body);
      expect(body['email'], equals('nimal@waypoint.lk'));
      return http.Response(
        jsonEncode({
          'token': 'jwt_mock_token_abc',
          'refreshToken': 'refresh_mock_123',
          'userId': 'usr-guid-1'
        }),
        200,
      );
    });

    final authService = AuthService(client: mockClient);
    final response = await authService.login(LoginRequest(
      email: 'nimal@waypoint.lk',
      password: 'Password123!',
    ));

    expect(response.token, equals('jwt_mock_token_abc'));
    expect(response.userId, equals('usr-guid-1'));
  });

  test('AuthService register executes successfully with valid response', () async {
    final mockClient = MockClient((request) async {
      expect(request.url.path, contains('/auth/register'));
      final body = jsonDecode(request.body);
      expect(body['role'], equals('Passenger'));
      expect(body['fullName'], equals('Nimal Silva'));
      return http.Response(
        jsonEncode({
          'token': 'jwt_mock_token_xyz',
          'refreshToken': 'refresh_mock_456',
          'userId': 'usr-guid-2'
        }),
        200,
      );
    });

    final authService = AuthService(client: mockClient);
    final response = await authService.register(RegisterRequest(
      email: 'nimal.new@waypoint.lk',
      password: 'Password123!',
      fullName: 'Nimal Silva',
      nicOrPassport: '200012345678',
    ));

    expect(response.token, equals('jwt_mock_token_xyz'));
  });
}
