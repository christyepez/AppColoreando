import 'package:app_coloreando/src/catalog/demo_artwork.dart';
import 'package:app_coloreando/src/coloring/coloring_page.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('duck hit testing follows real silhouette regions', () {
    final artwork = demoArtworkById('duck-tropical');

    expect(hitTestRegion(artwork, const Offset(.79, .32))?.id, 6); // beak
    expect(hitTestRegion(artwork, const Offset(.44, .50))?.id, 7); // wing
    expect(hitTestRegion(artwork, const Offset(.50, .82))?.id, 2); // water
    expect(hitTestRegion(artwork, const Offset(1.2, .5)), isNull);
  });
}
