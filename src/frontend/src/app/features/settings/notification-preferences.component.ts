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
  templateUrl: './notification-preferences.component.html',
  styleUrl: './notification-preferences.component.scss'
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

