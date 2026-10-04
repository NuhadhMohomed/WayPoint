import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/core/storage/local_cache_service.dart';
import 'package:waypoint_mobile/core/network/api_client.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('LocalCacheService saves and retrieves auth token and first run flag', () async {
    final cache = LocalCacheService();
    await cache.saveAuthToken('sample_token_xyz');
    expect(await cache.getAuthToken(), 'sample_token_xyz');

    await cache.setFirstRunCompleted(true);
    expect(await cache.isFirstRunCompleted(), isTrue);

    await cache.clearAuth();
    expect(await cache.getAuthToken(), isNull);
  });

  test('LocalCacheService saves and retrieves saved travelers and recent searches', () async {
    final cache = LocalCacheService();
    await cache.saveSavedTravelers([
      {'name': 'Amara Perera', 'nic': '199512345678'},
    ]);
    final travelers = await cache.getSavedTravelers();
    expect(travelers.length, 1);
    expect(travelers.first['name'], 'Amara Perera');

    await cache.saveRecentSearches(['Colombo -> Kandy', 'Colombo -> Ella']);
    final searches = await cache.getRecentSearches();
    expect(searches, contains('Colombo -> Kandy'));
  });

  test('ApiClient configures base URL and provides humanized error messages', () {
    final client = ApiClient();
    expect(client.dio.options.connectTimeout, const Duration(seconds: 10));

    final humanized = ApiClient.humanizeError(Exception('Network connection refused'));
    expect(humanized, contains('Unable to connect'));
  });
}
