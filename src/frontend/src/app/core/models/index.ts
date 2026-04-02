export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Admin' | 'FleetManager' | 'Technician' | 'Operator';
  tenantId: string;
  isActive: boolean;
  createdAt: string;
  lastLogin?: string;
}

export interface Equipment {
  id: string;
  name: string;
  serialNumber: string;
  model: string;
  make: string;
  year: number;
  category: string;
  status: string;
  latitude: number;
  longitude: number;
  location: string;
  lastServiceDate?: string;
  notes?: string;
  organizationId: string;
  createdAt: string;
  updatedAt: string;
  currentHours?: number;
  locationLat?: number;
  locationLng?: number;
  tenantId?: string;
}

export interface WorkOrder {
  id: string;
  organizationId?: string;
  workOrderNumber?: string;
  equipmentId: string;
  serviceType?: string;
  title?: string;
  description: string;
  status: string;
  priority: string;
  requestedDate?: string;
  scheduledDate?: string;
  completedDate?: string;
  assignedTo?: string | { displayName: string; role: string };
  assignedToUserId?: string;
  equipment?: { name: string; make: string; model: string };
  history?: Array<{ previousStatus: string; newStatus: string; notes?: string; changedAt: string }>;
  createdAt: string;
}

export interface TelemetryEvent {
  id: string;
  equipmentId: string;
  eventType: string;
  value: number;
  unit: string;
  timestamp: string;
  metadata?: Record<string, unknown>;
}

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

export interface AIPrediction {
  id: string;
  equipmentId: string;
  predictionType: string;
  confidence: number;
  predictedDate?: string;
  description: string;
  recommendedAction?: string;
  createdAt: string;
}

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

export interface Notification {
  id: string;
  userId: string;
  title: string;
  message: string;
  type: 'Alert' | 'WorkOrder' | 'System' | 'AI';
  isRead: boolean;
  createdAt: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
}

export interface NotificationPreference {
  id: string;
  userId: string;
  notificationType: string;
  emailEnabled: boolean;
  pushEnabled: boolean;
  smsEnabled: boolean;
}

export interface PartsOrder {
  id: string;
  orderNumber: string;
  status: 'Draft' | 'Submitted' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';
  totalAmount: number;
  lineItems: OrderLineItem[];
  createdAt: string;
  estimatedDelivery?: string;
}

export interface OrderLineItem {
  id: string;
  partNumber: string;
  description: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
  totalCount?: number;
  page?: number;
  pageSize?: number;
}
