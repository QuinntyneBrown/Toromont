import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { GridModule, DataStateChangeEvent, CellClickEvent } from '@progress/kendo-angular-grid';
import { SchedulerModule, SchedulerEvent, DateChangeEvent } from '@progress/kendo-angular-scheduler';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ApiService } from '../../core/services/api.service';
import { ApiResponse, WorkOrder } from '../../core/models';

interface StatusTab {
  label: string;
  value: string;
  count: number;
  testId: string;
}

interface HistoryItem {
  status: string;
  user: string;
  timestamp: string;
}

@Component({
  selector: 'app-work-orders',
  standalone: true,
  imports: [
    CommonModule, RouterModule, FormsModule,
    GridModule, SchedulerModule,
    BadgeComponent
  ],
  templateUrl: './work-orders.component.html',
  styleUrl: './work-orders.component.scss'
})
export default class WorkOrdersComponent implements OnInit {
  gridData: { data: WorkOrder[]; total: number } = { data: [], total: 0 };
  pageSize = 20;
  skip = 0;
  sort = '';
  filter = '';
  activeStatus = '';
  showCreateDialog = false;
  isCalendarView = false;
  equipmentDropdownOpen = false;
  assigneeDropdownOpen = false;

  selectedWorkOrder: WorkOrder | null = null;
  selectedWorkOrderHistory: HistoryItem[] = [];
  showCompletionForm = false;
  completionNotes = '';

  // Kendo Scheduler properties
  schedulerEvents: SchedulerEvent[] = [];
  selectedDate: Date = new Date();
  schedulerViews: string[] = ['day', 'week', 'month'];

  statusTabs: StatusTab[] = [
    { label: 'All', value: '', count: 0, testId: 'tab-all' },
    { label: 'Open', value: 'Open', count: 0, testId: 'tab-open' },
    { label: 'In Progress', value: 'InProgress', count: 0, testId: 'tab-in-progress' },
    { label: 'On Hold', value: 'OnHold', count: 0, testId: 'tab-on-hold' },
    { label: 'Completed', value: 'Completed', count: 0, testId: 'tab-completed' },
    { label: 'Closed', value: 'Closed', count: 0, testId: 'tab-closed' }
  ];

  priorities = ['Low', 'Medium', 'High', 'Critical'];
  serviceTypes = ['Preventive', 'Corrective', 'Emergency'];
  equipmentList: { label: string; value: string }[] = [];
  technicians: string[] = ['John Smith', 'Jane Doe', 'Mike Johnson', 'Sarah Williams'];

  newWorkOrder: any = {
    equipmentItem: null,
    title: 'Preventive',
    description: '',
    priority: 'Medium',
    scheduledDateStr: '',
    assignedTo: ''
  };

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.isCalendarView = this.router.url.includes('/calendar');
    if (this.isCalendarView) {
      this.loadCalendarData();
    } else {
      this.loadData();
      this.loadEquipmentList();
    }
  }

  loadData(): void {
    const params: Record<string, string | number | boolean> = {
      skip: this.skip,
      take: this.pageSize
    };
    if (this.activeStatus) params['status'] = this.activeStatus;
    if (this.sort) params['sort'] = this.sort;
    if (this.filter) params['filter'] = this.filter;

    this.api.get<any>('/work-orders', params).subscribe({
      next: (res) => {
        const items = res.items || [];
        const total = res.pagination?.totalItems || items.length;
        this.gridData = {
          data: items,
          total: total
        };
        this.updateTabCounts({ data: items, totalCount: total } as any);
      },
      error: () => {
        this.gridData = { data: [], total: 0 };
      }
    });
  }

  loadEquipmentList(): void {
    this.api.get<any>('/equipment', { skip: 0, take: 100 }).subscribe({
      next: (res) => {
        this.equipmentList = (res.items || []).map((e: any) => ({
          label: `${e.make} ${e.model} (${e.serialNumber})`,
          value: e.id
        }));
      },
      error: () => { this.equipmentList = []; }
    });
  }

  loadCalendarData(): void {
    const start = this.getCalendarRangeStart().toISOString();
    const end = this.getCalendarRangeEnd().toISOString();

    this.api.get<any[]>('/work-orders/calendar', { start, end }).subscribe({
      next: (events) => {
        const items = Array.isArray(events) ? events : (events as any).items || [];
        this.schedulerEvents = items.map((e: any) => ({
          id: e.id,
          title: e.title,
          start: new Date(e.start),
          end: new Date(e.end),
          color: e.color
        } as SchedulerEvent));
      },
      error: () => { this.schedulerEvents = []; }
    });
  }

  getCalendarRangeStart(): Date {
    const d = new Date(this.selectedDate);
    d.setDate(1);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  getCalendarRangeEnd(): Date {
    const d = new Date(this.selectedDate);
    d.setMonth(d.getMonth() + 1, 0);
    d.setHours(23, 59, 59, 999);
    return d;
  }

  onSchedulerDateChange(event: DateChangeEvent): void {
    this.selectedDate = event.selectedDate;
    this.loadCalendarData();
  }

  onSchedulerEventClick(event: any): void {
    if (event?.event?.id) {
      this.api.get<any>(`/work-orders/${event.event.id}`).subscribe({
        next: (detail) => {
          this.selectedWorkOrder = detail;
          this.selectedWorkOrderHistory = (detail.history || []).map((h: any) => ({
            status: h.newStatus,
            user: h.changedBy?.displayName || h.changedBy || 'System',
            timestamp: h.changedAt
          }));
        }
      });
    }
  }

  updateTabCounts(res: ApiResponse<WorkOrder[]>): void {
    if (!this.activeStatus && res.data) {
      const data = res.data;
      this.statusTabs[0].count = res.totalCount || data.length;
      this.statusTabs[1].count = data.filter(w => w.status === 'Open').length;
      this.statusTabs[2].count = data.filter(w => w.status === 'InProgress').length;
      this.statusTabs[3].count = data.filter(w => w.status === 'OnHold').length;
      this.statusTabs[4].count = data.filter(w => w.status === 'Completed').length;
      this.statusTabs[5].count = data.filter(w => w.status === 'Closed').length;
    }
  }

  onDataStateChange(state: DataStateChangeEvent): void {
    this.skip = state.skip ?? 0;
    this.pageSize = state.take ?? 20;

    if (state.sort && state.sort.length > 0) {
      this.sort = state.sort.map(s => `${s.field}-${s.dir}`).join(',');
    } else {
      this.sort = '';
    }

    if (state.filter) {
      this.filter = JSON.stringify(state.filter);
    } else {
      this.filter = '';
    }

    this.loadData();
  }

  onTabChange(status: string): void {
    this.activeStatus = status;
    this.skip = 0;
    this.loadData();
  }

  onRowClick(item: WorkOrder): void {
    this.showCompletionForm = false;
    this.completionNotes = '';

    // Load full detail from the detail endpoint (includes history via EF Core Include)
    this.api.get<any>(`/work-orders/${item.id}`).subscribe({
      next: (detail) => {
        this.selectedWorkOrder = detail;
        this.selectedWorkOrderHistory = (detail.history || []).map((h: any) => ({
          status: h.newStatus,
          user: h.changedBy?.displayName || h.changedBy || 'System',
          timestamp: h.changedAt
        }));
      },
      error: () => {
        // Show the item without history on error — no fabricated data
        this.selectedWorkOrder = { ...item } as any;
        this.selectedWorkOrderHistory = [];
      }
    });
  }

  startWork(): void {
    if (!this.selectedWorkOrder) return;
    const id = (this.selectedWorkOrder as any).id;
    this.api.put<any>(`/work-orders/${id}/status`, { status: 'InProgress' }).subscribe({
      next: (updated) => {
        this.selectedWorkOrder = updated;
        this.selectedWorkOrderHistory.unshift({
          status: 'InProgress',
          user: 'Current User',
          timestamp: new Date().toISOString()
        });
        this.loadData();
      },
      error: () => {
        // Do NOT update UI on failure — no optimistic updates
      }
    });
  }

  confirmComplete(): void {
    if (!this.selectedWorkOrder) return;
    const id = (this.selectedWorkOrder as any).id;
    this.api.put<any>(`/work-orders/${id}/status`, { status: 'Completed', notes: this.completionNotes }).subscribe({
      next: (updated) => {
        this.selectedWorkOrder = updated;
        this.showCompletionForm = false;
        this.selectedWorkOrderHistory.unshift({
          status: 'Completed',
          user: 'Current User',
          timestamp: new Date().toISOString()
        });
        this.loadData();
      },
      error: () => {
        // Do NOT update UI on failure — no optimistic updates
      }
    });
  }

  selectEquipment(eq: { label: string; value: string }): void {
    this.newWorkOrder.equipmentItem = eq;
    this.equipmentDropdownOpen = false;
  }

  selectAssignee(tech: string): void {
    this.newWorkOrder.assignedTo = tech;
    this.assigneeDropdownOpen = false;
  }

  createWorkOrder(): void {
    const body = {
      equipmentId: this.newWorkOrder.equipmentItem?.value || '',
      serviceType: this.newWorkOrder.title,
      description: this.newWorkOrder.description,
      priority: this.newWorkOrder.priority,
      requestedDate: this.newWorkOrder.scheduledDateStr ? new Date(this.newWorkOrder.scheduledDateStr).toISOString() : new Date().toISOString(),
      assignedToUserId: this.newWorkOrder.assignedTo || null
    };

    this.api.post<any>('/work-orders', body).subscribe({
      next: () => {
        this.showCreateDialog = false;
        this.resetNewWorkOrder();
        this.loadData();
      },
      error: (err) => {
        console.error('Failed to create work order', err);
      }
    });
  }

  private resetNewWorkOrder(): void {
    this.newWorkOrder = {
      equipmentItem: null,
      title: 'Preventive',
      description: '',
      priority: 'Medium',
      scheduledDateStr: '',
      assignedTo: ''
    };
  }

  getPriorityVariant(priority: string): 'success' | 'warning' | 'error' | 'info' {
    switch (priority) {
      case 'Critical': return 'error';
      case 'High': return 'warning';
      case 'Medium': return 'info';
      case 'Low': return 'info';
      default: return 'info';
    }
  }

  getStatusVariant(status: string): 'success' | 'warning' | 'error' | 'info' {
    switch (status) {
      case 'Completed': return 'success';
      case 'InProgress': case 'In Progress': return 'warning';
      case 'Open': return 'info';
      case 'Cancelled': return 'error';
      default: return 'info';
    }
  }

  getPriorityColor(priority: string): string {
    switch (priority) {
      case 'Critical': return 'danger';
      case 'High': return 'warning';
      case 'Medium': return 'info';
      default: return 'secondary';
    }
  }
}

