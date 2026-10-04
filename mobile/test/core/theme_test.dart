import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/core/theme/app_theme.dart';
import 'package:waypoint_mobile/core/theme/theme_cubit.dart';
import 'package:waypoint_mobile/core/widgets/waypoint_logo.dart';

void main() {
  test('AppTheme defines #32DE84 as primary color and calibrated tokens', () {
    expect(AppTheme.primaryColor, const Color(0xFF32DE84));
    expect(AppTheme.onPrimaryColor, const Color(0xFF042611));
    expect(AppTheme.primaryContainerLight, const Color(0xFFD9FBE8));
    expect(AppTheme.primaryContainerDark, const Color(0xFF0E3820));
    expect(AppTheme.lightBackground, const Color(0xFFF8FAFC));
    expect(AppTheme.darkBackground, const Color(0xFF090D16));
    expect(AppTheme.darkCardBackground, const Color(0xFF131B2E));
    expect(AppTheme.darkBorderColor, const Color(0xFF23304D));

    expect(AppTheme.lightTheme.colorScheme.primary, const Color(0xFF32DE84));
    expect(AppTheme.darkTheme.colorScheme.primary, const Color(0xFF32DE84));
    expect(AppTheme.darkTheme.scaffoldBackgroundColor, const Color(0xFF090D16));
  });

  test('ThemeCubit initializes and changes theme modes', () {
    final cubit = ThemeCubit();
    expect(cubit.state, ThemeMode.system);
    cubit.setTheme(ThemeMode.dark);
    expect(cubit.state, ThemeMode.dark);
    cubit.setTheme(ThemeMode.light);
    expect(cubit.state, ThemeMode.light);
  });

  testWidgets('WayPointLogo renders brand text and icon cleanly', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: WayPointLogo(size: 64, showText: true),
        ),
      ),
    );
    expect(find.text('WayPoint'), findsOneWidget);
    expect(find.byType(WayPointLogo), findsOneWidget);
  });
}
