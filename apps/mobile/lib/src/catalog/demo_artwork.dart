import 'package:flutter/material.dart';

const demoArtwork = DemoArtwork(
  id: 'andean-geometry-demo',
  title: 'Andean Geometry Demo',
  regionCount: 36,
);

class DemoArtwork {
  const DemoArtwork({required this.id, required this.title, required this.regionCount});

  final String id;
  final String title;
  final int regionCount;
}

class DemoArtworkPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final palette = [
      const Color(0xFF147D6F),
      const Color(0xFFE0552F),
      const Color(0xFFFFC857),
      const Color(0xFF2E5AAC),
      const Color(0xFFF7F2E8),
      const Color(0xFF323232),
    ];
    final cell = size.width / 6;
    final paint = Paint()..style = PaintingStyle.fill;
    for (var y = 0; y < 6; y++) {
      for (var x = 0; x < 6; x++) {
        paint.color = palette[(x + y) % palette.length];
        final rect = Rect.fromLTWH(x * cell, y * cell * .72, cell, cell * .72);
        canvas.drawRect(rect, paint);
        canvas.drawRect(rect, Paint()..style = PaintingStyle.stroke..color = Colors.black26);
      }
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
