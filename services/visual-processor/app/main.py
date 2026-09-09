from pathlib import Path
from uuid import UUID
import json

from fastapi import FastAPI
from pydantic import BaseModel

app = FastAPI(title="AppColoreando Visual Processor", version="0.1.0")
OUTPUT_ROOT = Path("/content-data/generation-jobs")

class ProcessRequest(BaseModel):
    jobId: UUID
    sourcePath: str
    presetCode: str
    difficulty: str
    targetRegions: int
    maxColors: int
    simplificationTolerance: float
    edgeSensitivity: float
    curveSmoothness: float
    saturationBoost: float
    contrastBoost: float

@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "healthy"}

@app.post("/process")
def process(request: ProcessRequest) -> dict:
    source = Path(request.sourcePath)
    if not source.exists():
        return {
            "success": False,
            "processorJobId": str(request.jobId),
            "resultManifestPath": None,
            "errorCode": "SOURCE_NOT_FOUND",
            "errorMessage": f"Source asset not found: {request.sourcePath}"
        }

    job_dir = OUTPUT_ROOT / str(request.jobId)
    job_dir.mkdir(parents=True, exist_ok=True)
    manifest = {
        "schemaVersion": "2.0",
        "jobId": str(request.jobId),
        "processor": "s16-foundation-stub",
        "sourcePath": request.sourcePath,
        "preset": request.presetCode,
        "difficulty": request.difficulty,
        "targetRegions": request.targetRegions,
        "maxColors": request.maxColors,
        "status": "preview-ready"
    }
    manifest_path = job_dir / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return {
        "success": True,
        "processorJobId": str(request.jobId),
        "resultManifestPath": str(manifest_path),
        "errorCode": None,
        "errorMessage": None
    }
