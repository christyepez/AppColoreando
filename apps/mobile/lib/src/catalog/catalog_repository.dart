import 'package:app_coloreando/src/config/app_config.dart';
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

final catalogRepositoryProvider = Provider<CatalogRepository>((ref) {
  final config = ref.watch(appConfigProvider);
  return CatalogRepository(Dio(BaseOptions(baseUrl: '${config.apiBaseUrl}/api')));
});

class CatalogFilter {
  const CatalogFilter({this.search, this.countryCode, this.categoryId, this.difficulty, this.licensedOnly, this.page = 1, this.pageSize = 24});
  final String? search;
  final String? countryCode;
  final String? categoryId;
  final int? difficulty;
  final bool? licensedOnly;
  final int page;
  final int pageSize;

  Map<String, Object?> toQuery() => {
        if (search != null && search!.trim().isNotEmpty) 'search': search!.trim(),
        if (countryCode != null && countryCode!.isNotEmpty) 'countryCode': countryCode,
        if (categoryId != null && categoryId!.isNotEmpty) 'categoryId': categoryId,
        if (difficulty != null) 'difficulty': difficulty,
        if (licensedOnly != null) 'licensedOnly': licensedOnly,
        'page': page,
        'pageSize': pageSize,
      };
}

class CatalogArtwork {
  const CatalogArtwork({required this.id, required this.title, required this.countryCode, required this.difficulty, required this.regionCount, required this.thumbnailUrl});
  final String id;
  final String title;
  final String? countryCode;
  final int difficulty;
  final int regionCount;
  final String? thumbnailUrl;

  factory CatalogArtwork.fromJson(Map<String, Object?> json) => CatalogArtwork(
        id: json['id']!.toString(),
        title: json['title']?.toString() ?? '',
        countryCode: json['countryCode']?.toString(),
        difficulty: (json['difficulty'] as num?)?.toInt() ?? 1,
        regionCount: (json['regionCount'] as num?)?.toInt() ?? 0,
        thumbnailUrl: json['thumbnailUrl']?.toString(),
      );
}

class CatalogPageResult {
  const CatalogPageResult({required this.items, required this.page, required this.pageSize, required this.total});
  final List<CatalogArtwork> items;
  final int page;
  final int pageSize;
  final int total;

  factory CatalogPageResult.fromJson(Map<String, Object?> json) => CatalogPageResult(
        items: ((json['items'] as List?) ?? const []).map((x) => CatalogArtwork.fromJson(Map<String, Object?>.from(x as Map))).toList(),
        page: (json['page'] as num?)?.toInt() ?? 1,
        pageSize: (json['pageSize'] as num?)?.toInt() ?? 24,
        total: (json['total'] as num?)?.toInt() ?? 0,
      );
}

class CatalogRepository {
  CatalogRepository(this._dio);
  final Dio _dio;

  Future<CatalogPageResult> search(CatalogFilter filter) async {
    final response = await _dio.get<Map<String, Object?>>('/catalog/artworks', queryParameters: filter.toQuery());
    return CatalogPageResult.fromJson(response.data ?? const {});
  }
}
