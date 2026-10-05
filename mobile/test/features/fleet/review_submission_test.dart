import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:waypoint_mobile/features/fleet/presentation/screens/review_submission_screen.dart';

void main() {
  testWidgets('ReviewSubmissionScreen renders star rating and anonymous options', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: ReviewSubmissionScreen(
          entityId: 'b1111111-1111-1111-1111-111111111111',
          bookingId: 'bk111111-1111-1111-1111-111111111111',
          entityType: 'bus',
          entityName: 'ND-4521 Luxury Super Express',
        ),
      ),
    );

    // Verify Title and Subtitle
    expect(find.text('Rate Transit Service'), findsOneWidget);
    expect(find.text('How was your journey on ND-4521 Luxury Super Express?'), findsOneWidget);

    // Verify Star Rating buttons (5 stars)
    expect(find.byType(IconButton), findsNWidgets(5));

    // Verify Anonymous Checkbox
    expect(find.text('Post anonymously'), findsOneWidget);

    // Verify Submit Button
    expect(find.text('Submit Verified Review'), findsOneWidget);
  });

  testWidgets('ReviewSubmissionScreen shows validation error when submitting with 0 stars', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(800, 1200);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    await tester.pumpWidget(
      const MaterialApp(
        home: ReviewSubmissionScreen(
          entityId: 'b1111111-1111-1111-1111-111111111111',
          bookingId: 'bk111111-1111-1111-1111-111111111111',
          entityType: 'driver',
          entityName: 'Sunil Perera',
        ),
      ),
    );

    // Tap Submit Review without selecting stars
    await tester.tap(find.text('Submit Verified Review'));
    await tester.pump();

    // Verify error message
    expect(find.text('Please select a star rating (1-5)'), findsOneWidget);
  });

  testWidgets('ReviewSubmissionScreen submits review with Auth header and payload correctly', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(800, 1200);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    bool requestCaptured = false;
    String? capturedAuthHeader;
    Map<String, dynamic>? capturedBody;

    final mockClient = MockClient((request) async {
      requestCaptured = true;
      capturedAuthHeader = request.headers['Authorization'];
      capturedBody = jsonDecode(request.body);
      return http.Response(jsonEncode({'id': 'rev-123'}), 200);
    });

    await tester.pumpWidget(
      MaterialApp(
        home: ReviewSubmissionScreen(
          entityId: 'b1111111-1111-1111-1111-111111111111',
          bookingId: 'bk111111-1111-1111-1111-111111111111',
          entityType: 'bus',
          entityName: 'Southern Star Express',
          authToken: 'mock_jwt_token_2026',
          httpClient: mockClient,
        ),
      ),
    );

    // Select 5 stars (the 5th IconButton)
    await tester.tap(find.byType(IconButton).at(4));
    await tester.pump();

    // Toggle anonymous checkbox
    await tester.tap(find.byType(CheckboxListTile));
    await tester.pump();

    // Submit review
    await tester.tap(find.text('Submit Verified Review'));
    await tester.pumpAndSettle();

    expect(requestCaptured, isTrue);
    expect(capturedAuthHeader, equals('Bearer mock_jwt_token_2026'));
    expect(capturedBody?['rating'], equals(5));
    expect(capturedBody?['isAnonymous'], isTrue);
    expect(capturedBody?['busId'], equals('b1111111-1111-1111-1111-111111111111'));
    expect(capturedBody?['bookingId'], equals('bk111111-1111-1111-1111-111111111111'));
  });
}
