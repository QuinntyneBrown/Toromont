import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';
import { Notification } from '../../../core/models';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <header class="app-header">
      <div class="header-left">
        <div class="search-bar">
          <svg class="search-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
          </svg>
          <input type="text" placeholder="Search equipment, orders, alerts..." class="search-input" data-testid="header-search" />
        </div>
      </div>
      <div class="header-right">
        <div class="notification-wrapper">
          <button class="notification-btn" data-testid="notification-bell" (click)="toggleNotifications()">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
              <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
            </svg>
            <span class="badge-count" data-testid="notification-badge" *ngIf="(unreadCount$ | async) as count">{{ count }}</span>
          </button>
          <div class="notification-dropdown" data-testid="notification-dropdown" *ngIf="notificationDropdownOpen">
            <div class="dropdown-header">
              <span>Notifications</span>
              <button class="mark-all-read-btn" data-testid="mark-all-read" (click)="markAllAsRead()">Mark All as Read</button>
            </div>
            <div class="dropdown-body">
              <div *ngFor="let n of notifications$ | async"
                   class="notification-item"
                   data-testid="notification-item"
                   [class.unread]="!n.isRead"
                   (click)="onNotificationClick(n)">
                <div class="notification-title">{{ n.title }}</div>
                <div class="notification-message">{{ n.message }}</div>
              </div>
              <div class="empty-state" *ngIf="(notifications$ | async)?.length === 0">
                No notifications
              </div>
            </div>
          </div>
        </div>
        <div class="user-info" *ngIf="user$ | async as user">
          <div class="user-avatar-wrapper">
            <div class="user-avatar" data-testid="user-avatar" (click)="toggleUserMenu()">{{ user.firstName?.charAt(0) }}{{ user.lastName?.charAt(0) }}</div>
            <div class="user-menu-dropdown" *ngIf="userMenuOpen">
              <button class="sign-out-btn" data-testid="sign-out-btn" (click)="signOut()">Sign Out</button>
            </div>
          </div>
          <span class="user-name">{{ user.firstName }} {{ user.lastName }}</span>
        </div>
      </div>
    </header>
  `,
  styles: [`
    .app-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      height: 60px;
      padding: 0 24px;
      background-color: var(--surface-secondary);
      border-bottom: 1px solid var(--border-subtle);
    }
    .header-left { flex: 1; }
    .search-bar {
      display: flex;
      align-items: center;
      max-width: 400px;
      background-color: var(--surface-primary);
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-md);
      padding: 8px 12px;
    }
    .search-icon {
      color: var(--foreground-secondary);
      margin-right: 8px;
      flex-shrink: 0;
    }
    .search-input {
      border: none;
      background: none;
      outline: none;
      font-size: 14px;
      color: var(--foreground-primary);
      width: 100%;
    }
    .search-input::placeholder { color: var(--foreground-disabled); }
    .header-right {
      display: flex;
      align-items: center;
      gap: 16px;
    }
    .notification-wrapper {
      position: relative;
    }
    .notification-btn {
      position: relative;
      background: none;
      border: none;
      cursor: pointer;
      color: var(--foreground-secondary);
      padding: 8px;
      border-radius: var(--radius-sm);
    }
    .notification-btn:hover {
      background-color: var(--surface-hover);
      color: var(--foreground-primary);
    }
    .badge-count {
      position: absolute;
      top: 2px;
      right: 2px;
      background-color: var(--status-error);
      color: var(--foreground-inverse);
      font-size: 10px;
      font-weight: 600;
      border-radius: var(--radius-md);
      min-width: 16px;
      height: 16px;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 0 4px;
    }
    .notification-dropdown {
      position: absolute;
      top: 100%;
      right: 0;
      width: 320px;
      max-height: 400px;
      background: var(--surface-secondary);
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-md);
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
      z-index: 2000;
      overflow: hidden;
    }
    .dropdown-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 12px 16px;
      border-bottom: 1px solid var(--border-subtle);
      font-weight: 600;
      font-size: 14px;
    }
    .mark-all-read-btn {
      background: none;
      border: none;
      color: var(--accent-primary);
      cursor: pointer;
      font-size: 12px;
      font-weight: 500;
    }
    .mark-all-read-btn:hover {
      text-decoration: underline;
    }
    .dropdown-body {
      overflow-y: auto;
      max-height: 340px;
    }
    .notification-item {
      padding: 12px 16px;
      border-bottom: 1px solid var(--border-subtle);
      cursor: pointer;
    }
    .notification-item:hover {
      background-color: var(--surface-hover);
    }
    .notification-item.unread {
      background-color: rgba(255,205,17,0.05);
    }
    .notification-title {
      font-size: 13px;
      font-weight: 600;
      color: var(--foreground-primary);
    }
    .notification-message {
      font-size: 12px;
      color: var(--foreground-secondary);
      margin-top: 2px;
    }
    .empty-state {
      padding: 24px;
      text-align: center;
      color: var(--foreground-disabled);
      font-size: 13px;
    }
    .user-info {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .user-avatar-wrapper {
      position: relative;
    }
    .user-avatar {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      background-color: var(--accent-primary);
      color: var(--accent-secondary);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 12px;
      font-weight: 600;
      cursor: pointer;
    }
    .user-menu-dropdown {
      position: absolute;
      top: 100%;
      right: 0;
      min-width: 140px;
      background: var(--surface-secondary);
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-md);
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
      z-index: 2000;
      padding: 4px;
      margin-top: 4px;
    }
    .sign-out-btn {
      display: block;
      width: 100%;
      text-align: left;
      background: none;
      border: none;
      padding: 8px 12px;
      font-size: 13px;
      color: var(--foreground-primary);
      cursor: pointer;
      border-radius: var(--radius-sm);
    }
    .sign-out-btn:hover {
      background-color: var(--surface-hover);
    }
    .user-name {
      font-size: 14px;
      font-weight: 500;
      color: var(--foreground-primary);
    }
  `]
})
export class HeaderComponent implements OnInit {
  private notificationService = inject(NotificationService);
  private authService = inject(AuthService);
  private router = inject(Router);

  unreadCount$ = this.notificationService.unreadCount$;
  notifications$ = this.notificationService.notifications$;
  user$ = this.authService.user$;

  notificationDropdownOpen = false;
  userMenuOpen = false;

  ngOnInit(): void {
    this.notificationService.loadInitialCount();
  }

  toggleNotifications(): void {
    this.notificationDropdownOpen = !this.notificationDropdownOpen;
    this.userMenuOpen = false;
  }

  toggleUserMenu(): void {
    this.userMenuOpen = !this.userMenuOpen;
    this.notificationDropdownOpen = false;
  }

  markAllAsRead(): void {
    const notifications = this.notificationService.notifications$.value;
    notifications.forEach(n => {
      if (!n.isRead) {
        this.notificationService.markAsRead(n.id);
      }
    });
  }

  onNotificationClick(notification: Notification): void {
    this.notificationService.markAsRead(notification.id);
    this.notificationDropdownOpen = false;
    if (notification.relatedEntityId && notification.relatedEntityType) {
      const route = this.getRouteForEntity(notification.relatedEntityType, notification.relatedEntityId);
      if (route) {
        this.router.navigate(route);
      }
    }
  }

  signOut(): void {
    this.userMenuOpen = false;
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  private getRouteForEntity(entityType: string, entityId: string): string[] | null {
    switch (entityType.toLowerCase()) {
      case 'equipment': return ['/equipment', entityId];
      case 'workorder': return ['/service', entityId];
      case 'partsorder': return ['/parts', entityId];
      default: return null;
    }
  }
}
