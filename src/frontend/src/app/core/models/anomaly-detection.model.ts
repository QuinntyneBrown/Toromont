export interface AnomalyDetection {
  id: string;
  organizationId?: string;
  equipmentId: string;
  anomalyType: string;
  metricName: string;
  expectedValue: number;
  actualValue: number;
  deviationSigma: number;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  detectedAt: string;
  equipment?: { id: string; name: string; make: string; model: string };
}
