import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';

interface NavItem {
  label: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <!-- Hamburger menu button (mobile only) -->
    <button class="hamburger-btn" data-testid="hamburger-menu" (click)="toggleMobileNav()">
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="18" x2="21" y2="18"/>
      </svg>
    </button>

    <!-- Mobile nav overlay -->
    <div class="mobile-nav-overlay" data-testid="mobile-nav-overlay" *ngIf="mobileNavOpen" (click)="closeMobileNav()">
      <div class="mobile-nav-panel" (click)="$event.stopPropagation()">
        <a *ngFor="let item of navItems"
           [routerLink]="item.route"
           class="mobile-nav-link"
           [attr.data-testid]="'mobile-nav-' + item.label.toLowerCase().replace(' ', '-')"
           (click)="closeMobileNav()">
          <span class="nav-icon" [innerHTML]="getIcon(item.icon)"></span>
          <span>{{ item.label }}</span>
        </a>
      </div>
    </div>

    <!-- Desktop sidebar -->
    <aside class="sidebar" [class.collapsed]="collapsed" data-testid="sidebar">
      <div class="sidebar-logo">
        <svg *ngIf="!collapsed" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="var(--sidebar-active)" stroke-width="2">
          <path d="M14 18V6a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2v11a1 1 0 0 0 1 1h2"/><path d="M15 18H9"/><path d="M19 18h2a1 1 0 0 0 1-1v-3.65a1 1 0 0 0-.22-.624l-3.48-4.35A1 1 0 0 0 17.52 8H14"/><circle cx="17" cy="18" r="2"/><circle cx="7" cy="18" r="2"/>
        </svg>
        <span class="logo-text" *ngIf="!collapsed">FLEET HUB</span>
        <svg *ngIf="collapsed" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="var(--sidebar-active)" stroke-width="2">
          <path d="M14 18V6a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2v11a1 1 0 0 0 1 1h2"/><path d="M15 18H9"/><path d="M19 18h2a1 1 0 0 0 1-1v-3.65a1 1 0 0 0-.22-.624l-3.48-4.35A1 1 0 0 0 17.52 8H14"/><circle cx="17" cy="18" r="2"/><circle cx="7" cy="18" r="2"/>
        </svg>
      </div>
      <nav class="sidebar-nav">
        <div *ngFor="let item of navItems" data-testid="nav-item" class="nav-item-wrapper">
          <a [routerLink]="item.route"
             class="nav-item"
             [attr.data-testid]="'nav-item-' + item.label.toLowerCase().replace(' ', '-')"
             [class.active]="currentRoute === item.route"
             [title]="item.label">
            <span class="nav-icon" [innerHTML]="getIcon(item.icon)"></span>
            <span class="nav-label" *ngIf="!collapsed">{{ item.label }}</span>
          </a>
        </div>
      </nav>
    </aside>

    <!-- Bottom navigation bar (mobile only) -->
    <nav class="bottom-nav" data-testid="bottom-nav">
      <div data-testid="bottom-nav-item" class="bottom-nav-item-wrapper">
        <a class="bottom-nav-item" data-testid="bottom-nav-home" routerLink="/dashboard">
          <span class="nav-icon" [innerHTML]="getIcon('dashboard')"></span>
          <span class="bottom-nav-label">Home</span>
        </a>
      </div>
      <div data-testid="bottom-nav-item" class="bottom-nav-item-wrapper">
        <a class="bottom-nav-item" data-testid="bottom-nav-equip" routerLink="/equipment">
          <span class="nav-icon" [innerHTML]="getIcon('equipment')"></span>
          <span class="bottom-nav-label">Equip</span>
        </a>
      </div>
      <div data-testid="bottom-nav-item" class="bottom-nav-item-wrapper">
        <a class="bottom-nav-item" data-testid="bottom-nav-orders" routerLink="/parts">
          <span class="nav-icon" [innerHTML]="getIcon('package')"></span>
          <span class="bottom-nav-label">Orders</span>
        </a>
      </div>
      <div data-testid="bottom-nav-item" class="bottom-nav-item-wrapper">
        <a class="bottom-nav-item" data-testid="bottom-nav-telem" routerLink="/telemetry">
          <span class="nav-icon" [innerHTML]="getIcon('activity')"></span>
          <span class="bottom-nav-label">Telem</span>
        </a>
      </div>
      <div data-testid="bottom-nav-item" class="bottom-nav-item-wrapper">
        <a class="bottom-nav-item" data-testid="bottom-nav-more" (click)="toggleMobileNav()">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="5" r="1"/><circle cx="12" cy="12" r="1"/><circle cx="12" cy="19" r="1"/>
          </svg>
          <span class="bottom-nav-label">More</span>
        </a>
      </div>
    </nav>
  `,
  styles: [`
    .sidebar {
      width: 240px;
      min-height: 100vh;
      background-color: var(--sidebar-bg);
      color: var(--sidebar-text);
      display: flex;
      flex-direction: column;
      transition: width 0.2s ease;
      position: fixed;
      left: 0;
      top: 0;
      z-index: 1000;
    }
    .sidebar.collapsed {
      width: 64px;
    }
    .sidebar-logo {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 0 0 20px 0;
    }
    .logo-text {
      font-size: 18px;
      font-weight: 700;
      color: var(--sidebar-active);
      letter-spacing: 1px;
    }
    .sidebar-nav {
      display: flex;
      flex-direction: column;
      padding: 24px 16px;
      gap: 4px;
    }
    .nav-item-wrapper {
      display: contents;
    }
    .nav-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px 12px;
      border-radius: var(--radius-md);
      color: var(--sidebar-text);
      text-decoration: none;
      font-size: 14px;
      transition: all 0.15s ease;
    }
    .nav-item:hover {
      background-color: rgba(255,255,255,0.08);
      color: var(--foreground-inverse);
    }
    .nav-item.active {
      background-color: rgba(255,205,17,0.1);
      color: var(--sidebar-active);
    }
    .nav-icon {
      display: flex;
      align-items: center;
      width: 20px;
      height: 20px;
    }
    .nav-icon :deep(svg) {
      width: 20px;
      height: 20px;
    }

    /* Hamburger button - mobile only */
    .hamburger-btn {
      display: none;
      position: fixed;
      top: 12px;
      left: 12px;
      z-index: 1100;
      background: var(--surface-secondary);
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-sm);
      padding: 8px;
      cursor: pointer;
      color: var(--foreground-primary);
    }

    /* Mobile nav overlay */
    .mobile-nav-overlay {
      display: flex;
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0,0,0,0.5);
      z-index: 1200;
    }
    .mobile-nav-panel {
      background: var(--sidebar-bg);
      width: 260px;
      height: 100%;
      padding: 60px 16px 16px;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .mobile-nav-link {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px;
      border-radius: var(--radius-md);
      color: var(--sidebar-text);
      text-decoration: none;
      font-size: 14px;
    }
    .mobile-nav-link:hover {
      background-color: rgba(255,255,255,0.08);
    }

    /* Bottom nav - mobile only */
    .bottom-nav {
      display: none;
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      background: var(--surface-secondary);
      border-top: 1px solid var(--border-subtle);
      z-index: 1000;
      justify-content: space-around;
      align-items: center;
      height: 56px;
    }
    .bottom-nav-item-wrapper {
      flex: 1;
      display: flex;
      justify-content: center;
    }
    .bottom-nav-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 2px;
      text-decoration: none;
      color: var(--foreground-secondary);
      font-size: 10px;
      cursor: pointer;
      padding: 4px 8px;
      background: none;
      border: none;
    }
    .bottom-nav-label {
      font-size: 10px;
    }

    @media (max-width: 768px) {
      .sidebar { display: none; }
      .hamburger-btn { display: block; }
      .mobile-nav-overlay { display: flex; }
      .bottom-nav { display: flex; }
    }
    @media (min-width: 769px) {
      .mobile-nav-overlay { display: none !important; }
      .bottom-nav { display: none !important; }
    }
  `]
})
export class SidebarComponent {
  @Input() currentRoute = '/dashboard';
  @Output() collapsedChange = new EventEmitter<boolean>();
  collapsed = false;
  mobileNavOpen = false;

  navItems: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
    { label: 'Equipment', route: '/equipment', icon: 'equipment' },
    { label: 'Service', route: '/service', icon: 'wrench' },
    { label: 'Parts', route: '/parts', icon: 'package' },
    { label: 'Telemetry', route: '/telemetry', icon: 'activity' },
    { label: 'AI Insights', route: '/ai-insights', icon: 'brain' },
    { label: 'Reports', route: '/reports', icon: 'chart' },
    { label: 'Admin', route: '/admin/users', icon: 'settings' }
  ];

  toggleCollapse(): void {
    this.collapsed = !this.collapsed;
    this.collapsedChange.emit(this.collapsed);
  }

  toggleMobileNav(): void {
    this.mobileNavOpen = !this.mobileNavOpen;
  }

  closeMobileNav(): void {
    this.mobileNavOpen = false;
  }

  getIcon(name: string): string {
    const icons: Record<string, string> = {
      dashboard: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>',
      equipment: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M2 18a1 1 0 0 0 1 1h18a1 1 0 0 0 1-1v-2a1 1 0 0 0-1-1H3a1 1 0 0 0-1 1v2z"/><path d="M10 15V5a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v10"/><path d="M4 15V8a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v7"/><path d="M16 15V8a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v7"/></svg>',
      wrench: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/></svg>',
      package: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M16.5 9.4l-9-5.19"/><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/></svg>',
      activity: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>',
      brain: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><path d="M12 2a7 7 0 0 1 0 14 7 7 0 0 1 0-14"/><path d="M12 8v4l2 2"/></svg>',
      chart: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>',
      settings: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>'
    };
    return icons[name] ?? '';
  }
}
