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
      final savedTheme = await _storage.read(key: _themeKey);
      if (savedTheme == 'light') {
        emit(ThemeMode.light);
      } else if (savedTheme == 'dark') {
        emit(ThemeMode.dark);
      } else if (savedTheme == 'system') {
        emit(ThemeMode.system);
      }
    } catch (_) {
      // In case of error reading storage (e.g. test environment), keep default
    }
  }

  Future<void> setTheme(ThemeMode mode) async {
    emit(mode);
    try {
      String value = 'system';
      if (mode == ThemeMode.light) value = 'light';
      if (mode == ThemeMode.dark) value = 'dark';
      await _storage.write(key: _themeKey, value: value);
    } catch (_) {
      // Ignore storage errors in test mode
    }
  }

  Future<void> setThemeMode(ThemeMode mode) => setTheme(mode);

  void toggleTheme() {
    if (state == ThemeMode.dark) {
      setTheme(ThemeMode.light);
    } else {
      setTheme(ThemeMode.dark);
    }
  }
}
