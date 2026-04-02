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
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
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
    if (this.notificationDropdownOpen) {
      this.notificationService.loadRecentNotifications();
    }
  }

  toggleUserMenu(): void {
    this.userMenuOpen = !this.userMenuOpen;
    this.notificationDropdownOpen = false;
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead();
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
      case 'workorder': return ['/service'];
      case 'alert': return ['/telemetry'];
      default: return ['/dashboard'];
    }
  }
}

