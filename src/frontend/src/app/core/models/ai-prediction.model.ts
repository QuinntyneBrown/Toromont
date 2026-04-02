export interface AIPrediction {
  id: string;
  organizationId?: string;
  equipmentId: string;
  component: string;
  confidenceScore: number;
  confidence?: number;
  recommendedAction: string;
  timeframe: string;
  priority: string;
  isDismissed?: boolean;
  generatedAt: string;
  createdAt?: string;
  equipment?: { id: string; name: string; make: string; model: string };
}
