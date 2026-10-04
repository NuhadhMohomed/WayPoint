import 'package:flutter/material.dart';

class AppTheme {
  // Authoritative Brand Color Tokens Anchored on #32DE84 (Spring Green)
  static const primaryColor = Color(0xFF32DE84);
  static const onPrimaryColor = Color(0xFF042611); // High contrast on #32DE84 (WCAG AAA >= 7:1)
  static const primaryDark = Color(0xFF1EAE60);
  
  static const primaryContainerLight = Color(0xFFD9FBE8);
  static const primaryContainerDark = Color(0xFF0E3820);

  // Semantic Status Accents
  static const secondaryColor = Color(0xFFF59E0B); // Sunset Amber (10m seat hold & buffer warning)
  static const accentAmber = secondaryColor;
  static const tertiaryColor = Color(0xFF0284C7); // Sky Blue (Corridors & expressways)
  static const errorColor = Color(0xFFEF4444); // Crimson Alert

  // Surfaces & Backgrounds (Light Theme)
  static const lightBackground = Color(0xFFF8FAFC);
  static const lightSurface = Color(0xFFFFFFFF);
  static const lightSurfaceSubdued = Color(0xFFF1F5F9);
  static const lightBorderColor = Color(0xFFE2E8F0);
  static const textPrimaryLight = Color(0xFF0F172A);
  static const textMutedLight = Color(0xFF64748B);

  // Surfaces & Backgrounds (Dark Theme)
  static const darkBackground = Color(0xFF090D16); // Midnight Navy/Black
  static const darkCardBackground = Color(0xFF131B2E); // Deep Navy Slate
  static const darkSurfaceSubdued = Color(0xFF1A243B);
  static const darkBorderColor = Color(0xFF23304D);
  static const textPrimaryDark = Color(0xFFF8FAFC);
  static const textMutedDark = Color(0xFF94A3B8);

  // Backward compatibility alias
  static const cardBackground = darkCardBackground;
  static const onSurfaceText = textPrimaryLight;
  static const onSurfaceMuted = textMutedLight;

  static ThemeData get lightTheme {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.light,
      primaryColor: primaryColor,
      scaffoldBackgroundColor: lightBackground,
      colorScheme: const ColorScheme.light(
        primary: primaryColor,
        onPrimary: onPrimaryColor,
        primaryContainer: primaryContainerLight,
        secondary: secondaryColor,
        tertiary: tertiaryColor,
        error: errorColor,
        surface: lightSurface,
        onSurface: textPrimaryLight,
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: lightSurface,
        foregroundColor: textPrimaryLight,
        elevation: 0,
        centerTitle: true,
      ),
      cardTheme: const CardThemeData(
        color: lightSurface,
        elevation: 0,
        shape: RoundedRectangleBorder(
          side: BorderSide(color: lightBorderColor, width: 1),
          borderRadius: BorderRadius.all(Radius.circular(16)),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: primaryColor,
          foregroundColor: onPrimaryColor,
          elevation: 0,
          minimumSize: const Size.fromHeight(48),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
          textStyle: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
        ),
      ),
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: lightSurface,
        selectedItemColor: onPrimaryColor,
        unselectedItemColor: textMutedLight,
        elevation: 8,
      ),
    );
  }

  static ThemeData get darkTheme {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      primaryColor: primaryColor,
      scaffoldBackgroundColor: darkBackground,
      colorScheme: const ColorScheme.dark(
        primary: primaryColor,
        onPrimary: onPrimaryColor,
        primaryContainer: primaryContainerDark,
        secondary: secondaryColor,
        tertiary: tertiaryColor,
        error: errorColor,
        surface: darkCardBackground,
        onSurface: textPrimaryDark,
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: darkBackground,
        foregroundColor: textPrimaryDark,
        elevation: 0,
        centerTitle: true,
      ),
      cardTheme: const CardThemeData(
        color: darkCardBackground,
        elevation: 0,
        shape: RoundedRectangleBorder(
          side: BorderSide(color: darkBorderColor, width: 1),
          borderRadius: BorderRadius.all(Radius.circular(16)),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: primaryColor,
          foregroundColor: onPrimaryColor,
          elevation: 0,
          minimumSize: const Size.fromHeight(48),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
          textStyle: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
        ),
      ),
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: darkCardBackground,
        selectedItemColor: primaryColor,
        unselectedItemColor: textMutedDark,
        elevation: 8,
      ),
    );
  }
}
