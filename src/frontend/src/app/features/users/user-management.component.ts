import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ApiService } from '../../core/services/api.service';
import { User } from '../../core/models';
import { Subject, takeUntil } from 'rxjs';

type UserRole = 'Admin' | 'FleetManager' | 'Technician' | 'Operator';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule, BadgeComponent],
  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.scss'
})
export default class UserManagementComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private destroy$ = new Subject<void>();

  users: User[] = [];
  roles: UserRole[] = ['Admin', 'FleetManager', 'Technician', 'Operator'];

  showInviteDialog = false;
  inviteEmail = '';
  inviteRole: UserRole = 'Operator';
  inviteSuccess = false;

  editingUserId: string | null = null;
  editRoleValue: UserRole = 'Operator';

  ngOnInit(): void {
    this.loadUsers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadUsers(): void {
    this.api.get<User[]>('/users')
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => this.users = data || [],
        error: (err) => console.error('Failed to load users', err)
      });
  }

  getInitials(user: any): string {
    const name = user.displayName || '';
    const parts = name.split(' ');
    return ((parts[0]?.[0] || '') + (parts[1]?.[0] || '')).toUpperCase() || '?';
  }

  startEdit(user: User): void {
    this.editingUserId = user.id;
    this.editRoleValue = user.role as UserRole;
  }

  saveRole(user: User): void {
    const newRole = this.editRoleValue;
    this.api.put(`/users/${user.id}/role`, { role: newRole })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          user.role = newRole;
          this.editingUserId = null;
        },
        error: (err) => console.error('Failed to update role', err)
      });
  }

  toggleUserStatus(user: User): void {
    const newStatus = !user.isActive;
    const action = newStatus ? 'activate' : 'deactivate';
    this.api.put(`/users/${user.id}/${action}`, {})
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          user.isActive = newStatus;
        },
        error: (err) => console.error('Failed to toggle user status', err)
      });
  }

  openInviteDialog(): void {
    this.inviteEmail = '';
    this.inviteRole = 'Operator';
    this.inviteSuccess = false;
    this.showInviteDialog = true;
  }

  closeInviteDialog(): void {
    this.showInviteDialog = false;
    this.inviteSuccess = false;
  }

  submitInvite(): void {
    if (!this.inviteEmail) return;

    this.api.post('/users/invite', { email: this.inviteEmail, role: this.inviteRole })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.inviteSuccess = true;
          this.loadUsers();
        },
        error: (err) => console.error('Failed to invite user', err)
      });
  }
}

