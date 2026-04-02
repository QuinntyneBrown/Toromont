export interface TelemetryEvent {
  id: string;
  equipmentId: string;
  eventType: string;
  value: number;
  unit: string;
  timestamp: string;
  metadata?: Record<string, unknown>;
}
