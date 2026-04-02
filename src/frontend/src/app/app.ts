import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet, NavigationEnd } from '@angular/router';
import { filter, map } from 'rxjs/operators';
import { SidebarComponent } from './shared/components/sidebar/sidebar.component';
import { HeaderComponent } from './shared/components/header/header.component';
import { NotificationService } from './core/services/notification.service';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent, HeaderComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private router = inject(Router);
  private notificationService = inject(NotificationService);
  private authService = inject(AuthService);
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

  ngOnInit(): void {
    this.authService.isAuthenticated$.subscribe(isAuth => {
      if (isAuth) {
        this.notificationService.startConnection('');
      }
    });
  }
}

