import 'package:app_coloreando/src/catalog/demo_artwork.dart';
import 'package:app_coloreando/src/coloring/coloring_page.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('hitTestRegion resolves deterministic demo regions', () {
    final artwork = demoArtworkById('andean-geometry');

    expect(hitTestRegion(artwork, const Offset(.01, .01))?.id, 1);
    expect(hitTestRegion(artwork, const Offset(.99, .99))?.id, 36);
    expect(hitTestRegion(artwork, const Offset(1.2, .5)), isNull);
  });
}
