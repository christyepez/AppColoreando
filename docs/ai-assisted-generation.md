# S29 — AI Assisted Generation

S29 adds an optional semantic-assistance contract to the Visual Processor without making generation depend on an external AI provider.

Semantic hints use normalized coordinates so the same suggestion can be applied across Kids, Easy, Normal, Detailed and Master variants even when region IDs differ.

Each hint contains `x`, `y`, `tag`, `role`, `confidence` and `provider`. Only hints with confidence >= 0.75 can override the deterministic S20 heuristic classification.

Generated regions expose `semanticSource` and `semanticConfidence`, making every semantic decision traceable in `regions.json` and the playable bundle.

When no hints are supplied, or a hint is below the confidence threshold, the existing deterministic heuristic remains authoritative. This preserves offline/local generation and avoids a hard dependency on a proprietary model or cloud API.

The contract is provider-neutral. A future approved local or cloud vision model can emit hints without changing the generation pipeline, bundle schema, mobile runtime or difficulty generator.