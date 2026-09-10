import 'package:app_coloreando/src/config/app_config.dart';
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

final syncRepositoryProvider = Provider<SyncRepository>((ref) {
  final config = ref.watch(appConfigProvider);
  return SyncRepository(Dio(BaseOptions(baseUrl: '${config.apiBaseUrl}/api')));
});

class SyncProgressPayload {
  const SyncProgressPayload({
    required this.completedRegionIds,
    required this.regionCount,
    required this.clientRevision,
    required this.clientOperationId,
    required this.isFavorite,
    required this.isDownloaded,
  });

  final Set<int> completedRegionIds;
  final int regionCount;
  final int clientRevision;
  final String clientOperationId;
  final bool isFavorite;
  final bool isDownloaded;

  Map<String, Object?> toJson() {
    final sorted = completedRegionIds.toList()..sort();
    final percent = regionCount <= 0 ? 0 : (sorted.length / regionCount * 100).clamp(0, 100).toDouble();
    return {
      'completionPercent': percent,
      'completedRegionIds': sorted,
      'isFavorite': isFavorite,
      'isDownloaded': isDownloaded,
      'clientRevision': clientRevision,
      'clientOperationId': clientOperationId,
    };
  }
}

class SyncRepository {
  SyncRepository(this._dio);
  final Dio _dio;

  Future<SyncResult> saveProgress({
    required String accessToken,
    required String artworkId,
    required SyncProgressPayload payload,
  }) async {
    final response = await _dio.put<Map<String, Object?>>(
      '/me/artworks/$artworkId/progress',
      options: Options(headers: {'Authorization': 'Bearer $accessToken'}),
      data: payload.toJson(),
    );
    final data = response.data ?? const <String, Object?>{};
    final progress = data['progress'] as Map<String, Object?>?;
    return SyncResult(
      status: data['status']?.toString() ?? 'Applied',
      conflictReason: data['conflictReason']?.toString(),
      serverRevision: (progress?['revision'] as num?)?.toInt(),
    );
  }
}

class SyncResult {
  const SyncResult({required this.status, this.conflictReason, this.serverRevision});
  final String status;
  final String? conflictReason;
  final int? serverRevision;
  bool get applied => status.toLowerCase() == 'applied';
  bool get conflict => status.toLowerCase() == 'conflict';
}
