#!/usr/bin/env python3
from __future__ import annotations
import argparse
import json
import mimetypes
import time
import uuid
from pathlib import Path
from urllib.request import Request, urlopen
from urllib.error import HTTPError

CATEGORY_META = {
    "animales": ("Animales", "animals"),
    "flores": ("Flores y botánica", "botanical"),
    "paisajes": ("Paisajes", "landscape"),
    "vehiculos": ("Vehículos", "vehicles"),
    "espacio": ("Espacio", "space"),
    "comida": ("Comida", "food"),
    "arquitectura": ("Arquitectura", "architecture"),
    "mundo-submarino": ("Mundo submarino", "underwater"),
}
DIFFICULTIES = {"Kids": 0, "Normal": 2, "Master": 4}

def request_json(method: str, url: str, token: str | None = None, body=None):
    data = None if body is None else json.dumps(body).encode("utf-8")
    headers = {"Accept": "application/json"}
    if data is not None:
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = Request(url, data=data, headers=headers, method=method)
    try:
        with urlopen(req, timeout=90) as response:
            payload = response.read()
            return None if not payload else json.loads(payload.decode("utf-8"))
    except HTTPError as exc:
        detail = exc.read().decode("utf-8", "replace")
        raise RuntimeError(f"{method} {url} -> HTTP {exc.code}: {detail}") from exc

def upload_file(url: str, token: str, path: Path):
    boundary = "----AppColoreando" + uuid.uuid4().hex
    content_type = mimetypes.guess_type(path.name)[0] or "application/octet-stream"
    content = path.read_bytes()
    head = (
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="file"; filename="{path.name}"\r\n'
        f"Content-Type: {content_type}\r\n\r\n"
    ).encode()
    tail = f"\r\n--{boundary}--\r\n".encode()
    req = Request(
        url,
        data=head + content + tail,
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": f"multipart/form-data; boundary={boundary}",
            "Accept": "application/json",
        },
        method="POST",
    )
    try:
        with urlopen(req, timeout=120) as response:
            return json.loads(response.read().decode("utf-8"))
    except HTTPError as exc:
        detail = exc.read().decode("utf-8", "replace")
        raise RuntimeError(f"upload {path.name} -> HTTP {exc.code}: {detail}") from exc

def wait_for_job(api: str, token: str, job_id: str, timeout_seconds: int):
    deadline = time.time() + timeout_seconds
    while time.time() < deadline:
        job = request_json("GET", f"{api}/admin/content-generation/jobs/{job_id}", token)
        status = int(job["status"])
        if status == 3:
            return job
        if status in (7, 8):
            raise RuntimeError(f"job {job_id} failed: {job.get('errorCode')} {job.get('errorMessage')}")
        time.sleep(2)
    raise TimeoutError(f"job {job_id} did not reach PreviewReady in {timeout_seconds}s")

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--api", default="http://127.0.0.1:18080/api")
    parser.add_argument("--root", type=Path, default=Path("content-seed/public-domain"))
    parser.add_argument("--timeout", type=int, default=900)
    parser.add_argument("--difficulties", nargs="+", default=["Kids", "Normal", "Master"])
    args = parser.parse_args()

    root = args.root.resolve()
    manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
    auth = request_json("POST", f"{args.api}/auth/dev-login", body={})
    token = auth["accessToken"]

    categories = {x["slug"]: x for x in request_json("GET", f"{args.api}/catalog/categories", token)}
    resolved_categories = {}
    for slug, (name, preset_code) in CATEGORY_META.items():
        if slug not in categories:
            categories[slug] = request_json(
                "POST",
                f"{args.api}/admin/categories",
                token,
                {"name": name, "slug": slug, "isActive": True},
            )
        resolved_categories[slug] = categories[slug]

    presets = {
        p["code"]: p for p in request_json("GET", f"{args.api}/admin/content-generation/style-presets", token)
    }
    missing = [preset for _, preset in CATEGORY_META.values() if preset not in presets]
    if missing:
        raise RuntimeError(f"missing premium presets: {missing}")

    existing = request_json("GET", f"{args.api}/catalog/artworks?pageSize=500", token)
    existing_titles = {x["title"] for x in existing.get("items", [])}
    category_artworks = {slug: [] for slug in CATEGORY_META}
    for item in existing.get("items", []):
        for slug, category in resolved_categories.items():
            if item["categoryId"] == category["id"]:
                category_artworks[slug].append(item["id"])

    for item in manifest:
        slug = item["category"]
        category = resolved_categories[slug]
        preset_code = CATEGORY_META[slug][1]
        source_path = root / item["file"]
        if not source_path.exists():
            raise FileNotFoundError(source_path)

        asset = upload_file(
            f"{args.api}/admin/content-generation/source-assets",
            token,
            source_path,
        )

        for difficulty_name in args.difficulties:
            title = f"{item['title']} · {difficulty_name}"
            if title in existing_titles:
                print(f"SKIP {title}: already published", flush=True)
                continue

            job = request_json(
                "POST",
                f"{args.api}/admin/content-generation/jobs",
                token,
                {
                    "sourceAssetId": asset["id"],
                    "stylePresetId": presets[preset_code]["id"],
                    "difficulty": DIFFICULTIES[difficulty_name],
                },
            )
            job_id = job["id"]
            print(f"PROCESS {title} job={job_id}", flush=True)
            wait_for_job(args.api, token, job_id, args.timeout)

            note = {
                "note": f"Automated licensed seed import from {item['sourcePage']}"
            }
            request_json("POST", f"{args.api}/admin/content-generation/jobs/{job_id}/submit-review", token, note)
            request_json("POST", f"{args.api}/admin/content-generation/jobs/{job_id}/approve", token, note)

            published = request_json(
                "POST",
                f"{args.api}/admin/content-generation/jobs/{job_id}/publish",
                token,
                {
                    "title": title,
                    "categoryId": category["id"],
                    "countryCode": None,
                    "description": f"Source: {item['sourcePage']} | Author: {item['author']} | License: {item['license']}",
                    "licenseType": item["license"],
                    "licenseReference": item["sourcePage"],
                },
            )
            category_artworks[slug].append(published["id"])
            existing_titles.add(title)
            print(f"PUBLISHED {title} regions={published['regionCount']}", flush=True)

    collections = {x["slug"]: x for x in request_json("GET", f"{args.api}/catalog/collections", token)}
    for slug, (name, _) in CATEGORY_META.items():
        collection_slug = f"all-ages-{slug}"
        body = {
            "name": f"{name} · Todas las edades",
            "slug": collection_slug,
            "description": "Colección CC0/Public Domain con variantes Kids, Normal y Master.",
            "countryCode": None,
            "isActive": True,
            "artworkIds": sorted(set(category_artworks[slug])),
        }
        if collection_slug in collections:
            request_json("PUT", f"{args.api}/admin/collections/{collections[collection_slug]['id']}", token, body)
        else:
            request_json("POST", f"{args.api}/admin/collections", token, body)

    final_catalog = request_json("GET", f"{args.api}/catalog/artworks?pageSize=500", token)
    print(json.dumps({
        "manifestImages": len(manifest),
        "publishedCatalogCount": final_catalog.get("total", len(final_catalog.get("items", []))),
        "difficulties": args.difficulties,
        "categories": list(CATEGORY_META.keys()),
    }, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
