import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ApiService } from '../../core/services/api.service';
import { ApiResponse, Equipment } from '../../core/models';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-equipment-list',
  standalone: true,
  imports: [
    CommonModule, RouterModule, FormsModule,
    BadgeComponent
  ],
  template: `
    <div class="container-fluid py-3">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <h2 data-testid="equipment-title" class="fw-bold mb-0">Equipment Registry</h2>
        <button data-testid="add-equipment-btn" class="btn btn-warning fw-semibold" (click)="openAddDialog()">+ Add Equipment</button>
      </div>

      <div class="d-flex gap-2 mb-3 flex-wrap">
        <div data-testid="status-filter" class="position-relative" style="width:170px">
          <select class="form-select" [ngModel]="selectedStatus" (ngModelChange)="onStatusFilter($event)">
            <option *ngFor="let s of statuses" [value]="s.value" [attr.data-testid]="'status-option-' + s.value">{{ s.text }}</option>
          </select>
        </div>
        <div data-testid="category-filter" class="position-relative" style="width:170px">
          <select class="form-select" [ngModel]="selectedCategory" (ngModelChange)="onCategoryFilter($event)">
            <option *ngFor="let c of categories" [value]="c.value" [attr.data-testid]="'category-option-' + c.value">{{ c.text }}</option>
          </select>
        </div>
        <input
          data-testid="equipment-search"
          type="text"
          class="form-control"
          placeholder="Search by name or serial..."
          [(ngModel)]="searchText"
          (ngModelChange)="onSearchInput($event)"
          (keydown.enter)="onSearchEnter()"
          style="flex:1;min-width:200px"
        />
      </div>

      <!-- Mobile Status Chips -->
      <div class="d-md-none mb-3">
        <div class="d-flex gap-2 flex-wrap">
          <span data-testid="status-chip">
            <button
              data-testid="status-chip-all"
              class="btn btn-sm"
              [class.btn-warning]="mobileStatusFilter === ''"
              [class.btn-outline-secondary]="mobileStatusFilter !== ''"
              (click)="onMobileStatusChip('')">All</button>
          </span>
          <span data-testid="status-chip">
            <button
              data-testid="status-chip-active"
              class="btn btn-sm"
              [class.btn-success]="mobileStatusFilter === 'Operational'"
              [class.btn-outline-secondary]="mobileStatusFilter !== 'Operational'"
              (click)="onMobileStatusChip('Operational')">Active</button>
          </span>
          <span data-testid="status-chip">
            <button
              data-testid="status-chip-maintenance"
              class="btn btn-sm"
              [class.btn-warning]="mobileStatusFilter === 'NeedsService'"
              [class.btn-outline-secondary]="mobileStatusFilter !== 'NeedsService'"
              (click)="onMobileStatusChip('NeedsService')">Maintenance</button>
          </span>
          <span data-testid="status-chip">
            <button
              data-testid="status-chip-idle"
              class="btn btn-sm"
              [class.btn-info]="mobileStatusFilter === 'Idle'"
              [class.btn-outline-secondary]="mobileStatusFilter !== 'Idle'"
              (click)="onMobileStatusChip('Idle')">Idle</button>
          </span>
        </div>
      </div>

      <!-- Desktop: HTML Table Grid -->
      <div data-testid="equipment-grid" class="d-none d-md-block">
        <table class="table table-hover table-bordered align-middle w-100">
          <thead data-testid="grid-header">
            <tr>
              <th data-testid="grid-header-name" class="sortable-header" (click)="toggleSort('name')" style="width:220px;cursor:pointer;">
                Name <span *ngIf="sortField === 'name'">{{ sortDir === 'asc' ? '▲' : '▼' }}</span>
              </th>
              <th data-testid="grid-header-model" class="sortable-header" (click)="toggleSort('model')" style="width:120px;cursor:pointer;">
                Model <span *ngIf="sortField === 'model'">{{ sortDir === 'asc' ? '▲' : '▼' }}</span>
              </th>
              <th data-testid="grid-header-serial" class="sortable-header" (click)="toggleSort('serialNumber')" style="width:150px;cursor:pointer;">
                Serial Number <span *ngIf="sortField === 'serialNumber'">{{ sortDir === 'asc' ? '▲' : '▼' }}</span>
              </th>
              <th data-testid="grid-header-category" class="sortable-header" (click)="toggleSort('category')" style="width:130px;cursor:pointer;">
                Category <span *ngIf="sortField === 'category'">{{ sortDir === 'asc' ? '▲' : '▼' }}</span>
              </th>
              <th data-testid="grid-header-status" class="sortable-header" (click)="toggleSort('status')" style="width:130px;cursor:pointer;">
                Status <span *ngIf="sortField === 'status'">{{ sortDir === 'asc' ? '▲' : '▼' }}</span>
              </th>
              <th data-testid="grid-header-location" class="sortable-header" (click)="toggleSort('location')" style="width:160px;cursor:pointer;">
                Location <span *ngIf="sortField === 'location'">{{ sortDir === 'asc' ? '▲' : '▼' }}</span>
              </th>
              <th data-testid="grid-header-lastService" style="width:130px;">
                Last Service
              </th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let item of pagedData" data-testid="equipment-row" class="cursor-pointer" (click)="navigateToDetail(item.id)">
              <td data-testid="cell-name">
                <a class="text-decoration-none fw-semibold" [routerLink]="['/equipment', item.id]">{{ item.name }}</a>
              </td>
              <td data-testid="cell-model">{{ item.model }}</td>
              <td data-testid="cell-serial">{{ item.serialNumber }}</td>
              <td data-testid="cell-category">{{ item.category }}</td>
              <td data-testid="cell-status">
                <app-badge [text]="item.status" [variant]="getStatusVariant(item.status)"></app-badge>
              </td>
              <td data-testid="cell-location">{{ item.location || 'N/A' }}</td>
              <td data-testid="cell-lastService">{{ item.lastServiceDate ? (item.lastServiceDate | date:'mediumDate') : 'None' }}</td>
            </tr>
          </tbody>
        </table>

        <!-- Pagination -->
        <div data-testid="pagination" class="d-flex justify-content-between align-items-center mt-2" *ngIf="gridData.total > 0">
          <span data-testid="pagination-info">Showing {{ pagedData.length }} of {{ gridData.total }}</span>
          <div class="d-flex gap-1">
            <button
              *ngFor="let p of pageNumbers"
              data-testid="pagination-btn"
              [attr.data-testid]="'pagination-page-' + p"
              class="btn btn-sm"
              [class.btn-primary]="p === currentPage"
              [class.btn-outline-secondary]="p !== currentPage"
              (click)="goToPage(p)">
              <span [attr.data-testid]="p === currentPage ? 'pagination-current' : null">{{ p }}</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Mobile: Card Layout -->
      <div class="d-md-none">
        <div *ngIf="gridData.data.length === 0" class="text-center py-5 text-muted">
          No equipment found.
        </div>
        <div *ngFor="let item of gridData.data" data-testid="equipment-card" class="card mb-2 cursor-pointer" (click)="navigateToDetail(item.id)">
          <div class="card-body">
            <div class="d-flex justify-content-between align-items-start mb-2">
              <h6 data-testid="card-name" class="mb-0 fw-semibold">{{ item.name || (item.make + ' ' + item.model) }}</h6>
              <span data-testid="card-status"><app-badge [text]="item.status" [variant]="getStatusVariant(item.status)"></app-badge></span>
            </div>
            <div class="text-muted small">
              <div><strong>Serial:</strong> <span data-testid="card-serial">{{ item.serialNumber }}</span></div>
              <div><strong>Hours:</strong> <span data-testid="card-hours">{{ item.engineHours || 'N/A' }}</span></div>
              <div><strong>Location:</strong> <span data-testid="card-location">{{ item.location || 'N/A' }}</span></div>
            </div>
          </div>
        </div>
      </div>

      <!-- Add Equipment Dialog -->
      <div *ngIf="showAddDialog" class="modal-backdrop fade show" (click)="showAddDialog = false"></div>
      <div *ngIf="showAddDialog" class="modal d-block" tabindex="-1">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Add Equipment</h5>
              <button type="button" class="btn-close" (click)="showAddDialog = false"></button>
            </div>
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label fw-semibold">Name</label>
                <input data-testid="input-name" type="text" class="form-control" [(ngModel)]="newEquipment.name" />
                <div *ngIf="formSubmitted && !newEquipment.name" data-testid="name-error" class="text-danger small mt-1">Name is required</div>
              </div>
              <div class="mb-3">
                <label class="form-label fw-semibold">Make</label>
                <input data-testid="input-make" type="text" class="form-control" [(ngModel)]="newEquipment.make" />
              </div>
              <div class="mb-3">
                <label class="form-label fw-semibold">Model</label>
                <input data-testid="input-model" type="text" class="form-control" [(ngModel)]="newEquipment.model" />
              </div>
              <div class="mb-3">
                <label class="form-label fw-semibold">Year</label>
                <input data-testid="input-year" type="number" class="form-control" [(ngModel)]="newEquipment.year" min="1990" max="2030" />
              </div>
              <div class="mb-3">
                <label class="form-label fw-semibold">Serial Number</label>
                <input data-testid="input-serial" type="text" class="form-control" [(ngModel)]="newEquipment.serialNumber" />
                <div *ngIf="formSubmitted && !newEquipment.serialNumber" data-testid="serial-error" class="text-danger small mt-1">Serial number is required</div>
                <div *ngIf="serialError" data-testid="serial-error" class="text-danger small mt-1">{{ serialError }}</div>
              </div>
              <div class="mb-3">
                <label class="form-label fw-semibold">Category</label>
                <select data-testid="select-category" class="form-select" [(ngModel)]="newEquipment.category">
                  <option *ngFor="let cat of categoryValues" [value]="cat">{{ cat }}</option>
                </select>
              </div>
              <div class="mb-3">
                <label class="form-label fw-semibold">Status</label>
                <select data-testid="select-status" class="form-select" [(ngModel)]="newEquipment.status">
                  <option value="Operational">Operational</option>
                  <option value="NeedsService">Needs Service</option>
                  <option value="OutOfService">Out of Service</option>
                  <option value="Idle">Idle</option>
                </select>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="showAddDialog = false">Cancel</button>
              <button data-testid="submit-equipment" type="button" class="btn btn-primary" (click)="addEquipment()">Save</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .cursor-pointer { cursor: pointer; }
    .card:hover { border-color: var(--accent-primary); }
    .sortable-header:hover { background: var(--surface-secondary, #f5f5f5); }
    .modal-backdrop { z-index: 1040; }
    .modal { z-index: 1050; }
  `]
})
export default class EquipmentListComponent implements OnInit {
  gridData: { data: any[]; total: number } = { data: [], total: 0 };
  pagedData: any[] = [];
  pageSize = 20;
  skip = 0;
  currentPage = 1;
  pageNumbers: number[] = [];
  sortField = '';
  sortDir: 'asc' | 'desc' = 'asc';
  searchText = '';
  showAddDialog = false;
  formSubmitted = false;
  serialError = '';
  mobileStatusFilter = '';

  statuses = [
    { text: 'All Statuses', value: '' },
    { text: 'Operational', value: 'Operational' },
    { text: 'Needs Service', value: 'NeedsService' },
    { text: 'Out of Service', value: 'OutOfService' },
    { text: 'Idle', value: 'Idle' }
  ];
  selectedStatus = '';

  categoryValues = ['Excavator', 'Loader', 'Dozer', 'Crane', 'Truck', 'Generator', 'Compressor', 'Other'];
  categories = [
    { text: 'All Categories', value: '' },
    ...['Excavator', 'Loader', 'Dozer', 'Crane', 'Truck', 'Generator', 'Compressor', 'Other'].map(c => ({ text: c, value: c }))
  ];
  selectedCategory = '';

  newEquipment: any = { name: '', make: '', model: '', serialNumber: '', year: 2026, category: 'Excavator', status: 'Operational' };

  private allData: any[] = [];
  private searchSubject = new Subject<string>();

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(() => {
      this.skip = 0;
      this.currentPage = 1;
      this.loadData();
    });
    this.loadData();
  }

  loadData(): void {
    const params: Record<string, string | number | boolean> = {
      skip: 0,
      take: 1000
    };
    if (this.selectedStatus) params['status'] = this.selectedStatus;
    if (this.selectedCategory) params['category'] = this.selectedCategory;
    if (this.searchText) params['search'] = this.searchText;

    this.api.get<any>('/equipment', params).subscribe({
      next: (res) => {
        this.allData = res.items || [];
        this.gridData = {
          data: this.allData,
          total: res.pagination?.totalItems || this.allData.length
        };
        this.applySortAndPage();
      },
      error: () => {
        this.allData = [];
        this.gridData = { data: [], total: 0 };
        this.pagedData = [];
        this.pageNumbers = [];
      }
    });
  }

  applySortAndPage(): void {
    let data = [...this.allData];

    // Apply client-side sort
    if (this.sortField) {
      data.sort((a, b) => {
        const aVal = (a[this.sortField] || '').toString().toLowerCase();
        const bVal = (b[this.sortField] || '').toString().toLowerCase();
        const cmp = aVal.localeCompare(bVal);
        return this.sortDir === 'asc' ? cmp : -cmp;
      });
    }

    this.gridData = { data: data, total: data.length };

    // Apply pagination
    const start = (this.currentPage - 1) * this.pageSize;
    this.pagedData = data.slice(start, start + this.pageSize);

    // Compute page numbers
    const totalPages = Math.ceil(data.length / this.pageSize);
    this.pageNumbers = [];
    for (let i = 1; i <= totalPages; i++) {
      this.pageNumbers.push(i);
    }
  }

  toggleSort(field: string): void {
    if (this.sortField === field) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDir = 'asc';
    }
    this.applySortAndPage();
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.skip = (page - 1) * this.pageSize;
    this.applySortAndPage();
  }

  onStatusFilter(value: string): void {
    this.selectedStatus = value;
    this.skip = 0;
    this.currentPage = 1;
    this.loadData();
  }

  onCategoryFilter(value: string): void {
    this.selectedCategory = value;
    this.skip = 0;
    this.currentPage = 1;
    this.loadData();
  }

  onSearchInput(value: string): void {
    this.searchText = value;
    this.searchSubject.next(value);
  }

  onSearchEnter(): void {
    this.skip = 0;
    this.currentPage = 1;
    this.loadData();
  }

  onMobileStatusChip(status: string): void {
    this.mobileStatusFilter = status;
    this.selectedStatus = status;
    this.skip = 0;
    this.currentPage = 1;
    this.loadData();
  }

  navigateToDetail(id: string): void {
    this.router.navigate(['/equipment', id]);
  }

  getStatusVariant(status: string): 'success' | 'warning' | 'error' | 'info' {
    switch (status) {
      case 'Operational': return 'success';
      case 'NeedsService': return 'warning';
      case 'OutOfService': return 'error';
      case 'Idle': return 'info';
      default: return 'info';
    }
  }

  openAddDialog(): void {
    this.formSubmitted = false;
    this.serialError = '';
    this.newEquipment = { name: '', make: '', model: '', serialNumber: '', year: 2026, category: 'Excavator', status: 'Operational' };
    this.showAddDialog = true;
  }

  addEquipment(): void {
    this.formSubmitted = true;
    this.serialError = '';

    if (!this.newEquipment.name || !this.newEquipment.serialNumber) {
      return;
    }

    this.api.post<any>('/equipment', this.newEquipment).subscribe({
      next: (res) => {
        this.showAddDialog = false;
        const newId = res?.id || res?.equipmentId;
        if (newId) {
          this.router.navigate(['/equipment', newId]);
        } else {
          this.loadData();
        }
      },
      error: (err) => {
        const msg = err?.error?.message || err?.error?.error || '';
        if (msg.toLowerCase().includes('serial')) {
          this.serialError = 'Serial number already exists';
        } else if (err?.status === 409) {
          this.serialError = 'Serial number already exists';
        } else {
          console.error('Failed to add equipment', err);
        }
      }
    });
  }
}
