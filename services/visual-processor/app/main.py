from pathlib import Path
from uuid import UUID

from fastapi import FastAPI
from pydantic import BaseModel

from app.processor import ProcessorOptions, process_image

app = FastAPI(title="AppColoreando Visual Processor", version="0.2.0")
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
    return {"status": "healthy", "engine": "s17-visual-processing-mvp"}


@app.post("/process")
def process(request: ProcessRequest) -> dict:
    source = Path(request.sourcePath)
    if not source.exists():
        return {
            "success": False,
            "processorJobId": str(request.jobId),
            "resultManifestPath": None,
            "errorCode": "SOURCE_NOT_FOUND",
            "errorMessage": f"Source asset not found: {source}",
        }

    options = ProcessorOptions(
        target_regions=request.targetRegions,
        max_colors=request.maxColors,
        simplification_tolerance=request.simplificationTolerance,
        edge_sensitivity=request.edgeSensitivity,
        curve_smoothness=request.curveSmoothness,
        saturation_boost=request.saturationBoost,
        contrast_boost=request.contrastBoost,
    )
    try:
        result = process_image(
            source,
            OUTPUT_ROOT / str(request.jobId),
            options,
        )
        return {
            "success": True,
            "processorJobId": str(request.jobId),
            "resultManifestPath": result["manifestPath"],
            "errorCode": None,
            "errorMessage": None,
        }
    except ValueError as exc:
        return {
            "success": False,
            "processorJobId": str(request.jobId),
            "resultManifestPath": None,
            "errorCode": str(exc),
            "errorMessage": str(exc),
        }
    except Exception as exc:
        return {
            "success": False,
            "processorJobId": str(request.jobId),
            "resultManifestPath": None,
            "errorCode": "PROCESSING_FAILED",
            "errorMessage": str(exc),
        }
