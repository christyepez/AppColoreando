import 'package:app_coloreando/src/sync/offline_sync_queue.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  setUp(() => SharedPreferences.setMockInitialValues({}));

  test('offline queue persists and deduplicates operation id', () async {
    final queue = OfflineSyncQueue();
    final first = QueuedProgressOperation(
      operationId: 'op-1',
      artworkId: 'art-1',
      completedRegionIds: {1, 2},
      clientRevision: 0,
      isFavorite: false,
      isDownloaded: true,
      createdAtUtc: DateTime.utc(2026, 9, 8, 10),
    );
    await queue.enqueue(first);
    await queue.enqueue(QueuedProgressOperation(
      operationId: 'op-1',
      artworkId: 'art-1',
      completedRegionIds: {1, 2, 3},
      clientRevision: 0,
      isFavorite: true,
      isDownloaded: true,
      createdAtUtc: DateTime.utc(2026, 9, 8, 11),
    ));

    final items = await queue.load();
    expect(items, hasLength(1));
    expect(items.single.completedRegionIds, {1, 2, 3});
    expect(items.single.isFavorite, isTrue);
  });

  test('replaceArtworkOperations keeps only latest artwork state', () async {
    final queue = OfflineSyncQueue();
    for (var i = 0; i < 2; i++) {
      await queue.replaceArtworkOperations(QueuedProgressOperation(
        operationId: 'op-$i',
        artworkId: 'art-1',
        completedRegionIds: {1, 2, i + 3},
        clientRevision: i,
        isFavorite: false,
        isDownloaded: true,
        createdAtUtc: DateTime.utc(2026, 9, 8, 10 + i),
      ));
    }
    expect(await queue.load(), hasLength(1));
  });
}
