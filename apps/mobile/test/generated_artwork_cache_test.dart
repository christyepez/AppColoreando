import 'package:app_coloreando/src/catalog/generated_artwork_cache.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  setUp(() => SharedPreferences.setMockInitialValues({}));

  test('generated artwork cache persists metadata and bundle', () async {
    final cache = GeneratedArtworkCache(
      now: () => DateTime.utc(2026, 9, 14, 10),
    );
    await cache.save(
      'art-1',
      {'title': 'Andes'},
      {'palette': <Object?>[], 'regions': <Object?>[]},
    );

    final entry = await cache.load('art-1');
    expect(entry, isNotNull);
    expect(entry!.metadata['title'], 'Andes');
    expect(await cache.listIds(), ['art-1']);
  });

  test('expired cache entries are evicted', () async {
    var now = DateTime.utc(2026, 9, 14, 10);
    final cache = GeneratedArtworkCache(
      ttl: const Duration(days: 1),
      now: () => now,
    );
    await cache.save('art-1', {'title': 'A'}, {'schemaVersion': '2.2'});
    now = now.add(const Duration(days: 2));

    expect(await cache.load('art-1'), isNull);
    expect(await cache.listIds(), isEmpty);
  });

  test('cache uses LRU order and evicts oldest entry', () async {
    final cache = GeneratedArtworkCache(maxEntries: 2);
    await cache.save('art-1', {'title': 'A'}, {'schemaVersion': '2.2'});
    await cache.save('art-2', {'title': 'B'}, {'schemaVersion': '2.2'});
    await cache.load('art-1');
    await cache.save('art-3', {'title': 'C'}, {'schemaVersion': '2.2'});

    expect(await cache.listIds(), ['art-3', 'art-1']);
    expect(await cache.load('art-2'), isNull);
  });
}
