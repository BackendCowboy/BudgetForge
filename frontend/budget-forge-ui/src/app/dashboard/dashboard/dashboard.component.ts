// src/app/dashboard/dashboard/dashboard.component.ts
import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule],
  template: `
    <div class="dashboard-container">
      <h1>Welcome to BudgetForge Dashboard</h1>
      
      <div class="cards-grid">
        <mat-card class="dashboard-card">
          <mat-card-header>
            <mat-card-title>Accounts</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <p>Manage your accounts</p>
          </mat-card-content>
          <mat-card-actions>
            <button mat-button color="primary">View Accounts</button>
          </mat-card-actions>
        </mat-card>

        <mat-card class="dashboard-card">
          <mat-card-header>
            <mat-card-title>Transactions</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <p>Track your transactions</p>
          </mat-card-content>
          <mat-card-actions>
            <button mat-button color="primary">View Transactions</button>
          </mat-card-actions>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 2rem;
    }

    h1 {
      margin-bottom: 2rem;
      color: #333;
    }

    .cards-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 1rem;
      max-width: 1200px;
    }

    .dashboard-card {
      height: 200px;
    }

    mat-card-content {
      flex: 1;
    }
  `]
})
export class DashboardComponent {
  
}