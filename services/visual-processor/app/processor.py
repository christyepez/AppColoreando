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
    difficulty: str = "Normal"
    style_code: str = "natural"


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
    semantic_tag: str = "unclassified"
    semantic_role: str = "detail"
    label_radius: float = 0.0
    label_min_zoom: float = 1.0
    label_font_size: float = 0.022
    label_visible_at_base: bool = True


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


def _opencv_lab_to_cie(value: np.ndarray) -> np.ndarray:
    result = value.astype(np.float32).copy()
    result[..., 0] = result[..., 0] * (100.0 / 255.0)
    result[..., 1:] -= 128.0
    return result


def _delta_e(first: np.ndarray, second: np.ndarray) -> float:
    a = _opencv_lab_to_cie(first.reshape((1, 3)))[0]
    b = _opencv_lab_to_cie(second.reshape((1, 3)))[0]
    return float(np.linalg.norm(a - b))


def _difficulty_delta_e(difficulty: str) -> float:
    return {"kids": 14.0, "easy": 11.0, "normal": 8.0, "detailed": 5.0, "master": 3.0}.get(difficulty.lower(), 8.0)


def _merge_similar_clusters(labels: np.ndarray, centers: np.ndarray, difficulty: str) -> tuple[np.ndarray, np.ndarray]:
    counts = np.bincount(labels.reshape(-1), minlength=len(centers))
    groups: list[list[int]] = []
    for cluster_id in sorted(range(len(centers)), key=lambda idx: (-int(counts[idx]), idx)):
        target = None
        best = float("inf")
        for group_id, members in enumerate(groups):
            distance = min(_delta_e(centers[cluster_id], centers[member]) for member in members)
            if distance <= _difficulty_delta_e(difficulty) and distance < best:
                target, best = group_id, distance
        if target is None:
            groups.append([cluster_id])
        else:
            groups[target].append(cluster_id)

    remap = np.zeros(len(centers), dtype=np.int32)
    merged_centers: list[np.ndarray] = []
    for new_id, members in enumerate(groups):
        weights = counts[members].astype(np.float64)
        merged = np.average(centers[members].astype(np.float64), axis=0, weights=weights)
        merged_centers.append(np.rint(merged).astype(np.uint8))
        for old_id in members:
            remap[old_id] = new_id
    return remap[labels], np.asarray(merged_centers, dtype=np.uint8)


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


def _hex_from_bgr(value: np.ndarray) -> str:
    blue, green, red = [int(np.clip(x, 0, 255)) for x in value]
    return f"#{red:02X}{green:02X}{blue:02X}"


def _apply_style_palette(palette: list[str], style_code: str) -> list[str]:
    style = style_code.lower()
    result: list[str] = []
    for color in palette:
        bgr = np.uint8([[[_hex_to_bgr(color)[0], _hex_to_bgr(color)[1], _hex_to_bgr(color)[2]]]])
        hsv = cv2.cvtColor(bgr, cv2.COLOR_BGR2HSV)[0, 0].astype(np.float32)
        if style == "aura":
            hsv[1] = np.clip(hsv[1] * 1.18, 0, 255); hsv[2] = np.clip(hsv[2] * 1.05 + 4, 0, 255)
        elif style == "tesoro":
            hsv[1] = np.clip(hsv[1] * 1.08, 0, 255); hsv[2] = np.clip((hsv[2] - 128) * 1.14 + 128, 22, 252)
        elif style == "postal-viva":
            vivid = cv2.cvtColor(np.uint8([[hsv]]), cv2.COLOR_HSV2BGR)[0, 0].astype(np.float32)
            vivid[0] *= 0.92; vivid[2] = min(255.0, vivid[2] * 1.08 + 4); result.append(_hex_from_bgr(vivid)); continue
        elif style == "lumina":
            hsv[1] = np.clip(hsv[1] * 1.10, 0, 255); hsv[2] = np.clip(hsv[2] * 1.16 + 10, 0, 255)
        elif style == "eclipse":
            hsv[1] = np.clip(hsv[1] * 1.24, 0, 255); hsv[2] = np.clip(hsv[2] * 0.62, 18, 190)
        result.append(_hex_from_bgr(cv2.cvtColor(np.uint8([[hsv]]), cv2.COLOR_HSV2BGR)[0, 0]))
    return result


def _friendly_color_name(value: str) -> str:
    blue, green, red = _hex_to_bgr(value)
    hsv = cv2.cvtColor(np.uint8([[[blue, green, red]]]), cv2.COLOR_BGR2HSV)[0, 0]
    hue = float(hsv[0]) * 2.0
    saturation = float(hsv[1]) / 255.0
    brightness = float(hsv[2]) / 255.0
    if saturation < 0.10:
        if brightness > 0.90:
            return "Blanco Nube"
        if brightness < 0.24:
            return "Carbon"
        return "Gris Perla"
    if hue < 15 or hue >= 345:
        return "Rojo Coral"
    if hue < 30:
        return "Naranja Mandarina"
    if hue < 46:
        return "Ambar Dorado"
    if hue < 68:
        return "Amarillo Sol"
    if hue < 105:
        return "Verde Lima"
    if hue < 155:
        return "Verde Bosque"
    if hue < 190:
        return "Turquesa"
    if hue < 235:
        return "Azul Laguna"
    if hue < 270:
        return "Azul Noche"
    if hue < 305:
        return "Violeta"
    return "Magenta"


def _semantic_for_region(region: Region, image_shape: tuple[int, int], color_hex: str) -> tuple[str, str]:
    height, width = image_shape
    area_ratio = region.area / max(1, height * width)
    x, y, w, h = region.bounds
    aspect = w / max(h, 1e-6)
    blue, green, red = _hex_to_bgr(color_hex)
    hsv = cv2.cvtColor(np.uint8([[[blue, green, red]]]), cv2.COLOR_BGR2HSV)[0, 0]
    hue = float(hsv[0]) * 2.0
    saturation = float(hsv[1]) / 255.0
    brightness = float(hsv[2]) / 255.0
    touches_border = x < 0.01 or y < 0.01 or x + w > 0.99 or y + h > 0.99
    if brightness < 0.28 and area_ratio < 0.025 and 0.45 <= aspect <= 2.2:
        return "eye-candidate", "subject-detail"
    if (hue < 70 or hue >= 345) and saturation > 0.45 and area_ratio < 0.12 and aspect > 1.35 and region.label_x > 0.52:
        return "beak-candidate", "subject-accent"
    if 175 <= hue <= 250 and saturation > 0.18:
        return ("water", "environment") if region.label_y >= 0.52 else ("sky", "environment")
    if 70 <= hue <= 170 and saturation > 0.18:
        return "foliage", "environment"
    if touches_border and area_ratio > 0.18:
        return "background", "background"
    if area_ratio > 0.06:
        return "subject", "subject"
    if brightness < 0.35:
        return "dark-detail", "subject-detail"
    return "detail", "detail"


def _assign_semantics(regions: list[Region], image_shape: tuple[int, int], palette: list[str]) -> None:
    for region in regions:
        tag, role = _semantic_for_region(region, image_shape, palette[region.color_id - 1])
        region.semantic_tag = tag
        region.semantic_role = role


def _edge_aware_smooth_labels(labels: np.ndarray, lab: np.ndarray, sensitivity: float) -> np.ndarray:
    l_channel = lab[:, :, 0]
    sensitivity = max(0.0, min(float(sensitivity), 1.0))
    low = int(20 + (1.0 - sensitivity) * 35)
    high = int(65 + (1.0 - sensitivity) * 75)
    edges = cv2.Canny(l_channel, low, high)
    barrier = cv2.dilate(edges, np.ones((3, 3), np.uint8), iterations=1)
    median = cv2.medianBlur(labels.astype(np.uint8), 3).astype(np.int32)
    result = labels.astype(np.int32).copy()
    result[barrier == 0] = median[barrier == 0]
    return result


def _difficulty_area_factor(difficulty: str) -> float:
    return {"kids": 0.62, "easy": 0.36, "normal": 0.16, "detailed": 0.07, "master": 0.03}.get(difficulty.lower(), 0.16)


def _micro_region_threshold(shape: tuple[int, int], options: ProcessorOptions) -> int:
    image_area = int(shape[0] * shape[1])
    baseline = image_area / max(options.target_regions, 8)
    return max(12, int(baseline * _difficulty_area_factor(options.difficulty)))


def _merge_micro_regions(labels: np.ndarray, centers: np.ndarray, options: ProcessorOptions) -> np.ndarray:
    result = labels.astype(np.int32).copy()
    threshold = _micro_region_threshold(result.shape, options)
    kernel = np.ones((3, 3), np.uint8)
    for _ in range(2):
        changed = False
        for color_id in range(len(centers)):
            mask = np.where(result == color_id, 255, 0).astype(np.uint8)
            count, components, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
            for component_id in range(1, count):
                area = int(stats[component_id, cv2.CC_STAT_AREA])
                if area >= threshold:
                    continue
                component_mask = components == component_id
                border = cv2.dilate(component_mask.astype(np.uint8), kernel, iterations=1).astype(bool)
                border &= ~component_mask
                neighbours = result[border]
                neighbours = neighbours[neighbours != color_id]
                if neighbours.size == 0:
                    continue
                candidates, counts = np.unique(neighbours, return_counts=True)
                best_id = None
                best_score = float("inf")
                for candidate, boundary_count in zip(candidates, counts):
                    distance = _delta_e(centers[color_id], centers[int(candidate)])
                    score = distance / max(1.0, float(boundary_count))
                    if score < best_score:
                        best_score = score
                        best_id = int(candidate)
                if best_id is not None:
                    result[component_mask] = best_id
                    changed = True
        if not changed:
            break
    return result


def _minimum_palette_delta_e(centers: np.ndarray) -> float:
    if len(centers) < 2:
        return 0.0
    values = [_delta_e(centers[i], centers[j]) for i in range(len(centers)) for j in range(i + 1, len(centers))]
    return round(min(values), 2) if values else 0.0


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

def _label_anchor(mask: np.ndarray, width: int, height: int) -> tuple[float, float, float]:
    distance = cv2.distanceTransform(mask, cv2.DIST_L2, 5)
    _, max_distance, _, max_location = cv2.minMaxLoc(distance)
    radius = float(max_distance) / max(1, min(width, height))
    return max_location[0] / width, max_location[1] / height, radius


def _configure_label_readability(region: Region) -> None:
    radius = region.label_radius
    region.label_visible_at_base = radius >= 0.012
    region.label_min_zoom = 1.0 if radius >= 0.018 else 1.5 if radius >= 0.012 else 2.5 if radius >= 0.007 else 4.0
    region.label_font_size = float(np.clip(radius * 1.15, 0.012, 0.028))


def _bounds(contour: np.ndarray, width: int, height: int) -> tuple[float, float, float, float]:
    x, y, w, h = cv2.boundingRect(contour)
    return x / width, y / height, w / width, h / height


def _extract_regions(labels: np.ndarray, options: ProcessorOptions) -> list[Region]:
    height, width = labels.shape
    image_area = width * height
    minimum_area = max(10, int(_micro_region_threshold(labels.shape, options) * 0.35))
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
            label_x, label_y, label_radius = _label_anchor(component_mask, width, height)
            region = Region(next_id, color_id + 1, area, contour,
                            label_x, label_y, _bounds(contour, width, height), path)
            region.label_radius = label_radius
            _configure_label_readability(region)
            regions.append(region)
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
        if region.area < max(80, width * height * 0.00035) or not region.label_visible_at_base:
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


def _square_preview(image: np.ndarray, size: int, margin_ratio: float = 0.04) -> np.ndarray:
    height, width = image.shape[:2]
    canvas = np.full((size, size, 3), 250, dtype=np.uint8)
    usable = max(1, int(size * (1.0 - 2.0 * margin_ratio)))
    scale = min(usable / max(1, width), usable / max(1, height))
    target_w = max(1, min(size, round(width * scale)))
    target_h = max(1, min(size, round(height * scale)))
    interpolation = cv2.INTER_AREA if scale <= 1.0 else cv2.INTER_CUBIC
    resized = cv2.resize(image, (target_w, target_h), interpolation=interpolation)
    x = (size - target_w) // 2
    y = (size - target_h) // 2
    canvas[y:y + target_h, x:x + target_w] = resized
    return canvas


def _style_effect_metadata(style_code: str) -> dict:
    style = style_code.lower()
    names = {
        "aura": "vivid-glow", "tesoro": "premium-detail", "revela": "partial-reveal",
        "postal-viva": "warm-editorial", "lumina": "highlight-boost", "eclipse": "dark-vivid",
    }
    return {"styleCode": style, "treatment": names.get(style, "natural")}


def _render_style_preview(colored: np.ndarray, line_art: np.ndarray, style_code: str) -> np.ndarray:
    style = style_code.lower()
    if style == "aura":
        blur = cv2.GaussianBlur(colored, (0, 0), 5)
        return cv2.addWeighted(colored, 1.08, blur, 0.18, 0)
    if style == "tesoro":
        blur = cv2.GaussianBlur(colored, (0, 0), 2)
        return cv2.addWeighted(colored, 1.35, blur, -0.35, 0)
    if style == "revela":
        yy, xx = np.indices(colored.shape[:2])
        mask = (xx + yy) < int((colored.shape[0] + colored.shape[1]) * 0.72)
        result = colored.copy(); result[mask] = line_art[mask]; return result
    if style == "postal-viva":
        overlay = np.full_like(colored, (215, 235, 252))
        return cv2.addWeighted(colored, 0.90, overlay, 0.10, 0)
    if style == "lumina":
        return cv2.convertScaleAbs(colored, alpha=1.08, beta=18)
    if style == "eclipse":
        hsv = cv2.cvtColor(colored, cv2.COLOR_BGR2HSV).astype(np.float32)
        hsv[:, :, 1] = np.clip(hsv[:, :, 1] * 1.20, 0, 255)
        hsv[:, :, 2] = np.clip(hsv[:, :, 2] * 0.58, 12, 190)
        return cv2.cvtColor(hsv.astype(np.uint8), cv2.COLOR_HSV2BGR)
    return colored.copy()


def _write_webp(path: Path, image: np.ndarray, quality: int = 92) -> None:
    if not cv2.imwrite(str(path), image, [cv2.IMWRITE_WEBP_QUALITY, quality]):
        raise ValueError("PREVIEW_WRITE_FAILED")


def _write_svg(path: Path, regions: list[Region], palette: list[str], line_art: bool = False) -> None:
    lines = ['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1 1">']
    for region in regions:
        color = "#FAFAFA" if line_art else palette[region.color_id - 1]
        lines.append(
            f'<path id="region-{region.id}" d="{region.svg_path}" fill="{color}" '
            'stroke="#363636" stroke-width="0.0015" vector-effect="non-scaling-stroke"/>'
        )
        if line_art and region.label_visible_at_base and 0.02 <= region.label_x <= 0.98 and 0.02 <= region.label_y <= 0.98:
            lines.append(
                f'<text x="{region.label_x:.6f}" y="{region.label_y:.6f}" '
                f'text-anchor="middle" dominant-baseline="middle" font-size="{region.label_font_size:.6f}" '
                f'fill="#777777">{region.color_id}</text>'
            )
    lines.append("</svg>")
    path.write_text("\n".join(lines), encoding="utf-8")

def process_image(source_path: Path, output_dir: Path, options: ProcessorOptions) -> dict:
    image = _resize_for_processing(_read_image(source_path))
    lab = _preprocess(image, options.edge_sensitivity)
    color_count = max(3, min(options.max_colors, 24))
    labels, centers = _cluster_colors(lab, color_count)
    labels, centers = _merge_similar_clusters(labels, centers, options.difficulty)
    labels = _edge_aware_smooth_labels(labels, lab, options.edge_sensitivity)
    labels = _merge_micro_regions(labels, centers, options)
    palette = _vivid_palette(centers, options.saturation_boost, options.contrast_boost)
    palette = _apply_style_palette(palette, options.style_code)
    regions = _extract_regions(labels, options)
    _assign_semantics(regions, labels.shape, palette)
    if not regions:
        raise ValueError("NO_PLAYABLE_REGIONS")

    output_dir.mkdir(parents=True, exist_ok=True)
    colored = _render_colored(labels, palette)
    line_art = _render_line_art(labels, regions)
    special_preview = _render_style_preview(colored, line_art, options.style_code)
    cv2.imwrite(str(output_dir / "preview-colored.png"), colored)
    cv2.imwrite(str(output_dir / "preview-lineart.png"), line_art)
    _write_webp(output_dir / "thumbnail.webp", _square_preview(colored, 512), 90)
    _write_webp(output_dir / "catalog-preview.webp", _square_preview(colored, 768), 92)
    _write_webp(output_dir / "lineart-preview.webp", _square_preview(line_art, 1024), 94)
    _write_webp(output_dir / "special-preview.webp", _square_preview(special_preview, 768), 92)
    _write_svg(output_dir / "artwork.svg", regions, palette)
    _write_svg(output_dir / "artwork-lineart.svg", regions, palette, line_art=True)

    palette_payload = [
        {"id": index + 1, "hex": color, "name": _friendly_color_name(color)}
        for index, color in enumerate(palette)
    ]
    (output_dir / "palette.json").write_text(
        json.dumps(palette_payload, indent=2), encoding="utf-8"
    )
    region_payload = [
        {
            "id": region.id,
            "colorId": region.color_id,
            "semanticTag": region.semantic_tag,
            "semanticRole": region.semantic_role,
            "area": region.area,
            "svgPath": region.svg_path,
            "labelX": round(region.label_x, 6),
            "labelY": round(region.label_y, 6),
            "labelRadius": round(region.label_radius, 6),
            "labelMinZoom": round(region.label_min_zoom, 2),
            "labelFontSize": round(region.label_font_size, 6),
            "labelVisibleAtBase": region.label_visible_at_base,
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
    semantic_counts = {
        tag: sum(1 for region in regions if region.semantic_tag == tag)
        for tag in sorted({region.semantic_tag for region in regions})
    }
    qa = {
        "regionCount": len(regions),
        "colorCount": len(palette),
        "playableCoverage": round(min(1.0, playable_area / image_area), 4),
        "averageRegionArea": round(playable_area / len(regions), 2),
        "minimumPaletteDeltaE": _minimum_palette_delta_e(centers),
        "microRegionThreshold": _micro_region_threshold(labels.shape, options),
        "difficulty": options.difficulty,
        "semanticCounts": semantic_counts,
        "labelsVisibleAtBase": sum(1 for region in regions if region.label_visible_at_base),
        "labelsRequiringZoom": sum(1 for region in regions if not region.label_visible_at_base),
    }
    playable_bundle = {
        "schemaVersion": "2.2",
        "width": int(labels.shape[1]),
        "height": int(labels.shape[0]),
        "difficulty": options.difficulty,
        "styleCode": options.style_code,
        "palette": palette_payload,
        "regions": region_payload,
        "adjustments": [],
        "effect": _style_effect_metadata(options.style_code),
    }
    bundle_path = output_dir / "bundle.json"
    bundle_path.write_text(json.dumps(playable_bundle, indent=2), encoding="utf-8")

    manifest = {
        "schemaVersion": "2.0",
        "width": int(labels.shape[1]),
        "height": int(labels.shape[0]),
        "bundlePath": str(bundle_path),
        "palettePath": str(output_dir / "palette.json"),
        "regionsPath": str(output_dir / "regions.json"),
        "svgPath": str(output_dir / "artwork.svg"),
        "lineArtSvgPath": str(output_dir / "artwork-lineart.svg"),
        "lineArtPreviewPath": str(output_dir / "preview-lineart.png"),
        "coloredPreviewPath": str(output_dir / "preview-colored.png"),
        "thumbnailPath": str(output_dir / "thumbnail.webp"),
        "catalogPreviewPath": str(output_dir / "catalog-preview.webp"),
        "lineArtWebpPath": str(output_dir / "lineart-preview.webp"),
        "specialPreviewPath": str(output_dir / "special-preview.webp"),
        "effect": _style_effect_metadata(options.style_code),
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


def _variant_options(base: ProcessorOptions, difficulty: str) -> ProcessorOptions:
    code = difficulty.lower()
    region_factor = {"kids": 0.45, "easy": 0.70, "normal": 1.0, "detailed": 1.55, "master": 2.2}[code]
    color_adjust = {"kids": -3, "easy": -1, "normal": 0, "detailed": 3, "master": 6}[code]
    simplify = {
        "kids": max(base.simplification_tolerance, 0.68),
        "easy": max(base.simplification_tolerance, 0.52),
        "normal": base.simplification_tolerance,
        "detailed": min(base.simplification_tolerance, 0.32),
        "master": min(base.simplification_tolerance, 0.22),
    }[code]
    return ProcessorOptions(
        target_regions=max(12, min(360, round(base.target_regions * region_factor))),
        max_colors=max(4, min(24, base.max_colors + color_adjust)),
        simplification_tolerance=simplify,
        edge_sensitivity=min(1.0, base.edge_sensitivity + (0.08 if code in {"detailed", "master"} else 0.0)),
        curve_smoothness=base.curve_smoothness,
        saturation_boost=base.saturation_boost,
        contrast_boost=base.contrast_boost,
        difficulty=difficulty,
        style_code=base.style_code,
    )


def process_variants(
    source_path: Path,
    output_dir: Path,
    base_options: ProcessorOptions,
    primary_difficulty: str = "Normal",
) -> dict:
    difficulties = ("Kids", "Easy", "Normal", "Detailed", "Master")
    output_dir.mkdir(parents=True, exist_ok=True)
    variants: list[dict] = []

    for difficulty in difficulties:
        variant_dir = output_dir / "variants" / difficulty.lower()
        options = _variant_options(base_options, difficulty)
        result = process_image(source_path, variant_dir, options)
        variants.append({
            "code": difficulty.lower(),
            "difficulty": difficulty,
            "manifestPath": result["manifestPath"],
            "regionCount": result["regionCount"],
            "colorCount": result["colorCount"],
            "qa": result["qa"],
            "isPrimary": difficulty.lower() == primary_difficulty.lower(),
        })

    primary = next(
        (item for item in variants if item["isPrimary"]),
        next(item for item in variants if item["difficulty"] == "Normal"),
    )
    pack_manifest = {
        "schemaVersion": "2.1",
        "type": "variant-pack",
        "sourcePath": str(source_path),
        "primaryDifficulty": primary["difficulty"],
        "primaryManifestPath": primary["manifestPath"],
        "variantCount": len(variants),
        "variants": variants,
    }
    manifest_path = output_dir / "manifest.json"
    manifest_path.write_text(
        json.dumps(pack_manifest, indent=2), encoding="utf-8"
    )
    return {
        "manifestPath": str(manifest_path),
        "variantCount": len(variants),
        "primaryManifestPath": primary["manifestPath"],
        "variants": variants,
    }
