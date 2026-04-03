import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActiveOrganizationService, Membership } from '../../../core/services/active-organization.service';

@Component({
  selector: 'app-org-switcher',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './org-switcher.component.html',
  styleUrl: './org-switcher.component.scss'
})
export class OrgSwitcherComponent implements OnInit {
  private orgService = inject(ActiveOrganizationService);

  context$ = this.orgService.context$;
  dropdownOpen = false;

  ngOnInit(): void {
    this.orgService.loadContext().subscribe();
  }

  toggleDropdown(): void {
    this.dropdownOpen = !this.dropdownOpen;
  }

  selectOrganization(membership: Membership): void {
    this.dropdownOpen = false;
    this.orgService.setActiveOrganization(membership.organizationId).subscribe();
  }

  getActiveOrgName(ctx: { memberships: Membership[]; activeOrganizationId: string }): string {
    const active = ctx.memberships.find(m => m.isActive);
    return active?.organizationName ?? 'Select Organization';
  }

  get hasMultipleMemberships(): boolean {
    const ctx = (this.orgService as any).contextSubject?.value;
    return ctx?.memberships?.length > 1;
  }
}
