#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import mimetypes
import time
import uuid
from pathlib import Path
from urllib.error import HTTPError
from urllib.parse import quote, urlencode
from urllib.request import Request, urlopen


STATUS_PREVIEW_READY = 3
STATUS_FAILED = 7
STATUS_CANCELLED = 8


def json_request(base: str, method: str, path: str, token: str | None = None, payload=None):
    headers = {"Accept": "application/json"}
    body = None
    if payload is not None:
        body = json.dumps(payload).encode("utf-8")
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = Request(base.rstrip("/") + path, data=body, headers=headers, method=method)
    try:
        with urlopen(request, timeout=60) as response:
            raw = response.read()
            return json.loads(raw.decode("utf-8")) if raw else None
    except HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"{method} {path} -> HTTP {exc.code}: {detail}") from exc


def multipart_upload(base: str, path: str, file_path: Path, token: str):
    boundary = "----AppColoreando" + uuid.uuid4().hex
    content_type = mimetypes.guess_type(file_path.name)[0] or "application/octet-stream"
    prefix = (
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="file"; filename="{file_path.name}"\r\n'
        f"Content-Type: {content_type}\r\n\r\n"
    ).encode("utf-8")
    suffix = f"\r\n--{boundary}--\r\n".encode("utf-8")
    body = prefix + file_path.read_bytes() + suffix
    request = Request(
        base.rstrip("/") + path,
        data=body,
        method="POST",
        headers={
            "Accept": "application/json",
            "Authorization": f"Bearer {token}",
            "Content-Type": f"multipart/form-data; boundary={boundary}",
        },
    )
    try:
        with urlopen(request, timeout=120) as response:
            return json.loads(response.read().decode("utf-8"))
    except HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"UPLOAD {file_path.name} -> HTTP {exc.code}: {detail}") from exc


def commons_urls(filename: str) -> tuple[str, str]:
    encoded = quote(filename)
    page = "https://commons.wikimedia.org/wiki/File:" + quote(filename.replace(" ", "_"))
    download = "https://commons.wikimedia.org/wiki/Special:Redirect/file/" + encoded + "?width=1600"
    return page, download


def download_if_missing(item: dict, root: Path) -> Path:
    category = item["categorySlug"]
    ext = Path(item["commonsFile"]).suffix.lower() or ".jpg"
    index = 1 + len(list((root / category).glob(f"{category}-*"))) if (root / category).exists() else 1
    # Prefer an already-downloaded source matching manifest order/title.
    existing = sorted((root / category).glob(f"{category}-*{ext}")) if (root / category).exists() else []
    if existing:
        title_token = item["title"].lower().replace(" ", "")
        for candidate in existing:
            sidecar = candidate.with_suffix(candidate.suffix + ".title")
            if sidecar.exists() and sidecar.read_text(encoding="utf-8").strip() == item["title"]:
                return candidate
        # Manifest is ordered two-per-category and local starter downloads use -01/-02.
        same_category_items = [x for x in MANIFEST_ITEMS if x["categorySlug"] == category]
        local_index = same_category_items.index(item) + 1
        expected = root / category / f"{category}-{local_index:02d}{ext}"
        if expected.exists():
            return expected

    folder = root / category
    folder.mkdir(parents=True, exist_ok=True)
    same_category_items = [x for x in MANIFEST_ITEMS if x["categorySlug"] == category]
    local_index = same_category_items.index(item) + 1
    target = folder / f"{category}-{local_index:02d}{ext}"
    _, download = commons_urls(item["commonsFile"])
    request = Request(download, headers={"User-Agent": "AppColoreandoSeed/1.0 local-development"})
    with urlopen(request, timeout=120) as response:
        target.write_bytes(response.read())
    target.with_suffix(target.suffix + ".title").write_text(item["title"], encoding="utf-8")
    return target


def ensure_category(base: str, token: str, item: dict, cache: dict[str, dict]) -> dict:
    slug = item["categorySlug"]
    if slug in cache:
        return cache[slug]
    result = json_request(base, "POST", "/api/admin/categories", token, {
        "name": item["categoryName"],
        "slug": slug,
        "isActive": True,
    })
    cache[slug] = result
    return result


def ensure_license(base: str, token: str, code: str, cache: dict[str, dict]) -> dict:
    if code in cache:
        return cache[code]
    name = "CC0 1.0" if code == "CC0" else "Public Domain"
    rights = (
        "CC0: may be copied, modified and distributed, including commercially, without asking permission."
        if code == "CC0"
        else "Public-domain source; reusable without copyright restriction. Source attribution retained for provenance."
    )
    result = json_request(base, "POST", "/api/admin/licenses", token, {
        "brandId": None,
        "name": name,
        "rightsSummary": rights,
        "validFromUtc": "2000-01-01T00:00:00Z",
        "validToUtc": None,
        "allowsStoreDistribution": True,
        "allowsKidsMode": True,
        "allowsOfflineDownload": True,
        "featureEnabled": True,
        "territories": [],
    })
    cache[code] = result
    return result


def status_number(value):
    if isinstance(value, int):
        return value
    names = {
        "Pending": 0, "Queued": 1, "Running": 2, "PreviewReady": 3,
        "NeedsReview": 4, "Approved": 5, "Published": 6, "Failed": 7, "Cancelled": 8,
    }
    return names.get(str(value), -1)


def wait_for_preview(base: str, token: str, job_id: str, timeout_minutes: int):
    deadline = time.time() + timeout_minutes * 60
    previous = None
    while time.time() < deadline:
        job = json_request(base, "GET", f"/api/admin/content-generation/jobs/{job_id}", token)
        current = status_number(job.get("status"))
        if current != previous:
            print(f"  job {job_id}: status={job.get('status')}", flush=True)
            previous = current
        if current == STATUS_PREVIEW_READY:
            return job
        if current in (STATUS_FAILED, STATUS_CANCELLED):
            raise RuntimeError(
                f"generation failed: {job.get('errorCode')} {job.get('errorMessage')}"
            )
        time.sleep(4)
    raise TimeoutError(f"job {job_id} did not reach PreviewReady in {timeout_minutes} minutes")


def update_artwork_license(base: str, token: str, artwork: dict, item: dict, license_record: dict):
    source_page, _ = commons_urls(item["commonsFile"])
    payload = {
        "title": artwork["title"],
        "description": artwork.get("description"),
        "categoryId": artwork["categoryId"],
        "countryCode": artwork.get("countryCode"),
        "brandId": artwork.get("brandId"),
        "licenseAgreementId": license_record["id"],
        "licenseType": item["licenseCode"],
        "licenseReference": source_page,
        "thumbnailUrl": artwork.get("thumbnailUrl"),
        "assetUrl": artwork.get("assetUrl"),
        "bundleChecksum": artwork.get("bundleChecksum"),
        "difficulty": artwork["difficulty"],
        "regionCount": artwork["regionCount"],
        "publishingStatus": artwork["publishingStatus"],
        "scheduledPublishAtUtc": artwork.get("scheduledPublishAtUtc"),
        "assets": [
            {
                "kind": asset["kind"],
                "uri": asset["uri"],
                "contentType": asset["contentType"],
                "checksum": asset.get("checksum"),
                "sizeBytes": asset["sizeBytes"],
                "isPrimary": asset["isPrimary"],
            }
            for asset in artwork.get("assets", [])
        ],
    }
    return json_request(base, "PUT", f"/api/admin/artworks/{artwork['id']}", token, payload)


def ensure_collection(base: str, token: str, item: dict, artwork_ids: list[str], existing_collections: dict[str, dict]):
    slug = item["collectionSlug"]
    if slug in existing_collections:
        collection = existing_collections[slug]
        page = json_request(
            base, "GET",
            f"/api/catalog/artworks?collectionId={collection['id']}&pageSize=100",
            token,
        )
        ids = {x["id"] for x in page.get("items", [])}
        ids.update(artwork_ids)
        result = json_request(base, "PUT", f"/api/admin/collections/{collection['id']}", token, {
            "name": item["collectionName"],
            "slug": slug,
            "description": "Colección inicial AppColoreando con fuentes CC0/Public Domain verificadas.",
            "countryCode": None,
            "isActive": True,
            "artworkIds": sorted(ids),
        })
    else:
        result = json_request(base, "POST", "/api/admin/collections", token, {
            "name": item["collectionName"],
            "slug": slug,
            "description": "Colección inicial AppColoreando con fuentes CC0/Public Domain verificadas.",
            "countryCode": None,
            "isActive": True,
            "artworkIds": artwork_ids,
        })
    existing_collections[slug] = result
    return result


def main():
    parser = argparse.ArgumentParser(description="Import curated public-domain/CC0 starter content into AppColoreando.")
    parser.add_argument("--api", default="http://127.0.0.1:18080")
    parser.add_argument("--manifest", default="tools/content-seed/public-domain-seed.json")
    parser.add_argument("--source-root", default="content-seed/public-domain")
    parser.add_argument("--timeout-minutes", type=int, default=25)
    parser.add_argument("--limit", type=int, default=0, help="0 imports all items.")
    args = parser.parse_args()

    global MANIFEST_ITEMS
    manifest = json.loads(Path(args.manifest).read_text(encoding="utf-8"))
    MANIFEST_ITEMS = manifest["items"]
    items = MANIFEST_ITEMS[:args.limit] if args.limit else MANIFEST_ITEMS

    auth = json_request(args.api, "POST", "/api/auth/dev-login", payload={})
    token = auth["accessToken"]
    print(f"DEV login: {auth['user']['email']}", flush=True)

    categories = {
        x["slug"]: x for x in json_request(args.api, "GET", "/api/catalog/categories", token)
    }
    presets = {
        x["code"]: x for x in json_request(args.api, "GET", "/api/admin/content-generation/style-presets", token)
    }
    licenses = {
        ("CC0" if "CC0" in x["name"].upper() else "PublicDomain"): x
        for x in json_request(args.api, "GET", "/api/admin/licenses", token)
        if "CC0" in x["name"].upper() or "PUBLIC DOMAIN" in x["name"].upper()
    }
    collections = {
        x["slug"]: x for x in json_request(args.api, "GET", "/api/catalog/collections", token)
    }
    existing_page = json_request(args.api, "GET", "/api/catalog/artworks?pageSize=100", token)
    existing_titles = {x["title"]: x for x in existing_page.get("items", [])}

    source_root = Path(args.source_root)
    published_by_collection: dict[str, list[str]] = {}

    for index, item in enumerate(items, 1):
        print(f"[{index}/{len(items)}] {item['categoryName']} / {item['title']}", flush=True)
        if item["title"] in existing_titles:
            artwork = existing_titles[item["title"]]
            published_by_collection.setdefault(item["collectionSlug"], []).append(artwork["id"])
            print("  already published; reusing existing artwork", flush=True)
            continue

        category = categories.get(item["categorySlug"]) or ensure_category(
            args.api, token, item, categories
        )
        license_record = licenses.get(item["licenseCode"]) or ensure_license(
            args.api, token, item["licenseCode"], licenses
        )
        preset = presets.get(item["presetCode"])
        if preset is None:
            raise RuntimeError(f"missing preset {item['presetCode']} - deploy S65 first")

        local_file = download_if_missing(item, source_root)
        source = multipart_upload(
            args.api, "/api/admin/content-generation/source-assets", local_file, token
        )
        job = json_request(args.api, "POST", "/api/admin/content-generation/jobs", token, {
            "sourceAssetId": source["id"],
            "stylePresetId": preset["id"],
            "difficulty": item["difficulty"],
        })
        wait_for_preview(args.api, token, job["id"], args.timeout_minutes)

        note = {
            "note": (
                f"Starter collection import. Source: Wikimedia Commons; "
                f"author={item['author']}; license={item['licenseCode']}."
            )
        }
        json_request(args.api, "POST", f"/api/admin/content-generation/jobs/{job['id']}/submit-review", token, note)
        json_request(args.api, "POST", f"/api/admin/content-generation/jobs/{job['id']}/approve", token, note)

        source_page, _ = commons_urls(item["commonsFile"])
        artwork = json_request(args.api, "POST", f"/api/admin/content-generation/jobs/{job['id']}/publish", token, {
            "title": item["title"],
            "categoryId": category["id"],
            "countryCode": None,
            "description": (
                f"Fuente: Wikimedia Commons. Autor: {item['author']}. "
                f"Licencia: {item['licenseCode']}. {source_page}"
            ),
        })
        artwork = update_artwork_license(args.api, token, artwork, item, license_record)
        published_by_collection.setdefault(item["collectionSlug"], []).append(artwork["id"])
        existing_titles[item["title"]] = artwork
        print(f"  published: artwork={artwork['id']} regions={artwork['regionCount']}", flush=True)

    by_slug = {item["collectionSlug"]: item for item in items}
    for slug, ids in published_by_collection.items():
        ensure_collection(args.api, token, by_slug[slug], ids, collections)
        print(f"collection {slug}: {len(ids)} imported artwork(s)", flush=True)

    print(f"DONE: {sum(len(v) for v in published_by_collection.values())} artwork(s) linked to collections.", flush=True)


MANIFEST_ITEMS = []

if __name__ == "__main__":
    main()
