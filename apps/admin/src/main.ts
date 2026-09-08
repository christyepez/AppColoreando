import { Component, signal } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="shell">
      <aside>
        <h2>AppColoreando</h2>
        <button *ngFor="let item of menu" (click)="section.set(item)">{{item}}</button>
      </aside>
      <main>
        <header><h1>{{section()}}</h1><span>Administración</span></header>
        <section class="cards">
          <article><strong>Usuarios</strong><div>—</div></article>
          <article><strong>Ilustraciones publicadas</strong><div>—</div></article>
          <article><strong>Completadas</strong><div>—</div></article>
          <article><strong>Eventos de actividad</strong><div>—</div></article>
        </section>
        <section class="panel">
          <h3>{{section()}}</h3>
          <p>Vista inicial preparada para conectar con los endpoints <code>/api/admin/*</code>.</p>
          <p *ngIf="section() === 'Contenido'">Administrar categorías, colecciones, ilustraciones, dificultad, país, assets, publicación y licencias.</p>
          <p *ngIf="section() === 'Usuarios'">Consultar cuentas, estado, rol, último acceso, progreso, favoritos, historia y métricas.</p>
          <p *ngIf="section() === 'Auditoría'">Consultar acciones administrativas con usuario, entidad, operación, cambios y fecha UTC.</p>
          <p *ngIf="section() === 'Métricas'">Sesiones, obras abiertas/completadas, regiones coloreadas y actividad diaria.</p>
        </section>
      </main>
    </div>
  `
})
class AppComponent {
  readonly menu = ['Dashboard', 'Contenido', 'Usuarios', 'Auditoría', 'Métricas', 'Licencias'];
  readonly section = signal('Dashboard');
}

bootstrapApplication(AppComponent).catch(console.error);
