import 'package:app_coloreando/src/telemetry/telemetry_queue.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  setUp(() => SharedPreferences.setMockInitialValues({}));

  test('telemetry queue persists events in order', () async {
    final queue = TelemetryQueue();
    await queue.enqueue(TelemetryEvent(
      name: 'artwork_open',
      artworkId: '11111111-1111-1111-1111-111111111111',
      properties: const {'source': 'catalog'},
      occurredAtUtc: DateTime.utc(2026, 9, 14, 12),
    ));
    await queue.enqueue(TelemetryEvent(
      name: 'color_region',
      properties: const {'difficulty': '2'},
      occurredAtUtc: DateTime.utc(2026, 9, 14, 12, 1),
    ));
    final events = await queue.load();
    expect(events.map((x) => x.name), ['artwork_open', 'color_region']);
  });

  test('telemetry queue caps oldest events', () async {
    final queue = TelemetryQueue();
    for (var i = 0; i < 105; i++) {
      await queue.enqueue(TelemetryEvent(
        name: 'home_view',
        properties: {'screen': '$i'},
        occurredAtUtc: DateTime.utc(2026, 9, 14, 12).add(Duration(minutes: i)),
      ));
    }
    final events = await queue.load();
    expect(events, hasLength(100));
    expect(events.first.properties['screen'], '5');
  });
}