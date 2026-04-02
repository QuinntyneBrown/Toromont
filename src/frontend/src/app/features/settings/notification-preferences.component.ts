import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface NotificationPreference {
  id: string;
  label: string;
  testId: string;
  email: boolean;
  sms: boolean;
}

@Component({
  selector: 'app-notification-preferences',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container-fluid p-4">
      <h2 class="mb-4 fw-bold">Notification Preferences</h2>

      <div class="card">
        <div class="card-body">
          <div *ngIf="saveSuccess" class="alert alert-success" data-testid="save-success">
            Preferences saved successfully!
          </div>

          <table class="table">
            <thead>
              <tr>
                <th>Notification</th>
                <th class="text-center">Email</th>
                <th class="text-center">SMS</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let pref of preferences" [attr.data-testid]="pref.testId">
                <td>{{ pref.label }}</td>
                <td class="text-center">
                  <input type="checkbox"
                         class="form-check-input"
                         data-testid="toggle-email"
                         [(ngModel)]="pref.email" />
                </td>
                <td class="text-center">
                  <input type="checkbox"
                         class="form-check-input"
                         data-testid="toggle-sms"
                         [(ngModel)]="pref.sms" />
                </td>
              </tr>
            </tbody>
          </table>

          <div class="mt-3">
            <button class="btn btn-primary"
                    data-testid="save-preferences-btn"
                    (click)="savePreferences()">
              Save Preferences
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .card {
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-lg);
    }
    .table th {
      font-size: 13px;
      font-weight: 600;
    }
    .table td {
      vertical-align: middle;
    }
  `]
})
export default class NotificationPreferencesComponent {
  saveSuccess = false;

  preferences: NotificationPreference[] = [
    { id: 'service-due', label: 'Service Due', testId: 'pref-service-due', email: true, sms: false },
    { id: 'critical-alert', label: 'Critical Alert', testId: 'pref-critical-alert', email: true, sms: true },
    { id: 'work-order-assigned', label: 'Work Order Assigned', testId: 'pref-work-order-assigned', email: true, sms: false },
    { id: 'parts-order-update', label: 'Parts Order Update', testId: 'pref-parts-order-update', email: true, sms: false }
  ];

  savePreferences(): void {
    // In a real app this would call an API
    this.saveSuccess = true;
  }
}
