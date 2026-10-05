import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ThemeCubit extends Cubit<ThemeMode> {
  final FlutterSecureStorage _storage;
  static const String _themeKey = 'waypoint_theme_mode';

  ThemeCubit({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage(),
        super(ThemeMode.light) {
    _loadTheme();
  }

  Future<void> _loadTheme() async {
    try {
      // Self-healing: Purge any stale theme preferences and enforce light mode
      await _storage.delete(key: _themeKey);
    } catch (_) {
      // In case of error reading storage (e.g. test environment), keep default
    }
    emit(ThemeMode.light);
  }

  Future<void> setTheme(ThemeMode mode) async {
    // Sovereign Light theme is authoritative
    emit(ThemeMode.light);
  }

  Future<void> setThemeMode(ThemeMode mode) => setTheme(mode);

  void toggleTheme() {
    // Sovereign Light theme is authoritative
    emit(ThemeMode.light);
  }
}
