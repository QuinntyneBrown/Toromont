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
  templateUrl: './parts-catalog.component.html',
  styleUrl: './parts-catalog.component.scss'
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

