import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

final localLibraryStoreProvider = Provider<LocalLibraryStore>((ref) => LocalLibraryStore());
final localLibrarySnapshotProvider = FutureProvider<LocalLibrarySnapshot>((ref) => ref.watch(localLibraryStoreProvider).load());

class LocalLibrarySnapshot {
  const LocalLibrarySnapshot({required this.favoriteIds, required this.recentIds});
  final List<String> favoriteIds;
  final List<String> recentIds;
}

class LocalLibraryStore {
  static const _favoritesKey = 'library:favorites:v1';
  static const _recentKey = 'library:recent:v1';

  Future<LocalLibrarySnapshot> load() async {
    final prefs = await SharedPreferences.getInstance();
    return LocalLibrarySnapshot(
      favoriteIds: _decode(prefs.getString(_favoritesKey)),
      recentIds: _decode(prefs.getString(_recentKey)),
    );
  }

  Future<bool> isFavorite(String artworkId) async => (await load()).favoriteIds.contains(artworkId);

  Future<void> setFavorite(String artworkId, bool favorite) async {
    final prefs = await SharedPreferences.getInstance();
    final values = _decode(prefs.getString(_favoritesKey)).toList();
    values.remove(artworkId);
    if (favorite) values.insert(0, artworkId);
    await prefs.setString(_favoritesKey, jsonEncode(values));
  }

  Future<void> markRecent(String artworkId) async {
    final prefs = await SharedPreferences.getInstance();
    final values = _decode(prefs.getString(_recentKey)).toList()..remove(artworkId);
    values.insert(0, artworkId);
    if (values.length > 20) values.removeRange(20, values.length);
    await prefs.setString(_recentKey, jsonEncode(values));
  }

  static List<String> _decode(String? raw) {
    if (raw == null || raw.isEmpty) return <String>[];
    return (jsonDecode(raw) as List).map((x) => x.toString()).toList();
  }
}
