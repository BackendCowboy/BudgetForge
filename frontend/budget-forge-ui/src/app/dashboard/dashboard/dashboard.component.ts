import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { AuthService } from "../../core/services/auth.service";
import { AccountService } from "../../core/services/account.service";
import { QuickExpenseDialogComponent } from "../../shared/quick-expense-dialog/quick-expense-dialog.component";
import { AccountResponse } from '../../core/models/account.models';

// ⬇️ transactions; we’ll reuse your service
import {
  TransactionService,
  TransactionResponse,
  TransactionType
} from '../../core/services/transaction.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    MatCardModule,
    MatButtonModule,
    MatToolbarModule,
    MatIconModule,
    MatDialogModule,
  ],
  template: `
    <mat-toolbar color="primary">
      <span>BudgetForge</span>
      <span class="spacer"></span>
      <span>Welcome, {{ currentUser?.firstName }}!</span>
      <button mat-icon-button (click)="logout()" title="Logout">
        <mat-icon>logout</mat-icon>
      </button>
    </mat-toolbar>

    <div class="dashboard-container">
      <!-- Summary -->
      <div class="summary-section">
        <mat-card class="summary-card">
          <mat-card-content>
            <div class="total-balance">
              <h2>Total Balance</h2>
              <div class="balance-amount" [class.negative]="totalBalance < 0">
                {{ totalBalance | currency:'CAD':'symbol':'1.2-2' }}
              </div>
              <div class="account-count">
                {{ accounts.length }} account{{ accounts.length !== 1 ? 's' : '' }}
              </div>
            </div>
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Quick Actions -->
      <div class="quick-actions">
        <h3>Quick Actions</h3>
        <div class="action-buttons">
          <button mat-raised-button color="primary" (click)="createAccount()">
            <mat-icon>add</mat-icon>
            New Account
          </button>

          <button mat-raised-button color="accent" (click)="addExpense()">
            <mat-icon>remove</mat-icon>
            Quick Expense
          </button>

          <button mat-raised-button (click)="addIncome()">
            <mat-icon>add</mat-icon>
            Add Income
          </button>
        </div>
      </div>

      <!-- Accounts -->
      <div class="accounts-section">
        <h3>Your Accounts</h3>

        <div class="account-grid" *ngIf="accounts.length > 0">
          <mat-card *ngFor="let account of accounts"
                    class="account-card"
                    [class.neg]="account.balance < 0"
                    (click)="viewAccount(account.id)">
            <mat-card-content>
              <div class="card-head">
                <div class="left">
                  <div class="type-chip">{{ accountTypeLabel(account.type) }}</div>
                  <h4 class="name">{{ account.name }}</h4>
                </div>
                <mat-icon class="type-icon">{{ getAccountIcon(account.type) }}</mat-icon>
              </div>

              <div class="balance">
                {{ account.balance | currency:account.currency:'symbol':'1.2-2' }}
              </div>

              <div class="meta">
                <span class="currency" *ngIf="account.currency !== 'CAD'">{{ account.currency }}</span>
              </div>
            </mat-card-content>

            <mat-card-actions>
              <button mat-button (click)="$event.stopPropagation(); viewAccount(account.id)">View</button>
            </mat-card-actions>
          </mat-card>
        </div>

        <div *ngIf="accounts.length === 0" class="empty-accounts">
          <mat-icon class="empty-icon">account_balance</mat-icon>
          <p>No accounts yet</p>
          <button mat-raised-button color="primary" (click)="createAccount()">
            Create Your First Account
          </button>
        </div>
      </div>

      <!-- Recent Transactions -->
      <div class="recent-section" *ngIf="recentTransactions.length">
        <h3>Recent Transactions</h3>

        <mat-card class="recent-card">
          <mat-card-content>
            <div class="tx-row" *ngFor="let tx of recentTransactions">
              <div class="tx-left">
                <div class="tx-icon" [class.income]="tx.type === TransactionType.Income"
                                     [class.expense]="tx.type === TransactionType.Expense">
                  <mat-icon>{{ tx.type === TransactionType.Income ? 'arrow_upward' : 'arrow_downward' }}</mat-icon>
                </div>
                <div class="tx-main">
                  <div class="tx-desc">{{ tx.description || '(no description)' }}</div>
                  <div class="tx-sub">
                    <span class="tx-account">{{ accountName(tx.accountId) }}</span>
                    <span class="tx-dot">•</span>
                    <span class="tx-time">{{ tx.timestamp | date:'short' }}</span>
                  </div>
                </div>
              </div>

              <div class="tx-amt" [class.income]="tx.type === TransactionType.Income"
                                  [class.expense]="tx.type === TransactionType.Expense">
                {{ tx.amount | currency: accountCurrency(tx.accountId) :'symbol':'1.2-2' }}
              </div>
            </div>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .spacer { flex: 1 1 auto; }
    .dashboard-container { padding: 1.5rem; max-width: 1200px; margin: 0 auto; }

    /* Summary */
    .summary-section { margin-bottom: 2rem; }
    .summary-card { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; }
    .total-balance { text-align: center; padding: 1rem; }
    .total-balance h2 { margin: 0 0 0.5rem 0; font-weight: 300; }
    .balance-amount { font-size: 2.5rem; font-weight: bold; margin: 0.5rem 0; }
    .balance-amount.negative { color: #ffcdd2; }
    .account-count { opacity: 0.9; font-size: 0.9rem; }

    /* Quick actions */
    .quick-actions { margin-bottom: 2rem; }
    .quick-actions h3 { margin-bottom: 1rem; color: #333; }
    .action-buttons { display: flex; gap: 1rem; flex-wrap: wrap; }
    .action-buttons button { display: flex; align-items: center; gap: 0.5rem; }

    /* Accounts */
    .accounts-section { margin: 1.5rem 0 2rem; }
    .accounts-section h3 { margin-bottom: 1rem; color: #333; }
    .account-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 1rem; }

    .account-card {
      cursor: pointer;
      transition: transform .18s ease, box-shadow .18s ease;
      border-left: 4px solid transparent;
    }
    .account-card:hover { transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,.14); }
    .account-card.neg { border-left-color: #ef5350; }

    .card-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: .5rem; }
    .card-head .left { display: flex; align-items: center; gap: .5rem; }
    .type-chip {
      font-size: .72rem; text-transform: uppercase; letter-spacing: .04em;
      background: #eef2ff; color: #3f51b5; border-radius: 999px; padding: .2rem .5rem;
    }
    .name { margin: 0; font-size: 1rem; font-weight: 600; color: #333; }
    .type-icon { color: #667eea; }
    .balance { font-size: 1.35rem; font-weight: 700; color: #2e7d32; margin: .25rem 0 .35rem; }
    .account-card.neg .balance { color: #c62828; }
    .meta { display: flex; gap: .75rem; color: #666; font-size: .8rem; }

    /* Recent transactions */
    .recent-section { margin-bottom: 2rem; }
    .recent-card { overflow: hidden; }
    .tx-row {
      display: flex; align-items: center; justify-content: space-between;
      padding: .6rem .25rem; border-top: 1px solid #eee;
    }
    .tx-row:first-child { border-top: 0; }

    .tx-left { display: flex; align-items: center; gap: .75rem; min-width: 0; }
    .tx-icon {
      width: 36px; height: 36px; border-radius: 50%;
      display: grid; place-items: center; background: #f5f5f5;
    }
    .tx-icon.income { background: #e8f5e9; color: #2e7d32; }
    .tx-icon.expense { background: #ffebee; color: #c62828; }

    .tx-main { display: grid; gap: 2px; min-width: 0; }
    .tx-desc { font-weight: 600; color: #333; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .tx-sub { color: #666; font-size: .85rem; display: flex; align-items: center; gap: .4rem; }
    .tx-dot { opacity: .6; }

    .tx-amt { font-weight: 700; }
    .tx-amt.income { color: #2e7d32; }
    .tx-amt.expense { color: #c62828; }

    @media (max-width: 768px) {
      .action-buttons { justify-content: center; }
      .account-grid { grid-template-columns: 1fr; }
    }
  `]
})
export class DashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private accountService = inject(AccountService);
  private transactionService = inject(TransactionService);
  private router = inject(Router);
  private dialog = inject(MatDialog);

  accounts: AccountResponse[] = [];
  totalBalance = 0;

  // recent tx state
  TransactionType = TransactionType; // expose enum to template
  recentTransactions: TransactionResponse[] = [];

  // fast lookup for account name/currency by id
  private accountIndex = new Map<number, AccountResponse>();

  get currentUser() {
    return this.authService.currentUserValue;
  }

  ngOnInit() {
    this.loadAccounts();
    this.loadRecentTransactions();
  }

  loadAccounts() {
    this.accountService.getAccounts().subscribe({
      next: (accounts: AccountResponse[]) => {
        this.accounts = accounts.filter(a => !a.isDeleted);
        this.totalBalance = this.accounts.reduce((sum, account) => sum + account.balance, 0);

        // index for quick name lookup
        this.accountIndex.clear();
        for (const a of this.accounts) this.accountIndex.set(a.id, a);
      },
      error: (error) => {
        console.error('Error loading accounts:', error);
      }
    });
  }

  loadRecentTransactions() {
    this.transactionService.getTransactions().subscribe({
      next: (list) => {
        // last 5 (assuming API returns sorted desc; otherwise sort locally)
        this.recentTransactions = (list ?? []).slice(0, 5);
      },
      error: (err) => console.error('Error loading transactions:', err)
    });
  }

  accountName(accountId: number): string {
    return this.accountIndex.get(accountId)?.name ?? `Account #${accountId}`;
    // we removed raw id from cards, but it’s fine to fall back here
  }

  accountCurrency(accountId: number): string {
    return this.accountIndex.get(accountId)?.currency ?? 'CAD';
  }

  getAccountIcon(type: number): string {
    switch(type) {
      case 0: return 'account_balance'; // Checking
      case 1: return 'savings';         // Savings
      case 2: return 'credit_card';     // Credit
      case 3: return 'trending_up';     // Investment
      case 4: return 'attach_money';    // Cash
      default: return 'account_balance';
    }
  }

  accountTypeLabel(type: number): string {
    switch (type) {
      case 0: return 'Checking';
      case 1: return 'Savings';
      case 2: return 'Credit Card';
      case 3: return 'Investment';
      case 4: return 'Cash';
      default: return 'Account';
    }
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  navigateToAccounts() {
    this.router.navigate(['/accounts']);
  }

  navigateToTransactions() {
    this.router.navigate(['/transactions']);
  }

  createAccount() {
    this.router.navigate(['/accounts/new']);
  }

  viewAccount(id: number) {
    this.router.navigate(['/accounts', id]);
  }

  addExpense() {
    this.dialog.open(QuickExpenseDialogComponent, {
      disableClose: true,
      autoFocus: true,
    }).afterClosed().subscribe(res => {
      if (res?.success) {
        this.loadAccounts();          // refresh totals
        this.loadRecentTransactions(); // refresh recent list
      }
    });
  }

  addIncome() {
    alert('Add income feature coming soon! 💰');
  }
}