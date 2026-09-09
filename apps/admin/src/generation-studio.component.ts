import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { apiBase } from './api-config';

type SourceAsset = {
  id: string; fileName: string; contentType: string; sizeBytes: number;
  status: number | string; createdAtUtc: string;
};
type StylePreset = {
  id: string; name: string; code: string; targetRegionCount: number;
  maxColors: number; isActive: boolean;
};
type GenerationJob = {
  id: string; sourceAssetId: string; stylePresetId: string;
  difficulty: number | string; status: number | string;
  errorCode?: string; errorMessage?: string; resultManifestPath?: string;
  createdAtUtc: string; startedAtUtc?: string; completedAtUtc?: string;
};

type ArtifactKind = 'catalog' | 'lineart' | 'special';
@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="studio-grid">
      <article class="panel studio-source">
        <div class="section-title"><div><span class="eyebrow">SOURCE</span><h3>Artwork source</h3></div><span class="status-pill">{{assets().length}} assets</span></div>
        <label class="drop-zone">
          <input type="file" accept="image/png,image/jpeg,image/svg+xml" (change)="onFileSelected($event)" hidden>
          <ng-container *ngIf="localPreview(); else uploadHint"><img [src]="localPreview()" alt="Selected source preview"></ng-container>
          <ng-template #uploadHint><strong>Choose JPG, PNG or SVG</strong><span>Up to 25 MB</span></ng-template>
        </label>
        <button (click)="upload()" [disabled]="!selectedFile || uploading()">{{uploading() ? 'Uploading…' : 'Upload source'}}</button>
        <label>Source asset<select [(ngModel)]="selectedAssetId"><option value="">Select source</option><option *ngFor="let asset of assets()" [value]="asset.id">{{asset.fileName}}</option></select></label>
      </article>

      <article class="panel studio-config">
        <div class="section-title"><div><span class="eyebrow">GENERATION</span><h3>Template settings</h3></div></div>
        <label>Style preset<select [(ngModel)]="selectedPresetId"><option value="">Select preset</option><option *ngFor="let preset of presets()" [value]="preset.id">{{preset.name}} · {{preset.targetRegionCount}} regions</option></select></label>
        <label>Difficulty<select [(ngModel)]="difficulty"><option *ngFor="let option of difficulties" [ngValue]="option.value">{{option.label}}</option></select></label>
        <div class="preset-card" *ngIf="selectedPreset() as preset"><strong>{{preset.name}}</strong><span>{{preset.maxColors}} colors · {{preset.targetRegionCount}} target regions</span><span class="effect-chip" *ngIf="isSpecial(preset.code)">{{effectName(preset.code)}}</span></div>
        <button (click)="generate()" [disabled]="!canGenerate() || generating()">{{generating() ? 'Generating…' : 'Generate five variants'}}</button>
        <p class="error" *ngIf="error()">{{error()}}</p>
      </article>
    </section>

    <section class="panel" *ngIf="selectedJob() as job">
      <div class="section-title"><div><span class="eyebrow">PREVIEW</span><h3>Generated artwork</h3></div><span class="status-pill">{{statusName(job.status)}}</span></div>
      <div class="preview-grid">
        <figure><figcaption>Catalog</figcaption><img *ngIf="previewUrls().catalog" [src]="previewUrls().catalog" alt="Catalog preview"><div *ngIf="!previewUrls().catalog" class="preview-empty">Waiting for preview</div></figure>
        <figure><figcaption>Line art</figcaption><img *ngIf="previewUrls().lineart" [src]="previewUrls().lineart" alt="Line art preview"><div *ngIf="!previewUrls().lineart" class="preview-empty">Waiting for preview</div></figure>
        <figure><figcaption>Special effect</figcaption><img *ngIf="previewUrls().special" [src]="previewUrls().special" alt="Special preview"><div *ngIf="!previewUrls().special" class="preview-empty">Waiting for preview</div></figure>
      </div>
    </section>

    <section class="panel jobs-panel">
      <div class="section-title"><div><span class="eyebrow">QUEUE</span><h3>Generation jobs</h3></div><button class="secondary" (click)="reloadJobs()">Refresh</button></div>
      <table><tr><th>Source</th><th>Preset</th><th>Difficulty</th><th>Status</th><th>Created</th><th></th></tr>
        <tr *ngFor="let job of jobs()"><td>{{assetName(job.sourceAssetId)}}</td><td>{{presetName(job.stylePresetId)}}</td><td>{{difficultyName(job.difficulty)}}</td><td><span class="status-pill">{{statusName(job.status)}}</span></td><td>{{job.createdAtUtc | date:'short'}}</td><td><button class="secondary" (click)="openJob(job)">Open</button></td></tr>
      </table>
    </section>
  `
})
export class GenerationStudioComponent {
  private readonly http = inject(HttpClient);
  readonly assets = signal<SourceAsset[]>([]);
  readonly presets = signal<StylePreset[]>([]);
  readonly jobs = signal<GenerationJob[]>([]);
  readonly selectedJob = signal<GenerationJob | null>(null);
  readonly previewUrls = signal<Record<ArtifactKind, string>>({ catalog: '', lineart: '', special: '' });
  readonly uploading = signal(false);
  readonly generating = signal(false);
  readonly error = signal('');
  readonly localPreview = signal('');
  selectedFile: File | null = null;
  selectedAssetId = '';
  selectedPresetId = '';
  difficulty = 2;
  readonly difficulties = [
    { label: 'Kids', value: 0 }, { label: 'Easy', value: 1 },
    { label: 'Normal', value: 2 }, { label: 'Detailed', value: 3 },
    { label: 'Master', value: 4 }
  ];
  private pollHandle?: ReturnType<typeof setTimeout>;

  constructor() {
    this.reloadAll();
  }
  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFile = file;
    this.revokeLocalPreview();
    if (file) this.localPreview.set(URL.createObjectURL(file));
  }

  upload() {
    if (!this.selectedFile) return;
    this.uploading.set(true);
    this.error.set('');
    const form = new FormData();
    form.append('file', this.selectedFile);
    this.http.post<SourceAsset>(`${apiBase}/admin/content-generation/source-assets`, form).subscribe({
      next: asset => {
        this.selectedAssetId = asset.id;
        this.uploading.set(false);
        this.loadAssets();
      },
      error: () => { this.error.set('Source upload failed.'); this.uploading.set(false); }
    });
  }

  generate() {
    if (!this.canGenerate()) return;
    this.generating.set(true);
    this.error.set('');
    this.http.post<GenerationJob>(`${apiBase}/admin/content-generation/jobs`, {
      sourceAssetId: this.selectedAssetId,
      stylePresetId: this.selectedPresetId,
      difficulty: this.difficulty
    }).subscribe({
      next: job => { this.generating.set(false); this.openJob(job); this.reloadJobs(); },
      error: () => { this.error.set('Generation job could not be queued.'); this.generating.set(false); }
    });
  }
  reloadAll() {
    this.loadAssets();
    this.http.get<StylePreset[]>(`${apiBase}/admin/content-generation/style-presets`).subscribe(x => {
      this.presets.set(x.filter(p => p.isActive));
      if (!this.selectedPresetId && x.length) this.selectedPresetId = x[0].id;
    });
    this.reloadJobs();
  }

  reloadJobs() {
    this.http.get<GenerationJob[]>(`${apiBase}/admin/content-generation/jobs?take=50`).subscribe(jobs => {
      this.jobs.set(jobs);
      const current = this.selectedJob();
      if (current) {
        const refreshed = jobs.find(x => x.id === current.id);
        if (refreshed) {
          this.selectedJob.set(refreshed);
          if (this.isPreviewReady(refreshed.status)) this.loadPreviews(refreshed.id);
        }
      }
      this.schedulePolling(jobs);
    });
  }

  openJob(job: GenerationJob) {
    this.selectedJob.set(job);
    this.clearPreviewUrls();
    if (this.isPreviewReady(job.status)) this.loadPreviews(job.id);
    this.schedulePolling([job]);
  }

  canGenerate() { return !!this.selectedAssetId && !!this.selectedPresetId; }
  selectedPreset() { return this.presets().find(x => x.id === this.selectedPresetId); }
  assetName(id: string) { return this.assets().find(x => x.id === id)?.fileName ?? id.slice(0, 8); }
  presetName(id: string) { return this.presets().find(x => x.id === id)?.name ?? id.slice(0, 8); }
  difficultyName(value: number | string) { return typeof value === 'number' ? (this.difficulties[value]?.label ?? String(value)) : value; }
  statusName(value: number | string) {
    const names = ['Pending', 'Queued', 'Running', 'Preview ready', 'Needs review', 'Approved', 'Published', 'Failed'];
    return typeof value === 'number' ? (names[value] ?? String(value)) : value;
  }
  isSpecial(code: string) { return ['aura', 'tesoro', 'revela', 'postal-viva', 'lumina', 'eclipse'].includes(code); }
  effectName(code: string) {
    return ({ aura: 'Aura', tesoro: 'Tesoro', revela: 'Revela', 'postal-viva': 'Postal Viva', lumina: 'Lumina', eclipse: 'Eclipse' } as Record<string, string>)[code] ?? code;
  }

  ngOnDestroy() {
    if (this.pollHandle) clearTimeout(this.pollHandle);
    this.clearPreviewUrls();
    this.revokeLocalPreview();
  }

  private loadAssets() {
    this.http.get<SourceAsset[]>(`${apiBase}/admin/content-generation/source-assets?take=100`).subscribe(x => this.assets.set(x));
  }

  private isPreviewReady(status: number | string) {
    if (typeof status === 'number') return status >= 3 && status < 7;
    return ['PreviewReady', 'NeedsReview', 'Approved', 'Published', 'Preview ready'].includes(status);
  }
  private loadPreviews(jobId: string) {
    (['catalog', 'lineart', 'special'] as ArtifactKind[]).forEach(kind => {
      this.http.get(`${apiBase}/admin/content-generation/jobs/${jobId}/artifacts/${kind}`, { responseType: 'blob' })
        .subscribe({
          next: blob => {
            const current = this.previewUrls();
            if (current[kind]) URL.revokeObjectURL(current[kind]);
            this.previewUrls.set({ ...current, [kind]: URL.createObjectURL(blob) });
          },
          error: () => { /* Preview can still be processing. */ }
        });
    });
  }

  private schedulePolling(jobs: GenerationJob[]) {
    if (this.pollHandle) clearTimeout(this.pollHandle);
    const active = jobs.some(job => {
      const status = job.status;
      return typeof status === 'number' ? status === 1 || status === 2 : ['Queued', 'Running'].includes(status);
    });
    if (active) this.pollHandle = setTimeout(() => this.reloadJobs(), 2500);
  }

  private clearPreviewUrls() {
    const current = this.previewUrls();
    Object.values(current).forEach(url => { if (url) URL.revokeObjectURL(url); });
    this.previewUrls.set({ catalog: '', lineart: '', special: '' });
  }

  private revokeLocalPreview() {
    const current = this.localPreview();
    if (current) URL.revokeObjectURL(current);
    this.localPreview.set('');
  }
}
