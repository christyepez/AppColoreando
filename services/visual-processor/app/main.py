from pathlib import Path
from uuid import UUID

from fastapi import FastAPI
from pydantic import BaseModel, Field

from app.processor import ProcessorOptions, process_image, process_variants

app = FastAPI(title="AppColoreando Visual Processor", version="0.9.0")
OUTPUT_ROOT = Path("/content-data/generation-jobs")


class SemanticHint(BaseModel):
    x: float = Field(ge=0.0, le=1.0)
    y: float = Field(ge=0.0, le=1.0)
    tag: str = Field(min_length=1, max_length=64)
    role: str = Field(min_length=1, max_length=64)
    confidence: float = Field(ge=0.0, le=1.0)
    provider: str = Field(default="external-ai", min_length=1, max_length=64)


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
    generateVariants: bool = True
    semanticHints: list[SemanticHint] = Field(default_factory=list)
@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "healthy", "engine": "s29-ai-assisted-generation"}


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
        difficulty=request.difficulty,
        style_code=request.presetCode,
        semantic_hints=tuple(h.model_dump() for h in request.semanticHints),
    )
    try:
        job_output = OUTPUT_ROOT / str(request.jobId)
        result = (
            process_variants(source, job_output, options, request.difficulty)
            if request.generateVariants
            else process_image(source, job_output, options)
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
