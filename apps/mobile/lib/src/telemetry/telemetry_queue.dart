import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

final telemetryQueueProvider = Provider<TelemetryQueue>((ref) => TelemetryQueue());

class TelemetryEvent {
  const TelemetryEvent({required this.name, this.artworkId, this.properties = const {}, required this.occurredAtUtc});
  final String name;
  final String? artworkId;
  final Map<String, String> properties;
  final DateTime occurredAtUtc;

  Map<String, Object?> toJson() => {
    'eventName': name,
    if (artworkId != null && RegExp(r'^[0-9a-fA-F]{8}-[0-9a-fA-F-]{27}$').hasMatch(artworkId!)) 'artworkId': artworkId,
    'properties': properties,
    'occurredAtUtc': occurredAtUtc.toUtc().toIso8601String(),
  };

  factory TelemetryEvent.fromJson(Map<String, Object?> json) => TelemetryEvent(
    name: json['eventName']?.toString() ?? '',
    artworkId: json['artworkId']?.toString(),
    properties: Map<String, String>.from((json['properties'] as Map?) ?? const {}),
    occurredAtUtc: DateTime.parse(json['occurredAtUtc']!.toString()).toUtc(),
  );
}
class TelemetryQueue {
  static const _key = 'telemetry-queue:v1';
  static const _maxItems = 100;

  Future<List<TelemetryEvent>> load() async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString(_key);
    if (raw == null || raw.isEmpty) return <TelemetryEvent>[];
    final decoded = (jsonDecode(raw) as List).cast<Map<String, Object?>>();
    return decoded.map(TelemetryEvent.fromJson).toList();
  }

  Future<void> enqueue(TelemetryEvent event) async {
    final current = await load();
    current.add(event);
    if (current.length > _maxItems) current.removeRange(0, current.length - _maxItems);
    await _save(current);
  }

  Future<void> removeFirst(int count) async {
    final current = await load();
    final removeCount = count.clamp(0, current.length);
    if (removeCount > 0) current.removeRange(0, removeCount);
    await _save(current);
  }

  Future<void> _save(List<TelemetryEvent> events) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_key, jsonEncode(events.map((x) => x.toJson()).toList()));
  }
}