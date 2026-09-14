import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

final generatedArtworkCacheProvider = Provider<GeneratedArtworkCache>(
  (ref) => GeneratedArtworkCache(),
);

class CachedGeneratedArtwork {
  const CachedGeneratedArtwork({
    required this.metadata,
    required this.bundle,
    required this.cachedAtUtc,
  });

  final Map<String, Object?> metadata;
  final Map<String, Object?> bundle;
  final DateTime cachedAtUtc;

  Map<String, Object?> toJson() => {
        'metadata': metadata,
        'bundle': bundle,
        'cachedAtUtc': cachedAtUtc.toUtc().toIso8601String(),
      };
  factory CachedGeneratedArtwork.fromJson(Map<String, Object?> json) {
    return CachedGeneratedArtwork(
      metadata: Map<String, Object?>.from(json['metadata'] as Map),
      bundle: Map<String, Object?>.from(json['bundle'] as Map),
      cachedAtUtc: DateTime.parse(json['cachedAtUtc']!.toString()).toUtc(),
    );
  }
}

class GeneratedArtworkCache {
  GeneratedArtworkCache({
    this.ttl = const Duration(days: 30),
    this.maxEntries = 8,
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now;

  static const _indexKey = 'generated-artwork-cache:index:v2';
  static const _entryPrefix = 'generated-artwork-cache:entry:v2:';

  final Duration ttl;
  final int maxEntries;
  final DateTime Function() _now;

  Future<CachedGeneratedArtwork?> load(String artworkId) async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString('$_entryPrefix$artworkId');
    if (raw == null || raw.isEmpty) return null;
    try {
      final entry = CachedGeneratedArtwork.fromJson(
        Map<String, Object?>.from(jsonDecode(raw) as Map),
      );
      if (_now().toUtc().difference(entry.cachedAtUtc) > ttl) {
        await remove(artworkId);
        return null;
      }
      await _touch(prefs, artworkId);
      return entry;
    } on FormatException {
      await remove(artworkId);
      return null;
    }
  }

  Future<void> save(
    String artworkId,
    Map<String, Object?> metadata,
    Map<String, Object?> bundle,
  ) async {
    final prefs = await SharedPreferences.getInstance();
    final entry = CachedGeneratedArtwork(
      metadata: Map<String, Object?>.from(metadata),
      bundle: Map<String, Object?>.from(bundle),
      cachedAtUtc: _now().toUtc(),
    );
    await prefs.setString('$_entryPrefix$artworkId', jsonEncode(entry.toJson()));
    await _touch(prefs, artworkId);
    await _prune(prefs);
  }

  Future<void> remove(String artworkId) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('$_entryPrefix$artworkId');
    final ids = _decodeIndex(prefs.getString(_indexKey))..remove(artworkId);
    await prefs.setString(_indexKey, jsonEncode(ids));
  }

  Future<List<String>> listIds() async {
    final prefs = await SharedPreferences.getInstance();
    return _decodeIndex(prefs.getString(_indexKey));
  }

  Future<void> clear() async {
    final prefs = await SharedPreferences.getInstance();
    final ids = _decodeIndex(prefs.getString(_indexKey));
    for (final id in ids) {
      await prefs.remove('$_entryPrefix$id');
    }
    await prefs.remove(_indexKey);
  }

  Future<void> _touch(SharedPreferences prefs, String artworkId) async {
    final ids = _decodeIndex(prefs.getString(_indexKey))..remove(artworkId);
    ids.insert(0, artworkId);
    await prefs.setString(_indexKey, jsonEncode(ids));
  }

  Future<void> _prune(SharedPreferences prefs) async {
    final ids = _decodeIndex(prefs.getString(_indexKey));
    if (ids.length <= maxEntries) return;
    final evicted = ids.sublist(maxEntries);
    for (final id in evicted) {
      await prefs.remove('$_entryPrefix$id');
    }
    await prefs.setString(_indexKey, jsonEncode(ids.take(maxEntries).toList()));
  }

  static List<String> _decodeIndex(String? raw) {
    if (raw == null || raw.isEmpty) return <String>[];
    return (jsonDecode(raw) as List).map((x) => x.toString()).toList();
  }
}
