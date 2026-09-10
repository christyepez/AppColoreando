import 'dart:async';
import 'dart:math' as math;
import 'package:app_coloreando/src/catalog/demo_artwork.dart';
import 'package:app_coloreando/src/catalog/generated_artwork_repository.dart';
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
  int? lastCompleted;
  Timer? _saveDebounce;
  DemoArtwork? _artwork;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final generated = await ref.read(generatedArtworkRepositoryProvider).load(widget.artworkId);
    final artwork = generated ?? demoArtworkById(widget.artworkId);
    final stored = await ref.read(localProgressStoreProvider).load(widget.artworkId);
    if (!mounted) return;
    setState(() { _artwork = artwork; completed = stored; selectedColorId = artwork.palette.first.id; });
  }

  Future<void> _save() async {
    final snapshot = Set<int>.from(completed);
    await ref.read(localProgressStoreProvider).save(widget.artworkId, snapshot);
  }

  void _scheduleSave() {
    _saveDebounce?.cancel();
    _saveDebounce = Timer(const Duration(milliseconds: 350), _save);
  }

  @override
  void dispose() {
    _saveDebounce?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final artwork = _artwork;
    if (artwork == null) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    final progress = artwork.regions.isEmpty ? 0.0 : completed.length / artwork.regions.length;
    final remainingForColor = artwork.regions.where((r) => r.colorId == selectedColorId && !completed.contains(r.id)).length;

    return Scaffold(
      backgroundColor: const Color(0xFFF4F2EE),
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(icon: const Icon(Icons.arrow_back_ios_new_rounded), onPressed: () => Navigator.of(context).pop()),
        titleSpacing: 0,
        title: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(artwork.title, style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w800)),
          Text('${(progress * 100).round()}% completado', style: const TextStyle(fontSize: 11, color: Colors.black45, fontWeight: FontWeight.w600)),
        ]),
        actions: [
          IconButton(tooltip: 'Deshacer', icon: const Icon(Icons.undo_rounded), onPressed: lastCompleted == null ? null : () {
            setState(() {
              completed.remove(lastCompleted);
              lastCompleted = null;
            });
            _scheduleSave();
          }),
          IconButton(tooltip: 'Pista', icon: const Icon(Icons.lightbulb_outline_rounded), onPressed: () {
            final candidates = artwork.regions.where((x) => x.colorId == selectedColorId && !completed.contains(x.id));
            setState(() => highlightedRegionId = candidates.isEmpty ? null : candidates.first.id);
          }),
        ],
      ),
      body: Column(children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(20, 0, 20, 12),
          child: ClipRRect(borderRadius: BorderRadius.circular(12), child: LinearProgressIndicator(value: progress, minHeight: 6, backgroundColor: Colors.black12, color: const Color(0xFF202020))),
        ),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(12, 0, 12, 4),
            child: LayoutBuilder(builder: (context, outer) {
              final side = math.min(outer.maxWidth, outer.maxHeight);
              return Center(
                child: SizedBox.square(
                  dimension: side,
                  child: Container(
                    decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(24), boxShadow: const [BoxShadow(color: Color(0x12000000), blurRadius: 18, offset: Offset(0, 5))]),
                    clipBehavior: Clip.antiAlias,
                    child: InteractiveViewer(
                      minScale: 1,
                      maxScale: 7,
                      boundaryMargin: const EdgeInsets.all(60),
                      child: LayoutBuilder(builder: (context, constraints) => GestureDetector(
                        behavior: HitTestBehavior.opaque,
                        onTapUp: (details) => _tap(details.localPosition, Size(constraints.maxWidth, constraints.maxHeight), artwork),
                        child: RepaintBoundary(child: CustomPaint(painter: ColoringPainter(artwork: artwork, selectedColorId: selectedColorId, completedRegionIds: completed, highlightedRegionId: highlightedRegionId))),
                      )),
                    ),
                  ),
                ),
              );
            }),
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
          child: Row(children: [
            Text('Color $selectedColorId', style: const TextStyle(fontWeight: FontWeight.w800)),
            const SizedBox(width: 8),
            Text(remainingForColor == 0 ? '¡Completado!' : '$remainingForColor zonas restantes', style: const TextStyle(color: Colors.black45, fontSize: 12)),
            const Spacer(),
            const Icon(Icons.zoom_in_rounded, size: 18, color: Colors.black38),
            const SizedBox(width: 4),
            const Text('Pellizca para ampliar', style: TextStyle(color: Colors.black38, fontSize: 11)),
          ]),
        ),
        SizedBox(
          height: 92,
          child: ListView.separated(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
            scrollDirection: Axis.horizontal,
            itemCount: artwork.palette.length,
            separatorBuilder: (_, __) => const SizedBox(width: 10),
            itemBuilder: (context, index) {
              final entry = artwork.palette[index];
              final remaining = artwork.regions.where((r) => r.colorId == entry.id && !completed.contains(r.id)).length;
              final selected = selectedColorId == entry.id;
              final finished = remaining == 0;
              return GestureDetector(
                onTap: () => setState(() {
                  selectedColorId = entry.id;
                  highlightedRegionId = null;
                }),
                child: AnimatedContainer(
                  duration: const Duration(milliseconds: 180),
                  width: 64,
                  transform: Matrix4.translationValues(0, selected ? -5 : 0, 0),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: selected ? const Color(0xFF222222) : Colors.transparent, width: 2),
                    boxShadow: selected ? const [BoxShadow(color: Color(0x18000000), blurRadius: 12, offset: Offset(0, 4))] : null,
                  ),
                  child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [
                    CircleAvatar(radius: 18, backgroundColor: entry.color, child: finished ? const Icon(Icons.check_rounded, color: Colors.white, size: 18) : Text('${entry.id}', style: TextStyle(color: ThemeData.estimateBrightnessForColor(entry.color) == Brightness.dark ? Colors.white : Colors.black87, fontWeight: FontWeight.w800))),
                    const SizedBox(height: 5),
                    Text(finished ? 'listo' : '$remaining', style: const TextStyle(fontSize: 10, color: Colors.black45, fontWeight: FontWeight.w700)),
                  ]),
                ),
              );
            },
          ),
        ),
      ]),
    );
  }

  void _tap(Offset localPosition, Size canvasSize, DemoArtwork artwork) {
    if (canvasSize.width <= 0 || canvasSize.height <= 0) return;
    final normalized = Offset(localPosition.dx / canvasSize.width, localPosition.dy / canvasSize.height);
    final region = hitTestRegion(artwork, normalized);
    if (region == null || completed.contains(region.id)) return;
    if (region.colorId != selectedColorId) {
      setState(() => highlightedRegionId = region.id);
      return;
    }
    setState(() {
      completed = {...completed, region.id};
      lastCompleted = region.id;
      highlightedRegionId = null;
    });
    _scheduleSave();
  }
}

DemoRegion? hitTestRegion(DemoArtwork artwork, Offset normalizedPoint) {
  if (normalizedPoint.dx < 0 || normalizedPoint.dx > 1 || normalizedPoint.dy < 0 || normalizedPoint.dy > 1) return null;
  for (final region in artwork.regions.reversed) {
    if (region.contains(normalizedPoint)) return region;
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
    canvas.drawRect(Offset.zero & size, Paint()..color = const Color(0xFFFCFBF9));
    final text = TextPainter(textDirection: TextDirection.ltr, textAlign: TextAlign.center);
    for (final region in artwork.regions) {
      final path = region.path(size);
      final done = completedRegionIds.contains(region.id);
      final active = region.colorId == selectedColorId;
      final highlighted = highlightedRegionId == region.id;
      canvas.drawPath(path, Paint()..color = done ? artwork.color(region.colorId).color : (active ? const Color(0xFFF7F5F1) : const Color(0xFFFBFAF8)));
      canvas.drawPath(path, Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = highlighted ? 3 : (active ? 1.25 : .8)
        ..color = highlighted ? const Color(0xFFE75D70) : (active ? const Color(0xFF8A8883) : const Color(0xFFD7D3CC)));
      if (!done && region.labelVisibleAtBase && (active || highlighted)) {
        text.text = TextSpan(text: '${region.colorId}', style: TextStyle(color: highlighted ? const Color(0xFFE75D70) : const Color(0xFF55524D), fontSize: (size.shortestSide / 34).clamp(9, 14), fontWeight: FontWeight.w700));
        text.layout();
        final anchor = region.labelOffset ?? region.rect.center;
        final center = Offset(anchor.dx * size.width, anchor.dy * size.height);
        text.paint(canvas, center - Offset(text.width / 2, text.height / 2));
      }
    }
    if (artwork.id == 'duck-tropical') {
      final ink = Paint()..style = PaintingStyle.stroke..strokeCap = StrokeCap.round..strokeWidth = 1.2..color = const Color(0xFFAAA59D);
      final wing = Path()..moveTo(.37 * size.width, .49 * size.height)..quadraticBezierTo(.45 * size.width, .45 * size.height, .54 * size.width, .49 * size.height)..quadraticBezierTo(.47 * size.width, .55 * size.height, .39 * size.width, .54 * size.height);
      canvas.drawPath(wing, ink);
      canvas.drawLine(Offset(.72 * size.width, .305 * size.height), Offset(.84 * size.width, .305 * size.height), ink);
      final water = Paint()..style = PaintingStyle.stroke..strokeWidth = .8..color = const Color(0xFFB9D8DE);
      for (var y = .75; y <= .90; y += .05) { final p = Path()..moveTo(.04 * size.width, y * size.height); p.cubicTo(.27 * size.width, (y-.015) * size.height, .50 * size.width, (y+.015) * size.height, .73 * size.width, y * size.height); p.cubicTo(.84 * size.width, (y-.012) * size.height, .93 * size.width, (y+.012) * size.height, .98 * size.width, y * size.height); canvas.drawPath(p, water); }
    }
  }

  @override
  bool shouldRepaint(covariant ColoringPainter oldDelegate) => oldDelegate.selectedColorId != selectedColorId || oldDelegate.highlightedRegionId != highlightedRegionId || oldDelegate.completedRegionIds.length != completedRegionIds.length;
}
