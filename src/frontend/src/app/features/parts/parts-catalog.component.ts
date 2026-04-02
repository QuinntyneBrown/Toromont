import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
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
    DropDownsModule, InputsModule, ButtonsModule,
    BadgeComponent
  ],
  template: `
    <div class="container-fluid py-3">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <h2 class="fw-bold mb-0">Parts Catalog</h2>
        <a routerLink="/parts/cart" data-testid="cart-icon" class="btn btn-outline-secondary position-relative">
          &#128722; Cart
          <span data-testid="cart-badge"
                class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-warning text-dark">
            {{ cartCount }}
          </span>
        </a>
      </div>

      <div class="row">
        <!-- Filter Sidebar (desktop) -->
        <div class="col-lg-3 d-none d-lg-block" data-testid="filter-sidebar">
          <div class="card">
            <div class="card-body">
              <h6 class="fw-bold mb-3">Filters</h6>

              <div class="mb-3">
                <label class="form-label fw-semibold small text-uppercase">Category</label>
                <div *ngFor="let cat of partCategories" class="form-check"
                     data-testid="category-filter-item"
                     [attr.data-testid-extra]="'category-' + cat">
                  <input class="form-check-input" type="checkbox" [id]="'cat-' + cat"
                         [checked]="selectedCategories.has(cat)"
                         (change)="toggleCategory(cat)"
                         [attr.data-testid]="'category-' + cat">
                  <label class="form-check-label" [for]="'cat-' + cat">{{ cat }}</label>
                </div>
              </div>

              <div class="mb-3">
                <label class="form-label fw-semibold small text-uppercase">Availability</label>
                <kendo-dropdownlist
                  data-testid="compatibility-filter"
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
          <button class="btn btn-outline-secondary w-100" data-testid="filter-toggle" (click)="showMobileFilters = !showMobileFilters">
            {{ showMobileFilters ? 'Hide Filters' : 'Show Filters' }}
          </button>
          <div *ngIf="showMobileFilters" class="card mt-2">
            <div class="card-body">
              <div class="mb-3">
                <label class="form-label fw-semibold small text-uppercase">Category</label>
                <div *ngFor="let cat of partCategories" class="form-check"
                     data-testid="category-filter-item"
                     [attr.data-testid-extra]="'category-' + cat">
                  <input class="form-check-input" type="checkbox" [id]="'mcat-' + cat"
                         [checked]="selectedCategories.has(cat)"
                         (change)="toggleCategory(cat)"
                         [attr.data-testid]="'category-' + cat">
                  <label class="form-check-label" [for]="'mcat-' + cat">{{ cat }}</label>
                </div>
              </div>
              <div class="mb-3">
                <label class="form-label fw-semibold small text-uppercase">Availability</label>
                <kendo-dropdownlist
                  data-testid="compatibility-filter"
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
                   data-testid="ai-search"
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

          <!-- Parts Table -->
          <div data-testid="parts-grid">
            <div class="table-responsive">
              <table class="table table-hover mb-0 align-middle">
                <thead class="table-light">
                  <tr>
                    <th style="width:120px">Part #</th>
                    <th style="width:180px">Name</th>
                    <th style="width:220px">Description</th>
                    <th style="width:100px">Price</th>
                    <th style="width:120px">Availability</th>
                    <th style="width:160px">Compatible Models</th>
                    <th style="width:130px"></th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let dataItem of pagedData" data-testid="part-row">
                    <td data-testid="part-number">{{ dataItem.partNumber }}</td>
                    <td data-testid="part-name">{{ dataItem.name }}</td>
                    <td>{{ dataItem.description | slice:0:60 }}{{ dataItem.description?.length > 60 ? '...' : '' }}</td>
                    <td data-testid="part-price">{{ dataItem.price | currency:'USD':'symbol':'1.2-2' }}</td>
                    <td data-testid="part-availability">
                      <app-badge [text]="getAvailabilityLabel(dataItem.availability)" [variant]="getAvailabilityVariant(dataItem.availability)"></app-badge>
                    </td>
                    <td data-testid="part-compatible">{{ dataItem.compatibleModels || 'Universal' }}</td>
                    <td>
                      <button class="btn btn-sm btn-warning fw-semibold"
                              data-testid="add-to-cart-btn"
                              [disabled]="dataItem.availability === 'OutOfStock'"
                              (click)="addToCart(dataItem, $event)">
                        {{ dataItem.availability === 'OutOfStock' ? 'Unavailable' : 'Add to Cart' }}
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <!-- Pagination -->
            <nav *ngIf="gridData.total > pageSize" class="mt-3 d-flex justify-content-between align-items-center">
              <span class="text-muted small">Showing {{ skip + 1 }}&ndash;{{ math_min(skip + pageSize, gridData.total) }} of {{ gridData.total }}</span>
              <ul class="pagination pagination-sm mb-0">
                <li class="page-item" [class.disabled]="skip === 0">
                  <button class="page-link" (click)="goToPage(skip - pageSize)" [disabled]="skip === 0">&laquo; Prev</button>
                </li>
                <li class="page-item" *ngFor="let p of pageNumbers" [class.active]="p === currentPage">
                  <button class="page-link" (click)="goToPage((p - 1) * pageSize)">{{ p }}</button>
                </li>
                <li class="page-item" [class.disabled]="skip + pageSize >= gridData.total">
                  <button class="page-link" (click)="goToPage(skip + pageSize)" [disabled]="skip + pageSize >= gridData.total">Next &raquo;</button>
                </li>
              </ul>
            </nav>
          </div>
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
  cartCount = 0;

  partCategories = ['Filters', 'Hydraulic', 'Electrical', 'Engine', 'Transmission', 'Undercarriage', 'Cab', 'Other'];
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

  get pagedData(): Part[] {
    return this.gridData.data;
  }

  get currentPage(): number {
    return Math.floor(this.skip / this.pageSize) + 1;
  }

  get totalPages(): number {
    return Math.ceil(this.gridData.total / this.pageSize);
  }

  get pageNumbers(): number[] {
    const pages: number[] = [];
    for (let i = 1; i <= this.totalPages; i++) {
      pages.push(i);
    }
    return pages;
  }

  math_min(a: number, b: number): number {
    return Math.min(a, b);
  }

  goToPage(newSkip: number): void {
    if (newSkip < 0 || newSkip >= this.gridData.total) return;
    this.skip = newSkip;
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
        this.cartCount++;
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
