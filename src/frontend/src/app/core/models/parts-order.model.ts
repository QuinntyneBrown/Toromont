import { OrderLineItem } from './order-line-item.model';

export interface PartsOrder {
  id: string;
  orderNumber: string;
  status: 'Draft' | 'Submitted' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';
  totalAmount: number;
  lineItems: OrderLineItem[];
  createdAt: string;
  estimatedDelivery?: string;
}
