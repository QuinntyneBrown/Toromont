import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { GridModule, PageChangeEvent } from '@progress/kendo-angular-grid';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { DateInputsModule } from '@progress/kendo-angular-dateinputs';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogsModule } from '@progress/kendo-angular-dialog';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ApiService } from '../../core/services/api.service';
import { ApiResponse, WorkOrder } from '../../core/models';

interface StatusTab {
  label: string;
  value: string;
  count: number;
}

@Component({
  selector: 'app-work-orders',
  standalone: true,
  imports: [
    CommonModule, RouterModule, FormsModule,
    GridModule, DropDownsModule, DateInputsModule, InputsModule, ButtonsModule, DialogsModule,
    BadgeComponent
  ],
  template: `
    <div class="container-fluid py-3">
      <!-- Header -->
      <div class="d-flex justify-content-between align-items-center mb-3">
        <h2 class="fw-bold mb-0" style="font-size:22px;">Service Management</h2>
        <button class="btn btn-warning fw-semibold" (click)="showCreateDialog = true">+ Create Work Order</button>
      </div>

      <!-- Status Tabs -->
      <ul class="nav nav-tabs mb-3">
        <li class="nav-item" *ngFor="let tab of statusTabs">
          <a class="nav-link" [class.active]="activeStatus === tab.value" (click)="onTabChange(tab.value)" role="button">
            {{ tab.label }}
            <span class="badge bg-secondary ms-1" *ngIf="tab.count > 0">{{ tab.count }}</span>
          </a>
        </li>
      </ul>

      <!-- Grid -->
      <kendo-grid
        [data]="gridData"
        [pageSize]="pageSize"
        [skip]="skip"
        [pageable]="true"
        [sortable]="true"
        (pageChange)="onPageChange($event)"
        [selectable]="true"
        (selectionChange)="onRowSelect($event)"
        [style.width]="'100%'">
        <kendo-grid-column field="id" title="WO #" [width]="110">
          <ng-template kendoGridCellTemplate let-dataItem>
            <span class="fw-semibold">{{ dataItem.id | slice:0:8 }}</span>
          </ng-template>
        </kendo-grid-column>
        <kendo-grid-column field="title" title="Title" [width]="200"></kendo-grid-column>
        <kendo-grid-column field="equipmentId" title="Equipment" [width]="140">
          <ng-template kendoGridCellTemplate let-dataItem>
            <a [routerLink]="['/equipment', dataItem.equipmentId]" class="text-decoration-none">
              {{ dataItem.equipmentId | slice:0:8 }}
            </a>
          </ng-template>
        </kendo-grid-column>
        <kendo-grid-column field="description" title="Service Type" [width]="180">
          <ng-template kendoGridCellTemplate let-dataItem>
            {{ dataItem.description | slice:0:50 }}{{ dataItem.description?.length > 50 ? '...' : '' }}
          </ng-template>
        </kendo-grid-column>
        <kendo-grid-column field="priority" title="Priority" [width]="110">
          <ng-template kendoGridCellTemplate let-dataItem>
            <app-badge [text]="dataItem.priority" [variant]="getPriorityVariant(dataItem.priority)"></app-badge>
          </ng-template>
        </kendo-grid-column>
        <kendo-grid-column field="status" title="Status" [width]="120">
          <ng-template kendoGridCellTemplate let-dataItem>
            <app-badge [text]="dataItem.status" [variant]="getStatusVariant(dataItem.status)"></app-badge>
          </ng-template>
        </kendo-grid-column>
        <kendo-grid-column field="scheduledDate" title="Scheduled" [width]="130">
          <ng-template kendoGridCellTemplate let-dataItem>
            {{ dataItem.scheduledDate ? (dataItem.scheduledDate | date:'mediumDate') : 'Unscheduled' }}
          </ng-template>
        </kendo-grid-column>
        <kendo-grid-column field="assignedTo" title="Assigned To" [width]="130">
          <ng-template kendoGridCellTemplate let-dataItem>
            {{ dataItem.assignedTo || 'Unassigned' }}
          </ng-template>
        </kendo-grid-column>
      </kendo-grid>

      <!-- Create Work Order Dialog -->
      <kendo-dialog *ngIf="showCreateDialog" title="Create Work Order" (close)="showCreateDialog = false" [width]="550">
        <div class="mb-3">
          <label class="form-label fw-semibold">Equipment</label>
          <kendo-dropdownlist
            [data]="equipmentList"
            [textField]="'label'"
            [valueField]="'value'"
            [(value)]="newWorkOrder.equipmentItem"
            [filterable]="true"
            (filterChange)="onEquipmentFilter($event)">
          </kendo-dropdownlist>
        </div>
        <div class="mb-3">
          <label class="form-label fw-semibold">Title</label>
          <kendo-textbox [(value)]="newWorkOrder.title"></kendo-textbox>
        </div>
        <div class="mb-3">
          <label class="form-label fw-semibold">Service Type / Description</label>
          <kendo-textarea [(value)]="newWorkOrder.description" [rows]="3"></kendo-textarea>
        </div>
        <div class="row mb-3">
          <div class="col-6">
            <label class="form-label fw-semibold">Priority</label>
            <kendo-dropdownlist
              [data]="priorities"
              [(value)]="newWorkOrder.priority">
            </kendo-dropdownlist>
          </div>
          <div class="col-6">
            <label class="form-label fw-semibold">Scheduled Date</label>
            <kendo-datepicker [(value)]="newWorkOrder.scheduledDate" [format]="'yyyy-MM-dd'"></kendo-datepicker>
          </div>
        </div>
        <div class="mb-3">
          <label class="form-label fw-semibold">Assigned Technician</label>
          <kendo-textbox [(value)]="newWorkOrder.assignedTo" [placeholder]="'Enter technician name'"></kendo-textbox>
        </div>
        <kendo-dialog-actions>
          <button kendoButton (click)="showCreateDialog = false">Cancel</button>
          <button kendoButton themeColor="primary" (click)="createWorkOrder()">Create</button>
        </kendo-dialog-actions>
      </kendo-dialog>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .nav-tabs { border-bottom-color: var(--border-subtle); }
    .nav-link {
      cursor: pointer;
      font-size: 14px;
      color: var(--foreground-secondary);
      border: none;
      padding: 10px 20px;
    }
    .nav-link:hover { color: var(--foreground-primary); background: none; border: none; }
    .nav-link.active {
      font-weight: 600;
      color: var(--accent-primary);
      background: none;
      border: none;
      border-bottom: 2px solid var(--accent-primary);
    }
    .nav-link .badge { font-size: 11px; }
  `]
})
export default class WorkOrdersComponent implements OnInit {
  gridData: { data: WorkOrder[]; total: number } = { data: [], total: 0 };
  pageSize = 20;
  skip = 0;
  activeStatus = '';
  showCreateDialog = false;

  statusTabs: StatusTab[] = [
    { label: 'All', value: '', count: 0 },
    { label: 'Open', value: 'Open', count: 0 },
    { label: 'In Progress', value: 'InProgress', count: 0 },
    { label: 'On Hold', value: 'OnHold', count: 0 },
    { label: 'Completed', value: 'Completed', count: 0 },
    { label: 'Closed', value: 'Closed', count: 0 }
  ];

  priorities = ['Low', 'Medium', 'High', 'Critical'];
  equipmentList: { label: string; value: string }[] = [];

  newWorkOrder: any = {
    equipmentItem: null,
    title: '',
    description: '',
    priority: 'Medium',
    scheduledDate: null,
    assignedTo: ''
  };

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.loadData();
    this.loadEquipmentList();
  }

  loadData(): void {
    const params: Record<string, string | number | boolean> = {
      skip: this.skip,
      take: this.pageSize
    };
    if (this.activeStatus) params['status'] = this.activeStatus;

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

  updateTabCounts(res: ApiResponse<WorkOrder[]>): void {
    if (!this.activeStatus && res.data) {
      const data = res.data;
      this.statusTabs[0].count = res.totalCount || data.length;
      this.statusTabs[1].count = data.filter(w => w.status === 'Open').length;
      this.statusTabs[2].count = data.filter(w => w.status === 'InProgress').length;
      this.statusTabs[3].count = data.filter(w => w.status === 'Completed').length;
      this.statusTabs[4].count = data.filter(w => w.status === 'Cancelled').length;
    }
  }

  onTabChange(status: string): void {
    this.activeStatus = status;
    this.skip = 0;
    this.loadData();
  }

  onPageChange(event: PageChangeEvent): void {
    this.skip = event.skip;
    this.loadData();
  }

  onRowSelect(event: any): void {
    if (event.selectedRows?.length) {
      const item = event.selectedRows[0].dataItem;
      // Could navigate to detail view; for now just log
      console.log('Selected work order:', item.id);
    }
  }

  onEquipmentFilter(filter: string): void {
    // Filter handled client-side on already loaded equipment list
  }

  createWorkOrder(): void {
    const body = {
      equipmentId: this.newWorkOrder.equipmentItem?.value || '',
      title: this.newWorkOrder.title,
      description: this.newWorkOrder.description,
      priority: this.newWorkOrder.priority,
      scheduledDate: this.newWorkOrder.scheduledDate ? this.newWorkOrder.scheduledDate.toISOString() : null,
      assignedTo: this.newWorkOrder.assignedTo || null
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
      title: '',
      description: '',
      priority: 'Medium',
      scheduledDate: null,
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
      case 'InProgress': return 'warning';
      case 'Open': return 'info';
      case 'Cancelled': return 'error';
      default: return 'info';
    }
  }
}
