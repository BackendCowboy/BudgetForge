import { Routes } from '@angular/router';

import { LoginComponent } from './auth/login/login.component';
import { DashboardComponent } from './dashboard/dashboard/dashboard.component';

import { AccountListComponent } from './accounts/account-list/account-list.component';
import { AccountFormComponent } from './accounts/account-form/account-form.component';

import { UpcomingBillsComponent } from './bills/upcoming-bills/upcoming-bills.component';
import { TransactionListComponent } from './transactions/transaction-list/transaction-list';

import { authGuard } from './core/guards/auth-guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },

  { path: 'login', component: LoginComponent },

  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },

  // Accounts
  { path: 'accounts', component: AccountListComponent, canActivate: [authGuard] },
  { path: 'accounts/new', component: AccountFormComponent, canActivate: [authGuard] },
  { path: 'accounts/:id', component: AccountFormComponent, canActivate: [authGuard] }, // detail/edit

  // Transactions (real page)
  { path: 'transactions', component: TransactionListComponent, canActivate: [authGuard] },

  // Bills
  { path: 'bills/upcoming', component: UpcomingBillsComponent, canActivate: [authGuard] },

  // Fallback
  { path: '**', redirectTo: '/dashboard' },
];