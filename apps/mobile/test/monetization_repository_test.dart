import 'package:app_coloreando/src/monetization/monetization_repository.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('free entitlement is conservative by default', () {
    const e = MonetizationEntitlements.free;
    expect(e.isPremium, isFalse);
    expect(e.adsEnabled, isTrue);
    expect(e.maxOfflineArtworks, 8);
    expect(e.premiumStylesEnabled, isFalse);
  });

  test('premium entitlement unlocks gated features', () {
    final e = MonetizationEntitlements.fromJson({
      'planCode': 'premium',
      'isPremium': true,
      'adsEnabled': false,
      'maxOfflineArtworks': 100,
      'premiumStylesEnabled': true,
      'eventBoostsEnabled': true,
      'priorityDownloadsEnabled': true,
    });
    final repo = MonetizationRepository(Dio());
    expect(repo.canUsePremiumStyle(e), isTrue);
    expect(repo.canDownload(e, 99), isTrue);
    expect(repo.canDownload(e, 100), isFalse);
  });
}