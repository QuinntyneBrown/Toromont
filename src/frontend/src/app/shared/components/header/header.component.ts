import { Component, ElementRef, HostListener, inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged, filter, switchMap } from 'rxjs/operators';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';
import { ApiService } from '../../../core/services/api.service';
import { Notification } from '../../../core/models';
import { OrgSwitcherComponent } from '../org-switcher/org-switcher.component';

interface SearchEquipmentResult {
  id: string;
  name: string;
  serialNumber: string;
  make: string;
  model: string;
  status: string;
  category: string;
}

interface SearchPartResult {
  id: string;
  name: string;
  partNumber: string;
  category: string;
  price: number;
}

interface SearchWorkOrderResult {
  id: string;
  workOrderNumber: string;
  description: string;
  status: string;
  priority: string;
  equipmentName: string | null;
}

interface GlobalSearchResult {
  equipment: SearchEquipmentResult[];
  parts: SearchPartResult[];
  workOrders: SearchWorkOrderResult[];
}

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, OrgSwitcherComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent implements OnInit, OnDestroy {
  private notificationService = inject(NotificationService);
  private authService = inject(AuthService);
  private api = inject(ApiService);
  private router = inject(Router);

  @ViewChild('searchContainer') searchContainerRef!: ElementRef;

  unreadCount$ = this.notificationService.unreadCount$;
  notifications$ = this.notificationService.notifications$;
  user$ = this.authService.user$;

  notificationDropdownOpen = false;
  userMenuOpen = false;

  searchQuery = '';
  searchResults: GlobalSearchResult | null = null;
  searchOpen = false;
  searching = false;

  private searchSubject = new Subject<string>();
  private searchSub!: Subscription;

  ngOnInit(): void {
    this.notificationService.loadInitialCount();

    this.searchSub = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      filter(q => q.trim().length >= 2),
      switchMap(q => {
        this.searching = true;
        return this.api.get<GlobalSearchResult>('/search', { q });
      }),
    ).subscribe({
      next: results => {
        this.searchResults = results;
        this.searchOpen = true;
        this.searching = false;
      },
      error: () => {
        this.searching = false;
      },
    });
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  onSearchInput(value: string): void {
    this.searchQuery = value;
    if (value.trim().length < 2) {
      this.searchResults = null;
      this.searchOpen = false;
      this.searching = false;
      return;
    }
    this.searching = true;
    this.searchSubject.next(value);
  }

  get hasResults(): boolean {
    if (!this.searchResults) return false;
    return (
      this.searchResults.equipment.length > 0 ||
      this.searchResults.parts.length > 0 ||
      this.searchResults.workOrders.length > 0
    );
  }

  navigateToEquipment(id: string): void {
    this.closeSearch();
    this.router.navigate(['/equipment', id]);
  }

  navigateToParts(): void {
    this.closeSearch();
    this.router.navigate(['/parts'], { queryParams: { search: this.searchQuery } });
  }

  navigateToWorkOrders(): void {
    this.closeSearch();
    this.router.navigate(['/service']);
  }

  closeSearch(): void {
    this.searchOpen = false;
    this.searchQuery = '';
    this.searchResults = null;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.searchContainerRef && !this.searchContainerRef.nativeElement.contains(event.target)) {
      this.searchOpen = false;
    }
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

