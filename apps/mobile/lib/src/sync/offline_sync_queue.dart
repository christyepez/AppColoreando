import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

final offlineSyncQueueProvider = Provider<OfflineSyncQueue>((ref) => OfflineSyncQueue());

class QueuedProgressOperation {
  const QueuedProgressOperation({
    required this.operationId,
    required this.artworkId,
    required this.completedRegionIds,
    required this.clientRevision,
    required this.isFavorite,
    required this.isDownloaded,
    required this.createdAtUtc,
  });

  final String operationId;
  final String artworkId;
  final Set<int> completedRegionIds;
  final int clientRevision;
  final bool isFavorite;
  final bool isDownloaded;
  final DateTime createdAtUtc;

  Map<String, Object?> toJson() => {
        'operationId': operationId,
        'artworkId': artworkId,
        'completedRegionIds': completedRegionIds.toList()..sort(),
        'clientRevision': clientRevision,
        'isFavorite': isFavorite,
        'isDownloaded': isDownloaded,
        'createdAtUtc': createdAtUtc.toUtc().toIso8601String(),
      };

  factory QueuedProgressOperation.fromJson(Map<String, Object?> json) {
    return QueuedProgressOperation(
      operationId: json['operationId']! as String,
      artworkId: json['artworkId']! as String,
      completedRegionIds: (json['completedRegionIds']! as List).cast<num>().map((x) => x.toInt()).toSet(),
      clientRevision: (json['clientRevision']! as num).toInt(),
      isFavorite: json['isFavorite']! as bool,
      isDownloaded: json['isDownloaded']! as bool,
      createdAtUtc: DateTime.parse(json['createdAtUtc']! as String).toUtc(),
    );
  }
}

class OfflineSyncQueue {
  static const _key = 'offline-sync-queue:v1';

  Future<List<QueuedProgressOperation>> load() async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString(_key);
    if (raw == null || raw.isEmpty) return [];
    final decoded = (jsonDecode(raw) as List).cast<Map<String, Object?>>();
    return decoded.map(QueuedProgressOperation.fromJson).toList()
      ..sort((a, b) => a.createdAtUtc.compareTo(b.createdAtUtc));
  }

  Future<void> enqueue(QueuedProgressOperation operation) async {
    final current = await load();
    current.removeWhere((x) => x.operationId == operation.operationId);
    current.add(operation);
    await _save(current);
  }

  Future<void> remove(String operationId) async {
    final current = await load();
    current.removeWhere((x) => x.operationId == operationId);
    await _save(current);
  }

  Future<void> replaceArtworkOperations(QueuedProgressOperation operation) async {
    final current = await load();
    current.removeWhere((x) => x.artworkId == operation.artworkId);
    current.add(operation);
    await _save(current);
  }

  Future<void> _save(List<QueuedProgressOperation> operations) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_key, jsonEncode(operations.map((x) => x.toJson()).toList()));
  }
}
