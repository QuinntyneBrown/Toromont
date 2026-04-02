export interface User {
  id: string;
  email: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  role: 'Admin' | 'FleetManager' | 'Technician' | 'Operator';
  organizationId?: string;
  tenantId?: string;
  isActive: boolean;
  createdAt: string;
  lastLogin?: string;
  lastLoginAt?: string;
}
