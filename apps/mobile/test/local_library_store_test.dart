import 'package:app_coloreando/src/coloring/local_library_store.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() => SharedPreferences.setMockInitialValues({}));

  test('favorites are persisted and can be removed', () async {
    final store = LocalLibraryStore();
    await store.setFavorite('art-1', true);
    expect((await store.load()).favoriteIds, ['art-1']);
    await store.setFavorite('art-1', false);
    expect((await store.load()).favoriteIds, isEmpty);
  });

  test('recent artwork is unique, newest first and capped', () async {
    final store = LocalLibraryStore();
    for (var i = 0; i < 25; i++) {
      await store.markRecent('art-$i');
    }
    await store.markRecent('art-10');
    final recent = (await store.load()).recentIds;
    expect(recent.first, 'art-10');
    expect(recent.where((x) => x == 'art-10'), hasLength(1));
    expect(recent.length, 20);
  });
}
