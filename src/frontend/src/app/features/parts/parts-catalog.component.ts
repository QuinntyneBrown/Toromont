import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { GridModule, PageChangeEvent } from '@progress/kendo-angular-grid';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ApiService } from '../../core/services/api.service';
import { ApiResponse } from '../../core/models';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

interface Part {
  id: string;
  partNumber: string;
  name: string;
  description: string;
  price: number;
  availability: 'InStock' | 'LowStock' | 'OutOfStock';
  category: string;
  compatibleModels: string;
}

@Component({
  selector: 'app-parts-catalog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterModule,
    GridModule, DropDownsModule, InputsModule, ButtonsModule,
    BadgeComponent
  ],
  template: `
    <div class="container-fluid py-3">
      <h2 class="fw-bold mb-3">Parts Catalog</h2>

      <div class="row">
        <!-- Filter Sidebar (desktop) -->
        <div class="col-lg-3 d-none d-lg-block">
          <div class="card">
            <div class="card-body">
              <h6 class="fw-bold mb-3">Filters</h6>

              <div class="mb-3">
                <label class="form-label fw-semibold small text-uppercase">Category</label>
                <div *ngFor="let cat of partCategories" class="form-check">
                  <input class="form-check-input" type="checkbox" [id]="'cat-' + cat"
                         [checked]="selectedCategories.has(cat)"
                         (change)="toggleCategory(cat)">
                  <label class="form-check-label" [for]="'cat-' + cat">{{ cat }}</label>
                </div>
              </div>

              <div class="mb-3">
                <label class="form-label fw-semibold small text-uppercase">Availability</label>
                <kendo-dropdownlist
                  [data]="availabilityOptions"
                  [value]="selectedAvailability"
                  (valueChange)="onAvailabilityChange($event)"
                  [textField]="'text'"
                  [valueField]="'value'"
                  style="width:100%">
                </kendo-dropdownlist>
              </div>

              <button class="btn btn-outline-secondary btn-sm w-100" (click)="clearFilters()">Clear Filters</button>
            </div>
          </div>
        </div>

        <!-- Mobile Filter Toggle -->
        <div class="col-12 d-lg-none mb-3">
          <button class="btn btn-outline-secondary w-100" (click)="showMobileFilters = !showMobileFilters">
            {{ showMobileFilters ? 'Hide Filters' : 'Show Filters' }}
          </button>
          <div *ngIf="showMobileFilters" class="card mt-2">
            <div class="card-body">
              <div class="mb-3">
                <label class="form-label fw-semibold small text-uppercase">Category</label>
                <div *ngFor="let cat of partCategories" class="form-check">
                  <input class="form-check-input" type="checkbox" [id]="'mcat-' + cat"
                         [checked]="selectedCategories.has(cat)"
                         (change)="toggleCategory(cat)">
                  <label class="form-check-label" [for]="'mcat-' + cat">{{ cat }}</label>
                </div>
              </div>
              <div class="mb-3">
                <label class="form-label fw-semibold small text-uppercase">Availability</label>
                <kendo-dropdownlist
                  [data]="availabilityOptions"
                  [value]="selectedAvailability"
                  (valueChange)="onAvailabilityChange($event)"
                  [textField]="'text'"
                  [valueField]="'value'"
                  style="width:100%">
                </kendo-dropdownlist>
              </div>
            </div>
          </div>
        </div>

        <!-- Main Content -->
        <div class="col-lg-9">
          <!-- AI Search Bar -->
          <div class="input-group mb-3">
            <span class="input-group-text bg-white">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#f59e0b" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon>
              </svg>
            </span>
            <input type="text" class="form-control" placeholder="Search parts in plain English..."
                   [(ngModel)]="aiSearchQuery" (keyup.enter)="onAISearch()">
            <button class="btn btn-warning" (click)="onAISearch()">Search</button>
          </div>

          <!-- Regular Search -->
          <div class="mb-3">
            <kendo-textbox
              [placeholder]="'Filter by part number or name...'"
              [(value)]="searchText"
              (valueChange)="onSearchInput($event)"
              style="width:100%">
            </kendo-textbox>
          </div>

          <!-- Grid -->
          <kendo-grid
            [data]="gridData"
            [pageSize]="pageSize"
            [skip]="skip"
            [pageable]="true"
            [sortable]="true"
            (pageChange)="onPageChange($event)"
            [style.width]="'100%'">
            <kendo-grid-column field="partNumber" title="Part #" [width]="120"></kendo-grid-column>
            <kendo-grid-column field="name" title="Name" [width]="180"></kendo-grid-column>
            <kendo-grid-column field="description" title="Description" [width]="220">
              <ng-template kendoGridCellTemplate let-dataItem>
                {{ dataItem.description | slice:0:60 }}{{ dataItem.description?.length > 60 ? '...' : '' }}
              </ng-template>
            </kendo-grid-column>
            <kendo-grid-column field="price" title="Price" [width]="100">
              <ng-template kendoGridCellTemplate let-dataItem>
                {{ dataItem.price | currency:'USD':'symbol':'1.2-2' }}
              </ng-template>
            </kendo-grid-column>
            <kendo-grid-column field="availability" title="Availability" [width]="120">
              <ng-template kendoGridCellTemplate let-dataItem>
                <app-badge [text]="getAvailabilityLabel(dataItem.availability)" [variant]="getAvailabilityVariant(dataItem.availability)"></app-badge>
              </ng-template>
            </kendo-grid-column>
            <kendo-grid-column field="compatibleModels" title="Compatible Models" [width]="160">
              <ng-template kendoGridCellTemplate let-dataItem>
                {{ dataItem.compatibleModels || 'Universal' }}
              </ng-template>
            </kendo-grid-column>
            <kendo-grid-column title="" [width]="130">
              <ng-template kendoGridCellTemplate let-dataItem>
                <button class="btn btn-sm btn-warning fw-semibold"
                        [disabled]="dataItem.availability === 'OutOfStock'"
                        (click)="addToCart(dataItem, $event)">
                  Add to Cart
                </button>
              </ng-template>
            </kendo-grid-column>
          </kendo-grid>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .form-check { margin-bottom: 6px; }
    .parts-layout { display: flex; }
    .filter-panel {
      width: 260px;
      flex-shrink: 0;
      background: var(--surface-secondary);
      border-right: 1px solid var(--border-subtle);
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 20px;
    }
    .filter-title { font-size: 16px; font-weight: 600; color: var(--foreground-primary); margin: 0; }
    .main-content { flex: 1; padding: 24px; display: flex; flex-direction: column; gap: 20px; }
    .ai-search {
      display: flex;
      align-items: center;
      gap: 12px;
      height: 48px;
      background: var(--surface-secondary);
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-md);
      padding: 0 16px;
    }
    .ai-search input {
      border: none;
      background: none;
      outline: none;
      flex: 1;
      font-size: 14px;
      color: var(--foreground-primary);
    }
    .ai-search input::placeholder { color: var(--foreground-disabled); }
    @media (max-width: 992px) {
      .filter-panel { display: none; }
      .main-content { padding: 16px; }
    }
  `]
})
export default class PartsCatalogComponent implements OnInit {
  gridData: { data: Part[]; total: number } = { data: [], total: 0 };
  pageSize = 20;
  skip = 0;
  searchText = '';
  aiSearchQuery = '';
  showMobileFilters = false;

  partCategories = ['Filters', 'Hydraulics', 'Electrical', 'Engine', 'Transmission', 'Undercarriage', 'Cab', 'Other'];
  selectedCategories = new Set<string>();

  availabilityOptions = [
    { text: 'All', value: '' },
    { text: 'In Stock', value: 'InStock' },
    { text: 'Low Stock', value: 'LowStock' },
    { text: 'Out of Stock', value: 'OutOfStock' }
  ];
  selectedAvailability = this.availabilityOptions[0];

  private searchSubject = new Subject<string>();

  constructor(private api: ApiService) {}

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
    if (this.selectedAvailability.value) params['availability'] = this.selectedAvailability.value;
    if (this.searchText) params['search'] = this.searchText;
    const cats = Array.from(this.selectedCategories);
    if (cats.length === 1) params['category'] = cats[0];

    this.api.get<any>('/parts', params).subscribe({
      next: (res) => {
        let data = res.items || [];
        // Client-side multi-category filter if needed
        if (this.selectedCategories.size > 1) {
          data = data.filter((p: any) => this.selectedCategories.has(p.category));
        }
        this.gridData = {
          data,
          total: res.pagination?.totalItems || data.length
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

  onSearchInput(value: string): void {
    this.searchText = value;
    this.searchSubject.next(value);
  }

  onAISearch(): void {
    if (!this.aiSearchQuery.trim()) return;
    this.api.get<any[]>('/parts/search', { q: this.aiSearchQuery }).subscribe({
      next: (res) => {
        const data = Array.isArray(res) ? res : [];
        this.gridData = {
          data,
          total: data.length
        };
      },
      error: () => {
        this.gridData = { data: [], total: 0 };
      }
    });
  }

  toggleCategory(cat: string): void {
    if (this.selectedCategories.has(cat)) {
      this.selectedCategories.delete(cat);
    } else {
      this.selectedCategories.add(cat);
    }
    this.skip = 0;
    this.loadData();
  }

  onAvailabilityChange(value: any): void {
    this.selectedAvailability = value;
    this.skip = 0;
    this.loadData();
  }

  clearFilters(): void {
    this.selectedCategories.clear();
    this.selectedAvailability = this.availabilityOptions[0];
    this.searchText = '';
    this.aiSearchQuery = '';
    this.skip = 0;
    this.loadData();
  }

  addToCart(part: Part, event: Event): void {
    event.stopPropagation();
    this.api.post<any>('/cart/items', {
      partId: part.id,
      partNumber: part.partNumber,
      description: part.name,
      quantity: 1,
      unitPrice: part.price
    }).subscribe({
      next: () => {
        // Could show a toast notification here
        console.log('Added to cart:', part.partNumber);
      },
      error: (err) => {
        console.error('Failed to add to cart', err);
      }
    });
  }

  getAvailabilityLabel(availability: string): string {
    switch (availability) {
      case 'InStock': return 'In Stock';
      case 'LowStock': return 'Low Stock';
      case 'OutOfStock': return 'Out of Stock';
      default: return availability;
    }
  }

  getAvailabilityVariant(availability: string): 'success' | 'warning' | 'error' | 'info' {
    switch (availability) {
      case 'InStock': return 'success';
      case 'LowStock': return 'warning';
      case 'OutOfStock': return 'error';
      default: return 'info';
    }
  }
}
