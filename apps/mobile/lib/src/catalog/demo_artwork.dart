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

  // Andes - original AppColoreando scenes.
  DemoArtwork.generate('cotopaxi-sunrise', 'Amanecer en el Cotopaxi', 'EC', 4, 'Paisajes Andinos', 6),
  DemoArtwork.generate('quilotoa-lagoon', 'Laguna esmeralda andina', 'EC', 4, 'Paisajes Andinos', 7),
  DemoArtwork.generate('paramo-trail', 'Sendero de paramo', 'EC', 3, 'Paisajes Andinos', 8),
  DemoArtwork.generate('andean-condor', 'Condor de los Andes', 'EC', 4, 'Fauna Andina', 9),
  DemoArtwork.generate('llama-valley', 'Llamas del valle', 'SA', 3, 'Fauna Andina', 10),
  DemoArtwork.generate('andean-market', 'Mercado andino', 'EC', 5, 'Cultura Andina', 11),
  DemoArtwork.generate('andean-textiles', 'Textiles de los Andes', 'SA', 4, 'Cultura Andina', 12),
  DemoArtwork.generate('snowy-andes', 'Cumbres nevadas', 'SA', 5, 'Paisajes Andinos', 13),

  // Original space-opera collection (genre inspired, no franchise assets).
  DemoArtwork.generate('double-sun-world', 'Planeta de dos soles', 'SA', 4, 'Space Opera', 14),
  DemoArtwork.generate('stellar-fighter', 'Caza estelar', 'SA', 5, 'Naves Galacticas', 15),
  DemoArtwork.generate('orbital-city', 'Ciudad orbital', 'SA', 5, 'Mundos Galacticos', 16),
  DemoArtwork.generate('explorer-droid', 'Droide explorador', 'SA', 3, 'Space Opera', 17),
  DemoArtwork.generate('nebula-cruiser', 'Crucero de la nebulosa', 'SA', 5, 'Naves Galacticas', 18),
  DemoArtwork.generate('ringed-world', 'Mundo de anillos', 'SA', 4, 'Mundos Galacticos', 19),
  DemoArtwork.generate('energy-duel', 'Duelo de energia', 'SA', 5, 'Space Opera', 20),
  DemoArtwork.generate('galactic-temple', 'Templo galactico', 'SA', 5, 'Mundos Galacticos', 21),

  // Original comic collection.
  DemoArtwork.generate('urban-hero', 'Heroe urbano', 'SA', 4, 'Heroes Comic', 22),
  DemoArtwork.generate('tech-villain', 'Villano tecnologico', 'SA', 5, 'Villanos Comic', 23),
  DemoArtwork.generate('comic-rooftop', 'Batalla en la azotea', 'SA', 5, 'Accion Comic', 24),
  DemoArtwork.generate('neon-comic-city', 'Ciudad comic neon', 'SA', 4, 'Ciudad Comic', 25),
  DemoArtwork.generate('noir-detective', 'Detective noir', 'SA', 4, 'Comic Noir', 26),
  DemoArtwork.generate('retro-heroine', 'Heroina retro', 'SA', 4, 'Heroes Comic', 27),
  DemoArtwork.generate('giant-guardian', 'Guardian mecanico', 'SA', 5, 'Accion Comic', 28),
  DemoArtwork.generate('pulp-adventure', 'Aventura pulp', 'SA', 3, 'Accion Comic', 29),

  // Original anime collection.
  DemoArtwork.generate('anime-adventurer', 'Aventurera del cielo', 'SA', 4, 'Aventura Anime', 30),
  DemoArtwork.generate('anime-swordsman', 'Espadachin del viento', 'SA', 5, 'Aventura Anime', 31),
  DemoArtwork.generate('anime-dragon', 'Dragon de cristal', 'SA', 5, 'Fantasia Anime', 32),
  DemoArtwork.generate('anime-magic-girl', 'Guardiana estelar', 'SA', 4, 'Fantasia Anime', 33),
  DemoArtwork.generate('anime-mecha', 'Mecha guardian', 'SA', 5, 'Mecha Anime', 34),
  DemoArtwork.generate('anime-chibi-team', 'Equipo chibi', 'SA', 2, 'Chibi', 35),
  DemoArtwork.generate('anime-city', 'Ciudad anime futurista', 'SA', 4, 'Aventura Anime', 36),
  DemoArtwork.generate('anime-portrait', 'Retrato anime', 'SA', 3, 'Retratos Anime', 37),

  // Complete initial Andean pack: 20 base artworks.
  DemoArtwork.generate('andean-waterfall', 'Cascada de altura', 'EC', 4, 'Paisajes Andinos', 38),
  DemoArtwork.generate('andes-valley', 'Valle interandino', 'EC', 3, 'Paisajes Andinos', 39),
  DemoArtwork.generate('andean-village', 'Pueblo de la sierra', 'EC', 4, 'Cultura Andina', 40),
  DemoArtwork.generate('adobe-house', 'Casa de adobe y montanas', 'EC', 3, 'Cultura Andina', 41),
  DemoArtwork.generate('andean-musician', 'Musico de los Andes', 'SA', 4, 'Cultura Andina', 42),
  DemoArtwork.generate('andean-hummingbird', 'Colibri andino', 'EC', 3, 'Fauna Andina', 43),
  DemoArtwork.generate('andean-fox', 'Zorro de montana', 'SA', 4, 'Fauna Andina', 44),
  DemoArtwork.generate('alpaca-meadow', 'Alpaca en la pradera', 'SA', 3, 'Fauna Andina', 45),
  DemoArtwork.generate('andean-lake-reflection', 'Reflejo en el lago andino', 'EC', 5, 'Paisajes Andinos', 46),
  DemoArtwork.generate('andean-woman', 'Vestimenta andina', 'EC', 5, 'Cultura Andina', 47),
  DemoArtwork.generate('andean-flower-field', 'Flores del paramo', 'EC', 4, 'Paisajes Andinos', 48),
  DemoArtwork.generate('llama-mountain-group', 'Llamas y montanas', 'SA', 4, 'Fauna Andina', 49),

  // Complete initial Space Opera pack: 20 base artworks.
  DemoArtwork.generate('desert-starship', 'Nave sobre el desierto', 'SA', 5, 'Naves Galacticas', 50),
  DemoArtwork.generate('galactic-warrior', 'Guerrero galactico original', 'SA', 5, 'Space Opera', 51),
  DemoArtwork.generate('night-future-city', 'Ciudad futurista nocturna', 'SA', 5, 'Mundos Galacticos', 52),
  DemoArtwork.generate('stellar-battle', 'Batalla estelar', 'SA', 5, 'Space Opera', 53),
  DemoArtwork.generate('space-station', 'Estacion espacial', 'SA', 5, 'Mundos Galacticos', 54),
  DemoArtwork.generate('galactic-pilot', 'Piloto de las estrellas', 'SA', 4, 'Space Opera', 55),
  DemoArtwork.generate('starship-hangar', 'Hangar de naves', 'SA', 5, 'Naves Galacticas', 56),
  DemoArtwork.generate('friendly-alien', 'Criatura de otro mundo', 'SA', 3, 'Mundos Galacticos', 57),
  DemoArtwork.generate('moon-ruins', 'Ruinas en la luna', 'SA', 5, 'Mundos Galacticos', 58),
  DemoArtwork.generate('stellar-squadron', 'Escuadron estelar', 'SA', 5, 'Naves Galacticas', 59),
  DemoArtwork.generate('future-control-room', 'Centro de control futurista', 'SA', 4, 'Space Opera', 60),
  DemoArtwork.generate('rocky-planet-fleet', 'Flota sobre planeta rocoso', 'SA', 5, 'Mundos Galacticos', 61),

  // Complete initial Comic pack: 20 base artworks.
  DemoArtwork.generate('flying-comic-hero', 'Heroe en vuelo', 'SA', 4, 'Heroes Comic', 62),
  DemoArtwork.generate('comic-heroine-cape', 'Heroina de la capa roja', 'SA', 4, 'Heroes Comic', 63),
  DemoArtwork.generate('future-motorcyclist', 'Motociclista futurista', 'SA', 4, 'Accion Comic', 64),
  DemoArtwork.generate('secret-lab', 'Laboratorio secreto', 'SA', 5, 'Comic Noir', 65),
  DemoArtwork.generate('comic-team-cover', 'Equipo de heroes', 'SA', 5, 'Heroes Comic', 66),
  DemoArtwork.generate('comic-street-chase', 'Persecucion urbana', 'SA', 5, 'Accion Comic', 67),
  DemoArtwork.generate('comic-mad-scientist', 'Cientifico excentrico', 'SA', 4, 'Villanos Comic', 68),
  DemoArtwork.generate('comic-explosion', 'Explosion pop', 'SA', 3, 'Accion Comic', 69),
  DemoArtwork.generate('hero-villain-duel', 'Heroe contra villano', 'SA', 5, 'Accion Comic', 70),
  DemoArtwork.generate('future-warrior-comic', 'Guerrera futurista', 'SA', 4, 'Heroes Comic', 71),
  DemoArtwork.generate('retro-comic-cover', 'Portada comic retro', 'SA', 4, 'Heroes Comic', 72),
  DemoArtwork.generate('comic-mecha-protector', 'Protector mecanico', 'SA', 5, 'Accion Comic', 73),

  // Complete initial Anime pack: 20 base artworks.
  DemoArtwork.generate('anime-friends', 'Amigos de aventura', 'SA', 4, 'Aventura Anime', 74),
  DemoArtwork.generate('anime-spirit-creature', 'Criatura espiritual', 'SA', 4, 'Fantasia Anime', 75),
  DemoArtwork.generate('anime-school-day', 'Dia de escuela', 'SA', 3, 'Aventura Anime', 76),
  DemoArtwork.generate('anime-future-warrior', 'Guerrera del futuro', 'SA', 5, 'Aventura Anime', 77),
  DemoArtwork.generate('anime-magic-forest', 'Bosque encantado', 'SA', 4, 'Fantasia Anime', 78),
  DemoArtwork.generate('anime-adventure-pair', 'Dupla de aventura', 'SA', 4, 'Aventura Anime', 79),
  DemoArtwork.generate('anime-pet-friend', 'Heroe y mascota', 'SA', 3, 'Retratos Anime', 80),
  DemoArtwork.generate('anime-energy-battle', 'Batalla de energia', 'SA', 5, 'Aventura Anime', 81),
  DemoArtwork.generate('anime-samurai', 'Samurai del amanecer', 'SA', 5, 'Aventura Anime', 82),
  DemoArtwork.generate('anime-urban-mecha', 'Mecha urbano', 'SA', 5, 'Mecha Anime', 83),
  DemoArtwork.generate('anime-temple', 'Templo de la montana', 'SA', 4, 'Fantasia Anime', 84),
  DemoArtwork.generate('anime-hero-team', 'Equipo de heroes anime', 'SA', 5, 'Aventura Anime', 85),
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
      [const Color(0xFF145A32), const Color(0xFFFF8C1A), const Color(0xFFE63961), const Color(0xFFFFD23F), const Color(0xFF21A179), const Color(0xFFF2E8D5)],
      [const Color(0xFF4B2417), const Color(0xFFB85C24), const Color(0xFFF19C3D), const Color(0xFFFFD08A), const Color(0xFF487A4B), const Color(0xFFE9D5B4)],
      [const Color(0xFF0A2F5A), const Color(0xFFF0454F), const Color(0xFFFF8A24), const Color(0xFFFFD43B), const Color(0xFF10A7A5), const Color(0xFFF2E6D0)],
      [const Color(0xFF216837), const Color(0xFFF0386B), const Color(0xFFFF7A21), const Color(0xFFFFD42A), const Color(0xFF5CAF78), const Color(0xFFF3E2C7)],
      [const Color(0xFF083D77), const Color(0xFF0077B6), const Color(0xFF00B4D8), const Color(0xFFFFC300), const Color(0xFFFF5A36), const Color(0xFFF4EDE0)],
      [const Color(0xFF131629), const Color(0xFF403D8F), const Color(0xFF7B2CBF), const Color(0xFFE63E8C), const Color(0xFFFFC857), const Color(0xFFF5EEE4)],

      // Andean vivid.
      [const Color(0xFF125C2E), const Color(0xFF00A6A6), const Color(0xFF1976D2), const Color(0xFFFFC107), const Color(0xFFE63946), const Color(0xFF8D5524), const Color(0xFFF4E2C6)],
      [const Color(0xFF0B6E4F), const Color(0xFF17A398), const Color(0xFF1261A0), const Color(0xFFFFB000), const Color(0xFFF24C3D), const Color(0xFF6B3E26), const Color(0xFFF2D7B6)],

      // Galactic / space opera.
      [const Color(0xFF050816), const Color(0xFF132A63), const Color(0xFF4D2DB7), const Color(0xFF00D4FF), const Color(0xFFFF2E88), const Color(0xFFFFB000), const Color(0xFFE8F7FF)],
      [const Color(0xFF070A18), const Color(0xFF243B80), const Color(0xFF7A1CAC), const Color(0xFF00E0FF), const Color(0xFFFF3B30), const Color(0xFFFFC857), const Color(0xFFDCEBFF)],

      // Comic.
      [const Color(0xFF101820), const Color(0xFF0057B8), const Color(0xFFE31B23), const Color(0xFFFFC72C), const Color(0xFF00A651), const Color(0xFFF58220), const Color(0xFFF7F7F7)],
      [const Color(0xFF171717), const Color(0xFF0066FF), const Color(0xFFFF1744), const Color(0xFFFFD600), const Color(0xFF00C853), const Color(0xFFAA00FF), const Color(0xFFF5F5F5)],

      // Anime / mecha.
      [const Color(0xFF13293D), const Color(0xFF1B98E0), const Color(0xFFFF4D8D), const Color(0xFFFFC145), const Color(0xFF53D769), const Color(0xFF8E44AD), const Color(0xFFFFE0C2)],
      [const Color(0xFF121826), const Color(0xFF00A8E8), const Color(0xFFFF2D55), const Color(0xFFFFCC00), const Color(0xFF30D158), const Color(0xFF5E5CE6), const Color(0xFFFAD7B5)],
    ];
    final colors = palettes[variant % palettes.length];
    final palette = [for (var i = 0; i < colors.length; i++) DemoColor(i + 1, colors[i])];
    final regions = _themedRegions(category, variant, palette.length, difficulty);
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


List<DemoRegion> _themedRegions(String category, int variant, int colorCount, int difficulty) {
  final normalized = category.toLowerCase();
  if (normalized.contains('andino') || normalized.contains('andina')) {
    return _andeanSceneRegions(variant, colorCount);
  }
  if (normalized.contains('space') || normalized.contains('galact') || normalized.contains('naves')) {
    return _spaceSceneRegions(variant, colorCount);
  }
  if (normalized.contains('comic')) {
    return _comicSceneRegions(variant, colorCount);
  }
  if (normalized.contains('anime') || normalized.contains('chibi') || normalized.contains('mecha')) {
    return _animeSceneRegions(variant, colorCount, normalized.contains('mecha'));
  }
  return DemoArtwork._radialRegions(variant, colorCount, difficulty);
}

DemoRegion _poly(int id, int colorId, List<Offset> points, {bool smooth = true}) =>
    DemoRegion(id: id, colorId: colorId, points: points, smooth: smooth);

DemoRegion _ellipse(int id, int colorId, Offset center, double rx, double ry) {
  final bounds = [
    Offset(center.dx - rx, center.dy - ry),
    Offset(center.dx + rx, center.dy - ry),
    Offset(center.dx + rx, center.dy + ry),
    Offset(center.dx - rx, center.dy + ry),
  ];
  return DemoRegion(
    id: id,
    colorId: colorId,
    points: bounds,
    pathBuilder: (size) => Path()
      ..addOval(Rect.fromCenter(
        center: Offset(center.dx * size.width, center.dy * size.height),
        width: rx * 2 * size.width,
        height: ry * 2 * size.height,
      )),
  );
}

List<DemoRegion> _andeanSceneRegions(int variant, int colorCount) {
  var id = 1;
  int c(int offset) => ((variant + offset) % colorCount) + 1;
  final shift = ((variant % 5) - 2) * .012;
  final regions = <DemoRegion>[
    _poly(id++, c(0), const [Offset(0,0), Offset(1,0), Offset(1,.48), Offset(0,.48)], smooth:false),
    _ellipse(id++, c(3), Offset(.78 + shift, .17), .075, .075),
    _poly(id++, c(4), [Offset(0,.50), Offset(.18,.30), Offset(.34,.50), Offset(.50,.23 + shift), Offset(.68,.50), Offset(.82,.35), const Offset(1,.51)], smooth:false),
    _poly(id++, c(2), [Offset(0,.56), Offset(.16,.43), Offset(.29,.57), Offset(.47,.38), Offset(.63,.57), Offset(.83,.45), const Offset(1,.57)], smooth:true),
    _poly(id++, c(5), const [Offset(0,.58), Offset(1,.58), Offset(1,.77), Offset(0,.77)], smooth:true),
    _poly(id++, c(1), const [Offset(0,.74), Offset(1,.74), Offset(1,1), Offset(0,1)], smooth:true),
  ];
  for (var i = 0; i < 7; i++) {
    final x = .08 + i * .145 + shift * .3;
    regions.add(_poly(id++, c(i + 1), [
      Offset(x, .79),
      Offset(x + .035, .69 - (i % 2) * .025),
      Offset(x + .07, .79),
      Offset(x + .055, .96),
      Offset(x + .015, .96),
    ]));
  }
  regions.add(_poly(id++, c(2), const [Offset(.38,.48), Offset(.50,.25), Offset(.62,.48), Offset(.57,.48), Offset(.50,.34), Offset(.44,.48)], smooth:false));
  regions.add(_ellipse(id++, c(4), const Offset(.24,.83), .035, .022));
  regions.add(_ellipse(id++, c(3), const Offset(.31,.85), .042, .025));
  regions.add(_ellipse(id++, c(2), const Offset(.72,.84), .038, .024));
  regions.add(_ellipse(id++, c(5), const Offset(.79,.82), .044, .026));
  return regions;
}

List<DemoRegion> _spaceSceneRegions(int variant, int colorCount) {
  var id = 1;
  int c(int offset) => ((variant + offset) % colorCount) + 1;
  final shift = ((variant % 7) - 3) * .01;
  final regions = <DemoRegion>[
    _poly(id++, c(0), const [Offset(0,0), Offset(1,0), Offset(1,1), Offset(0,1)], smooth:false),
    _ellipse(id++, c(3), Offset(.78 + shift, .22), .16, .16),
    _ellipse(id++, c(4), Offset(.18 - shift, .18), .045, .045),
    _poly(id++, c(2), [
      Offset(.18 + shift,.58), Offset(.50 + shift,.43), Offset(.82 + shift,.58),
      Offset(.58 + shift,.62), Offset(.52 + shift,.70), Offset(.46 + shift,.62),
    ], smooth:false),
    _poly(id++, c(5), [
      Offset(.31 + shift,.57), Offset(.50 + shift,.49), Offset(.69 + shift,.57),
      Offset(.57 + shift,.59), Offset(.50 + shift,.64), Offset(.43 + shift,.59),
    ], smooth:false),
    _poly(id++, c(1), const [Offset(0,.80), Offset(.20,.73), Offset(.42,.80), Offset(.63,.75), Offset(.83,.82), Offset(1,.77), Offset(1,1), Offset(0,1)], smooth:true),
  ];
  for (var i = 0; i < 12; i++) {
    final x = ((i * 37 + variant * 11) % 92) / 100 + .04;
    final y = ((i * 53 + variant * 7) % 62) / 100 + .04;
    regions.add(_ellipse(id++, c(i + 1), Offset(x, y), .007 + (i % 3) * .003, .007 + (i % 2) * .002));
  }
  regions.add(_poly(id++, c(3), const [Offset(.49,.70), Offset(.46,.88), Offset(.50,.82), Offset(.54,.88), Offset(.51,.70)], smooth:false));
  return regions;
}

List<DemoRegion> _comicSceneRegions(int variant, int colorCount) {
  var id = 1;
  int c(int offset) => ((variant + offset) % colorCount) + 1;
  final regions = <DemoRegion>[
    _poly(id++, c(6), const [Offset(0,0), Offset(1,0), Offset(1,1), Offset(0,1)], smooth:false),
    _poly(id++, c(3), const [Offset(.04,.10), Offset(.32,.10), Offset(.32,.74), Offset(.04,.74)], smooth:false),
    _poly(id++, c(1), const [Offset(.35,.20), Offset(.57,.20), Offset(.57,.78), Offset(.35,.78)], smooth:false),
    _poly(id++, c(4), const [Offset(.60,.08), Offset(.96,.08), Offset(.96,.76), Offset(.60,.76)], smooth:false),
    _ellipse(id++, c(2), const Offset(.50,.34), .10, .11),
    _poly(id++, c(0), const [Offset(.40,.44), Offset(.60,.44), Offset(.68,.77), Offset(.33,.77)], smooth:true),
    _poly(id++, c(2), const [Offset(.39,.48), Offset(.28,.61), Offset(.35,.64), Offset(.45,.53)], smooth:false),
    _poly(id++, c(2), const [Offset(.61,.48), Offset(.74,.57), Offset(.68,.64), Offset(.55,.54)], smooth:false),
    _poly(id++, c(5), const [Offset(.43,.76), Offset(.48,.76), Offset(.45,.96), Offset(.37,.96)], smooth:false),
    _poly(id++, c(5), const [Offset(.53,.76), Offset(.58,.76), Offset(.65,.96), Offset(.57,.96)], smooth:false),
  ];
  for (var i = 0; i < 8; i++) {
    final a = i * math.pi / 4 + variant * .09;
    final inner = .18;
    final outer = .31 + (i % 2) * .03;
    regions.add(_poly(id++, c(i + 1), [
      const Offset(.50,.43),
      Offset(.50 + math.cos(a - .10) * inner, .43 + math.sin(a - .10) * inner),
      Offset(.50 + math.cos(a) * outer, .43 + math.sin(a) * outer),
      Offset(.50 + math.cos(a + .10) * inner, .43 + math.sin(a + .10) * inner),
    ], smooth:false));
  }
  return regions;
}

List<DemoRegion> _animeSceneRegions(int variant, int colorCount, bool mecha) {
  var id = 1;
  int c(int offset) => ((variant + offset) % colorCount) + 1;
  final regions = <DemoRegion>[
    _poly(id++, c(1), const [Offset(0,0), Offset(1,0), Offset(1,1), Offset(0,1)], smooth:false),
    _ellipse(id++, c(6), const Offset(.50,.42), .19, .23),
    _poly(id++, c(5), const [Offset(.30,.39), Offset(.34,.20), Offset(.45,.13), Offset(.50,.20), Offset(.57,.12), Offset(.70,.28), Offset(.68,.46), Offset(.61,.28), Offset(.53,.33), Offset(.44,.26), Offset(.37,.44)], smooth:true),
    _ellipse(id++, c(0), const Offset(.43,.42), .035, .022),
    _ellipse(id++, c(0), const Offset(.57,.42), .035, .022),
    _ellipse(id++, c(2), const Offset(.43,.42), .012, .016),
    _ellipse(id++, c(2), const Offset(.57,.42), .012, .016),
    _poly(id++, c(3), const [Offset(.47,.52), Offset(.50,.535), Offset(.53,.52), Offset(.50,.55)], smooth:true),
    _poly(id++, c(4), const [Offset(.36,.62), Offset(.64,.62), Offset(.78,.96), Offset(.22,.96)], smooth:true),
  ];
  if (mecha) {
    regions.addAll([
      _poly(id++, c(0), const [Offset(.25,.24), Offset(.36,.18), Offset(.38,.57), Offset(.28,.70), Offset(.20,.54)], smooth:false),
      _poly(id++, c(2), const [Offset(.75,.24), Offset(.64,.18), Offset(.62,.57), Offset(.72,.70), Offset(.80,.54)], smooth:false),
      _poly(id++, c(3), const [Offset(.39,.60), Offset(.61,.60), Offset(.66,.84), Offset(.34,.84)], smooth:false),
    ]);
  } else {
    regions.add(_poly(id++, c(2), const [Offset(.33,.28), Offset(.24,.18), Offset(.32,.15), Offset(.41,.23)], smooth:true));
    regions.add(_poly(id++, c(4), const [Offset(.67,.28), Offset(.76,.18), Offset(.68,.15), Offset(.59,.23)], smooth:true));
  }
  for (var i = 0; i < 6; i++) {
    final x = .10 + i * .16;
    regions.add(_ellipse(id++, c(i + 1), Offset(x, .12 + (i % 2) * .05), .015, .015));
  }
  return regions;
}

class DemoColor {
  const DemoColor(this.id, this.color);
  final int id;
  final Color color;
}

class DemoRegion {
  DemoRegion({required this.id, required this.colorId, required this.points, this.smooth = false, this.pathBuilder, this.labelOffset, this.labelVisibleAtBase = true, this.labelMinZoom = 1.0}) : rect = _bounds(points);
  final int id;
  final int colorId;
  final List<Offset> points;
  final Rect rect;
  final bool smooth;
  final RegionPathBuilder? pathBuilder;
  final Offset? labelOffset;
  final bool labelVisibleAtBase;
  final double labelMinZoom;
  Path? _normalizedPathCache;
  Size? _scaledPathSize;
  Path? _scaledPathCache;

  Path path(Size size) {
    if (size == const Size(1, 1) && _normalizedPathCache != null) return _normalizedPathCache!;
    if (_scaledPathSize == size && _scaledPathCache != null) return _scaledPathCache!;
    final built = _buildPath(size);
    if (size == const Size(1, 1)) _normalizedPathCache = built;
    _scaledPathSize = size;
    _scaledPathCache = built;
    return built;
  }

  Path _buildPath(Size size) {
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