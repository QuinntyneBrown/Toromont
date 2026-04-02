import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <header class="app-header">
      <div class="header-left">
        <div class="search-bar">
          <svg class="search-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
          </svg>
          <input type="text" placeholder="Search equipment, orders, alerts..." class="search-input" />
        </div>
      </div>
      <div class="header-right">
        <button class="notification-btn" (click)="toggleNotifications()">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
            <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
          </svg>
          <span class="badge-count" *ngIf="(unreadCount$ | async) as count">{{ count }}</span>
        </button>
        <div class="user-info" *ngIf="user$ | async as user">
          <div class="user-avatar">{{ user.firstName.charAt(0) }}{{ user.lastName.charAt(0) }}</div>
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
    .user-info {
      display: flex;
      align-items: center;
      gap: 8px;
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

  unreadCount$ = this.notificationService.unreadCount$;
  user$ = this.authService.user$;

  ngOnInit(): void {
    this.notificationService.loadInitialCount();
  }

  toggleNotifications(): void {
    // Notification panel toggle - will be implemented later
  }
}
