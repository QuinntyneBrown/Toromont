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
