import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { AccountService } from '../../core/services/account.service';
import { TransactionService, TransactionType, CreateTransactionRequest } from '../../core/services/transaction.service';
import { AccountResponse } from '../../core/models/account.models';

@Component({
  selector: 'app-quick-expense-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatInputModule,
    MatSelectModule,
    MatFormFieldModule,
    MatIconModule
  ],
  template: `
    <div class="dialog-header">
      <h2 mat-dialog-title>
        <mat-icon>remove</mat-icon>
        Quick Expense
      </h2>
    </div>

    <mat-dialog-content>
      <form [formGroup]="expenseForm">
        <div class="quick-amounts">
          <h4>Common Amounts</h4>
          <div class="amount-buttons">
            <button type="button" mat-stroked-button 
                    *ngFor="let amount of quickAmounts" 
                    (click)="setAmount(amount.value)"
                    [color]="expenseForm.get('amount')?.value === amount.value ? 'primary' : ''">
              {{ amount.label }}
            </button>
          </div>
        </div>

        <mat-form-field appearance="outline">
          <mat-label>Amount</mat-label>
          <input matInput type="number" formControlName="amount" placeholder="0.00" step="0.01">
          <span matPrefix>$&nbsp;</span>
          <mat-error *ngIf="expenseForm.get('amount')?.hasError('required')">
            Amount is required
          </mat-error>
          <mat-error *ngIf="expenseForm.get('amount')?.hasError('min')">
            Amount must be greater than 0
          </mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>What did you buy?</mat-label>
          <input matInput formControlName="description" placeholder="Coffee, lunch, gas...">
          <mat-error *ngIf="expenseForm.get('description')?.hasError('required')">
            Description is required
          </mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>From Account</mat-label>
          <mat-select formControlName="accountId">
            <mat-option *ngFor="let account of accounts" [value]="account.id">
              {{ account.name }} ({{ account.balance | currency:account.currency }})
            </mat-option>
          </mat-select>
          <mat-error *ngIf="expenseForm.get('accountId')?.hasError('required')">
            Please select an account
          </mat-error>
        </mat-form-field>

        <div class="quick-categories">
          <h4>Category</h4>
          <div class="category-buttons">
            <button type="button" mat-stroked-button 
                    *ngFor="let category of expenseCategories" 
                    (click)="setCategory(category)"
                    [color]="selectedCategory === category ? 'primary' : ''">
              {{ category }}
            </button>
          </div>
        </div>

        <div *ngIf="errorMessage" class="error-message">
          {{ errorMessage }}
        </div>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions>
      <button mat-button (click)="cancel()">Cancel</button>
      <button mat-raised-button color="primary" 
              [disabled]="expenseForm.invalid || isLoading"
              (click)="addExpense()">
        {{ isLoading ? 'Adding...' : 'Add Expense' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .dialog-header h2 {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin: 0;
      color: #d32f2f;
    }

    mat-dialog-content {
      padding: 1rem 0;
      min-width: 400px;
    }

    .quick-amounts, .quick-categories {
      margin-bottom: 1.5rem;
    }

    .quick-amounts h4, .quick-categories h4 {
      margin: 0 0 0.5rem 0;
      color: #333;
      font-size: 0.9rem;
    }

    .amount-buttons, .category-buttons {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .amount-buttons button, .category-buttons button {
      min-width: auto;
      padding: 0.25rem 0.75rem;
    }

    mat-form-field {
      width: 100%;
      margin-bottom: 1rem;
    }

    .error-message {
      color: #f44336;
      text-align: center;
      margin-top: 1rem;
      padding: 0.5rem;
      background: #ffebee;
      border-radius: 4px;
    }

    mat-dialog-actions {
      display: flex;
      gap: 0.5rem;
      justify-content: flex-end;
      padding: 1rem 0 0 0;
    }
  `]
})
export class QuickExpenseDialogComponent {
  private fb = inject(FormBuilder);
  private dialogRef = inject(MatDialogRef<QuickExpenseDialogComponent>);
  private accountService = inject(AccountService);
  private transactionService = inject(TransactionService);

  expenseForm: FormGroup;
  accounts: AccountResponse[] = [];
  isLoading = false;
  selectedCategory = '';
  errorMessage = '';

  quickAmounts = [
    { label: '$5', value: 5 },
    { label: '$10', value: 10 },
    { label: '$15', value: 15 },
    { label: '$25', value: 25 },
    { label: '$50', value: 50 }
  ];

  expenseCategories = [
    'Food & Dining', 'Coffee', 'Transportation', 'Shopping', 
    'Entertainment', 'Bills', 'Health', 'Other'
  ];

  constructor() {
    this.expenseForm = this.fb.group({
      amount: ['', [Validators.required, Validators.min(0.01)]],
      description: ['', [Validators.required]],
      accountId: ['', [Validators.required]]
    });

    this.loadAccounts();
  }

  loadAccounts() {
    this.accountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts.filter(a => !a.isDeleted);
        if (this.accounts.length > 0) {
          this.expenseForm.patchValue({ accountId: this.accounts[0].id });
        }
      },
      error: (error) => {
        console.error('Error loading accounts:', error);
        this.errorMessage = 'Failed to load accounts';
      }
    });
  }

  setAmount(amount: number) {
    this.expenseForm.patchValue({ amount });
  }

  setCategory(category: string) {
    this.selectedCategory = category;
  }

  addExpense() {
    if (this.expenseForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      
      const description = this.selectedCategory 
        ? `${this.expenseForm.value.description} (${this.selectedCategory})`
        : this.expenseForm.value.description;

      const transactionRequest: CreateTransactionRequest = {
        accountId: this.expenseForm.value.accountId,
        amount: this.expenseForm.value.amount,
        type: TransactionType.Expense,
        description: description,
        timestamp: new Date()
      };

      this.transactionService.createTransaction(transactionRequest).subscribe({
        next: (transaction) => {
          this.isLoading = false;
          this.dialogRef.close({ success: true, transaction });
        },
        error: (error) => {
          this.isLoading = false;
          console.error('Error creating transaction:', error);
          this.errorMessage = 'Failed to add expense. Please try again.';
        }
      });
    }
  }

  cancel() {
    this.dialogRef.close();
  }
}
