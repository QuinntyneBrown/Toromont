import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet, NavigationEnd } from '@angular/router';
import { filter, map } from 'rxjs/operators';
import { SidebarComponent } from './shared/components/sidebar/sidebar.component';
import { HeaderComponent } from './shared/components/header/header.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent, HeaderComponent],
  template: `
    <div class="app-layout" *ngIf="!(isLoginPage$ | async); else loginLayout">
      <app-sidebar [currentRoute]="(currentRoute$ | async) ?? '/dashboard'" (collapsedChange)="sidebarCollapsed = $event"></app-sidebar>
      <div class="main-area" [class.sidebar-collapsed]="sidebarCollapsed">
        <app-header></app-header>
        <main class="main-content">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>
    <ng-template #loginLayout>
      <router-outlet></router-outlet>
    </ng-template>
  `,
  styles: [`
    .app-layout {
      display: flex;
      min-height: 100vh;
    }
    .main-area {
      flex: 1;
      margin-left: 240px;
      display: flex;
      flex-direction: column;
      transition: margin-left 0.2s ease;
    }
    .main-area.sidebar-collapsed {
      margin-left: 64px;
    }
    .main-content {
      flex: 1;
      overflow-y: auto;
    }
    @media (max-width: 768px) {
      .main-area { margin-left: 64px; }
    }
  `]
})
export class App {
  private router = inject(Router);
  sidebarCollapsed = false;

  currentRoute$ = this.router.events.pipe(
    filter((e): e is NavigationEnd => e instanceof NavigationEnd),
    map(e => {
      const url = e.urlAfterRedirects;
      // Match to the top-level route for sidebar highlighting
      const segments = url.split('/').filter(Boolean);
      if (segments.length === 0) return '/dashboard';
      if (segments[0] === 'admin') return '/admin/users';
      return '/' + segments[0];
    })
  );

  isLoginPage$ = this.router.events.pipe(
    filter((e): e is NavigationEnd => e instanceof NavigationEnd),
    map(e => e.urlAfterRedirects.startsWith('/login'))
  );
}
