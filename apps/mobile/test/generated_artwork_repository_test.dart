import 'package:app_coloreando/src/catalog/generated_artwork_repository.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('generated bundle becomes playable artwork and applies overrides', () {
    final repository = GeneratedArtworkRepository('http://localhost:8080', Dio());
    final artwork = repository.decode(
      '11111111-1111-1111-1111-111111111111',
      {'title': 'Generated Duck', 'countryCode': 'EC', 'difficulty': 2},
      {
        'schemaVersion': '2.2',
        'styleCode': 'aura',
        'palette': [
          {'id': 1, 'hex': '#F4D35E', 'name': 'Amarillo Sol'},
          {'id': 2, 'hex': '#F29E4C', 'name': 'Naranja Mandarina'},
        ],
        'regions': [
          {
            'id': 7,
            'colorId': 2,
            'svgPath': 'M 0.1 0.1 L 0.9 0.1 L 0.9 0.9 L 0.1 0.9 Z',
            'labelX': 0.5,
            'labelY': 0.5,
            'labelVisibleAtBase': true,
            'bounds': {'x': 0.1, 'y': 0.1, 'width': 0.8, 'height': 0.8},
          }
        ],
        'adjustments': [
          {
            'regionId': 7,
            'colorHex': '#123456',
            'labelVisibleAtBase': false,
          }
        ],
      },
    );

    expect(artwork.title, 'Generated Duck');
    expect(artwork.effectLabel, 'aura');
    expect(artwork.regions, hasLength(1));
    final region = artwork.regions.single;
    expect(region.contains(const Offset(.5, .5)), isTrue);
    expect(region.contains(const Offset(.02, .02)), isFalse);
    expect(region.labelOffset, const Offset(.5, .5));
    expect(region.labelVisibleAtBase, isFalse);
    expect(artwork.color(region.colorId).color, const Color(0xFF123456));
  });
}
