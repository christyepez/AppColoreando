import 'package:flutter/material.dart';

final demoArtworks = [
  DemoArtwork.generate('andean-geometry', 'Andean Geometry Demo', 'EC', 2),
  DemoArtwork.generate('coffee-pattern', 'Coffee Pattern Demo', 'CO', 2),
  DemoArtwork.generate('south-mosaic', 'South Mosaic Demo', 'SA', 3),
  DemoArtwork.generate('prairie-quilt', 'Prairie Quilt Demo', 'US', 1),
];

DemoArtwork demoArtworkById(String id) => demoArtworks.firstWhere((x) => x.id == id, orElse: () => demoArtworks.first);

class DemoArtwork {
  const DemoArtwork({
    required this.id,
    required this.title,
    required this.countryCode,
    required this.difficulty,
    required this.palette,
    required this.regions,
  });

  final String id;
  final String title;
  final String countryCode;
  final int difficulty;
  final List<DemoColor> palette;
  final List<DemoRegion> regions;

  int get regionCount => regions.length;
  DemoColor color(int id) => palette.firstWhere((x) => x.id == id);

  static DemoArtwork generate(String id, String title, String countryCode, int difficulty) {
    final palette = [
      const DemoColor(1, Color(0xFF147D6F)),
      const DemoColor(2, Color(0xFFE0552F)),
      const DemoColor(3, Color(0xFFFFC857)),
      const DemoColor(4, Color(0xFF2E5AAC)),
      const DemoColor(5, Color(0xFFF7F2E8)),
      const DemoColor(6, Color(0xFF323232)),
    ];
    final regions = <DemoRegion>[];
    var number = 1;
    for (var y = 0; y < 6; y++) {
      for (var x = 0; x < 6; x++) {
        regions.add(DemoRegion(
          id: number++,
          colorId: ((x + y + difficulty) % palette.length) + 1,
          rect: Rect.fromLTWH(x / 6, y / 6, 1 / 6, 1 / 6),
        ));
      }
    }
    return DemoArtwork(id: id, title: title, countryCode: countryCode, difficulty: difficulty, palette: palette, regions: regions);
  }
}

class DemoColor {
  const DemoColor(this.id, this.color);
  final int id;
  final Color color;
}

class DemoRegion {
  const DemoRegion({required this.id, required this.colorId, required this.rect});
  final int id;
  final int colorId;
  final Rect rect;
}

class DemoArtworkPainter extends CustomPainter {
  DemoArtworkPainter({DemoArtwork? artwork}) : artwork = artwork ?? demoArtworks.first;

  final DemoArtwork artwork;

  @override
  void paint(Canvas canvas, Size size) {
    final outline = Paint()
      ..style = PaintingStyle.stroke
      ..color = Colors.black26;
    for (final region in artwork.regions) {
      final rect = Rect.fromLTWH(
        region.rect.left * size.width,
        region.rect.top * size.height,
        region.rect.width * size.width,
        region.rect.height * size.height,
      );
      canvas.drawRect(rect, Paint()..color = artwork.color(region.colorId).color);
      canvas.drawRect(rect, outline);
    }
  }

  @override
  bool shouldRepaint(covariant DemoArtworkPainter oldDelegate) => oldDelegate.artwork.id != artwork.id;
}
