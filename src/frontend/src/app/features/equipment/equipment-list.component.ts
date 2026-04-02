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
  templateUrl: './equipment-list.component.html',
  styleUrl: './equipment-list.component.scss'
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

