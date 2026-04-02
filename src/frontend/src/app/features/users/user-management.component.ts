import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GridModule } from '@progress/kendo-angular-grid';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogsModule } from '@progress/kendo-angular-dialog';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ApiService } from '../../core/services/api.service';
import { User } from '../../core/models';
import { Subject, takeUntil } from 'rxjs';

type UserRole = 'Admin' | 'FleetManager' | 'Technician' | 'Operator';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule, GridModule, DropDownsModule, ButtonsModule, DialogsModule, InputsModule, BadgeComponent],
  template: `
    <div class="container-fluid p-4">
      <!-- Title Row -->
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h2 class="mb-0 fw-bold">User Management</h2>
        <button kendoButton [themeColor]="'primary'" (click)="openInviteDialog()">
          + Invite User
        </button>
      </div>

      <!-- Users Grid -->
      <div class="card">
        <div class="card-body p-0">
          <kendo-grid [data]="users" [pageable]="true" [pageSize]="15" [sortable]="true" [style.font-size.px]="13">
            <kendo-grid-column field="name" title="Name" [width]="200">
              <ng-template kendoGridCellTemplate let-dataItem>
                <div class="d-flex align-items-center gap-2">
                  <div class="user-avatar">{{ getInitials(dataItem) }}</div>
                  <div>
                    <div class="fw-semibold">{{ dataItem.firstName }} {{ dataItem.lastName }}</div>
                  </div>
                </div>
              </ng-template>
            </kendo-grid-column>
            <kendo-grid-column field="email" title="Email" [width]="240">
            </kendo-grid-column>
            <kendo-grid-column field="role" title="Role" [width]="180">
              <ng-template kendoGridCellTemplate let-dataItem>
                <kendo-dropdownlist
                  [data]="roles"
                  [value]="dataItem.role"
                  [valuePrimitive]="true"
                  (valueChange)="onRoleChange(dataItem, $event)"
                  [style.width.px]="160"
                  [popupSettings]="{ width: 180 }">
                </kendo-dropdownlist>
              </ng-template>
            </kendo-grid-column>
            <kendo-grid-column field="isActive" title="Status" [width]="120">
              <ng-template kendoGridCellTemplate let-dataItem>
                <app-badge
                  [text]="dataItem.isActive ? 'Active' : 'Inactive'"
                  [variant]="dataItem.isActive ? 'success' : 'error'">
                </app-badge>
              </ng-template>
            </kendo-grid-column>
            <kendo-grid-column field="lastLogin" title="Last Login" [width]="160">
              <ng-template kendoGridCellTemplate let-dataItem>
                {{ dataItem.lastLogin ? (dataItem.lastLogin | date:'medium') : 'Never' }}
              </ng-template>
            </kendo-grid-column>
            <kendo-grid-column title="Actions" [width]="140">
              <ng-template kendoGridCellTemplate let-dataItem>
                <button class="btn btn-sm"
                        [class.btn-outline-danger]="dataItem.isActive"
                        [class.btn-outline-success]="!dataItem.isActive"
                        (click)="toggleUserStatus(dataItem)">
                  {{ dataItem.isActive ? 'Deactivate' : 'Activate' }}
                </button>
              </ng-template>
            </kendo-grid-column>
          </kendo-grid>
        </div>
      </div>

      <!-- Invite User Dialog -->
      <kendo-dialog *ngIf="showInviteDialog" title="Invite User" (close)="closeInviteDialog()" [width]="440">
        <div class="mb-3">
          <label class="form-label fw-semibold">Email Address</label>
          <input kendoTextBox [(ngModel)]="inviteEmail" placeholder="user@company.com" class="w-100" />
        </div>
        <div class="mb-3">
          <label class="form-label fw-semibold">Role</label>
          <kendo-dropdownlist
            [data]="roles"
            [(value)]="inviteRole"
            [valuePrimitive]="true"
            class="w-100">
          </kendo-dropdownlist>
        </div>
        <kendo-dialog-actions>
          <button kendoButton (click)="closeInviteDialog()">Cancel</button>
          <button kendoButton [themeColor]="'primary'" (click)="submitInvite()" [disabled]="!inviteEmail">
            Send Invite
          </button>
        </kendo-dialog-actions>
      </kendo-dialog>
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

  getInitials(user: User): string {
    return ((user.firstName?.[0] || '') + (user.lastName?.[0] || '')).toUpperCase() || '?';
  }

  onRoleChange(user: User, newRole: UserRole): void {
    this.api.put(`/users/${user.id}/role`, { role: newRole })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          user.role = newRole;
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
    this.inviteRole = 'ReadOnly';
    this.showInviteDialog = true;
  }

  closeInviteDialog(): void {
    this.showInviteDialog = false;
  }

  submitInvite(): void {
    if (!this.inviteEmail) return;

    this.api.post('/users/invite', { email: this.inviteEmail, role: this.inviteRole })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.closeInviteDialog();
          this.loadUsers();
        },
        error: (err) => console.error('Failed to invite user', err)
      });
  }
}
