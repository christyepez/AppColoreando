import 'package:flutter/widgets.dart';

class AppStrings {
  const AppStrings._(this.homeTitle, this.homeSubtitle, this.sectionsTitle);

  final String homeTitle;
  final String homeSubtitle;
  final String sectionsTitle;

  static AppStrings of(Locale locale) {
    if (locale.languageCode == 'en') {
      return const AppStrings._(
        'Color your library',
        'Original color-by-number artwork with offline progress.',
        'Sections',
      );
    }
    return const AppStrings._(
      'Colorea tu biblioteca',
      'Dibujos originales por numeros con progreso sin conexion.',
      'Secciones',
    );
  }
}
