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
