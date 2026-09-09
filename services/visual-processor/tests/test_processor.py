from pathlib import Path
import json

import cv2
import numpy as np

from app.processor import (
    ProcessorOptions, _friendly_color_name, _merge_similar_clusters,
    _micro_region_threshold, process_image,
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
                 "artwork.svg", "artwork-lineart.svg", "preview-colored.png", "preview-lineart.png"):
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
