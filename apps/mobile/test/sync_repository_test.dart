import 'package:app_coloreando/src/sync/sync_repository.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('sync payload is deterministic and clamps completion', () {
    const payload = SyncProgressPayload(
      completedRegionIds: {3, 1, 2},
      regionCount: 4,
      clientRevision: 7,
      clientOperationId: 'op-1',
      isFavorite: true,
      isDownloaded: false,
    );

    expect(payload.toJson(), {
      'completionPercent': 75.0,
      'completedRegionIds': [1, 2, 3],
      'isFavorite': true,
      'isDownloaded': false,
      'clientRevision': 7,
      'clientOperationId': 'op-1',
    });
  });

  test('sync result exposes applied/conflict semantics', () {
    const applied = SyncResult(status: 'Applied', serverRevision: 2);
    const conflict = SyncResult(status: 'Conflict', conflictReason: 'revision');
    expect(applied.applied, isTrue);
    expect(applied.conflict, isFalse);
    expect(conflict.conflict, isTrue);
  });
}
