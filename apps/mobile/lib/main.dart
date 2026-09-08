import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

void main() => runApp(const ProviderScope(child: AppColoreando()));

class AppColoreando extends StatelessWidget {
  const AppColoreando({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'AppColoreando',
      theme: ThemeData(useMaterial3: true, colorSchemeSeed: Colors.deepPurple),
      home: const HomeScreen(),
    );
  }
}

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});
  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int index = 0;
  static const pages = [DiscoverPage(), MyArtPage(), ProfilePage()];

  @override
  Widget build(BuildContext context) => Scaffold(
        body: SafeArea(child: pages[index]),
        bottomNavigationBar: NavigationBar(
          selectedIndex: index,
          onDestinationSelected: (value) => setState(() => index = value),
          destinations: const [
            NavigationDestination(icon: Icon(Icons.explore_outlined), selectedIcon: Icon(Icons.explore), label: 'Explorar'),
            NavigationDestination(icon: Icon(Icons.palette_outlined), selectedIcon: Icon(Icons.palette), label: 'Mis dibujos'),
            NavigationDestination(icon: Icon(Icons.person_outline), selectedIcon: Icon(Icons.person), label: 'Perfil'),
          ],
        ),
      );
}

class DiscoverPage extends StatelessWidget {
  const DiscoverPage({super.key});
  @override
  Widget build(BuildContext context) {
    final categories = ['Ecuador', 'Colombia', 'Sudamérica', 'USA', 'Infantiles', 'Fantasía', 'Anime', 'Retro 80'];
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text('Descubre y colorea', style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 8),
        const Text('Paisajes, cultura, fantasía y colecciones para colorear por números.'),
        const SizedBox(height: 20),
        Wrap(spacing: 8, runSpacing: 8, children: categories.map((x) => ActionChip(label: Text(x), onPressed: () {})).toList()),
        const SizedBox(height: 28),
        Text('Nuevos', style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 12),
        GridView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 2, childAspectRatio: .78, crossAxisSpacing: 12, mainAxisSpacing: 12),
          itemCount: 6,
          itemBuilder: (_, i) => Card(
            clipBehavior: Clip.antiAlias,
            child: InkWell(
              onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => ColoringScreen(title: 'Ilustración ${i + 1}'))),
              child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                Expanded(child: Container(color: Theme.of(context).colorScheme.surfaceContainerHighest, child: const Icon(Icons.landscape_outlined, size: 70))),
                Padding(padding: const EdgeInsets.all(12), child: Text('Ilustración ${i + 1}', style: const TextStyle(fontWeight: FontWeight.w600))),
              ]),
            ),
          ),
        )
      ],
    );
  }
}

class ColoringScreen extends StatefulWidget {
  final String title;
  const ColoringScreen({super.key, required this.title});
  @override
  State<ColoringScreen> createState() => _ColoringScreenState();
}

class _ColoringScreenState extends State<ColoringScreen> {
  int selected = 1;
  final Set<int> painted = {};

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: Text(widget.title)),
        body: Column(children: [
          Expanded(
            child: InteractiveViewer(
              minScale: .7,
              maxScale: 6,
              child: Center(
                child: Wrap(spacing: 4, runSpacing: 4, children: List.generate(36, (i) {
                  final number = (i % 8) + 1;
                  final done = painted.contains(i);
                  return GestureDetector(
                    onTap: () {
                      if (number == selected) setState(() => painted.add(i));
                    },
                    child: AnimatedContainer(
                      duration: const Duration(milliseconds: 180),
                      width: 46,
                      height: 46,
                      alignment: Alignment.center,
                      decoration: BoxDecoration(border: Border.all(color: Colors.black26), color: done ? Colors.primaries[(number * 2) % Colors.primaries.length].shade200 : Colors.white),
                      child: done ? const Icon(Icons.check, size: 18) : Text('$number'),
                    ),
                  );
                })),
              ),
            ),
          ),
          LinearProgressIndicator(value: painted.length / 36),
          SizedBox(
            height: 78,
            child: ListView.separated(
              padding: const EdgeInsets.all(12),
              scrollDirection: Axis.horizontal,
              itemCount: 8,
              separatorBuilder: (_, __) => const SizedBox(width: 8),
              itemBuilder: (_, i) {
                final n = i + 1;
                return ChoiceChip(label: Text('$n'), selected: selected == n, onSelected: (_) => setState(() => selected = n));
              },
            ),
          )
        ]),
      );
}

class MyArtPage extends StatelessWidget {
  const MyArtPage({super.key});
  @override
  Widget build(BuildContext context) => const Center(child: Text('Progreso, favoritos e historial del usuario'));
}

class ProfilePage extends StatelessWidget {
  const ProfilePage({super.key});
  @override
  Widget build(BuildContext context) => const Center(child: Text('Cuenta, métricas, logros y configuración'));
}
