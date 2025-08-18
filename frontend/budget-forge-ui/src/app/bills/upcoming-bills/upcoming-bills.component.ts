import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BillService } from '../../core/services/bill.service';
import { BillListItem, BillStatus } from '../../core/models/bill.models';

@Component({
  selector: 'app-upcoming-bills',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './upcoming-bills.component.html',
})
export class UpcomingBillsComponent implements OnInit {
  private billsSvc = inject(BillService);

  days = signal<number>(30);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);
  bills = signal<BillListItem[]>([]);
  BillStatus = BillStatus; // expose enum to template

  ngOnInit() { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.billsSvc.getUpcoming(this.days()).subscribe({
      next: (rows: BillListItem[]) => { this.bills.set(rows); this.loading.set(false); },
      error: (err: unknown) => {
        const msg =
          typeof err === 'object' && err !== null && 'error' in err && typeof (err as any).error?.message === 'string'
            ? (err as any).error.message
            : (err as any)?.message ?? 'Failed to load bills';
        this.error.set(msg);
        this.loading.set(false);
      }
    });
  }

  statusClass(s: BillStatus): string {
    switch (s) {
      case BillStatus.DueSoon: return 'bg-yellow-100 text-yellow-800';
      case BillStatus.Overdue: return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  }

  markPaid(bill: BillListItem): void {
    this.loading.set(true);
    this.billsSvc.pay(bill.id, { amount: bill.amount, paidAt: new Date().toISOString(), notes: 'Paid from UI' })
      .subscribe({
        next: (_: { paymentId: string }) => this.load(),
        error: (err: unknown) => {
          const msg =
            typeof err === 'object' && err !== null && 'error' in err && typeof (err as any).error?.message === 'string'
              ? (err as any).error.message
              : (err as any)?.message ?? 'Payment failed';
          this.error.set(msg);
          this.loading.set(false);
        }
      });
  }
}