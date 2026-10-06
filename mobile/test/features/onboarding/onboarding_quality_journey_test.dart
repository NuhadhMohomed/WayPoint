import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:waypoint_mobile/core/storage/secure_storage_service.dart';
import 'package:waypoint_mobile/core/network/api_client.dart';
import 'package:waypoint_mobile/main.dart';
import 'package:waypoint_mobile/features/onboarding/screens/onboarding_screen.dart';
import 'package:waypoint_mobile/features/journey/widgets/first_run_welcome_card.dart';

class MockFlutterSecureStorage extends Mock implements FlutterSecureStorage {}

void main() {
  late MockFlutterSecureStorage mockStorage;
  late SecureStorageService storageService;
  late ApiClient apiClient;

  setUp(() {
    mockStorage = MockFlutterSecureStorage();
    storageService = SecureStorageService(storage: mockStorage);
    apiClient = ApiClient(storage: storageService);
  });

  testWidgets('full onboarding journey: new user sees onboarding, skips, lands on explore with first-run card', (tester) async {
    // 1. Initial state: onboarding not completed, user unauthenticated
    when(() => mockStorage.read(key: 'waypoint_has_completed_onboarding'))
        .thenAnswer((_) async => null);
    when(() => mockStorage.read(key: 'waypoint_auth_token'))
        .thenAnswer((_) async => null);
    when(() => mockStorage.read(key: 'waypoint_refresh_token'))
        .thenAnswer((_) async => null);
    when(() => mockStorage.read(key: 'waypoint_user_profile'))
        .thenAnswer((_) async => null);
    when(() => mockStorage.write(key: 'waypoint_has_completed_onboarding', value: 'true'))
        .thenAnswer((_) async => {});

    await tester.pumpWidget(
      WayPointApp(
        storageService: storageService,
        apiClient: apiClient,
      ),
    );
    await tester.pumpAndSettle();

    // Verify Onboarding is visible to new user
    expect(find.byType(OnboardingScreen), findsOneWidget);
    expect(find.text('Explore Intercity Transit'), findsOneWidget);

    // 2. User taps Skip
    await tester.tap(find.text('Skip'));
    await tester.pumpAndSettle();

    // Verify storage write was invoked to remember onboarding completion
    verify(() => mockStorage.write(key: 'waypoint_has_completed_onboarding', value: 'true')).called(1);

    // Verify user reaches Explore screen with FirstRunWelcomeCard
    expect(find.byType(FirstRunWelcomeCard), findsOneWidget);
    expect(find.text('Welcome to WayPoint! 🚌'), findsOneWidget);

    // 3. User taps corridor chip
    await tester.tap(find.text('Colombo ⇄ Galle'));
    await tester.pumpAndSettle();

    // 4. Simulate returning user on cold start / app restart
    when(() => mockStorage.read(key: 'waypoint_has_completed_onboarding'))
        .thenAnswer((_) async => 'true');

    await tester.pumpWidget(
      WayPointApp(
        storageService: storageService,
        apiClient: apiClient,
      ),
    );
    await tester.pumpAndSettle();

    // Onboarding must NOT be shown to returning users
    expect(find.byType(OnboardingScreen), findsNothing);
  });
}
