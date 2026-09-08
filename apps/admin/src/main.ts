import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { Component, Injectable, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { bootstrapApplication } from '@angular/platform-browser';
import { CanActivateFn, Router, RouterLink, RouterOutlet, provideRouter } from '@angular/router';

const apiBase = (globalThis as { APP_COLOREANDO_API?: string }).APP_COLOREANDO_API ?? 'http://localhost:8080/api';

type AuthResponse = { accessToken: string; refreshToken: string; accessTokenExpiresAtUtc: string; user: UserDto };
type UserDto = { id: string; email: string; displayName: string; role: string; isActive: boolean; lastLoginAtUtc?: string };
type Metrics = { users: number; publishedArtworks: number; completions: number; activityEvents: number };
type Category = { id: string; name: string; slug: string; isActive: boolean };
type Country = { id: string; code: string; name: string; isActive: boolean };
type Artwork = { id: string; title: string; countryCode?: string; difficulty: number; regionCount: number; publishingStatus: string };
type Page<T> = { items: T[]; page: number; pageSize: number; total: number };

@Injectable({ providedIn: 'root' })
class SessionStore {
  readonly token = signal(localStorage.getItem('accessToken'));
  readonly user = signal<UserDto | null>(JSON.parse(localStorage.getItem('user') ?? 'null') as UserDto | null);
  readonly isAuthenticated = computed(() => !!this.token());

  set(auth: AuthResponse) {
    localStorage.setItem('accessToken', auth.accessToken);
    localStorage.setItem('refreshToken', auth.refreshToken);
    localStorage.setItem('user', JSON.stringify(auth.user));
    this.token.set(auth.accessToken);
    this.user.set(auth.user);
  }

  clear() {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    this.token.set(null);
    this.user.set(null);
  }
}

const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(SessionStore).token();
  return next(token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req);
};

const authGuard: CanActivateFn = () => {
  const session = inject(SessionStore);
  if (session.isAuthenticated()) return true;
  return inject(Router).parseUrl('/login');
};

@Injectable({ providedIn: 'root' })
class ApiClient {
  private readonly http = inject(HttpClient);
  login(email: string, password: string) { return this.http.post<AuthResponse>(`${apiBase}/auth/login`, { email, password, deviceId: 'admin-web', deviceName: 'Angular Admin' }); }
  metrics() { return this.http.get<Metrics>(`${apiBase}/admin/metrics`); }
  users() { return this.http.get<UserDto[]>(`${apiBase}/admin/users`); }
  updateUser(user: UserDto) { return this.http.put<UserDto>(`${apiBase}/admin/users/${user.id}`, { displayName: user.displayName, role: user.role, isActive: user.isActive }); }
  categories() { return this.http.get<Category[]>(`${apiBase}/catalog/categories`); }
  countries() { return this.http.get<Country[]>(`${apiBase}/catalog/countries`); }
  collections() { return this.http.get<unknown[]>(`${apiBase}/catalog/collections`); }
  artworks() { return this.http.get<Page<Artwork>>(`${apiBase}/catalog/artworks?pageSize=100`); }
  createCategory(name: string, slug: string) { return this.http.post<Category>(`${apiBase}/admin/categories`, { name, slug, isActive: true }); }
  upsertCountry(code: string, name: string) { return this.http.post<Country>(`${apiBase}/admin/countries`, { code, name, isActive: true }); }
  audit() { return this.http.get<unknown[]>(`${apiBase}/admin/audit`); }
  brands() { return this.http.get<unknown[]>(`${apiBase}/admin/brands`); }
  licenses() { return this.http.get<unknown[]>(`${apiBase}/admin/licenses`); }
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="login">
      <section class="login-panel">
        <h1>AppColoreando Admin</h1>
        <label>Email<input [(ngModel)]="email" autocomplete="username"></label>
        <label>Password<input [(ngModel)]="password" type="password" autocomplete="current-password"></label>
        <button (click)="login()" [disabled]="busy()">Sign in</button>
        <p class="error" *ngIf="error()">{{error()}}</p>
      </section>
    </main>
  `
})
class LoginComponent {
  private readonly api = inject(ApiClient);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  email = 'admin@appcoloreando.local';
  password = '';
  readonly busy = signal(false);
  readonly error = signal('');
  login() {
    this.busy.set(true);
    this.error.set('');
    this.api.login(this.email, this.password).subscribe({
      next: auth => { this.session.set(auth); void this.router.navigateByUrl('/'); },
      error: (e: HttpErrorResponse) => { this.error.set(e.status === 401 ? 'Invalid credentials' : 'Login failed'); this.busy.set(false); }
    });
  }
}

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink, RouterOutlet],
  template: `
    <div class="shell">
      <aside>
        <h2>AppColoreando</h2>
        <a routerLink="/">Dashboard</a><a routerLink="/content">Content</a><a routerLink="/users">Users</a><a routerLink="/licenses">Licenses</a><a routerLink="/audit">Audit</a>
        <button class="ghost" (click)="logout()">Sign out</button>
      </aside>
      <main><header><h1>{{title()}}</h1><span>{{session.user()?.role}}</span></header><router-outlet /></main>
    </div>
  `
})
class ShellComponent {
  readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  readonly title = signal('Dashboard');
  logout() { this.session.clear(); void this.router.navigateByUrl('/login'); }
}

@Component({ standalone: true, imports: [RouterOutlet], selector: 'app-root', template: `<router-outlet />` })
class RootComponent {}

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `<section class="cards"><article><strong>Users</strong><div>{{metrics()?.users ?? '-'}}</div></article><article><strong>Published</strong><div>{{metrics()?.publishedArtworks ?? '-'}}</div></article><article><strong>Completions</strong><div>{{metrics()?.completions ?? '-'}}</div></article><article><strong>Events</strong><div>{{metrics()?.activityEvents ?? '-'}}</div></article></section>`
})
class DashboardComponent { readonly metrics = signal<Metrics | null>(null); constructor() { inject(ApiClient).metrics().subscribe(x => this.metrics.set(x)); } }

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="toolbar"><input [(ngModel)]="categoryName" placeholder="Category"><input [(ngModel)]="categorySlug" placeholder="slug"><button (click)="createCategory()">Add category</button><input [(ngModel)]="countryCode" placeholder="EC"><input [(ngModel)]="countryName" placeholder="Country"><button (click)="saveCountry()">Save country</button></section>
    <section class="grid two"><div class="panel"><h3>Categories</h3><table><tr *ngFor="let c of categories()"><td>{{c.name}}</td><td>{{c.slug}}</td></tr></table></div><div class="panel"><h3>Artworks</h3><table><tr *ngFor="let a of artworks()"><td>{{a.title}}</td><td>{{a.countryCode}}</td><td>{{a.publishingStatus}}</td></tr></table></div></section>
  `
})
class ContentComponent {
  private readonly api = inject(ApiClient);
  readonly categories = signal<Category[]>([]);
  readonly artworks = signal<Artwork[]>([]);
  categoryName = ''; categorySlug = ''; countryCode = ''; countryName = '';
  constructor() { this.reload(); }
  reload() { this.api.categories().subscribe(x => this.categories.set(x)); this.api.artworks().subscribe(x => this.artworks.set(x.items)); }
  createCategory() { this.api.createCategory(this.categoryName, this.categorySlug).subscribe(() => this.reload()); }
  saveCountry() { this.api.upsertCountry(this.countryCode, this.countryName).subscribe(() => { this.countryCode = ''; this.countryName = ''; }); }
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `<section class="panel"><table><tr><th>Email</th><th>Name</th><th>Role</th><th>Active</th><th></th></tr><tr *ngFor="let u of users()"><td>{{u.email}}</td><td><input [(ngModel)]="u.displayName"></td><td><select [(ngModel)]="u.role"><option>Admin</option><option>ContentManager</option><option>Analyst</option><option>User</option></select></td><td><input type="checkbox" [(ngModel)]="u.isActive"></td><td><button (click)="save(u)">Save</button></td></tr></table></section>`
})
class UsersComponent { private readonly api = inject(ApiClient); readonly users = signal<UserDto[]>([]); constructor() { this.api.users().subscribe(x => this.users.set(x)); } save(u: UserDto) { this.api.updateUser(u).subscribe(); } }

@Component({ standalone: true, imports: [CommonModule], template: `<section class="grid two"><div class="panel"><h3>Brands</h3><pre>{{brands() | json}}</pre></div><div class="panel"><h3>Licenses</h3><pre>{{licenses() | json}}</pre></div></section>` })
class LicensesComponent { private readonly api = inject(ApiClient); readonly brands = signal<unknown[]>([]); readonly licenses = signal<unknown[]>([]); constructor() { this.api.brands().subscribe(x => this.brands.set(x)); this.api.licenses().subscribe(x => this.licenses.set(x)); } }

@Component({ standalone: true, imports: [CommonModule], template: `<section class="panel"><pre>{{audit() | json}}</pre></section>` })
class AuditComponent { readonly audit = signal<unknown[]>([]); constructor() { inject(ApiClient).audit().subscribe(x => this.audit.set(x)); } }

bootstrapApplication(RootComponent, {
  providers: [
    provideHttpClient(withInterceptors([authInterceptor])),
    provideRouter([
      { path: 'login', component: LoginComponent },
      { path: '', component: ShellComponent, canActivate: [authGuard], children: [
        { path: '', component: DashboardComponent },
        { path: 'content', component: ContentComponent },
        { path: 'users', component: UsersComponent },
        { path: 'licenses', component: LicensesComponent },
        { path: 'audit', component: AuditComponent }
      ] }
    ])
  ]
}).catch(console.error);
