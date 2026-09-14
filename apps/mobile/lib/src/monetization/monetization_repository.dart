import 'package:app_coloreando/src/config/app_config.dart';
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

final monetizationRepositoryProvider = Provider<MonetizationRepository>((ref) {
  final config = ref.watch(appConfigProvider);
  return MonetizationRepository(Dio(BaseOptions(baseUrl: '${config.apiBaseUrl}/api')));
});

class MonetizationEntitlements {
  const MonetizationEntitlements({required this.planCode, required this.isPremium, required this.adsEnabled, required this.maxOfflineArtworks, required this.premiumStylesEnabled, required this.eventBoostsEnabled, required this.priorityDownloadsEnabled});
  final String planCode;
  final bool isPremium;
  final bool adsEnabled;
  final int maxOfflineArtworks;
  final bool premiumStylesEnabled;
  final bool eventBoostsEnabled;
  final bool priorityDownloadsEnabled;

  static const free = MonetizationEntitlements(planCode: 'free', isPremium: false, adsEnabled: true, maxOfflineArtworks: 8, premiumStylesEnabled: false, eventBoostsEnabled: false, priorityDownloadsEnabled: false);

  factory MonetizationEntitlements.fromJson(Map<String, Object?> json) => MonetizationEntitlements(
    planCode: json['planCode']?.toString() ?? 'free',
    isPremium: json['isPremium'] == true,
    adsEnabled: json['adsEnabled'] != false,
    maxOfflineArtworks: (json['maxOfflineArtworks'] as num?)?.toInt() ?? 8,
    premiumStylesEnabled: json['premiumStylesEnabled'] == true,
    eventBoostsEnabled: json['eventBoostsEnabled'] == true,
    priorityDownloadsEnabled: json['priorityDownloadsEnabled'] == true,
  );
}
class MonetizationRepository {
  MonetizationRepository(this._dio);
  final Dio _dio;

  Future<MonetizationEntitlements> load(String? accessToken) async {
    if (accessToken == null || accessToken.isEmpty) return MonetizationEntitlements.free;
    try {
      final response = await _dio.get<Map<String, Object?>>(
        '/me/entitlements',
        options: Options(headers: {'Authorization': 'Bearer $accessToken'}),
      );
      return MonetizationEntitlements.fromJson(response.data ?? const {});
    } on DioException {
      return MonetizationEntitlements.free;
    }
  }

  bool canUsePremiumStyle(MonetizationEntitlements e) => e.premiumStylesEnabled;
  bool canDownload(MonetizationEntitlements e, int downloadedCount) => downloadedCount < e.maxOfflineArtworks;
}