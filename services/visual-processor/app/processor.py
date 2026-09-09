from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import json
import math

import cv2
import numpy as np


@dataclass(frozen=True)
class ProcessorOptions:
    target_regions: int
    max_colors: int
    simplification_tolerance: float
    edge_sensitivity: float
    curve_smoothness: float
    saturation_boost: float
    contrast_boost: float
@dataclass
class Region:
    id: int
    color_id: int
    area: int
    contour: np.ndarray
    label_x: float
    label_y: float
    bounds: tuple[float, float, float, float]
    svg_path: str


def _read_image(path: Path) -> np.ndarray:
    image = cv2.imread(str(path), cv2.IMREAD_UNCHANGED)
    if image is None:
        raise ValueError("SOURCE_DECODE_FAILED")
    if image.ndim == 2:
        image = cv2.cvtColor(image, cv2.COLOR_GRAY2BGR)
    if image.shape[2] == 4:
        alpha = image[:, :, 3:4].astype(np.float32) / 255.0
        rgb = image[:, :, :3].astype(np.float32)
        image = (rgb * alpha + 255.0 * (1.0 - alpha)).astype(np.uint8)
    return image[:, :, :3]
def _resize_for_processing(image: np.ndarray, max_side: int = 1024) -> np.ndarray:
    height, width = image.shape[:2]
    scale = min(1.0, max_side / max(height, width))
    if scale == 1.0:
        return image
    target = (max(1, round(width * scale)), max(1, round(height * scale)))
    return cv2.resize(image, target, interpolation=cv2.INTER_AREA)


def _preprocess(image: np.ndarray, edge_sensitivity: float) -> np.ndarray:
    diameter = max(5, int(7 + 8 * max(0.0, min(edge_sensitivity, 1.0))))
    if diameter % 2 == 0:
        diameter += 1
    filtered = cv2.bilateralFilter(image, diameter, 45, 45)
    lab = cv2.cvtColor(filtered, cv2.COLOR_BGR2LAB)
    l, a, b = cv2.split(lab)
    clahe = cv2.createCLAHE(clipLimit=1.8, tileGridSize=(8, 8))
    l = clahe.apply(l)
    return cv2.merge((l, a, b))


def _cluster_colors(lab: np.ndarray, color_count: int) -> tuple[np.ndarray, np.ndarray]:
    pixels = lab.reshape((-1, 3)).astype(np.float32)
    criteria = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 35, 0.35)
    cv2.setRNGSeed(42)
    _, labels, centers = cv2.kmeans(
        pixels, color_count, None, criteria, 3, cv2.KMEANS_PP_CENTERS
    )
    return labels.reshape(lab.shape[:2]), centers.astype(np.uint8)
def _vivid_palette(centers_lab: np.ndarray, saturation_boost: float, contrast_boost: float) -> list[str]:
    lab = centers_lab.reshape((-1, 1, 3))
    bgr = cv2.cvtColor(lab, cv2.COLOR_LAB2BGR).reshape((-1, 3))
    colors: list[str] = []
    for value in bgr:
        pixel = np.uint8([[value]])
        hsv = cv2.cvtColor(pixel, cv2.COLOR_BGR2HSV)[0, 0].astype(np.float32)
        hsv[1] = min(255.0, hsv[1] * (1.0 + max(0.0, saturation_boost)))
        hsv[2] = np.clip((hsv[2] - 128.0) * (1.0 + contrast_boost) + 128.0, 28.0, 250.0)
        vivid = cv2.cvtColor(np.uint8([[hsv]]), cv2.COLOR_HSV2BGR)[0, 0]
        blue, green, red = [int(x) for x in vivid]
        colors.append(f"#{red:02X}{green:02X}{blue:02X}")
    return colors


def _chaikin(points: np.ndarray, iterations: int) -> np.ndarray:
    pts = points.reshape((-1, 2)).astype(np.float32)
    if len(pts) < 4:
        return pts
    for _ in range(max(0, iterations)):
        expanded: list[np.ndarray] = []
        for idx, current in enumerate(pts):
            nxt = pts[(idx + 1) % len(pts)]
            expanded.append(current * 0.75 + nxt * 0.25)
            expanded.append(current * 0.25 + nxt * 0.75)
        pts = np.asarray(expanded, dtype=np.float32)
    return pts
def _svg_path(points: np.ndarray, width: int, height: int) -> str:
    if len(points) < 3:
        return ""
    pts = np.asarray([(float(x) / width, float(y) / height) for x, y in points])
    commands = [f"M {pts[0][0]:.6f} {pts[0][1]:.6f}"]
    count = len(pts)
    for i in range(count):
        previous = pts[(i - 1) % count]
        current = pts[i]
        nxt = pts[(i + 1) % count]
        after = pts[(i + 2) % count]
        c1 = current + (nxt - previous) / 6.0
        c2 = nxt - (after - current) / 6.0
        commands.append(f"C {c1[0]:.6f} {c1[1]:.6f} {c2[0]:.6f} {c2[1]:.6f} {nxt[0]:.6f} {nxt[1]:.6f}")
    commands.append("Z")
    return " ".join(commands)

def _label_anchor(mask: np.ndarray, width: int, height: int) -> tuple[float, float]:
    distance = cv2.distanceTransform(mask, cv2.DIST_L2, 5)
    _, _, _, max_location = cv2.minMaxLoc(distance)
    return max_location[0] / width, max_location[1] / height


def _bounds(contour: np.ndarray, width: int, height: int) -> tuple[float, float, float, float]:
    x, y, w, h = cv2.boundingRect(contour)
    return x / width, y / height, w / width, h / height


def _extract_regions(labels: np.ndarray, options: ProcessorOptions) -> list[Region]:
    height, width = labels.shape
    image_area = width * height
    minimum_area = max(18, int(image_area / max(options.target_regions, 8) * 0.16))
    regions: list[Region] = []
    next_id = 1
    for color_id in range(int(labels.max()) + 1):
        mask = np.where(labels == color_id, 255, 0).astype(np.uint8)
        kernel = np.ones((3, 3), np.uint8)
        mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel, iterations=1)
        count, components, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
        for component_id in range(1, count):
            area = int(stats[component_id, cv2.CC_STAT_AREA])
            if area < minimum_area:
                continue
            component_mask = np.where(components == component_id, 255, 0).astype(np.uint8)
            contours, _ = cv2.findContours(component_mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
            if not contours:
                continue
            contour = max(contours, key=cv2.contourArea)
            perimeter = cv2.arcLength(contour, True)
            epsilon = max(0.6, perimeter * options.simplification_tolerance * 0.006)
            simplified = cv2.approxPolyDP(contour, epsilon, True)
            smooth_steps = 1 if options.curve_smoothness >= 0.55 else 0
            points = _chaikin(simplified, smooth_steps)
            path = _svg_path(points, width, height)
            if not path:
                continue
            label_x, label_y = _label_anchor(component_mask, width, height)
            regions.append(Region(next_id, color_id + 1, area, contour,
                                  label_x, label_y, _bounds(contour, width, height), path))
            next_id += 1
    return regions
def _hex_to_bgr(value: str) -> tuple[int, int, int]:
    value = value.lstrip("#")
    red = int(value[0:2], 16)
    green = int(value[2:4], 16)
    blue = int(value[4:6], 16)
    return blue, green, red


def _render_colored(labels: np.ndarray, palette: list[str]) -> np.ndarray:
    result = np.zeros((*labels.shape, 3), dtype=np.uint8)
    for idx, color in enumerate(palette):
        result[labels == idx] = _hex_to_bgr(color)
    return result


def _render_line_art(labels: np.ndarray, regions: list[Region]) -> np.ndarray:
    canvas = np.full((*labels.shape, 3), 250, dtype=np.uint8)
    horizontal = labels[:, 1:] != labels[:, :-1]
    vertical = labels[1:, :] != labels[:-1, :]
    canvas[:, 1:][horizontal] = (55, 55, 55)
    canvas[1:, :][vertical] = (55, 55, 55)
    height, width = labels.shape
    for region in regions:
        if region.area < max(80, width * height * 0.00035):
            continue
        if region.label_x < 0.02 or region.label_x > 0.98 or region.label_y < 0.02 or region.label_y > 0.98:
            continue
        x = int(region.label_x * width)
        y = int(region.label_y * height)
        text = str(region.color_id)
        scale = max(0.32, min(0.72, math.sqrt(region.area) / 155.0))
        thickness = 1
        size, _ = cv2.getTextSize(text, cv2.FONT_HERSHEY_SIMPLEX, scale, thickness)
        cv2.putText(canvas, text, (x - size[0] // 2, y + size[1] // 2), cv2.FONT_HERSHEY_SIMPLEX, scale, (105, 105, 105), thickness, cv2.LINE_AA)
    return canvas


def _write_svg(path: Path, regions: list[Region], palette: list[str], line_art: bool = False) -> None:
    lines = ['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1 1">']
    for region in regions:
        color = "#FAFAFA" if line_art else palette[region.color_id - 1]
        lines.append(
            f'<path id="region-{region.id}" d="{region.svg_path}" fill="{color}" '
            'stroke="#363636" stroke-width="0.0015" vector-effect="non-scaling-stroke"/>'
        )
        if line_art and 0.02 <= region.label_x <= 0.98 and 0.02 <= region.label_y <= 0.98:
            lines.append(
                f'<text x="{region.label_x:.6f}" y="{region.label_y:.6f}" '
                'text-anchor="middle" dominant-baseline="middle" font-size="0.022" '
                f'fill="#777777">{region.color_id}</text>'
            )
    lines.append("</svg>")
    path.write_text("\n".join(lines), encoding="utf-8")

def process_image(source_path: Path, output_dir: Path, options: ProcessorOptions) -> dict:
    image = _resize_for_processing(_read_image(source_path))
    lab = _preprocess(image, options.edge_sensitivity)
    color_count = max(3, min(options.max_colors, 24))
    labels, centers = _cluster_colors(lab, color_count)
    palette = _vivid_palette(centers, options.saturation_boost, options.contrast_boost)
    regions = _extract_regions(labels, options)
    if not regions:
        raise ValueError("NO_PLAYABLE_REGIONS")

    output_dir.mkdir(parents=True, exist_ok=True)
    colored = _render_colored(labels, palette)
    line_art = _render_line_art(labels, regions)
    cv2.imwrite(str(output_dir / "preview-colored.png"), colored)
    cv2.imwrite(str(output_dir / "preview-lineart.png"), line_art)
    _write_svg(output_dir / "artwork.svg", regions, palette)
    _write_svg(output_dir / "artwork-lineart.svg", regions, palette, line_art=True)

    palette_payload = [
        {"id": index + 1, "hex": color, "name": f"Color {index + 1}"}
        for index, color in enumerate(palette)
    ]
    (output_dir / "palette.json").write_text(
        json.dumps(palette_payload, indent=2), encoding="utf-8"
    )
    region_payload = [
        {
            "id": region.id,
            "colorId": region.color_id,
            "area": region.area,
            "svgPath": region.svg_path,
            "labelX": round(region.label_x, 6),
            "labelY": round(region.label_y, 6),
            "bounds": {
                "x": round(region.bounds[0], 6),
                "y": round(region.bounds[1], 6),
                "width": round(region.bounds[2], 6),
                "height": round(region.bounds[3], 6),
            },
        }
        for region in regions
    ]
    (output_dir / "regions.json").write_text(
        json.dumps(region_payload, indent=2), encoding="utf-8"
    )

    image_area = labels.shape[0] * labels.shape[1]
    playable_area = sum(region.area for region in regions)
    qa = {
        "regionCount": len(regions),
        "colorCount": len(palette),
        "playableCoverage": round(min(1.0, playable_area / image_area), 4),
        "averageRegionArea": round(playable_area / len(regions), 2),
    }
    manifest = {
        "schemaVersion": "2.0",
        "width": int(labels.shape[1]),
        "height": int(labels.shape[0]),
        "palettePath": str(output_dir / "palette.json"),
        "regionsPath": str(output_dir / "regions.json"),
        "svgPath": str(output_dir / "artwork.svg"),
        "lineArtSvgPath": str(output_dir / "artwork-lineart.svg"),
        "lineArtPreviewPath": str(output_dir / "preview-lineart.png"),
        "coloredPreviewPath": str(output_dir / "preview-colored.png"),
        "qa": qa,
    }
    manifest_path = output_dir / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return {
        "manifestPath": str(manifest_path),
        "regionCount": len(regions),
        "colorCount": len(palette),
        "qa": qa,
    }
