import 'dart:math' as math;
import 'package:flutter/material.dart';

typedef RegionPathBuilder = Path Function(Size size);

final demoArtworks = [
  _duckArtwork(),
  DemoArtwork.generate('andean-geometry', 'Flores de los Andes', 'EC', 2, 'Naturaleza', 0),
  DemoArtwork.generate('coffee-pattern', 'Cafe de montana', 'CO', 3, 'Arte & cultura', 1),
  DemoArtwork.generate('south-mosaic', 'Atardecer tropical', 'SA', 4, 'Paisajes', 2),
  DemoArtwork.generate('prairie-quilt', 'Jardin de verano', 'US', 2, 'Flores', 3),
  DemoArtwork.generate('ocean-dream', 'Sueno del oceano', 'EC', 3, 'Animales', 4),
];

DemoArtwork demoArtworkById(String id) => demoArtworks.firstWhere((x) => x.id == id, orElse: () => demoArtworks.first);


DemoArtwork _duckArtwork() {
  const palette = [
    DemoColor(1, Color(0xFFF4D35E)),
    DemoColor(2, Color(0xFFF29E4C)),
    DemoColor(3, Color(0xFF69B7C9)),
    DemoColor(4, Color(0xFF5A9367)),
    DemoColor(5, Color(0xFFE9F5F2)),
    DemoColor(6, Color(0xFF2B2D42)),
  ];
  final regions = <DemoRegion>[
    DemoRegion(id: 1, colorId: 5, points: const [Offset(0,0),Offset(1,0),Offset(1,1),Offset(0,1)]),
    DemoRegion(id: 2, colorId: 3, points: const [Offset(0,.63),Offset(.18,.59),Offset(.38,.64),Offset(.58,.60),Offset(.78,.66),Offset(1,.61),Offset(1,1),Offset(0,1)]),
    DemoRegion(id: 3, colorId: 4, points: const [Offset(0,.62),Offset(.10,.50),Offset(.20,.61),Offset(.30,.48),Offset(.40,.63),Offset(.50,.52),Offset(.60,.64),Offset(.70,.49),Offset(.82,.63),Offset(.92,.50),Offset(1,.62),Offset(1,.70),Offset(0,.70)]),
    DemoRegion(id: 4, colorId: 1, points: const [Offset(.17,.60),Offset(.24,.35),Offset(.68,.31),Offset(.76,.57),Offset(.52,.72),Offset(.22,.68)], pathBuilder: _duckBodyPath),
    DemoRegion(id: 5, colorId: 1, points: const [Offset(.48,.12),Offset(.73,.12),Offset(.75,.40),Offset(.48,.40)], pathBuilder: _duckHeadPath),
    DemoRegion(id: 6, colorId: 2, points: const [Offset(.68,.24),Offset(.90,.29),Offset(.69,.38),Offset(.64,.32)], pathBuilder: _duckBeakPath),
    DemoRegion(id: 7, colorId: 1, points: const [Offset(.30,.43),Offset(.46,.36),Offset(.62,.45),Offset(.54,.61),Offset(.34,.59)], pathBuilder: _duckWingPath),
    DemoRegion(id: 8, colorId: 6, points: const [Offset(.615,.215),Offset(.655,.215),Offset(.655,.255),Offset(.615,.255)], pathBuilder: _duckEyePath),
    DemoRegion(id: 9, colorId: 4, points: const [Offset(.04,.65),Offset(.09,.32),Offset(.22,.70)], pathBuilder: _leftReedPath),
    DemoRegion(id: 10, colorId: 4, points: const [Offset(.79,.68),Offset(.88,.34),Offset(.98,.70)], pathBuilder: _rightReedPath),
    DemoRegion(id: 11, colorId: 2, points: const [Offset(.10,.16),Offset(.16,.10),Offset(.22,.16),Offset(.16,.22)]),
  ];
  return DemoArtwork(id: 'duck-tropical', title: 'Pato tropical', countryCode: 'EC', difficulty: 1, category: 'Animales', palette: palette, regions: regions, variant: -1, effectLabel: 'Aura', previewColored: true);
}

class DemoArtwork {
  const DemoArtwork({
    required this.id,
    required this.title,
    required this.countryCode,
    required this.difficulty,
    required this.category,
    required this.palette,
    required this.regions,
    required this.variant,
    this.effectLabel,
    this.previewColored = false,
  });

  final String id;
  final String title;
  final String countryCode;
  final int difficulty;
  final String category;
  final List<DemoColor> palette;
  final List<DemoRegion> regions;
  final int variant;
  final String? effectLabel;
  final bool previewColored;

  int get regionCount => regions.length;
  DemoColor color(int id) => palette.firstWhere((x) => x.id == id);

  static DemoArtwork generate(String id, String title, String countryCode, int difficulty, String category, int variant) {
    final palettes = <List<Color>>[
      [const Color(0xFF205C4C), const Color(0xFFF0A35E), const Color(0xFFE85D75), const Color(0xFFF6D96B), const Color(0xFF79B9A5), const Color(0xFFEBE3D6)],
      [const Color(0xFF4A2B24), const Color(0xFF9E5C3A), const Color(0xFFD99A5C), const Color(0xFFF2D0A4), const Color(0xFF607B63), const Color(0xFFECE2D3)],
      [const Color(0xFF152A47), const Color(0xFFEA6F64), const Color(0xFFF3A453), const Color(0xFFF2D36D), const Color(0xFF4DA6A8), const Color(0xFFEDE8DF)],
      [const Color(0xFF3A6548), const Color(0xFFE96E83), const Color(0xFFF3A05E), const Color(0xFFF2D55D), const Color(0xFF86B7A0), const Color(0xFFEDE5DA)],
      [const Color(0xFF17435D), const Color(0xFF2D80AA), const Color(0xFF56B5B1), const Color(0xFFF0C54E), const Color(0xFFF47E55), const Color(0xFFE9E5DD)],
      [const Color(0xFF1C1F35), const Color(0xFF4E5289), const Color(0xFF7D6CAA), const Color(0xFFE3A5B7), const Color(0xFFF0D2A5), const Color(0xFFE9E6DF)],
    ];
    final colors = palettes[variant % palettes.length];
    final palette = [for (var i = 0; i < colors.length; i++) DemoColor(i + 1, colors[i])];
    final regions = _radialRegions(variant, palette.length, difficulty);
    final labels = ['Tesoro', 'Revela', 'Postal Viva', 'Lumina', 'Eclipse', 'Aura'];
    return DemoArtwork(id: id, title: title, countryCode: countryCode, difficulty: difficulty, category: category, palette: palette, regions: regions, variant: variant, effectLabel: labels[variant % labels.length], previewColored: variant.isEven);
  }

  static List<DemoRegion> _radialRegions(int variant, int colorCount, int difficulty) {
    const sectors = 12;
    const radii = [0.0, .17, .31, .47];
    final center = Offset(.5 + ((variant % 3) - 1) * .012, .5 + ((variant % 2) - .5) * .012);
    final rotation = variant * .13 - math.pi / 2;
    final regions = <DemoRegion>[];
    var id = 1;

    for (var ring = 0; ring < 3; ring++) {
      for (var sector = 0; sector < sectors; sector++) {
        final a0 = rotation + sector * math.pi * 2 / sectors;
        final a1 = rotation + (sector + 1) * math.pi * 2 / sectors;
        final inner = radii[ring];
        final outer = radii[ring + 1];
        final wobble0 = 1 + math.sin((sector + variant) * 1.7) * .035;
        final wobble1 = 1 + math.cos((sector + variant) * 1.35) * .035;
        final p0 = center + Offset(math.cos(a0), math.sin(a0)) * inner;
        final p1 = center + Offset(math.cos(a1), math.sin(a1)) * inner;
        final p2 = center + Offset(math.cos(a1), math.sin(a1)) * outer * wobble1;
        final p3 = center + Offset(math.cos(a0), math.sin(a0)) * outer * wobble0;
        final points = ring == 0 ? [center, p2, p3] : [p0, p1, p2, p3];
        regions.add(DemoRegion(
          id: id,
          colorId: ((sector * 2 + ring * 3 + variant + difficulty) % colorCount) + 1,
          points: points,
        ));
        id++;
      }
    }
    return regions;
  }
}

class DemoColor {
  const DemoColor(this.id, this.color);
  final int id;
  final Color color;
}

class DemoRegion {
  DemoRegion({required this.id, required this.colorId, required this.points, this.smooth = false, this.pathBuilder, this.labelOffset, this.labelVisibleAtBase = true}) : rect = _bounds(points);
  final int id;
  final int colorId;
  final List<Offset> points;
  final Rect rect;
  final bool smooth;
  final RegionPathBuilder? pathBuilder;
  final Offset? labelOffset;
  final bool labelVisibleAtBase;

  Path path(Size size) {
    if (pathBuilder != null) return pathBuilder!(size);
    final scaled = points.map((p) => Offset(p.dx * size.width, p.dy * size.height)).toList();
    final result = Path();
    if (!smooth || scaled.length < 3) {
      result.moveTo(scaled.first.dx, scaled.first.dy);
      for (final point in scaled.skip(1)) {
        result.lineTo(point.dx, point.dy);
      }
      return result..close();
    }
    final firstMid = Offset((scaled.last.dx + scaled.first.dx) / 2, (scaled.last.dy + scaled.first.dy) / 2);
    result.moveTo(firstMid.dx, firstMid.dy);
    for (var i = 0; i < scaled.length; i++) {
      final current = scaled[i];
      final next = scaled[(i + 1) % scaled.length];
      final mid = Offset((current.dx + next.dx) / 2, (current.dy + next.dy) / 2);
      result.quadraticBezierTo(current.dx, current.dy, mid.dx, mid.dy);
    }
    return result..close();
  }

  bool contains(Offset normalized) {
    if (!rect.contains(normalized)) return false;
    return path(const Size(1, 1)).contains(normalized);
  }

  static Rect _bounds(List<Offset> points) {
    final xs = points.map((p) => p.dx);
    final ys = points.map((p) => p.dy);
    return Rect.fromLTRB(xs.reduce(math.min), ys.reduce(math.min), xs.reduce(math.max), ys.reduce(math.max));
  }
}

class DemoArtworkPainter extends CustomPainter {
  DemoArtworkPainter({DemoArtwork? artwork, this.soft = false, this.lineArt = false}) : artwork = artwork ?? demoArtworks.first;
  final DemoArtwork artwork;
  final bool soft;
  final bool lineArt;

  @override
  void paint(Canvas canvas, Size size) {
    canvas.drawRect(Offset.zero & size, Paint()..color = lineArt ? const Color(0xFFFFFEFC) : const Color(0xFFF8F5F0));
    for (final region in artwork.regions) {
      final path = region.path(size);
      final original = artwork.color(region.colorId).color;
      final shouldAccent = lineArt && region.id % 11 == artwork.variant % 11;
      final fill = lineArt
          ? (shouldAccent ? Color.lerp(original, Colors.white, .28)! : const Color(0xFFFFFEFC))
          : (soft ? Color.lerp(original, Colors.white, .10)! : original);
      canvas.drawPath(path, Paint()..color = fill);
      canvas.drawPath(path, Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = lineArt ? 1.15 : .9
        ..color = lineArt ? const Color(0xFF4B4A47) : Colors.white.withValues(alpha: .82));
    }
    if (artwork.variant >= 0) _paintBotanicalLines(canvas, size);
    if (artwork.id == 'duck-tropical') _paintDuckDetails(canvas, size);
  }

  void _paintDuckDetails(Canvas canvas, Size size) {
    final ink = Paint()..style = PaintingStyle.stroke..strokeCap = StrokeCap.round..strokeWidth = lineArt ? 1.4 : 1.8..color = lineArt ? const Color(0xFF3D3B38) : const Color(0x665A4A3A);
    final wing = Path()..moveTo(.37 * size.width, .49 * size.height)..quadraticBezierTo(.45 * size.width, .45 * size.height, .54 * size.width, .49 * size.height)..quadraticBezierTo(.47 * size.width, .55 * size.height, .39 * size.width, .54 * size.height);
    canvas.drawPath(wing, ink);
    canvas.drawLine(Offset(.72 * size.width, .305 * size.height), Offset(.84 * size.width, .305 * size.height), ink);
    canvas.drawCircle(Offset(.63 * size.width, .23 * size.height), .006 * size.shortestSide, Paint()..color = lineArt ? const Color(0xFF3D3B38) : Colors.white);
    final wave = Paint()..style = PaintingStyle.stroke..strokeWidth = lineArt ? .9 : 1.3..color = lineArt ? const Color(0xFF9AB7BE) : Colors.white.withValues(alpha: .55);
    for (var y = .72; y <= .90; y += .06) { final p = Path()..moveTo(.05 * size.width, y * size.height); p.cubicTo(.25 * size.width, (y-.025) * size.height, .45 * size.width, (y+.025) * size.height, .65 * size.width, y * size.height); p.cubicTo(.78 * size.width, (y-.018) * size.height, .90 * size.width, (y+.018) * size.height, .98 * size.width, y * size.height); canvas.drawPath(p, wave); }
  }

  void _paintBotanicalLines(Canvas canvas, Size size) {
    final center = Offset(size.width * .5, size.height * .5);
    final radius = size.shortestSide * .17;
    final paint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = lineArt ? 1.5 : size.shortestSide * .008
      ..color = lineArt ? const Color(0xFF333230) : Colors.white.withValues(alpha: .62);
    for (var i = 0; i < 8; i++) {
      final a = i * math.pi / 4 + artwork.variant * .08;
      final c = center + Offset(math.cos(a), math.sin(a)) * radius;
      canvas.save();
      canvas.translate(c.dx, c.dy);
      canvas.rotate(a);
      canvas.drawOval(Rect.fromCenter(center: Offset.zero, width: radius * 1.15, height: radius * .58), paint);
      canvas.restore();
    }
    canvas.drawCircle(center, radius * .34, paint);
  }

  @override
  bool shouldRepaint(covariant DemoArtworkPainter oldDelegate) =>
      oldDelegate.artwork.id != artwork.id || oldDelegate.soft != soft || oldDelegate.lineArt != lineArt;
}

Path _duckBodyPath(Size s) {
  final p = Path()..moveTo(.17 * s.width, .56 * s.height);
  p.cubicTo(.15 * s.width, .40 * s.height, .27 * s.width, .31 * s.height, .46 * s.width, .31 * s.height);
  p.cubicTo(.61 * s.width, .31 * s.height, .75 * s.width, .40 * s.height, .76 * s.width, .53 * s.height);
  p.cubicTo(.77 * s.width, .66 * s.height, .61 * s.width, .73 * s.height, .43 * s.width, .72 * s.height);
  p.cubicTo(.27 * s.width, .71 * s.height, .18 * s.width, .65 * s.height, .17 * s.width, .56 * s.height);
  return p..close();
}

Path _duckHeadPath(Size s) {
  return Path()..addOval(Rect.fromCenter(center: Offset(.60 * s.width, .27 * s.height), width: .25 * s.width, height: .25 * s.height));
}

Path _duckBeakPath(Size s) {
  final p = Path()..moveTo(.69 * s.width, .25 * s.height);
  p.quadraticBezierTo(.81 * s.width, .26 * s.height, .91 * s.width, .30 * s.height);
  p.quadraticBezierTo(.81 * s.width, .34 * s.height, .70 * s.width, .37 * s.height);
  p.quadraticBezierTo(.65 * s.width, .33 * s.height, .69 * s.width, .25 * s.height);
  return p..close();
}
Path _duckWingPath(Size s) {
  final p = Path()..moveTo(.31 * s.width, .48 * s.height);
  p.cubicTo(.36 * s.width, .39 * s.height, .50 * s.width, .37 * s.height, .60 * s.width, .45 * s.height);
  p.cubicTo(.58 * s.width, .56 * s.height, .49 * s.width, .62 * s.height, .39 * s.width, .59 * s.height);
  p.cubicTo(.33 * s.width, .57 * s.height, .29 * s.width, .53 * s.height, .31 * s.width, .48 * s.height);
  return p..close();
}

Path _duckEyePath(Size s) {
  return Path()..addOval(Rect.fromCenter(center: Offset(.635 * s.width, .235 * s.height), width: .032 * s.width, height: .032 * s.height));
}

Path _leftReedPath(Size s) {
  final p = Path()..moveTo(.06 * s.width, .67 * s.height);
  p.cubicTo(.08 * s.width, .53 * s.height, .08 * s.width, .40 * s.height, .13 * s.width, .31 * s.height);
  p.cubicTo(.20 * s.width, .38 * s.height, .19 * s.width, .54 * s.height, .15 * s.width, .67 * s.height);
  return p..close();
}

Path _rightReedPath(Size s) {
  final p = Path()..moveTo(.82 * s.width, .69 * s.height);
  p.cubicTo(.84 * s.width, .54 * s.height, .85 * s.width, .41 * s.height, .90 * s.width, .33 * s.height);
  p.cubicTo(.96 * s.width, .42 * s.height, .95 * s.width, .56 * s.height, .93 * s.width, .69 * s.height);
  return p..close();
}
