import 'package:app_coloreando/src/catalog/catalog_repository.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('catalog filter emits only active filters', () {
    const filter = CatalogFilter(search: ' Andes ', countryCode: 'EC', collectionId: 'collection-1', difficulty: 2, page: 3, pageSize: 12);
    expect(filter.toQuery(), {
      'search': 'Andes',
      'countryCode': 'EC',
      'collectionId': 'collection-1',
      'difficulty': 2,
      'page': 3,
      'pageSize': 12,
    });
  });

  test('catalog page parses API payload', () {
    final page = CatalogPageResult.fromJson({
      'items': [
        {'id': 'a1', 'title': 'Andes', 'countryCode': 'EC', 'difficulty': 2, 'regionCount': 36, 'thumbnailUrl': '/a.png'}
      ],
      'page': 1,
      'pageSize': 24,
      'total': 1,
    });
    expect(page.items.single.title, 'Andes');
    expect(page.total, 1);
  });

  test('catalog discovery parses countries and collections', () {
    final country = CatalogCountry.fromJson({'code': 'EC', 'name': 'Ecuador'});
    final collection = CatalogCollection.fromJson({'id': 'c1', 'name': 'Andes', 'artworkCount': 8});
    expect(country.code, 'EC');
    expect(collection.name, 'Andes');
    expect(collection.artworkCount, 8);
  });
  test('recommendation profile infers dominant country and average difficulty', () {
    final profile = RecommendationProfile.infer([
      const CatalogArtwork(id: '1', title: 'A', countryCode: 'EC', difficulty: 2, regionCount: 20, thumbnailUrl: null),
      const CatalogArtwork(id: '2', title: 'B', countryCode: 'EC', difficulty: 3, regionCount: 30, thumbnailUrl: null),
      const CatalogArtwork(id: '3', title: 'C', countryCode: 'CO', difficulty: 4, regionCount: 40, thumbnailUrl: null),
    ]);
    expect(profile.countryCode, 'EC');
    expect(profile.difficulty, 3);
  });}
