import 'dart:typed_data';

import 'package:app_coloreando/src/catalog/demo_artwork.dart';
import 'package:app_coloreando/src/config/app_config.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:path_drawing/path_drawing.dart';

final generatedArtworkRepositoryProvider = Provider<GeneratedArtworkRepository>((ref) {
  final config = ref.watch(appConfigProvider);
  return GeneratedArtworkRepository(config.apiBaseUrl, Dio());
});

class GeneratedArtworkRepository {
  GeneratedArtworkRepository(this.apiRoot, this._dio);
  final String apiRoot;
  final Dio _dio;

  Future<DemoArtwork?> load(String artworkId) async {
    if (!_looksLikeGuid(artworkId)) return null;
    try {
      final response = await _dio.get<Map<String, Object?>>('$apiRoot/api/catalog/artworks/$artworkId');
      final metadata = response.data;
      if (metadata == null) return null;
      final bundleUri = _bundleUri(metadata);
      if (bundleUri == null) return null;
      final bundleResponse = await _dio.getUri<Map<String, Object?>>(bundleUri);
      final bundle = bundleResponse.data;
      return bundle == null ? null : decode(artworkId, metadata, bundle);
    } on DioException {
      return null;
    } on FormatException {
      return null;
    }
  }

  Uri? _bundleUri(Map<String, Object?> metadata) {
    var value = metadata['assetUrl']?.toString();
    final assets = (metadata['assets'] as List?) ?? const [];
    if (value == null || value.isEmpty) {
      for (final raw in assets) {
        final asset = Map<String, Object?>.from(raw as Map);
        if (asset['isPrimary'] == true && asset['contentType']?.toString() == 'application/json') {
          value = asset['uri']?.toString();
          break;
        }
      }
    }
    if (value == null || value.isEmpty) return null;
    final candidate = Uri.parse(value);
    return candidate.hasScheme ? candidate : Uri.parse('$apiRoot/').resolve(value);
  }
  DemoArtwork decode(
    String artworkId,
    Map<String, Object?> metadata,
    Map<String, Object?> bundle,
  ) {
    final paletteJson = (bundle['palette'] as List?) ?? const [];
    final regionsJson = (bundle['regions'] as List?) ?? const [];
    final adjustmentsJson = (bundle['adjustments'] as List?) ?? const [];
    final adjustments = <int, Map<String, Object?>>{};
    for (final raw in adjustmentsJson) {
      final item = Map<String, Object?>.from(raw as Map);
      final id = (item['regionId'] as num?)?.toInt();
      if (id != null) adjustments[id] = item;
    }

    final palette = <DemoColor>[
      for (final raw in paletteJson)
        DemoColor(
          (Map<String, Object?>.from(raw as Map)['id'] as num).toInt(),
          _parseColor(Map<String, Object?>.from(raw)['hex']!.toString()),
        ),
    ];
    var nextColorId = palette.isEmpty ? 1 : palette.map((x) => x.id).reduce((a, b) => a > b ? a : b) + 1;
    final regions = <DemoRegion>[];
    for (final raw in regionsJson) {
      final item = Map<String, Object?>.from(raw as Map);
      final id = (item['id'] as num).toInt();
      final adjustment = adjustments[id];
      var colorId = (item['colorId'] as num).toInt();
      final colorHex = adjustment?['colorHex']?.toString();
      if (colorHex != null && colorHex.isNotEmpty) {
        colorId = nextColorId++;
        palette.add(DemoColor(colorId, _parseColor(colorHex)));
      }
      final bounds = Map<String, Object?>.from(item['bounds'] as Map);
      final left = (bounds['x'] as num).toDouble();
      final top = (bounds['y'] as num).toDouble();
      final width = (bounds['width'] as num).toDouble();
      final height = (bounds['height'] as num).toDouble();
      final svgPath = item['svgPath']!.toString();
      final normalizedPath = parseSvgPathData(svgPath);
      regions.add(DemoRegion(
        id: id,
        colorId: colorId,
        points: [Offset(left, top), Offset(left + width, top + height)],
        labelOffset: Offset(
          (item['labelX'] as num?)?.toDouble() ?? left + width / 2,
          (item['labelY'] as num?)?.toDouble() ?? top + height / 2,
        ),
        labelVisibleAtBase: adjustment?['labelVisibleAtBase'] as bool? ??
            (item['labelVisibleAtBase'] as bool? ?? true),
        pathBuilder: (size) => normalizedPath.transform(Float64List.fromList([
          size.width, 0, 0, 0,
          0, size.height, 0, 0,
          0, 0, 1, 0,
          0, 0, 0, 1,
        ])),
      ));
    }

    if (palette.isEmpty || regions.isEmpty) {
      throw const FormatException('Generated bundle has no playable data.');
    }
    return DemoArtwork(
      id: artworkId,
      title: metadata['title']?.toString() ?? 'Artwork',
      countryCode: metadata['countryCode']?.toString() ?? '',
      difficulty: (metadata['difficulty'] as num?)?.toInt() ?? 2,
      category: 'Generado',
      palette: palette,
      regions: regions,
      variant: -2,
      effectLabel: bundle['styleCode']?.toString(),
    );
  }

  static Color _parseColor(String value) {
    final hex = value.replaceFirst('#', '');
    if (hex.length != 6) throw const FormatException('Invalid palette color.');
    return Color(0xFF000000 | int.parse(hex, radix: 16));
  }

  static bool _looksLikeGuid(String value) => RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$',
  ).hasMatch(value);
}
