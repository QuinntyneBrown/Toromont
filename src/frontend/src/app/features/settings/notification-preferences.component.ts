import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

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
  templateUrl: './notification-preferences.component.html',
  styleUrl: './notification-preferences.component.scss'
})
export default class NotificationPreferencesComponent implements OnInit {
  private api = inject(ApiService);
  saveSuccess = false;

  preferences: NotificationPreference[] = [
    { id: 'service-due', label: 'Service Due', testId: 'pref-service-due', email: true, sms: false },
    { id: 'critical-alert', label: 'Critical Alert', testId: 'pref-critical-alert', email: true, sms: true },
    { id: 'work-order-assigned', label: 'Work Order Assigned', testId: 'pref-work-order-assigned', email: true, sms: false },
    { id: 'parts-order-update', label: 'Parts Order Update', testId: 'pref-parts-order-update', email: true, sms: false }
  ];

  ngOnInit(): void {
    this.api.get<any[]>('/notifications/preferences').subscribe({
      next: (prefs) => {
        if (prefs && prefs.length > 0) {
          prefs.forEach(p => {
            const match = this.preferences.find(
              local => local.id === p.notificationType || local.id === p.id
            );
            if (match) {
              match.email = p.emailEnabled ?? p.email ?? match.email;
              match.sms = p.smsEnabled ?? p.sms ?? match.sms;
            }
          });
        }
      },
      error: () => {} // silently fail if not available
    });
  }

  savePreferences(): void {
    const payload = this.preferences.map(p => ({
      notificationType: p.id,
      emailEnabled: p.email,
      smsEnabled: p.sms,
      pushEnabled: true
    }));

    this.api.put('/notifications/preferences', payload).subscribe({
      next: () => {
        this.saveSuccess = true;
      },
      error: () => {} // silently fail
    });
  }
}

