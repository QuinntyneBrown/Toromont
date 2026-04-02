import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { GridModule, PageChangeEvent, SortSettings } from '@progress/kendo-angular-grid';
import { SortDescriptor } from '@progress/kendo-data-query';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { DialogsModule } from '@progress/kendo-angular-dialog';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DateInputsModule } from '@progress/kendo-angular-dateinputs';
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
    GridModule, DropDownsModule, InputsModule, DialogsModule, ButtonsModule, DateInputsModule,
    BadgeComponent
  ],
  template: `
    <div class="container-fluid py-3">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <h2 class="fw-bold mb-0">Equipment Registry</h2>
        <button class="btn btn-warning fw-semibold" (click)="showAddDialog = true">+ Add Equipment</button>
      </div>

      <div class="d-flex gap-2 mb-3 flex-wrap">
        <kendo-dropdownlist
          [data]="statuses"
          [value]="selectedStatus"
          (valueChange)="onStatusFilter($event)"
          [textField]="'text'"
          [valueField]="'value'"
          style="width:170px">
        </kendo-dropdownlist>
        <kendo-dropdownlist
          [data]="categories"
          [value]="selectedCategory"
          (valueChange)="onCategoryFilter($event)"
          [textField]="'text'"
          [valueField]="'value'"
          style="width:170px">
        </kendo-dropdownlist>
        <kendo-textbox
          [placeholder]="'Search by name or serial...'"
          [(value)]="searchText"
          (valueChange)="onSearchInput($event)"
          style="flex:1;min-width:200px">
        </kendo-textbox>
      </div>

      <!-- Desktop: Kendo Grid -->
      <div class="d-none d-md-block">
        <kendo-grid
          [data]="gridData"
          [pageSize]="pageSize"
          [skip]="skip"
          [pageable]="true"
          [sortable]="true"
          [sort]="sort"
          (pageChange)="onPageChange($event)"
          (sortChange)="onSortChange($event)"
          [selectable]="true"
          (selectionChange)="onRowSelect($event)"
          [style.width]="'100%'">
          <kendo-grid-column field="serialNumber" title="#" [width]="130"></kendo-grid-column>
          <kendo-grid-column field="name" title="Name" [width]="180">
            <ng-template kendoGridCellTemplate let-dataItem>
              <a class="text-decoration-none fw-semibold" [routerLink]="['/equipment', dataItem.id]">
                {{ dataItem.name }}
              </a>
            </ng-template>
          </kendo-grid-column>
          <kendo-grid-column field="model" title="Model" [width]="120"></kendo-grid-column>
          <kendo-grid-column field="serialNumber" title="Serial Number" [width]="150"></kendo-grid-column>
          <kendo-grid-column field="category" title="Category" [width]="130"></kendo-grid-column>
          <kendo-grid-column field="status" title="Status" [width]="130">
            <ng-template kendoGridCellTemplate let-dataItem>
              <app-badge [text]="dataItem.status" [variant]="getStatusVariant(dataItem.status)"></app-badge>
            </ng-template>
          </kendo-grid-column>
          <kendo-grid-column field="location" title="Location" [width]="160">
            <ng-template kendoGridCellTemplate let-dataItem>
              {{ dataItem.location || 'N/A' }}
            </ng-template>
          </kendo-grid-column>
          <kendo-grid-column field="lastServiceDate" title="Last Service" [width]="130">
            <ng-template kendoGridCellTemplate let-dataItem>
              {{ dataItem.lastServiceDate ? (dataItem.lastServiceDate | date:'mediumDate') : 'None' }}
            </ng-template>
          </kendo-grid-column>
        </kendo-grid>
      </div>

      <!-- Mobile: Card Layout -->
      <div class="d-md-none">
        <div *ngIf="gridData.data.length === 0" class="text-center py-5 text-muted">
          No equipment found.
        </div>
        <div *ngFor="let item of gridData.data" class="card mb-2 cursor-pointer" (click)="navigateToDetail(item.id)">
          <div class="card-body">
            <div class="d-flex justify-content-between align-items-start mb-2">
              <h6 class="mb-0 fw-semibold">{{ item.make }} {{ item.model }}</h6>
              <app-badge [text]="item.status" [variant]="getStatusVariant(item.status)"></app-badge>
            </div>
            <div class="text-muted small">
              <div><strong>Serial:</strong> {{ item.serialNumber }}</div>
              <div><strong>Category:</strong> {{ item.category }}</div>
              <div><strong>Location:</strong> {{ item.location || 'N/A' }}</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Add Equipment Dialog -->
      <kendo-dialog *ngIf="showAddDialog" title="Add Equipment" (close)="showAddDialog = false" [width]="500">
        <div class="mb-3">
          <label class="form-label fw-semibold">Make</label>
          <kendo-textbox [(value)]="newEquipment.make"></kendo-textbox>
        </div>
        <div class="mb-3">
          <label class="form-label fw-semibold">Model</label>
          <kendo-textbox [(value)]="newEquipment.model"></kendo-textbox>
        </div>
        <div class="mb-3">
          <label class="form-label fw-semibold">Serial Number</label>
          <kendo-textbox [(value)]="newEquipment.serialNumber"></kendo-textbox>
        </div>
        <div class="mb-3">
          <label class="form-label fw-semibold">Year</label>
          <kendo-numerictextbox [(value)]="newEquipment.year" [format]="'#'" [min]="1990" [max]="2030"></kendo-numerictextbox>
        </div>
        <div class="mb-3">
          <label class="form-label fw-semibold">Category</label>
          <kendo-dropdownlist
            [data]="categoryValues"
            [(value)]="newEquipment.category">
          </kendo-dropdownlist>
        </div>
        <kendo-dialog-actions>
          <button kendoButton (click)="showAddDialog = false">Cancel</button>
          <button kendoButton themeColor="primary" (click)="addEquipment()">Save</button>
        </kendo-dialog-actions>
      </kendo-dialog>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .cursor-pointer { cursor: pointer; }
    .card:hover { border-color: var(--accent-primary); }
  `]
})
export default class EquipmentListComponent implements OnInit {
  gridData: { data: any[]; total: number } = { data: [], total: 0 };
  pageSize = 20;
  skip = 0;
  sort: SortDescriptor[] = [];
  searchText = '';
  showAddDialog = false;

  statuses = [
    { text: 'All Statuses', value: '' },
    { text: 'Operational', value: 'Operational' },
    { text: 'Needs Service', value: 'NeedsService' },
    { text: 'Out of Service', value: 'OutOfService' },
    { text: 'Idle', value: 'Idle' }
  ];
  selectedStatus = this.statuses[0];

  categoryValues = ['Excavator', 'Loader', 'Dozer', 'Crane', 'Truck', 'Generator', 'Compressor', 'Other'];
  categories = [
    { text: 'All Categories', value: '' },
    ...['Excavator', 'Loader', 'Dozer', 'Crane', 'Truck', 'Generator', 'Compressor', 'Other'].map(c => ({ text: c, value: c }))
  ];
  selectedCategory = this.categories[0];

  newEquipment: any = { make: '', model: '', serialNumber: '', year: 2026, category: 'Excavator' };

  private searchSubject = new Subject<string>();

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(() => {
      this.skip = 0;
      this.loadData();
    });
    this.loadData();
  }

  loadData(): void {
    const params: Record<string, string | number | boolean> = {
      skip: this.skip,
      take: this.pageSize
    };
    if (this.selectedStatus.value) params['status'] = this.selectedStatus.value;
    if (this.selectedCategory.value) params['category'] = this.selectedCategory.value;
    if (this.searchText) params['search'] = this.searchText;

    this.api.get<any>('/equipment', params).subscribe({
      next: (res) => {
        this.gridData = {
          data: res.items || [],
          total: res.pagination?.totalItems || (res.items ? res.items.length : 0)
        };
      },
      error: () => {
        this.gridData = { data: [], total: 0 };
      }
    });
  }

  onPageChange(event: PageChangeEvent): void {
    this.skip = event.skip;
    this.loadData();
  }

  onSortChange(sort: SortDescriptor[]): void {
    this.sort = sort;
    this.loadData();
  }

  onStatusFilter(value: any): void {
    this.selectedStatus = value;
    this.skip = 0;
    this.loadData();
  }

  onCategoryFilter(value: any): void {
    this.selectedCategory = value;
    this.skip = 0;
    this.loadData();
  }

  onSearchInput(value: string): void {
    this.searchText = value;
    this.searchSubject.next(value);
  }

  onRowSelect(event: any): void {
    if (event.selectedRows?.length) {
      const item = event.selectedRows[0].dataItem;
      this.navigateToDetail(item.id);
    }
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

  addEquipment(): void {
    this.api.post<any>('/equipment', this.newEquipment).subscribe({
      next: () => {
        this.showAddDialog = false;
        this.newEquipment = { make: '', model: '', serialNumber: '', year: 2026, category: 'Excavator' };
        this.loadData();
      },
      error: (err) => {
        console.error('Failed to add equipment', err);
      }
    });
  }
}
