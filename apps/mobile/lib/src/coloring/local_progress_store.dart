import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

final localProgressStoreProvider = Provider<LocalProgressStore>((ref) => LocalProgressStore());

class LocalProgressStore {
  Future<Set<int>> load(String artworkId) async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString('progress:$artworkId') ?? '[]';
    return (jsonDecode(raw) as List).cast<int>().toSet();
  }

  Future<void> save(String artworkId, Set<int> completedRegionIds) async {
    final prefs = await SharedPreferences.getInstance();
    final sorted = completedRegionIds.toList()..sort();
    await prefs.setString('progress:$artworkId', jsonEncode(sorted));
  }
}
