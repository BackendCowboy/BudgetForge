import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { AccountService } from '../../core/services/account.service';
import { AccountType, CreateAccountRequest, UpdateAccountRequest } from '../../core/models/account.models';

@Component({
  selector: 'app-account-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatFormFieldModule,
    MatToolbarModule,
    MatIconModule
  ],
  template: `
    <mat-toolbar>
      <button mat-icon-button (click)="goBack()">
        <mat-icon>arrow_back</mat-icon>
      </button>
      <span>{{ isEditMode ? 'Edit Account' : 'Create New Account' }}</span>
    </mat-toolbar>

    <div class="container">
      <mat-card class="form-card">
        <mat-card-header>
          <mat-card-title>
            {{ isEditMode ? 'Edit Account' : 'Create New Account' }}
          </mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="accountForm" (ngSubmit)="onSubmit()">
            <mat-form-field appearance="outline">
              <mat-label>Account Name</mat-label>
              <input matInput formControlName="name" placeholder="e.g., Main Checking">
              <mat-error *ngIf="accountForm.get('name')?.hasError('required')">
                Account name is required
              </mat-error>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Account Type</mat-label>
              <mat-select formControlName="type">
                <mat-option [value]="AccountType.Checking">Checking</mat-option>
                <mat-option [value]="AccountType.Savings">Savings</mat-option>
                <mat-option [value]="AccountType.Credit">Credit Card</mat-option>
                <mat-option [value]="AccountType.Investment">Investment</mat-option>
                <mat-option [value]="AccountType.Cash">Cash</mat-option>
              </mat-select>
              <mat-error *ngIf="accountForm.get('type')?.hasError('required')">
                Account type is required
              </mat-error>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Currency</mat-label>
              <mat-select formControlName="currency">
                <mat-option value="CAD">CAD (Canadian Dollar)</mat-option>
                <mat-option value="USD">USD (US Dollar)</mat-option>
                <mat-option value="EUR">EUR (Euro)</mat-option>
                <mat-option value="GBP">GBP (British Pound)</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" *ngIf="!isEditMode">
              <mat-label>Initial Balance</mat-label>
              <input matInput type="number" formControlName="initialBalance" placeholder="0.00" step="0.01">
              <span matPrefix>$&nbsp;</span>
              <mat-error *ngIf="accountForm.get('initialBalance')?.hasError('min')">
                Initial balance cannot be negative
              </mat-error>
            </mat-form-field>

            <div class="error-message" *ngIf="errorMessage">
              {{ errorMessage }}
            </div>

            <div class="form-actions">
              <button mat-button type="button" (click)="goBack()">Cancel</button>
              <button mat-raised-button color="primary" type="submit" 
                      [disabled]="accountForm.invalid || isLoading">
                {{ isLoading ? 'Saving...' : (isEditMode ? 'Update Account' : 'Create Account') }}
              </button>
            </div>
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .container {
      padding: 2rem;
      display: flex;
      justify-content: center;
    }

    .form-card {
      width: 100%;
      max-width: 500px;
    }

    mat-form-field {
      width: 100%;
      margin-bottom: 1rem;
    }

    .form-actions {
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
      margin-top: 1rem;
    }

    .error-message {
      color: #f44336;
      margin-bottom: 1rem;
      text-align: center;
    }
  `]
})
export class AccountFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  AccountType = AccountType;

  accountForm: FormGroup;
  isEditMode = false;
  isLoading = false;
  errorMessage = '';
  accountId: number | null = null;

  constructor() {
    this.accountForm = this.fb.group({
      name: ['', [Validators.required]],
      type: ['', [Validators.required]],
      currency: ['CAD', [Validators.required]],
      initialBalance: [0, [Validators.min(0)]]
    });
  }

  ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.accountId = idParam ? parseInt(idParam, 10) : null;
    this.isEditMode = !!this.accountId;

    if (this.isEditMode) {
      this.loadAccount();
      this.accountForm.removeControl('initialBalance');
    }
  }

  loadAccount() {
    if (!this.accountId) return;

    this.accountService.getAccount(this.accountId).subscribe({
      next: (account) => {
        this.accountForm.patchValue({
          name: account.name,
          type: account.type,
          currency: account.currency
        });
      },
      error: (error) => {
        console.error('Error loading account:', error);
        this.errorMessage = 'Failed to load account details';
      }
    });
  }

  onSubmit() {
    if (this.accountForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';

      if (this.isEditMode) {
        this.updateAccount();
      } else {
        this.createAccount();
      }
    }
  }

  createAccount() {
    const request: CreateAccountRequest = this.accountForm.value;
    
    this.accountService.createAccount(request).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/accounts']);
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Failed to create account. Please try again.';
        console.error('Create account error:', error);
      }
    });
  }

  updateAccount() {
    if (!this.accountId) return;

    const request: UpdateAccountRequest = {
      name: this.accountForm.value.name,
      type: this.accountForm.value.type,
      currency: this.accountForm.value.currency
    };

    this.accountService.updateAccount(this.accountId, request).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/accounts']);
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Failed to update account. Please try again.';
        console.error('Update account error:', error);
      }
    });
  }

  goBack() {
    this.router.navigate(['/accounts']);
  }
}
