import 'package:app_coloreando/src/config/app_config.dart';
import 'package:app_coloreando/src/telemetry/telemetry_queue.dart';
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

final telemetryRepositoryProvider = Provider<TelemetryRepository>((ref) {
  final config = ref.watch(appConfigProvider);
  return TelemetryRepository(Dio(BaseOptions(baseUrl: '${config.apiBaseUrl}/api')), ref.watch(telemetryQueueProvider));
});

class TelemetryRepository {
  TelemetryRepository(this._dio, this._queue);
  final Dio _dio;
  final TelemetryQueue _queue;

  Future<void> track(String name, {String? artworkId, Map<String, String> properties = const {}}) =>
      _queue.enqueue(TelemetryEvent(name: name, artworkId: artworkId, properties: properties, occurredAtUtc: DateTime.now().toUtc()));

  Future<bool> flush(String? accessToken) async {
    if (accessToken == null || accessToken.isEmpty) return false;
    final events = await _queue.load();
    if (events.isEmpty) return true;
    final batch = events.take(50).toList();
    try {
      await _dio.post('/me/telemetry', data: {'events': batch.map((x) => x.toJson()).toList()}, options: Options(headers: {'Authorization': 'Bearer $accessToken'}));
      await _queue.removeFirst(batch.length);
      return true;
    } on DioException {
      return false;
    }
  }
}