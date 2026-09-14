import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

final catalogCacheProvider = Provider<CatalogCache>((ref) => CatalogCache());

class CatalogCache {
  static const _prefix = 'catalog-cache:v2:';

  Future<void> writeMap(String key, Map<String, Object?> value) async {
    await _write(key, value);
  }

  Future<void> writeList(String key, List<Object?> value) async {
    await _write(key, value);
  }

  Future<Map<String, Object?>?> readMap(
    String key, {
    Duration maxAge = const Duration(days: 7),
    bool allowStale = true,
  }) async {
    final data = await _read(key, maxAge: maxAge, allowStale: allowStale);
    return data is Map ? Map<String, Object?>.from(data) : null;
  }

  Future<List<Object?>?> readList(
    String key, {
    Duration maxAge = const Duration(days: 7),
    bool allowStale = true,
  }) async {
    final data = await _read(key, maxAge: maxAge, allowStale: allowStale);
    return data is List ? List<Object?>.from(data) : null;
  }

  Future<void> remove(String key) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('$_prefix$key');
  }

  Future<void> _write(String key, Object value) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('$_prefix$key', jsonEncode({
      'savedAtUtc': DateTime.now().toUtc().toIso8601String(),
      'data': value,
    }));
  }

  Future<Object?> _read(
    String key, {
    required Duration maxAge,
    required bool allowStale,
  }) async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString('$_prefix$key');
    if (raw == null || raw.isEmpty) return null;
    try {
      final envelope = Map<String, Object?>.from(jsonDecode(raw) as Map);
      final savedAt = DateTime.tryParse(envelope['savedAtUtc']?.toString() ?? '');
      if (!allowStale && savedAt != null && DateTime.now().toUtc().difference(savedAt) > maxAge) {
        return null;
      }
      return envelope['data'];
    } catch (_) {
      return null;
    }
  }
}
