import 'package:flutter/material.dart';

class AppTheme {
  static final light = ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(
      seedColor: const Color(0xFF147D6F),
      primary: const Color(0xFF147D6F),
      secondary: const Color(0xFFE0552F),
      tertiary: const Color(0xFF2E5AAC),
    ),
    cardTheme: const CardThemeData(elevation: 0, shape: RoundedRectangleBorder(borderRadius: BorderRadius.all(Radius.circular(8)))),
  );

  static final dark = ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF147D6F), brightness: Brightness.dark),
    cardTheme: const CardThemeData(elevation: 0, shape: RoundedRectangleBorder(borderRadius: BorderRadius.all(Radius.circular(8)))),
  );
}
