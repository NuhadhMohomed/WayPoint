import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class LocalCacheService {
  final FlutterSecureStorage _storage;
  final Map<String, String> _memoryFallback = {};

  static const String _keyToken = 'waypoint_auth_token';
  static const String _keyUser = 'waypoint_user_profile';
  static const String _keyFirstRun = 'waypoint_first_run_completed';
  static const String _keyTravelers = 'waypoint_saved_travelers';
  static const String _keyRecentSearches = 'waypoint_recent_searches';
  static const String _keyCachedTickets = 'waypoint_offline_tickets';

  LocalCacheService({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage();

  Future<void> _write(String key, String value) async {
    _memoryFallback[key] = value;
    try {
      await _storage.write(key: key, value: value);
    } catch (_) {}
  }

  Future<String?> _read(String key) async {
    try {
      final val = await _storage.read(key: key);
      if (val != null) return val;
    } catch (_) {}
    return _memoryFallback[key];
  }

  Future<void> _delete(String key) async {
    _memoryFallback.remove(key);
    try {
      await _storage.delete(key: key);
    } catch (_) {}
  }

  // Auth & Token
  Future<void> saveAuthToken(String token) => _write(_keyToken, token);
  Future<String?> getAuthToken() => _read(_keyToken);
  Future<void> clearAuth() async {
    await _delete(_keyToken);
    await _delete(_keyUser);
  }

  // User Profile
  Future<void> saveUserData(Map<String, dynamic> user) =>
      _write(_keyUser, jsonEncode(user));

  Future<Map<String, dynamic>?> getUserData() async {
    final str = await _read(_keyUser);
    if (str == null || str.isEmpty) return null;
    try {
      return jsonDecode(str) as Map<String, dynamic>;
    } catch (_) {
      return null;
    }
  }

  // First Run / Onboarding
  Future<void> setFirstRunCompleted(bool completed) =>
      _write(_keyFirstRun, completed ? 'true' : 'false');

  Future<bool> isFirstRunCompleted() async {
    final str = await _read(_keyFirstRun);
    return str == 'true';
  }

  // Saved Travelers
  Future<void> saveSavedTravelers(List<Map<String, String>> travelers) =>
      _write(_keyTravelers, jsonEncode(travelers));

  Future<List<Map<String, String>>> getSavedTravelers() async {
    final str = await _read(_keyTravelers);
    if (str == null || str.isEmpty) return [];
    try {
      final list = jsonDecode(str) as List;
      return list.map((e) => Map<String, String>.from(e as Map)).toList();
    } catch (_) {
      return [];
    }
  }

  // Recent Searches
  Future<void> saveRecentSearches(List<String> searches) =>
      _write(_keyRecentSearches, jsonEncode(searches));

  Future<List<String>> getRecentSearches() async {
    final str = await _read(_keyRecentSearches);
    if (str == null || str.isEmpty) {
      return ['Colombo ⇄ Kandy', 'Colombo ⇄ Ella', 'Colombo ⇄ Galle'];
    }
    try {
      final list = jsonDecode(str) as List;
      return list.map((e) => e.toString()).toList();
    } catch (_) {
      return ['Colombo ⇄ Kandy', 'Colombo ⇄ Ella', 'Colombo ⇄ Galle'];
    }
  }

  // Offline Tickets
  Future<void> cacheTickets(List<Map<String, dynamic>> tickets) =>
      _write(_keyCachedTickets, jsonEncode(tickets));

  Future<List<Map<String, dynamic>>> getCachedTickets() async {
    final str = await _read(_keyCachedTickets);
    if (str == null || str.isEmpty) return [];
    try {
      final list = jsonDecode(str) as List;
      return list.map((e) => Map<String, dynamic>.from(e as Map)).toList();
    } catch (_) {
      return [];
    }
  }
}
