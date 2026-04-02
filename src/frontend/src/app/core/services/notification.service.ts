import { Injectable, inject } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { Notification } from '../models';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private hubConnection!: signalR.HubConnection;
  private api = inject(ApiService);
  notifications$ = new BehaviorSubject<Notification[]>([]);
  unreadCount$ = new BehaviorSubject<number>(0);

  loadInitialCount(): void {
    this.api.get<{ unreadCount: number }>('/notifications/unread-count').subscribe({
      next: (data) => this.unreadCount$.next(data.unreadCount),
      error: () => {} // silently fail if not authenticated
    });
  }

  startConnection(accessToken: string): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => accessToken
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start().catch(err => console.error('SignalR connection error:', err));

    this.hubConnection.on('ReceiveNotification', (notification: Notification) => {
      const current = this.notifications$.value;
      this.notifications$.next([notification, ...current]);
    });

    this.hubConnection.on('UpdateBadgeCount', (count: number) => {
      this.unreadCount$.next(count);
    });
  }

  stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }

  markAsRead(notificationId: string): void {
    if (this.hubConnection) {
      this.hubConnection.invoke('MarkAsRead', notificationId);
    }
  }
}
