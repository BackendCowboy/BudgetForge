import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { AccountService } from '../../core/services/account.service';
import { AccountResponse } from '../../core/models/account.models';

@Component({
  selector: 'app-account-list',
  standalone: true,
  imports: [
    CommonModule, 
    MatCardModule, 
    MatButtonModule, 
    MatToolbarModule, 
    MatIconModule
  ],
  template: `
    <mat-toolbar>
      <button mat-icon-button (click)="goBack()">
        <mat-icon>arrow_back</mat-icon>
      </button>
      <span>Accounts</span>
      <span class="spacer"></span>
      <button mat-raised-button color="primary" (click)="createAccount()">
        <mat-icon>add</mat-icon>
        New Account
      </button>
    </mat-toolbar>

    <div class="container">
      <div class="accounts-grid" *ngIf="accounts.length > 0">
        <mat-card *ngFor="let account of accounts" class="account-card">
          <mat-card-header>
            <mat-card-title>{{ account.name }}</mat-card-title>
            <mat-card-subtitle>{{ account.type }} • {{ account.currency }}</mat-card-subtitle>
          </mat-card-header>
          
          <mat-card-content>
            <div class="balance">
              <span class="balance-label">Balance:</span>
              <span class="balance-amount" [class.negative]="account.balance < 0">
                {{ account.balance | currency:account.currency }}
              </span>
            </div>
          </mat-card-content>
          
          <mat-card-actions>
            <button mat-button (click)="viewAccount(account.id)">View Details</button>
            <button mat-button (click)="editAccount(account.id)">Edit</button>
            <button mat-button color="warn" (click)="confirmDelete(account)">Delete</button>
          </mat-card-actions>
        </mat-card>
      </div>

      <div *ngIf="accounts.length === 0 && !loading" class="empty-state">
        <mat-icon class="empty-icon">account_balance</mat-icon>
        <h2>No accounts yet</h2>
        <p>Create your first account to get started</p>
        <button mat-raised-button color="primary" (click)="createAccount()">
          Create Account
        </button>
      </div>

      <div *ngIf="loading" class="loading-state">
        <p>Loading accounts...</p>
      </div>

      <div *ngIf="errorMessage" class="error-state">
        <mat-icon class="error-icon">error</mat-icon>
        <h2>{{ errorMessage }}</h2>
        <button mat-button (click)="loadAccounts()">Retry</button>
      </div>
    </div>
  `,
  styles: [`
    .spacer { flex: 1 1 auto; }
    .container { padding: 2rem; }
    .accounts-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 1rem;
    }
    .account-card { height: 200px; display: flex; flex-direction: column; }
    .balance { display: flex; justify-content: space-between; align-items: center; margin-top: 1rem; }
    .balance-label { font-weight: 500; color: #666; }
    .balance-amount { font-size: 1.5rem; font-weight: 600; color: #4caf50; }
    .balance-amount.negative { color: #f44336; }
    .empty-state, .loading-state, .error-state { text-align: center; padding: 3rem; }
    .empty-icon, .error-icon { font-size: 4rem; height: 4rem; width: 4rem; color: #ccc; }
    .error-icon { color: #f44336; }
    mat-card-content { flex: 1; }
  `]
})
export class AccountListComponent implements OnInit {
  private accountService = inject(AccountService);
  private router = inject(Router);

  accounts: AccountResponse[] = [];
  loading = false;
  errorMessage = '';

  ngOnInit() {
    this.loadAccounts();
  }

  loadAccounts() {
    this.loading = true;
    this.errorMessage = '';
    
    this.accountService.getAccounts().subscribe({
      next: (accounts: AccountResponse[]) => {
        this.accounts = accounts.filter(a => !a.isDeleted);
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Error loading accounts:', error);
        this.loading = false;
        this.errorMessage = 'Failed to load accounts. Please check your API connection.';
      }
    });
  }

  confirmDelete(account: AccountResponse) {
    if (confirm(`Are you sure you want to delete "${account.name}"? This cannot be undone.`)) {
      this.deleteAccount(account.id);
    }
  }

  deleteAccount(id: number) {
    this.accountService.deleteAccount(id).subscribe({
      next: () => {
        this.loadAccounts(); // Refresh the list
      },
      error: (error: any) => {
        console.error('Error deleting account:', error);
        alert('Failed to delete account. Please try again.');
      }
    });
  }

  createAccount() {
    this.router.navigate(['/accounts/new']);
  }

  viewAccount(id: number) {
    this.router.navigate(['/accounts', id]);
  }

  editAccount(id: number) {
    this.router.navigate(['/accounts', id, 'edit']);
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}
