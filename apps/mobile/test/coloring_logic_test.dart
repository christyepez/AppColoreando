import 'package:app_coloreando/src/catalog/demo_artwork.dart';
import 'package:app_coloreando/src/coloring/coloring_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('duck hit testing follows real silhouette regions', () {
    final artwork = demoArtworkById('duck-tropical');
    expect(hitTestRegion(artwork, const Offset(.79, .32))?.id, 6);
    expect(hitTestRegion(artwork, const Offset(.44, .50))?.id, 7);
    expect(hitTestRegion(artwork, const Offset(.50, .82))?.id, 2);
    expect(hitTestRegion(artwork, const Offset(1.2, .5)), isNull);
  });

  test('spatial index narrows hit-test candidates for 500 regions', () {
    final regions = <DemoRegion>[];
    var id = 1;
    for (var y = 0; y < 25; y++) {
      for (var x = 0; x < 20; x++) {
        final left = x / 20.0;
        final top = y / 25.0;
        regions.add(DemoRegion(id: id++, colorId: 1, points: [
          Offset(left, top), Offset((x + 1) / 20.0, top),
          Offset((x + 1) / 20.0, (y + 1) / 25.0),
          Offset(left, (y + 1) / 25.0),
        ]));
      }
    }
    final artwork = DemoArtwork(
      id: 'stress', title: 'Stress', countryCode: 'EC', difficulty: 5,
      category: 'QA', palette: const [DemoColor(1, Color(0xFF112233))],
      regions: regions, variant: -3,
    );
    final index = ArtworkSpatialIndex(artwork);
    expect(index.candidates(const Offset(.51, .51)).length, lessThan(50));
    expect(hitTestRegion(artwork, const Offset(.51, .51)), isNotNull);
  });

  test('region path cache reuses normalized and scaled geometry', () {
    final region = DemoRegion(
      id: 1, colorId: 1,
      points: const [Offset(0, 0), Offset(1, 0), Offset(1, 1), Offset(0, 1)],
    );
    expect(identical(region.path(const Size(1, 1)), region.path(const Size(1, 1))), isTrue);
    expect(identical(region.path(const Size(512, 512)), region.path(const Size(512, 512))), isTrue);
  });
}
