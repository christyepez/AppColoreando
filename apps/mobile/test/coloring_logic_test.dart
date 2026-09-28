import 'dart:ui' as ui;

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

  test('spatial index narrows hit-test candidates for 2500 regions', () {
    final regions = <DemoRegion>[];
    var id = 1;
    for (var y = 0; y < 50; y++) {
      for (var x = 0; x < 50; x++) {
        final left = x / 50.0;
        final top = y / 50.0;
        regions.add(DemoRegion(id: id++, colorId: ((x + y) % 150) + 1, points: [
          Offset(left, top), Offset((x + 1) / 50.0, top),
          Offset((x + 1) / 50.0, (y + 1) / 50.0),
          Offset(left, (y + 1) / 50.0),
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

  test('custom painter renders 2500 regions with a 150-color palette', () {
    final palette = List.generate(
      150,
      (index) => DemoColor(
        index + 1,
        Color.fromARGB(
          255,
          (index * 47) % 256,
          (index * 83) % 256,
          (index * 131) % 256,
        ),
      ),
    );
    final regions = <DemoRegion>[];
    var id = 1;
    for (var y = 0; y < 50; y++) {
      for (var x = 0; x < 50; x++) {
        final left = x / 50.0;
        final top = y / 50.0;
        regions.add(DemoRegion(
          id: id,
          colorId: ((id - 1) % 150) + 1,
          points: [
            Offset(left, top),
            Offset((x + 1) / 50.0, top),
            Offset((x + 1) / 50.0, (y + 1) / 50.0),
            Offset(left, (y + 1) / 50.0),
          ],
          labelVisibleAtBase: false,
          labelMinZoom: 4,
        ));
        id++;
      }
    }
    final artwork = DemoArtwork(
      id: 'render-stress',
      title: 'Render Stress',
      countryCode: 'EC',
      difficulty: 5,
      category: 'QA',
      palette: palette,
      regions: regions,
      variant: -4,
    );
    final recorder = ui.PictureRecorder();
    final canvas = Canvas(recorder);
    final stopwatch = Stopwatch()..start();
    ColoringPainter(
      artwork: artwork,
      selectedColorId: 1,
      completedRegionIds: const <int>{},
      highlightedRegionId: null,
      paintRevision: 0,
      currentScale: 1,
    ).paint(canvas, const Size(600, 600));
    stopwatch.stop();
    final picture = recorder.endRecording();

    expect(picture, isNotNull);
    expect(stopwatch.elapsedMilliseconds, lessThan(5000));
  }, tags: ['benchmark']);


  test('themed demo collections use distinct scene geometry', () {
    final andes = demoArtworkById('cotopaxi-sunrise');
    final space = demoArtworkById('stellar-fighter');
    final comic = demoArtworkById('urban-hero');
    final anime = demoArtworkById('anime-adventurer');

    expect(andes.regions.length, greaterThan(12));
    expect(space.regions.length, greaterThan(12));
    expect(comic.regions.length, greaterThan(12));
    expect(anime.regions.length, greaterThan(12));

    expect(andes.regions.first.points, isNot(equals(space.regions.first.points)));
    expect(comic.regions[4].path(const Size(1, 1)).contains(const Offset(.5, .34)), isTrue);
    expect(anime.regions[1].path(const Size(1, 1)).contains(const Offset(.5, .42)), isTrue);
  });


  test('initial themed packs contain 20 base artworks each', () {
    final andean = demoArtworks.where((x) => x.category.contains('Andin') || x.category.contains('Fauna Andina')).length;
    final space = demoArtworks.where((x) => x.category.contains('Space Opera') || x.category.contains('Galact')).length;
    final comic = demoArtworks.where((x) => x.category.contains('Comic')).length;
    final anime = demoArtworks.where((x) => x.category.contains('Anime') || x.category == 'Chibi').length;

    expect(andean, greaterThanOrEqualTo(20));
    expect(space, greaterThanOrEqualTo(20));
    expect(comic, greaterThanOrEqualTo(20));
    expect(anime, greaterThanOrEqualTo(20));
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
