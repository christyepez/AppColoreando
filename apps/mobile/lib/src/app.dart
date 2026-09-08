import 'package:app_coloreando/src/catalog/catalog_repository.dart';
import 'package:app_coloreando/src/catalog/demo_artwork.dart';
import 'package:app_coloreando/src/coloring/coloring_page.dart';
import 'package:app_coloreando/src/config/app_config.dart';
import 'package:app_coloreando/src/l10n/app_strings.dart';
import 'package:app_coloreando/src/theme/app_theme.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

final localeProvider = StateProvider<Locale>((ref) => const Locale('es'));

final routerProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: '/',
    routes: [
      ShellRoute(
        builder: (context, state, child) => AppShell(child: child),
        routes: [
          GoRoute(path: '/', builder: (_, __) => const HomePage()),
          GoRoute(path: '/catalog', builder: (_, __) => const CatalogPage()),
          GoRoute(path: '/profile', builder: (_, __) => const ProfilePage()),
          GoRoute(path: '/settings', builder: (_, __) => const SettingsPage()),
        ],
      ),
      GoRoute(path: '/color/:id', builder: (_, state) => ColoringPage(artworkId: state.pathParameters['id']!)),
    ],
  );
});

class AppColoreandoApp extends ConsumerWidget {
  const AppColoreandoApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final locale = ref.watch(localeProvider);
    return MaterialApp.router(
      debugShowCheckedModeBanner: false,
      title: 'AppColoreando',
      locale: locale,
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      routerConfig: ref.watch(routerProvider),
    );
  }
}

class AppShell extends StatelessWidget {
  const AppShell({super.key, required this.child});

  final Widget child;

  static const destinations = [
    _Destination('/', Icons.home_outlined, Icons.home, 'Inicio'),
    _Destination('/catalog', Icons.grid_view_outlined, Icons.grid_view, 'Catalogo'),
    _Destination('/profile', Icons.person_outline, Icons.person, 'Perfil'),
    _Destination('/settings', Icons.settings_outlined, Icons.settings, 'Ajustes'),
  ];

  @override
  Widget build(BuildContext context) {
    final path = GoRouterState.of(context).uri.path;
    final index = destinations.indexWhere((x) => x.path == path).clamp(0, destinations.length - 1);
    return Scaffold(
      body: SafeArea(child: child),
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (value) => context.go(destinations[value].path),
        destinations: [
          for (final item in destinations)
            NavigationDestination(
              icon: Icon(item.icon),
              selectedIcon: Icon(item.selectedIcon),
              label: item.label,
            ),
        ],
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
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text(strings.homeTitle, style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w700)),
        const SizedBox(height: 8),
        Text(strings.homeSubtitle),
        const SizedBox(height: 20),
        const _DemoArtworkCard(),
        const SizedBox(height: 24),
        Text(strings.sectionsTitle, style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 12),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: const [
            Chip(label: Text('Ecuador')),
            Chip(label: Text('Colombia')),
            Chip(label: Text('South America')),
            Chip(label: Text('USA')),
            Chip(label: Text('Geometry')),
            Chip(label: Text('Culture')),
          ],
        ),
      ],
    );
  }
}

class CatalogPage extends ConsumerStatefulWidget {
  const CatalogPage({super.key});

  @override
  ConsumerState<CatalogPage> createState() => _CatalogPageState();
}

class _CatalogPageState extends ConsumerState<CatalogPage> {
  final searchController = TextEditingController();
  String? countryCode;
  int? difficulty;
  late Future<CatalogPageResult> future;

  @override
  void initState() {
    super.initState();
    future = _load();
  }

  Future<CatalogPageResult> _load() => ref.read(catalogRepositoryProvider).search(
        CatalogFilter(search: searchController.text, countryCode: countryCode, difficulty: difficulty),
      );

  void refresh() => setState(() => future = _load());

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text('Catalogo', style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700)),
        const SizedBox(height: 12),
        TextField(controller: searchController, onSubmitted: (_) => refresh(), decoration: InputDecoration(prefixIcon: const Icon(Icons.search), hintText: 'Buscar dibujos', border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)))),
        const SizedBox(height: 12),
        Wrap(spacing: 8, children: [
          DropdownButton<String?>(value: countryCode, hint: const Text('Pais'), items: const [DropdownMenuItem(value: null, child: Text('Todos')), DropdownMenuItem(value: 'EC', child: Text('Ecuador')), DropdownMenuItem(value: 'CO', child: Text('Colombia')), DropdownMenuItem(value: 'US', child: Text('USA'))], onChanged: (v) { countryCode = v; refresh(); }),
          DropdownButton<int?>(value: difficulty, hint: const Text('Dificultad'), items: const [DropdownMenuItem(value: null, child: Text('Todas')), DropdownMenuItem(value: 1, child: Text('1')), DropdownMenuItem(value: 2, child: Text('2')), DropdownMenuItem(value: 3, child: Text('3')), DropdownMenuItem(value: 4, child: Text('4')), DropdownMenuItem(value: 5, child: Text('5'))], onChanged: (v) { difficulty = v; refresh(); }),
        ]),
        const SizedBox(height: 16),
        FutureBuilder<CatalogPageResult>(
          future: future,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) return const Center(child: CircularProgressIndicator());
            if (snapshot.hasError) return const Column(children: [Text('Catalogo remoto no disponible. Mostrando contenido local.'), SizedBox(height: 12), _DemoArtworkCard()]);
            final items = snapshot.data?.items ?? const <CatalogArtwork>[];
            if (items.isEmpty) return const Text('No hay dibujos para estos filtros.');
            return Column(children: [for (final item in items) Card(child: ListTile(title: Text(item.title), subtitle: Text('${item.countryCode ?? '-'} · dificultad ${item.difficulty} · ${item.regionCount} regiones'), trailing: const Icon(Icons.chevron_right)))]);
          },
        ),
      ],
    );
  }
}

class ProfilePage extends StatelessWidget {
  const ProfilePage({super.key});

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text('Perfil', style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700)),
        const SizedBox(height: 12),
        const ListTile(leading: Icon(Icons.workspace_premium_outlined), title: Text('0 XP'), subtitle: Text('Logros y rachas')),
        const ListTile(leading: Icon(Icons.favorite_outline), title: Text('Favoritos'), subtitle: Text('Tus dibujos guardados')),
      ],
    );
  }
}

class SettingsPage extends ConsumerWidget {
  const SettingsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final config = ref.watch(appConfigProvider);
    final locale = ref.watch(localeProvider);
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text('Ajustes', style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700)),
        const SizedBox(height: 12),
        SegmentedButton<Locale>(
          segments: const [
            ButtonSegment(value: Locale('es'), label: Text('ES')),
            ButtonSegment(value: Locale('en'), label: Text('EN')),
          ],
          selected: {locale},
          onSelectionChanged: (value) => ref.read(localeProvider.notifier).state = value.single,
        ),
        const SizedBox(height: 16),
        ListTile(leading: const Icon(Icons.cloud_outlined), title: const Text('API'), subtitle: Text(config.apiBaseUrl)),
      ],
    );
  }
}

class _DemoArtworkCard extends StatelessWidget {
  const _DemoArtworkCard();

  @override
  Widget build(BuildContext context) {
    final artwork = demoArtworks.first;
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push('/color/${artwork.id}'),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            AspectRatio(aspectRatio: 16 / 10, child: CustomPaint(painter: DemoArtworkPainter())),
            Padding(
              padding: const EdgeInsets.all(14),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(artwork.title, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                  const SizedBox(height: 4),
                  Text('${artwork.regionCount} regiones - original'),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

