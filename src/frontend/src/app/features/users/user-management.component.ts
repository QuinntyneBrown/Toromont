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
  template: `
    <div class="container-fluid p-4">
      <!-- Title Row -->
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h2 class="mb-0 fw-bold">User Management</h2>
        <button class="btn btn-primary" data-testid="invite-user-btn" (click)="openInviteDialog()">
          + Invite User
        </button>
      </div>

      <!-- Users Grid -->
      <div class="card">
        <div class="card-body p-0">
          <table class="table table-hover mb-0" data-testid="users-grid">
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Status</th>
                <th>Last Login</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let user of users" data-testid="user-row">
                <td>
                  <div class="d-flex align-items-center gap-2">
                    <div class="user-avatar">{{ getInitials(user) }}</div>
                    <div class="fw-semibold">{{ user.displayName }}</div>
                  </div>
                </td>
                <td data-testid="user-email">{{ user.email }}</td>
                <td>
                  <span *ngIf="editingUserId !== user.id" data-testid="user-role">{{ user.role }}</span>
                  <select *ngIf="editingUserId === user.id"
                          class="form-select form-select-sm"
                          data-testid="edit-role"
                          [(ngModel)]="editRoleValue">
                    <option *ngFor="let r of roles" [value]="r">{{ r }}</option>
                  </select>
                </td>
                <td>
                  <app-badge
                    [text]="user.isActive ? 'Active' : 'Inactive'"
                    [variant]="user.isActive ? 'success' : 'error'">
                  </app-badge>
                </td>
                <td>{{ user.lastLoginAt ? (user.lastLoginAt | date:'medium') : 'Never' }}</td>
                <td>
                  <button *ngIf="editingUserId !== user.id"
                          class="btn btn-sm btn-outline-secondary me-1"
                          data-testid="edit-user-btn"
                          (click)="startEdit(user)">
                    Edit
                  </button>
                  <button *ngIf="editingUserId === user.id"
                          class="btn btn-sm btn-primary me-1"
                          data-testid="save-role-btn"
                          (click)="saveRole(user)">
                    Save
                  </button>
                  <button class="btn btn-sm"
                          [class.btn-outline-danger]="user.isActive"
                          [class.btn-outline-success]="!user.isActive"
                          (click)="toggleUserStatus(user)">
                    {{ user.isActive ? 'Deactivate' : 'Activate' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Invite User Dialog -->
      <div *ngIf="showInviteDialog" class="modal-backdrop fade show"></div>
      <div *ngIf="showInviteDialog" class="modal d-block" tabindex="-1">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Invite User</h5>
              <button type="button" class="btn-close" (click)="closeInviteDialog()"></button>
            </div>
            <div class="modal-body">
              <div *ngIf="inviteSuccess" class="alert alert-success" data-testid="invite-success">
                Invitation sent successfully!
              </div>
              <div class="mb-3">
                <label class="form-label fw-semibold">Email Address</label>
                <input type="email"
                       class="form-control"
                       data-testid="invite-email"
                       [(ngModel)]="inviteEmail"
                       placeholder="user@company.com" />
              </div>
              <div class="mb-3">
                <label class="form-label fw-semibold">Role</label>
                <select class="form-select" data-testid="invite-role" [(ngModel)]="inviteRole">
                  <option *ngFor="let r of roles" [value]="r">{{ r }}</option>
                </select>
              </div>
            </div>
            <div class="modal-footer">
              <button class="btn btn-secondary" (click)="closeInviteDialog()">Cancel</button>
              <button class="btn btn-primary"
                      data-testid="invite-submit"
                      (click)="submitInvite()"
                      [disabled]="!inviteEmail">
                Send Invite
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .card {
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-lg);
      overflow: hidden;
    }
    .user-avatar {
      width: 34px;
      height: 34px;
      border-radius: 50%;
      background: var(--accent-primary);
      color: var(--surface-inverse);
      font-size: 13px;
      font-weight: 700;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }
    .table th {
      font-size: 13px;
      font-weight: 600;
      border-bottom: 2px solid var(--border-subtle);
    }
    .table td {
      font-size: 13px;
      vertical-align: middle;
    }
  `]
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
