import 'package:app_coloreando/src/catalog/demo_artwork.dart';
import 'package:app_coloreando/src/coloring/local_progress_store.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class ColoringPage extends ConsumerStatefulWidget {
  const ColoringPage({super.key, required this.artworkId});

  final String artworkId;

  @override
  ConsumerState<ColoringPage> createState() => _ColoringPageState();
}

class _ColoringPageState extends ConsumerState<ColoringPage> {
  int selectedColorId = 1;
  int? highlightedRegionId;
  Set<int> completed = {};

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    completed = await ref.read(localProgressStoreProvider).load(widget.artworkId);
    if (mounted) setState(() {});
  }

  Future<void> _save() => ref.read(localProgressStoreProvider).save(widget.artworkId, completed);

  @override
  Widget build(BuildContext context) {
    final artwork = demoArtworkById(widget.artworkId);
    final progress = completed.length / artwork.regions.length;
    return Scaffold(
      appBar: AppBar(
        title: Text(artwork.title),
        actions: [
          IconButton(
            tooltip: 'Hint',
            icon: const Icon(Icons.lightbulb_outline),
            onPressed: () {
              final next = artwork.regions.where((x) => x.colorId == selectedColorId && !completed.contains(x.id)).firstOrNull;
              setState(() => highlightedRegionId = next?.id);
            },
          ),
        ],
      ),
      body: Column(
        children: [
          Expanded(
            child: InteractiveViewer(
              minScale: .7,
              maxScale: 7,
              child: Center(
                child: AspectRatio(
                  aspectRatio: 1,
                  child: LayoutBuilder(
                    builder: (context, constraints) => GestureDetector(
                      onTapUp: (details) => _tap(details.localPosition, Size(constraints.maxWidth, constraints.maxHeight), artwork),
                      child: CustomPaint(
                        painter: ColoringPainter(
                          artwork: artwork,
                          selectedColorId: selectedColorId,
                          completedRegionIds: completed,
                          highlightedRegionId: highlightedRegionId,
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ),
          LinearProgressIndicator(value: progress),
          SizedBox(
            height: 78,
            child: ListView.separated(
              padding: const EdgeInsets.all(12),
              scrollDirection: Axis.horizontal,
              itemCount: artwork.palette.length,
              separatorBuilder: (_, __) => const SizedBox(width: 8),
              itemBuilder: (context, index) {
                final color = artwork.palette[index];
                return ChoiceChip(
                  avatar: CircleAvatar(backgroundColor: color.color),
                  label: Text('${color.id}'),
                  selected: selectedColorId == color.id,
                  onSelected: (_) => setState(() => selectedColorId = color.id),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  void _tap(Offset localPosition, Size canvasSize, DemoArtwork artwork) {
    final size = canvasSize.shortestSide;
    final normalized = Offset(localPosition.dx / size, localPosition.dy / size);
    final region = hitTestRegion(artwork, normalized);
    if (region == null || completed.contains(region.id)) return;
    if (region.colorId != selectedColorId) {
      setState(() => highlightedRegionId = region.id);
      return;
    }
    setState(() {
      completed = {...completed, region.id};
      highlightedRegionId = null;
    });
    _save();
  }
}

DemoRegion? hitTestRegion(DemoArtwork artwork, Offset normalizedPoint) {
  for (final region in artwork.regions) {
    if (region.rect.contains(normalizedPoint)) return region;
  }
  return null;
}

class ColoringPainter extends CustomPainter {
  ColoringPainter({required this.artwork, required this.selectedColorId, required this.completedRegionIds, required this.highlightedRegionId});

  final DemoArtwork artwork;
  final int selectedColorId;
  final Set<int> completedRegionIds;
  final int? highlightedRegionId;

  @override
  void paint(Canvas canvas, Size size) {
    final text = TextPainter(textDirection: TextDirection.ltr, textAlign: TextAlign.center);
    for (final region in artwork.regions) {
      final rect = Rect.fromLTWH(region.rect.left * size.width, region.rect.top * size.height, region.rect.width * size.width, region.rect.height * size.height);
      final done = completedRegionIds.contains(region.id);
      canvas.drawRect(rect.deflate(1), Paint()..color = done ? artwork.color(region.colorId).color : Colors.white);
      canvas.drawRect(
        rect.deflate(1),
        Paint()
          ..style = PaintingStyle.stroke
          ..strokeWidth = highlightedRegionId == region.id ? 4 : 1
          ..color = highlightedRegionId == region.id ? Colors.redAccent : (region.colorId == selectedColorId ? Colors.black87 : Colors.black26),
      );
      if (!done) {
        text.text = TextSpan(text: '${region.colorId}', style: const TextStyle(color: Colors.black87, fontWeight: FontWeight.w600));
        text.layout();
        text.paint(canvas, rect.center - Offset(text.width / 2, text.height / 2));
      }
    }
  }

  @override
  bool shouldRepaint(covariant ColoringPainter oldDelegate) => true;
}
