import { Component, OnInit, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TransactionService, TransactionResponse, TransactionType } from '../../core/services/transaction.service';
import { AccountService } from '../../core/services/account.service';
import { AccountResponse } from '../../core/models/account.models';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-transaction-list',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatCardModule, MatProgressSpinnerModule],
  template: `
    <div class="wrap">
      <h2>Transactions</h2>

      <div class="toolbar">
        <span>Total: {{ txs().length }}</span>
      </div>

      <ng-container *ngIf="loading(); else listTpl">
        <div class="loading">
          <mat-progress-spinner mode="indeterminate" diameter="36"></mat-progress-spinner>
        </div>
      </ng-container>

      <ng-template #listTpl>
        <ng-container *ngIf="txs().length; else emptyTpl">
          <mat-card class="tx" *ngFor="let tx of txs()">
            <div class="left">
              <div class="icon" [class.expense]="tx.type === TransactionType.Expense" [class.income]="tx.type === TransactionType.Income">
                <mat-icon>{{ tx.type === TransactionType.Expense ? 'south' : 'north' }}</mat-icon>
              </div>
              <div class="meta">
                <div class="desc">{{ tx.description }}</div>
                <div class="sub">
                  <span>{{ accountName(tx.accountId) }}</span>
                  <span>•</span>
                  <span>{{ tx.timestamp | date:'MMM d, h:mm a' }}</span>
                </div>
              </div>
            </div>
            <div class="amt" [class.neg]="tx.type === TransactionType.Expense" [class.pos]="tx.type === TransactionType.Income">
              {{ (tx.type === TransactionType.Expense ? -tx.amount : tx.amount) | currency:'CAD':'symbol':'1.2-2' }}
            </div>
          </mat-card>
        </ng-container>

        <ng-template #emptyTpl>
          <div class="empty">
            <mat-icon>receipt_long</mat-icon>
            <p>No transactions yet</p>
          </div>
        </ng-template>
      </ng-template>
    </div>
  `,
  styles: [`
    .wrap { max-width: 900px; margin: 1.5rem auto; padding: 0 1rem; }
    h2 { margin: 0 0 1rem 0; }
    .toolbar { display:flex; justify-content: space-between; align-items:center; margin-bottom: .75rem; color:#666; }
    .loading { display:flex; justify-content:center; padding:2rem; }
    .tx { display:flex; justify-content:space-between; align-items:center; padding: .75rem 1rem; margin-bottom:.5rem; }
    .left { display:flex; gap:.75rem; align-items:center; }
    .icon { border-radius:50%; width:36px; height:36px; display:grid; place-items:center; background:#e8f5e9; }
    .icon.expense { background:#ffebee; }
    .icon.income { background:#e8f5e9; }
    .meta .desc { font-weight:600; }
    .meta .sub { color:#666; font-size:.85rem; display:flex; gap:.5rem; }
    .amt { font-weight:700; }
    .amt.neg { color:#d32f2f; }
    .amt.pos { color:#2e7d32; }
    .empty { text-align:center; color:#666; padding:2rem; }
    .empty mat-icon { font-size: 48px; width:48px; height:48px; color:#bbb; }
  `]
})
export class TransactionListComponent implements OnInit {
  TransactionType = TransactionType;

  private txSvc = inject(TransactionService);
  private acctSvc = inject(AccountService);

  loading = signal<boolean>(false);
  txs = signal<TransactionResponse[]>([]);
  accounts = signal<AccountResponse[]>([]);

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    // Load accounts (for name lookup) and transactions in parallel
    this.acctSvc.getAccounts().subscribe({
      next: accs => this.accounts.set(accs || []),
      error: () => this.accounts.set([])
    });

    this.txSvc.getTransactions().subscribe({
      next: list => { this.txs.set(list); this.loading.set(false); },
      error: _ => { this.txs.set([]); this.loading.set(false); }
    });
  }

  accountName(accountId: number): string {
    const a = this.accounts().find(x => x.id === accountId);
    return a ? a.name : `Account #${accountId}`;
  }
}