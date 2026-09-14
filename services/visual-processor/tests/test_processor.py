from pathlib import Path
import json
from dataclasses import replace

import cv2
import numpy as np

from app.processor import (
    ProcessorOptions, _cluster_colors, _contour_compactness, _foreground_likelihood, _friendly_color_name, _apply_art_palette, _harmonize_palette, _illustration_preprocess, _is_sliver_region, _label_scale, _merge_similar_clusters, _resolve_art_style,
    _micro_region_threshold, _spatial_compactness, process_image,
)


def _sample_image(path: Path) -> None:
    image = np.full((320, 320, 3), 245, dtype=np.uint8)
    cv2.circle(image, (120, 150), 70, (60, 210, 240), -1)
    cv2.circle(image, (185, 110), 45, (60, 210, 240), -1)
    triangle = np.array([[220, 105], [285, 125], [220, 140]], np.int32)
    cv2.fillPoly(image, [triangle], (20, 120, 245))
    cv2.ellipse(image, (115, 155), (38, 24), -15, 0, 360, (40, 160, 220), -1)
    cv2.rectangle(image, (0, 235), (319, 319), (210, 155, 65), -1)
    assert cv2.imwrite(str(path), image)
def _options() -> ProcessorOptions:
    return ProcessorOptions(
        target_regions=40,
        max_colors=6,
        simplification_tolerance=0.45,
        edge_sensitivity=0.55,
        curve_smoothness=0.8,
        saturation_boost=0.18,
        contrast_boost=0.1,
    )


def test_process_image_creates_playable_bundle(tmp_path: Path) -> None:
    source = tmp_path / "duck.png"
    output = tmp_path / "out"
    _sample_image(source)
    result = process_image(source, output, _options())

    assert result["regionCount"] >= 4
    assert 3 <= result["colorCount"] <= 6
    for name in ("manifest.json", "regions.json", "palette.json",
                 "artwork.svg", "artwork-lineart.svg", "preview-colored.png", "preview-lineart.png", "bundle.json"):
        assert (output / name).exists()
    regions = json.loads((output / "regions.json").read_text(encoding="utf-8"))
    assert regions
    assert all(region["svgPath"].startswith("M ") and " C " in region["svgPath"] for region in regions)
    assert all(0.0 <= region["labelX"] <= 1.0 for region in regions)
    assert all(0.0 <= region["labelY"] <= 1.0 for region in regions)

    palette = json.loads((output / "palette.json").read_text(encoding="utf-8"))
    assert palette
    assert all(color["hex"].startswith("#") and len(color["hex"]) == 7 for color in palette)
    assert all(not color["name"].startswith("Color ") for color in palette)

    manifest = json.loads((output / "manifest.json").read_text(encoding="utf-8"))
    assert manifest["schemaVersion"] == "2.0"
    assert manifest["qa"]["regionCount"] == len(regions)
    assert 0.0 < manifest["qa"]["playableCoverage"] <= 1.0
    assert 0 <= manifest["qa"]["score"] <= 100
    assert isinstance(manifest["qa"]["publishable"], bool)
    assert isinstance(manifest["qa"]["issues"], list)
    assert manifest["qa"]["publishable"] == (manifest["qa"]["score"] >= 90 and not any(x["severity"] == "Error" for x in manifest["qa"]["issues"]))
    bundle = json.loads((output / "bundle.json").read_text(encoding="utf-8"))
    assert bundle["schemaVersion"] == "2.2"
    assert bundle["regions"] == regions
    assert bundle["palette"] == palette
    assert bundle["adjustments"] == []
    assert bundle["qa"] == manifest["qa"]


def test_process_image_is_deterministic(tmp_path: Path) -> None:
    source = tmp_path / "duck.png"
    first = tmp_path / "first"
    second = tmp_path / "second"
    _sample_image(source)

    process_image(source, first, _options())
    process_image(source, second, _options())

    first_regions = (first / "regions.json").read_text(encoding="utf-8")
    second_regions = (second / "regions.json").read_text(encoding="utf-8")
    first_palette = (first / "palette.json").read_text(encoding="utf-8")
    second_palette = (second / "palette.json").read_text(encoding="utf-8")

    assert first_regions == second_regions
    assert first_palette == second_palette

def test_near_colors_merge_perceptually() -> None:
    labels = np.array([[0, 1, 2], [0, 1, 2]], dtype=np.int32)
    centers = np.array([[150, 140, 130], [153, 142, 131], [90, 180, 170]], dtype=np.uint8)

    merged_labels, merged_centers = _merge_similar_clusters(labels, centers, "Normal")

    assert len(merged_centers) == 2
    assert merged_labels[0, 0] == merged_labels[0, 1]
    assert merged_labels[0, 2] != merged_labels[0, 0]


def test_difficulty_controls_micro_region_threshold() -> None:
    kids = ProcessorOptions(40, 8, 0.45, 0.5, 0.8, 0.1, 0.1, "Kids")
    detailed = ProcessorOptions(40, 8, 0.45, 0.5, 0.8, 0.1, 0.1, "Detailed")

    assert _micro_region_threshold((400, 400), kids) > _micro_region_threshold((400, 400), detailed)
    assert _friendly_color_name("#FFD429") == "Amarillo Sol"


def test_process_variants_creates_complete_pack(tmp_path: Path) -> None:
    source = tmp_path / "duck.png"
    output = tmp_path / "pack"
    _sample_image(source)
    from app.processor import process_variants

    result = process_variants(source, output, _options(), "Normal")
    assert result["variantCount"] == 5
    manifest = json.loads((output / "manifest.json").read_text(encoding="utf-8"))
    assert manifest["schemaVersion"] == "2.1"
    assert manifest["type"] == "variant-pack"
    assert manifest["primaryDifficulty"] == "Normal"
    assert [v["difficulty"] for v in manifest["variants"]] == [
        "Kids", "Easy", "Normal", "Detailed", "Master"
    ]
    assert all(Path(v["manifestPath"]).exists() for v in manifest["variants"])


def test_variant_options_scale_from_kids_to_master() -> None:
    from app.processor import _variant_options

    base = _options()
    variants = [
        _variant_options(base, difficulty)
        for difficulty in ("Kids", "Easy", "Normal", "Detailed", "Master")
    ]
    assert [v.target_regions for v in variants] == sorted(
        v.target_regions for v in variants
    )
    assert [v.max_colors for v in variants] == sorted(
        v.max_colors for v in variants
    )
    assert variants[0].simplification_tolerance > variants[-1].simplification_tolerance


def test_semantic_candidates_are_emitted(tmp_path: Path) -> None:
    source = tmp_path / "duck.png"
    output = tmp_path / "semantic"
    _sample_image(source)
    process_image(source, output, _options())

    regions = json.loads((output / "regions.json").read_text(encoding="utf-8"))
    tags = {region["semanticTag"] for region in regions}
    roles = {region["semanticRole"] for region in regions}
    assert "beak-candidate" in tags
    assert "water" in tags
    assert "environment" in roles
    assert all(region["semanticTag"] for region in regions)
    assert all(region["semanticRole"] for region in regions)


def test_label_readability_metadata_is_consistent(tmp_path: Path) -> None:
    source = tmp_path / "duck.png"
    output = tmp_path / "labels"
    _sample_image(source)
    process_image(source, output, _options())

    regions = json.loads((output / "regions.json").read_text(encoding="utf-8"))
    manifest = json.loads((output / "manifest.json").read_text(encoding="utf-8"))
    assert regions
    assert all(region["labelRadius"] >= 0.0 for region in regions)
    assert all(region["labelMinZoom"] >= 1.0 for region in regions)
    assert all(0.012 <= region["labelFontSize"] <= 0.028 for region in regions)
    visible = sum(1 for region in regions if region["labelVisibleAtBase"])
    assert manifest["qa"]["labelsVisibleAtBase"] == visible
    assert manifest["qa"]["labelsRequiringZoom"] == len(regions) - visible


def test_preview_renderer_creates_catalog_assets(tmp_path: Path) -> None:
    source = tmp_path / "duck.png"
    output = tmp_path / "previews"
    _sample_image(source)
    process_image(source, output, _options())

    expected = {
        "thumbnail.webp": 512,
        "catalog-preview.webp": 768,
        "lineart-preview.webp": 1024,
    }
    for name, size in expected.items():
        path = output / name
        assert path.exists()
        image = cv2.imread(str(path))
        assert image is not None
        assert image.shape[:2] == (size, size)


def test_special_effects_emit_distinct_previews(tmp_path: Path) -> None:
    source = tmp_path / "duck.png"
    _sample_image(source)
    expected = {
        "aura": "vivid-glow", "tesoro": "premium-detail",
        "revela": "partial-reveal", "postal-viva": "warm-editorial",
        "lumina": "highlight-boost", "eclipse": "dark-vivid",
    }
    for style, treatment in expected.items():
        output = tmp_path / style
        base = _options()
        options = ProcessorOptions(
            base.target_regions, base.max_colors, base.simplification_tolerance,
            base.edge_sensitivity, base.curve_smoothness, base.saturation_boost,
            base.contrast_boost, base.difficulty, style,
        )
        process_image(source, output, options)
        manifest = json.loads((output / "manifest.json").read_text(encoding="utf-8"))
        assert manifest["effect"] == {"styleCode": style, "treatment": treatment}
        special = cv2.imread(str(output / "special-preview.webp"))
        catalog = cv2.imread(str(output / "catalog-preview.webp"))
        assert special is not None and special.shape[:2] == (768, 768)
        assert catalog is not None and np.mean(cv2.absdiff(special, catalog)) > 0.1


def test_high_confidence_semantic_hint_overrides_heuristic(tmp_path: Path) -> None:
    source = tmp_path / "duck.png"
    output = tmp_path / "hinted"
    _sample_image(source)
    options = replace(_options(), semantic_hints=({"x": 0.78, "y": 0.39, "tag": "beak", "role": "subject-accent", "confidence": 0.94, "provider": "vision-test"},))
    process_image(source, output, options)
    regions = json.loads((output / "regions.json").read_text(encoding="utf-8"))
    hinted = [r for r in regions if r["semanticSource"] == "vision-test"]
    assert hinted
    assert hinted[0]["semanticTag"] == "beak"
    assert hinted[0]["semanticRole"] == "subject-accent"
    assert hinted[0]["semanticConfidence"] == 0.94


def test_low_confidence_semantic_hint_keeps_heuristic(tmp_path: Path) -> None:
    source = tmp_path / "duck.png"
    output = tmp_path / "low-hint"
    _sample_image(source)
    options = replace(_options(), semantic_hints=({"x": 0.78, "y": 0.39, "tag": "forced", "role": "forced", "confidence": 0.40, "provider": "vision-test"},))
    process_image(source, output, options)
    regions = json.loads((output / "regions.json").read_text(encoding="utf-8"))
    assert all(r["semanticSource"] == "heuristic" for r in regions)
    assert all(r["semanticConfidence"] == 0.55 for r in regions)

def test_spatial_compactness_scales_with_difficulty() -> None:
    assert _spatial_compactness("Kids") > _spatial_compactness("Normal")
    assert _spatial_compactness("Normal") > _spatial_compactness("Master")


def test_spatial_color_clustering_is_deterministic() -> None:
    bgr = np.zeros((96, 144, 3), dtype=np.uint8)
    bgr[:, :48] = (40, 180, 230)
    bgr[:, 48:96] = (180, 80, 45)
    bgr[:, 96:] = (40, 180, 230)
    lab = cv2.cvtColor(bgr, cv2.COLOR_BGR2LAB)

    first_labels, first_centers = _cluster_colors(lab, 3, "Normal")
    second_labels, second_centers = _cluster_colors(lab, 3, "Normal")

    assert np.array_equal(first_labels, second_labels)
    assert np.array_equal(first_centers, second_centers)
    assert len(np.unique(first_labels)) == 3
    assert first_labels[48, 20] != first_labels[48, 120]

def test_foreground_likelihood_prefers_center_subject() -> None:
    image = np.full((120, 120, 3), (220, 220, 220), dtype=np.uint8)
    cv2.circle(image, (60, 60), 24, (30, 80, 210), -1)
    lab = cv2.cvtColor(image, cv2.COLOR_BGR2LAB)
    likelihood = _foreground_likelihood(lab)
    assert likelihood[60, 60] > likelihood[5, 5]
    assert 0.0 <= float(likelihood.min()) <= float(likelihood.max()) <= 1.0


def test_contour_quality_detects_thin_sliver() -> None:
    contour = np.array([[[5, 5]], [[115, 5]], [[115, 7]], [[5, 7]]], dtype=np.int32)
    assert _contour_compactness(contour) < 0.1
    assert _is_sliver_region(contour, 120 * 120)

def test_harmonize_palette_preserves_neutrals_and_is_deterministic() -> None:
    source = ['#808080', '#D96A45', '#D96A49', '#3A7FBF']
    first = _harmonize_palette(source)
    second = _harmonize_palette(source)
    assert first == second
    assert first[0] == '#808080'
    assert len(first) == len(source)
    assert first[1] != first[2]


def test_illustration_preprocess_is_deterministic_and_preserves_shape() -> None:
    image = np.zeros((80, 120, 3), dtype=np.uint8)
    image[:, :60] = (30, 160, 220)
    image[:, 60:] = (190, 70, 40)
    first = _illustration_preprocess(image, "Normal")
    second = _illustration_preprocess(image, "Normal")
    assert first.shape == image.shape
    assert np.array_equal(first, second)


def test_label_scale_accounts_for_available_radius() -> None:
    from types import SimpleNamespace
    roomy = SimpleNamespace(label_radius=0.05, color_id=3, area=1200)
    tight = SimpleNamespace(label_radius=0.012, color_id=12, area=1200)
    assert _label_scale(roomy, 400, 400) > _label_scale(tight, 400, 400)

def test_auto_art_style_resolves_from_semantic_hints() -> None:
    hints = ({"tag": "bird", "role": "subject", "confidence": 0.95},)
    assert _resolve_art_style("auto", hints) == "animals"
    assert _resolve_art_style("portrait", ()) == "portrait"
    assert _resolve_art_style("unknown", ()) == "natural"


def test_art_palette_profile_is_deterministic_and_style_specific() -> None:
    palette = ["#D96A45", "#3A7FBF", "#808080"]
    kawaii = _apply_art_palette(palette, "kawaii")
    portrait = _apply_art_palette(palette, "portrait")
    assert kawaii == _apply_art_palette(palette, "kawaii")
    assert kawaii != portrait
    assert len(kawaii) == len(palette)


def test_art_style_is_emitted_in_bundle(tmp_path: Path) -> None:
    source = tmp_path / "duck.png"
    output = tmp_path / "art-style"
    _sample_image(source)
    options = replace(_options(), art_style="animals")
    process_image(source, output, options)
    bundle = json.loads((output / "bundle.json").read_text(encoding="utf-8"))
    manifest = json.loads((output / "manifest.json").read_text(encoding="utf-8"))
    assert bundle["artStyle"] == "animals"
    assert manifest["artStyle"] == "animals"

def test_visual_qa_v2_emits_quality_metrics(tmp_path: Path) -> None:
    source = tmp_path / "duck.png"
    output = tmp_path / "qa-v2"
    _sample_image(source)
    result = process_image(source, output, _options())
    qa = result["qa"]
    assert "sliverRegionCount" in qa
    assert "sliverRegionRatio" in qa
    assert "averageContourCompactness" in qa
    assert "crampedLabelCount" in qa
    assert "crampedLabelRatio" in qa
    assert "semanticCoverage" in qa
    assert 0.0 <= qa["sliverRegionRatio"] <= 1.0
    assert 0.0 <= qa["crampedLabelRatio"] <= 1.0
    assert 0.0 <= qa["semanticCoverage"] <= 1.0
    assert 0.0 <= qa["averageContourCompactness"] <= 1.0

def test_repair_options_reduce_complexity_for_quality_issues() -> None:
    from app.processor import _repair_options
    base = _options()
    qa = {"issues": [{"code": "QA_MICRO_REGIONS"}, {"code": "QA_LABEL_CRAMPED"}, {"code": "QA_PALETTE_DELTA"}]}
    repaired = _repair_options(base, qa)
    assert repaired.target_regions < base.target_regions
    assert repaired.max_colors < base.max_colors
    assert repaired.simplification_tolerance > base.simplification_tolerance


def test_auto_repair_only_accepts_improvement(tmp_path: Path) -> None:
    from app.processor import process_image_auto_repair
    source = tmp_path / "duck.png"
    output = tmp_path / "repair"
    _sample_image(source)
    result = process_image_auto_repair(source, output, _options())
    assert "autoRepair" in result
    info = result["autoRepair"]
    assert info["finalScore"] >= info["baselineScore"]
    assert (output / "manifest.json").exists()