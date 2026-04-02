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
  assignedTo?: any;
  assignedToUserId?: string;
  equipment?: { name: string; make: string; model: string };
  history?: Array<{ previousStatus: string; newStatus: string; notes?: string; changedAt: string }>;
  createdAt: string;
}
