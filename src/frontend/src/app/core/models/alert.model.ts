export interface Alert {
  id: string;
  equipmentId: string;
  alertType: string;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  message: string;
  isAcknowledged: boolean;
  createdAt: string;
  acknowledgedAt?: string;
  equipment?: { id: string; name: string };
}
