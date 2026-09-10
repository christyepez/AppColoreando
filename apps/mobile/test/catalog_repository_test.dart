import 'package:app_coloreando/src/catalog/catalog_repository.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('catalog filter emits only active filters', () {
    const filter = CatalogFilter(search: ' Andes ', countryCode: 'EC', difficulty: 2, page: 3, pageSize: 12);
    expect(filter.toQuery(), {
      'search': 'Andes',
      'countryCode': 'EC',
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
}
