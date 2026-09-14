import 'dart:async';
import 'package:app_coloreando/src/catalog/demo_artwork.dart';
import 'package:app_coloreando/src/coloring/coloring_page.dart';
import 'package:app_coloreando/src/coloring/local_library_store.dart';
import 'package:app_coloreando/src/catalog/catalog_repository.dart';
import 'package:app_coloreando/src/catalog/demo_artwork.dart';
import 'package:app_coloreando/src/config/app_config.dart';
import 'package:app_coloreando/src/l10n/app_strings.dart';
import 'package:app_coloreando/src/theme/app_theme.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

final localeProvider = StateProvider<Locale>((ref) => const Locale('es'));
final personalizedRecommendationsProvider = FutureProvider<List<CatalogArtwork>>((ref) async {
  final library = await ref.watch(localLibrarySnapshotProvider.future);
  final ids = <String>{...library.favoriteIds, ...library.recentIds}.toList();
  try {
    return await ref.watch(catalogRepositoryProvider).recommendations(signalIds: ids);
  } catch (_) {
    return const <CatalogArtwork>[];
  }
});

final routerProvider = Provider<GoRouter>((ref) => GoRouter(
  initialLocation: '/',
  routes: [
    ShellRoute(builder: (context, state, child) => AppShell(child: child), routes: [
      GoRoute(path: '/', builder: (_, __) => const HomePage()),
      GoRoute(path: '/catalog', builder: (_, __) => const CatalogPage()),
      GoRoute(path: '/profile', builder: (_, __) => const ProfilePage()),
      GoRoute(path: '/settings', builder: (_, __) => const SettingsPage()),
    ]),
    GoRoute(path: '/color/:id', builder: (_, state) => ColoringPage(artworkId: state.pathParameters['id']!)),
  ],
));

class AppColoreandoApp extends ConsumerWidget {
  const AppColoreandoApp({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => MaterialApp.router(
    debugShowCheckedModeBanner: false,
    title: 'AppColoreando',
    locale: ref.watch(localeProvider),
    theme: AppTheme.light,
    darkTheme: AppTheme.dark,
    routerConfig: ref.watch(routerProvider),
  );
}

class AppShell extends StatelessWidget {
  const AppShell({super.key, required this.child});
  final Widget child;
  static const destinations = [
    _Destination('/', Icons.home_outlined, Icons.home_rounded, 'Inicio'),
    _Destination('/catalog', Icons.grid_view_outlined, Icons.grid_view_rounded, 'Catalogo'),
    _Destination('/profile', Icons.favorite_border_rounded, Icons.favorite_rounded, 'Perfil'),
    _Destination('/settings', Icons.tune_rounded, Icons.tune_rounded, 'Ajustes'),
  ];
  @override
  Widget build(BuildContext context) {
    final path = GoRouterState.of(context).uri.path;
    final found = destinations.indexWhere((x) => x.path == path);
    return Scaffold(
      backgroundColor: const Color(0xFFFAF9F7),
      body: SafeArea(child: child),
      bottomNavigationBar: NavigationBar(
        height: 68,
        selectedIndex: found < 0 ? 0 : found,
        onDestinationSelected: (value) => context.go(destinations[value].path),
        destinations: [for (final item in destinations) NavigationDestination(icon: Icon(item.icon), selectedIcon: Icon(item.selectedIcon), label: item.label)],
      ),
    );
  }
}

class _Destination {
  const _Destination(this.path, this.icon, this.selectedIcon, this.label);
  final String path;
  final IconData icon;
  final IconData selectedIcon;
  final String label;
}

class HomePage extends ConsumerWidget {
  const HomePage({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final strings = AppStrings.of(ref.watch(localeProvider));
    final recommendations = ref.watch(personalizedRecommendationsProvider).valueOrNull ?? const <CatalogArtwork>[];
    return CustomScrollView(slivers: [
      SliverToBoxAdapter(child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 16, 20, 12),
        child: Row(children: [
          Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(strings.homeTitle, style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800)),
            const SizedBox(height: 4),
            Text('Un momento para ti, color a color', style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: Colors.black54)),
          ])),
          _RoundAction(icon: Icons.search_rounded, onTap: () => context.go('/catalog')),
          const SizedBox(width: 8),
          _RoundAction(icon: Icons.notifications_none_rounded, onTap: () {}),
        ]),
      )),
      const SliverToBoxAdapter(child: _DailyBanner()),
      const SliverToBoxAdapter(child: _EventsStrip()),
      SliverToBoxAdapter(child: SizedBox(height: 52, child: ListView(
        padding: const EdgeInsets.symmetric(horizontal: 20),
        scrollDirection: Axis.horizontal,
        children: const [_Pill('Todo', true), _Pill('Para ti', false), _Pill('Popular', false), _Pill('Aura', false), _Pill('Tesoro', false), _Pill('Postales', false), _Pill('Animales', false)],
      ))),
      const SliverToBoxAdapter(child: _SectionHeader('Para ti', 'Recomendaciones segun tus favoritos y recientes')),
      SliverToBoxAdapter(child: SizedBox(height: 262, child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(20, 4, 20, 16),
        scrollDirection: Axis.horizontal,
        itemCount: recommendations.isEmpty ? 3 : recommendations.length,
        separatorBuilder: (_, __) => const SizedBox(width: 14),
        itemBuilder: (_, i) => SizedBox(width: 190, child: recommendations.isEmpty ? ArtworkCard(artwork: demoArtworks[i]) : _CatalogRemoteCard(artwork: recommendations[i])),
      ))),
      const SliverToBoxAdapter(child: _SectionHeader('Descubre mas', 'Explora nuevas colecciones')),
      SliverPadding(
        padding: const EdgeInsets.fromLTRB(20, 4, 20, 28),
        sliver: SliverGrid.builder(
          itemCount: demoArtworks.length,
          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 2, mainAxisSpacing: 14, crossAxisSpacing: 14, childAspectRatio: .72),
          itemBuilder: (_, i) => ArtworkCard(artwork: demoArtworks[i]),
        ),
      ),
    ]);
  }
}

class _DailyBanner extends ConsumerWidget {
  const _DailyBanner();
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final fallback = demoArtworks[2];
    final daily = ref.watch(dailyContentProvider).valueOrNull;
    final config = ref.watch(appConfigProvider);
    final title = daily?.artwork.title ?? fallback.title;
    final artworkId = daily?.artwork.id ?? fallback.id;
    final thumbnail = daily?.artwork.thumbnailUrl;
    final thumbnailUrl = thumbnail == null || thumbnail.isEmpty ? null : (thumbnail.startsWith('http') ? thumbnail : '${config.apiBaseUrl}$thumbnail');
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 6, 20, 18),
      child: Container(
        height: 220,
        clipBehavior: Clip.antiAlias,
        decoration: BoxDecoration(borderRadius: BorderRadius.circular(26), gradient: const LinearGradient(colors: [Color(0xFF243B55), Color(0xFF4CA6A8)])),
        child: Stack(children: [
          Positioned(right: -20, top: -34, width: 185, height: 185, child: Opacity(opacity: .92, child: thumbnailUrl == null ? CustomPaint(painter: DemoArtworkPainter(artwork: fallback)) : Image.network(thumbnailUrl, fit: BoxFit.cover, errorBuilder: (_, __, ___) => CustomPaint(painter: DemoArtworkPainter(artwork: fallback))))),
          Padding(padding: const EdgeInsets.all(20), child: SizedBox(width: 190, child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Text('IMAGEN DEL DIA', style: TextStyle(color: Colors.white70, fontSize: 11, fontWeight: FontWeight.w800, letterSpacing: 1.1)),
            const SizedBox(height: 8),
            Text(title, maxLines: 2, overflow: TextOverflow.ellipsis, style: const TextStyle(color: Colors.white, fontSize: 22, fontWeight: FontWeight.w800)),
            const Spacer(),
            FilledButton.tonalIcon(
              style: FilledButton.styleFrom(backgroundColor: Colors.white, foregroundColor: const Color(0xFF243B55)),
              onPressed: () => context.push('/color/$artworkId'),
              icon: const Icon(Icons.palette_outlined, size: 18), label: const Text('Colorear'),
            ),
          ]))),
        ]),
      ),
    );
  }
}

class _EventsStrip extends ConsumerWidget {
  const _EventsStrip();
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final events = ref.watch(catalogEventsProvider).valueOrNull ?? const <CatalogEvent>[];
    if (events.isEmpty) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 0, 20, 12),
      child: SizedBox(height: 78, child: ListView.separated(
        scrollDirection: Axis.horizontal,
        itemCount: events.length,
        separatorBuilder: (_, __) => const SizedBox(width: 10),
        itemBuilder: (_, i) {
          final event = events[i];
          return Container(
            width: 220,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(18), border: Border.all(color: const Color(0xFFE9E6E1))),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(event.name, maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(fontWeight: FontWeight.w800)),
              const SizedBox(height: 4),
              Text('${event.artworkCount} ilustraciones${event.countryCode == null ? '' : ' · ${event.countryCode}'}', style: const TextStyle(fontSize: 11, color: Colors.black45)),
            ]),
          );
        },
      )),
    );
  }
}
class _Pill extends StatelessWidget {
  const _Pill(this.label, this.selected);
  final String label;
  final bool selected;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(right: 8, top: 6, bottom: 6),
    child: Container(
      padding: const EdgeInsets.symmetric(horizontal: 16),
      alignment: Alignment.center,
      decoration: BoxDecoration(color: selected ? const Color(0xFF222222) : Colors.white, borderRadius: BorderRadius.circular(22), border: Border.all(color: selected ? const Color(0xFF222222) : const Color(0xFFE9E6E1))),
      child: Text(label, style: TextStyle(fontWeight: FontWeight.w700, color: selected ? Colors.white : Colors.black87)),
    ),
  );
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader(this.title, this.subtitle);
  final String title;
  final String subtitle;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.fromLTRB(20, 14, 20, 12),
    child: Row(children: [
      Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(title, style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
        const SizedBox(height: 2),
        Text(subtitle, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.black45)),
      ])),
      TextButton(onPressed: () => context.go('/catalog'), child: const Text('Ver todo')),
    ]),
  );
}

class ArtworkCard extends StatelessWidget {
  const ArtworkCard({super.key, required this.artwork});
  final DemoArtwork artwork;
  @override
  Widget build(BuildContext context) => Material(
    color: Colors.white,
    borderRadius: BorderRadius.circular(22),
    clipBehavior: Clip.antiAlias,
    child: InkWell(
      onTap: () => context.push('/color/${artwork.id}'),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Expanded(child: Stack(fit: StackFit.expand, children: [
          CustomPaint(painter: DemoArtworkPainter(artwork: artwork, lineArt: !artwork.previewColored, soft: artwork.previewColored)),
          Positioned(right: 10, top: 10, child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
            decoration: BoxDecoration(color: Colors.white.withValues(alpha: .92), borderRadius: BorderRadius.circular(15)),
            child: Text(artwork.difficulty <= 2 ? 'Facil' : artwork.difficulty <= 3 ? 'Medio' : 'Detalle', style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700)),
          )),
          if (artwork.effectLabel != null) Positioned(right: 0, bottom: 0, child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            decoration: const BoxDecoration(color: Color(0xFF66C7F4), borderRadius: BorderRadius.only(topLeft: Radius.circular(16))),
            child: Text(artwork.effectLabel!, style: const TextStyle(color: Color(0xFF0C5B86), fontWeight: FontWeight.w800, fontSize: 11)),
          )),
        ])),
        Padding(padding: const EdgeInsets.fromLTRB(12, 11, 12, 12), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(artwork.title, maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800)),
          const SizedBox(height: 3),
          Text(artwork.category, style: const TextStyle(fontSize: 12, color: Colors.black45)),
        ])),
      ]),
    ),
  );
}

class _RoundAction extends StatelessWidget {
  const _RoundAction({required this.icon, required this.onTap});
  final IconData icon;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => Material(color: Colors.white, shape: const CircleBorder(), child: InkWell(customBorder: const CircleBorder(), onTap: onTap, child: Padding(padding: const EdgeInsets.all(11), child: Icon(icon, size: 21))));
}

class CatalogPage extends ConsumerStatefulWidget {
  const CatalogPage({super.key});
  @override
  ConsumerState<CatalogPage> createState() => _CatalogPageState();
}

class _CatalogPageState extends ConsumerState<CatalogPage> {
  final search = TextEditingController();
  Timer? _debounce;
  CatalogPageResult? _remote;
  CatalogDiscovery? _discovery;
  String? countryCode;
  String? collectionId;
  int? difficulty;
  bool licensedOnly = false;
  bool loading = false;

  @override
  void initState() {
    super.initState();
    Future.microtask(() async {
      await _loadDiscovery();
      await _runSearch();
    });
  }

  Future<void> _loadDiscovery() async {
    try {
      final value = await ref.read(catalogRepositoryProvider).discovery();
      if (mounted) setState(() => _discovery = value);
    } catch (_) {}
  }

  Future<void> _runSearch() async {
    if (mounted) setState(() => loading = true);
    try {
      final value = await ref.read(catalogRepositoryProvider).search(CatalogFilter(
        search: search.text,
        countryCode: countryCode,
        collectionId: collectionId,
        difficulty: difficulty,
        licensedOnly: licensedOnly ? true : null,
        pageSize: 48,
      ));
      if (mounted) setState(() => _remote = value);
    } catch (_) {
      if (mounted) setState(() => _remote = null);
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  void _scheduleSearch() {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 320), _runSearch);
  }

  @override
  void dispose() {
    _debounce?.cancel();
    search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final query = search.text.trim().toLowerCase();
    final fallback = demoArtworks.where((x) {
      if (difficulty != null && x.difficulty != difficulty) return false;
      if (countryCode != null && x.countryCode != countryCode) return false;
      return query.isEmpty || x.title.toLowerCase().contains(query) || x.category.toLowerCase().contains(query);
    }).toList();
    final remoteItems = _remote?.items ?? const <CatalogArtwork>[];
    final useRemote = _remote != null;
    final countries = _discovery?.countries ?? const <CatalogCountry>[];
    final collections = _discovery?.collections ?? const <CatalogCollection>[];

    return CustomScrollView(slivers: [
      SliverToBoxAdapter(child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 18, 20, 12),
        child: Row(children: [
          Expanded(child: Text('Catalogo', style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800))),
          if (loading) const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2)),
        ]),
      )),
      SliverToBoxAdapter(child: Padding(padding: const EdgeInsets.symmetric(horizontal: 20), child: TextField(
        controller: search,
        onChanged: (_) { setState(() {}); _scheduleSearch(); },
        decoration: InputDecoration(prefixIcon: const Icon(Icons.search_rounded), hintText: 'Que quieres colorear hoy?', suffixIcon: search.text.isEmpty ? null : IconButton(icon: const Icon(Icons.close_rounded), onPressed: () { search.clear(); setState(() {}); _runSearch(); }), filled: true, fillColor: Colors.white, border: OutlineInputBorder(borderSide: BorderSide.none, borderRadius: BorderRadius.circular(18))),
      ))),
      SliverToBoxAdapter(child: SizedBox(height: 58, child: ListView(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10), scrollDirection: Axis.horizontal,
        children: [
          ChoiceChip(label: const Text('Todos'), selected: difficulty == null, onSelected: (_) { setState(() => difficulty = null); _runSearch(); }),
          const SizedBox(width: 8),
          for (final entry in const [(1, 'Facil'), (2, 'Intermedio'), (3, 'Medio'), (4, 'Detalle')]) ...[
            ChoiceChip(label: Text(entry.$2), selected: difficulty == entry.$1, onSelected: (_) { setState(() => difficulty = entry.$1); _runSearch(); }),
            const SizedBox(width: 8),
          ],
          FilterChip(label: const Text('Licenciados'), selected: licensedOnly, onSelected: (value) { setState(() => licensedOnly = value); _runSearch(); }),
        ],
      ))),
      if (countries.isNotEmpty || collections.isNotEmpty) SliverToBoxAdapter(child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 2, 20, 10),
        child: Wrap(spacing: 10, runSpacing: 8, children: [
          if (countries.isNotEmpty) SizedBox(width: 180, child: DropdownButtonFormField<String?>(
            value: countryCode,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Pais', border: OutlineInputBorder()),
            items: [const DropdownMenuItem<String?>(value: null, child: Text('Todos')), ...countries.map((x) => DropdownMenuItem<String?>(value: x.code, child: Text(x.name)))],
            onChanged: (value) { setState(() => countryCode = value); _runSearch(); },
          )),
          if (collections.isNotEmpty) SizedBox(width: 220, child: DropdownButtonFormField<String?>(
            value: collectionId,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Coleccion', border: OutlineInputBorder()),
            items: [const DropdownMenuItem<String?>(value: null, child: Text('Todas')), ...collections.map((x) => DropdownMenuItem<String?>(value: x.id, child: Text('${x.name} (${x.artworkCount})', overflow: TextOverflow.ellipsis)))],
            onChanged: (value) { setState(() => collectionId = value); _runSearch(); },
          )),
        ]),
      )),
      SliverToBoxAdapter(child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 4, 20, 4),
        child: Text(useRemote ? '${_remote!.total} resultados' : 'Modo sin conexion · ${fallback.length} resultados', style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.black45)),
      )),
      SliverPadding(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
        sliver: SliverGrid.builder(
          itemCount: useRemote ? remoteItems.length : fallback.length,
          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 2, mainAxisSpacing: 14, crossAxisSpacing: 14, childAspectRatio: .72),
          itemBuilder: (_, i) => useRemote ? _CatalogRemoteCard(artwork: remoteItems[i]) : ArtworkCard(artwork: fallback[i]),
        ),
      ),
    ]);
  }
}

class _CatalogRemoteCard extends StatelessWidget {
  const _CatalogRemoteCard({required this.artwork});
  final CatalogArtwork artwork;
  @override
  Widget build(BuildContext context) => Material(
    color: Colors.white,
    borderRadius: BorderRadius.circular(22),
    clipBehavior: Clip.antiAlias,
    child: InkWell(
      onTap: () => context.push('/color/${artwork.id}'),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Expanded(child: Container(
          color: const Color(0xFFF4F2EE),
          alignment: Alignment.center,
          child: artwork.thumbnailUrl != null && artwork.thumbnailUrl!.startsWith('http')
              ? Image.network(artwork.thumbnailUrl!, fit: BoxFit.cover, width: double.infinity, height: double.infinity, errorBuilder: (_, __, ___) => const Icon(Icons.palette_outlined, size: 54, color: Colors.black26))
              : const Icon(Icons.palette_outlined, size: 54, color: Colors.black26),
        )),
        Padding(padding: const EdgeInsets.fromLTRB(12, 10, 12, 12), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(artwork.title, maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800)),
          const SizedBox(height: 3),
          Text('${artwork.countryCode ?? 'Global'} · ${artwork.regionCount} zonas', style: const TextStyle(fontSize: 11, color: Colors.black45)),
        ])),
      ]),
    ),
  );
}
class ProfilePage extends ConsumerWidget {
  const ProfilePage({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final library = ref.watch(localLibrarySnapshotProvider).valueOrNull;
    final favorites = library?.favoriteIds ?? const <String>[];
    final recent = library?.recentIds ?? const <String>[];
    return ListView(padding: const EdgeInsets.all(20), children: [
      Text('Perfil', style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800)),
      const SizedBox(height: 18),
      Container(padding: const EdgeInsets.all(20), decoration: BoxDecoration(color: const Color(0xFF252525), borderRadius: BorderRadius.circular(24)), child: Row(children: [
        const CircleAvatar(radius: 28, backgroundColor: Color(0xFFF4D35E), child: Icon(Icons.palette_rounded, color: Colors.black87)),
        const SizedBox(width: 14),
        Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          const Text('Mi espacio creativo', style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.w800)),
          const SizedBox(height: 4),
          Text('${favorites.length} favoritos · ${recent.length} recientes', style: const TextStyle(color: Colors.white60)),
        ])),
      ])),
      const SizedBox(height: 22),
      ListTile(leading: const Icon(Icons.favorite_rounded), title: const Text('Favoritos'), subtitle: Text(favorites.isEmpty ? 'Aun no guardas dibujos' : '${favorites.length} dibujos guardados'), trailing: const Icon(Icons.chevron_right), onTap: () => context.go('/catalog')),
      ListTile(leading: const Icon(Icons.history_rounded), title: const Text('Jugados recientemente'), subtitle: Text(recent.isEmpty ? 'Todavia no hay actividad' : '${recent.length} dibujos recientes'), trailing: const Icon(Icons.chevron_right), onTap: () => context.go('/catalog')),
      const ListTile(leading: Icon(Icons.download_outlined), title: Text('Sin conexion'), subtitle: Text('Dibujos descargados'), trailing: Icon(Icons.chevron_right)),
      const ListTile(leading: Icon(Icons.emoji_events_outlined), title: Text('Logros'), subtitle: Text('XP, niveles, rachas y metas'), trailing: Icon(Icons.chevron_right)),
    ]);
  }
}

class SettingsPage extends ConsumerWidget {
  const SettingsPage({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final config = ref.watch(appConfigProvider);
    final locale = ref.watch(localeProvider);
    return ListView(padding: const EdgeInsets.all(20), children: [
      Text('Ajustes', style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800)),
      const SizedBox(height: 18),
      SegmentedButton<Locale>(segments: const [ButtonSegment(value: Locale('es'), label: Text('ES')), ButtonSegment(value: Locale('en'), label: Text('EN'))], selected: {locale}, onSelectionChanged: (value) => ref.read(localeProvider.notifier).state = value.single),
      const SizedBox(height: 18),
      const ListTile(leading: Icon(Icons.notifications_none_rounded), title: Text('Recordatorios'), subtitle: Text('Tu momento diario para colorear')),
      ListTile(leading: const Icon(Icons.cloud_outlined), title: const Text('API'), subtitle: Text(config.apiBaseUrl)),
    ]);
  }
}
