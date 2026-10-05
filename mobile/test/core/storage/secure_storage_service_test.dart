import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:waypoint_mobile/core/storage/secure_storage_service.dart';

class MockFlutterSecureStorage extends Mock implements FlutterSecureStorage {}

void main() {
  late MockFlutterSecureStorage mockStorage;
  late SecureStorageService service;

  setUp(() {
    mockStorage = MockFlutterSecureStorage();
    service = SecureStorageService(storage: mockStorage);
  });

  test('isOnboardingCompleted returns false when key is absent', () async {
    when(() => mockStorage.read(key: 'waypoint_has_completed_onboarding'))
        .thenAnswer((_) async => null);

    final completed = await service.isOnboardingCompleted();
    expect(completed, isFalse);
  });

  test('isOnboardingCompleted returns true when key is set to true', () async {
    when(() => mockStorage.read(key: 'waypoint_has_completed_onboarding'))
        .thenAnswer((_) async => 'true');

    final completed = await service.isOnboardingCompleted();
    expect(completed, isTrue);
  });

  test('setOnboardingCompleted writes true string to storage by default', () async {
    when(() => mockStorage.write(
          key: 'waypoint_has_completed_onboarding',
          value: 'true',
        )).thenAnswer((_) async => {});

    await service.setOnboardingCompleted();
    verify(() => mockStorage.write(
          key: 'waypoint_has_completed_onboarding',
          value: 'true',
        )).called(1);
  });
}
