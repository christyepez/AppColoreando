import 'package:app_coloreando/src/catalog/demo_artwork.dart';
import 'package:app_coloreando/src/coloring/coloring_page.dart';
import 'package:app_coloreando/src/config/app_config.dart';
import 'package:app_coloreando/src/l10n/app_strings.dart';
import 'package:app_coloreando/src/theme/app_theme.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

final localeProvider = StateProvider<Locale>((ref) => const Locale('es'));

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
      SliverToBoxAdapter(child: SizedBox(height: 52, child: ListView(
        padding: const EdgeInsets.symmetric(horizontal: 20),
        scrollDirection: Axis.horizontal,
        children: const [_Pill('Todo', true), _Pill('Para ti', false), _Pill('Popular', false), _Pill('Aura', false), _Pill('Tesoro', false), _Pill('Postales', false), _Pill('Animales', false)],
      ))),
      const SliverToBoxAdapter(child: _SectionHeader('Seleccion del dia', 'Ilustraciones originales para relajarte')),
      SliverToBoxAdapter(child: SizedBox(height: 262, child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(20, 4, 20, 16),
        scrollDirection: Axis.horizontal,
        itemCount: 3,
        separatorBuilder: (_, __) => const SizedBox(width: 14),
        itemBuilder: (_, i) => SizedBox(width: 190, child: ArtworkCard(artwork: demoArtworks[i])),
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

class _DailyBanner extends StatelessWidget {
  const _DailyBanner();
  @override
  Widget build(BuildContext context) {
    final artwork = demoArtworks[2];
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 6, 20, 18),
      child: Container(
        height: 220,
        clipBehavior: Clip.antiAlias,
        decoration: BoxDecoration(borderRadius: BorderRadius.circular(26), gradient: const LinearGradient(colors: [Color(0xFF243B55), Color(0xFF4CA6A8)])),
        child: Stack(children: [
          Positioned(right: -20, top: -34, width: 185, height: 185, child: Opacity(opacity: .92, child: CustomPaint(painter: DemoArtworkPainter(artwork: artwork)))),
          Padding(padding: const EdgeInsets.all(20), child: SizedBox(width: 190, child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Text('IMAGEN DEL DIA', style: TextStyle(color: Colors.white70, fontSize: 11, fontWeight: FontWeight.w800, letterSpacing: 1.1)),
            const SizedBox(height: 8),
            const Text('Atardecer tropical', style: TextStyle(color: Colors.white, fontSize: 22, fontWeight: FontWeight.w800)),
            const Spacer(),
            FilledButton.tonalIcon(
              style: FilledButton.styleFrom(backgroundColor: Colors.white, foregroundColor: const Color(0xFF243B55)),
              onPressed: () => context.push('/color/${artwork.id}'),
              icon: const Icon(Icons.palette_outlined, size: 18), label: const Text('Colorear'),
            ),
          ]))),
        ]),
      ),
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

class CatalogPage extends StatefulWidget {
  const CatalogPage({super.key});
  @override
  State<CatalogPage> createState() => _CatalogPageState();
}

class _CatalogPageState extends State<CatalogPage> {
  final search = TextEditingController();
  String category = 'Todos';
  @override
  Widget build(BuildContext context) {
    final query = search.text.toLowerCase();
    final items = demoArtworks.where((x) => (category == 'Todos' || x.category == category) && (query.isEmpty || x.title.toLowerCase().contains(query))).toList();
    return CustomScrollView(slivers: [
      SliverToBoxAdapter(child: Padding(padding: const EdgeInsets.fromLTRB(20, 18, 20, 12), child: Text('Catalogo', style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800)))),
      SliverToBoxAdapter(child: Padding(padding: const EdgeInsets.symmetric(horizontal: 20), child: TextField(
        controller: search,
        onChanged: (_) => setState(() {}),
        decoration: InputDecoration(prefixIcon: const Icon(Icons.search_rounded), hintText: 'Que quieres colorear hoy?', filled: true, fillColor: Colors.white, border: OutlineInputBorder(borderSide: BorderSide.none, borderRadius: BorderRadius.circular(18))),
      ))),
      SliverToBoxAdapter(child: SizedBox(height: 58, child: ListView(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10), scrollDirection: Axis.horizontal,
        children: [for (final item in const ['Todos', 'Naturaleza', 'Paisajes', 'Flores', 'Animales']) Padding(padding: const EdgeInsets.only(right: 8), child: ChoiceChip(label: Text(item), selected: category == item, onSelected: (_) => setState(() => category = item)))],
      ))),
      SliverPadding(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
        sliver: SliverGrid.builder(
          itemCount: items.length,
          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 2, mainAxisSpacing: 14, crossAxisSpacing: 14, childAspectRatio: .72),
          itemBuilder: (_, i) => ArtworkCard(artwork: items[i]),
        ),
      ),
    ]);
  }
}

class ProfilePage extends StatelessWidget {
  const ProfilePage({super.key});
  @override
  Widget build(BuildContext context) => ListView(padding: const EdgeInsets.all(20), children: [
    Text('Perfil', style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800)),
    const SizedBox(height: 18),
    Container(padding: const EdgeInsets.all(20), decoration: BoxDecoration(color: const Color(0xFF252525), borderRadius: BorderRadius.circular(24)), child: const Row(children: [
      CircleAvatar(radius: 28, backgroundColor: Color(0xFFF4D35E), child: Icon(Icons.palette_rounded, color: Colors.black87)),
      SizedBox(width: 14),
      Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text('Mi espacio creativo', style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.w800)), SizedBox(height: 4), Text('0 obras terminadas - 0 XP', style: TextStyle(color: Colors.white60))])),
    ])),
    const SizedBox(height: 22),
    const ListTile(leading: Icon(Icons.favorite_outline_rounded), title: Text('Favoritos'), subtitle: Text('Tus dibujos guardados'), trailing: Icon(Icons.chevron_right)),
    const ListTile(leading: Icon(Icons.download_outlined), title: Text('Sin conexion'), subtitle: Text('Dibujos descargados'), trailing: Icon(Icons.chevron_right)),
    const ListTile(leading: Icon(Icons.emoji_events_outlined), title: Text('Logros'), subtitle: Text('Colecciones, rachas y metas'), trailing: Icon(Icons.chevron_right)),
  ]);
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
