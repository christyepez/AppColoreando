import 'package:app_coloreando/src/config/app_config.dart';
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

final catalogRepositoryProvider = Provider<CatalogRepository>((ref) {
  final config = ref.watch(appConfigProvider);
  return CatalogRepository(Dio(BaseOptions(baseUrl: '${config.apiBaseUrl}/api')));
});

final dailyContentProvider = FutureProvider<DailyContent?>((ref) => ref.watch(catalogRepositoryProvider).daily());
final catalogEventsProvider = FutureProvider<List<CatalogEvent>>((ref) => ref.watch(catalogRepositoryProvider).events());

class CatalogFilter {
  const CatalogFilter({this.search, this.countryCode, this.categoryId, this.collectionId, this.difficulty, this.licensedOnly, this.page = 1, this.pageSize = 24});
  final String? search;
  final String? countryCode;
  final String? categoryId;
  final String? collectionId;
  final int? difficulty;
  final bool? licensedOnly;
  final int page;
  final int pageSize;

  Map<String, Object?> toQuery() => {
        if (search != null && search!.trim().isNotEmpty) 'search': search!.trim(),
        if (countryCode != null && countryCode!.isNotEmpty) 'countryCode': countryCode,
        if (categoryId != null && categoryId!.isNotEmpty) 'categoryId': categoryId,
        if (collectionId != null && collectionId!.isNotEmpty) 'collectionId': collectionId,
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

class DailyContent {
  const DailyContent({required this.date, required this.artwork});
  final DateTime date;
  final CatalogArtwork artwork;
  factory DailyContent.fromJson(Map<String, Object?> json) => DailyContent(
    date: DateTime.tryParse(json['date']?.toString() ?? '') ?? DateTime.now(),
    artwork: CatalogArtwork.fromJson(Map<String, Object?>.from(json['artwork'] as Map)),
  );
}

class CatalogEvent {
  const CatalogEvent({required this.collectionId, required this.name, required this.slug, required this.description, required this.countryCode, required this.artworkCount, required this.artworks});
  final String collectionId;
  final String name;
  final String slug;
  final String? description;
  final String? countryCode;
  final int artworkCount;
  final List<CatalogArtwork> artworks;
  factory CatalogEvent.fromJson(Map<String, Object?> json) => CatalogEvent(
    collectionId: json['collectionId']!.toString(),
    name: json['name']?.toString() ?? '',
    slug: json['slug']?.toString() ?? '',
    description: json['description']?.toString(),
    countryCode: json['countryCode']?.toString(),
    artworkCount: (json['artworkCount'] as num?)?.toInt() ?? 0,
    artworks: ((json['artworks'] as List?) ?? const []).map((x) => CatalogArtwork.fromJson(Map<String, Object?>.from(x as Map))).toList(),
  );
}

class CatalogCountry {
  const CatalogCountry({required this.code, required this.name});
  final String code;
  final String name;
  factory CatalogCountry.fromJson(Map<String, Object?> json) => CatalogCountry(
    code: json['code']?.toString() ?? '',
    name: json['name']?.toString() ?? '',
  );
}

class CatalogCollection {
  const CatalogCollection({required this.id, required this.name, required this.artworkCount});
  final String id;
  final String name;
  final int artworkCount;
  factory CatalogCollection.fromJson(Map<String, Object?> json) => CatalogCollection(
    id: json['id']!.toString(),
    name: json['name']?.toString() ?? '',
    artworkCount: (json['artworkCount'] as num?)?.toInt() ?? 0,
  );
}

class CatalogDiscovery {
  const CatalogDiscovery({required this.countries, required this.collections});
  final List<CatalogCountry> countries;
  final List<CatalogCollection> collections;
}

class RecommendationProfile {
  const RecommendationProfile({this.countryCode, this.difficulty});
  final String? countryCode;
  final int? difficulty;

  static RecommendationProfile infer(List<CatalogArtwork> signals) {
    if (signals.isEmpty) return const RecommendationProfile();
    final countries = <String, int>{};
    var difficultyTotal = 0;
    for (final item in signals) {
      final code = item.countryCode;
      if (code != null && code.isNotEmpty) countries[code] = (countries[code] ?? 0) + 1;
      difficultyTotal += item.difficulty;
    }
    String? country;
    if (countries.isNotEmpty) {
      country = countries.entries.reduce((a, b) => a.value >= b.value ? a : b).key;
    }
    final avg = (difficultyTotal / signals.length).round().clamp(1, 4);
    return RecommendationProfile(countryCode: country, difficulty: avg);
  }
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



  Future<CatalogArtwork?> getArtwork(String id) async {
    try {
      final response = await _dio.get<Map<String, Object?>>('/catalog/artworks/$id');
      final data = response.data;
      return data == null ? null : CatalogArtwork.fromJson(data);
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) return null;
      rethrow;
    }
  }

  Future<List<CatalogArtwork>> recommendations({required List<String> signalIds, int take = 8}) async {
    final uniqueIds = signalIds.where((x) => x.isNotEmpty).toSet().take(12).toList();
    final signals = <CatalogArtwork>[];
    for (final id in uniqueIds) {
      final item = await getArtwork(id);
      if (item != null) signals.add(item);
    }
    final profile = RecommendationProfile.infer(signals);
    var page = await search(CatalogFilter(countryCode: profile.countryCode, difficulty: profile.difficulty, pageSize: 48));
    var result = page.items.where((x) => !uniqueIds.contains(x.id)).take(take).toList();
    if (result.length < take && (profile.countryCode != null || profile.difficulty != null)) {
      page = await search(const CatalogFilter(pageSize: 48));
      final existing = result.map((x) => x.id).toSet();
      result.addAll(page.items.where((x) => !uniqueIds.contains(x.id) && !existing.contains(x.id)).take(take - result.length));
    }
    return result;
  }
  Future<CatalogDiscovery> discovery() async {
    final responses = await Future.wait([
      _dio.get<List<Object?>>('/catalog/countries'),
      _dio.get<List<Object?>>('/catalog/collections'),
    ]);
    final countries = (responses[0].data ?? const []).map((x) => CatalogCountry.fromJson(Map<String, Object?>.from(x as Map))).toList();
    final collections = (responses[1].data ?? const []).map((x) => CatalogCollection.fromJson(Map<String, Object?>.from(x as Map))).where((x) => x.artworkCount > 0).toList();
    return CatalogDiscovery(countries: countries, collections: collections);
  }
  Future<DailyContent?> daily({DateTime? date}) async {
    final response = await _dio.get<Map<String, Object?>>('/catalog/daily', queryParameters: date == null ? null : {'date': date.toIso8601String().split('T').first});
    final data = response.data;
    return data == null ? null : DailyContent.fromJson(data);
  }

  Future<List<CatalogEvent>> events() async {
    final response = await _dio.get<List<Object?>>('/catalog/events');
    return (response.data ?? const []).map((x) => CatalogEvent.fromJson(Map<String, Object?>.from(x as Map))).toList();
  }
}
