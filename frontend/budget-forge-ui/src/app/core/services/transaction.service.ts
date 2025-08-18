import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export enum TransactionType {
  Income = 0,
  Expense = 1
}

export interface CreateTransactionRequest {
  accountId: number;
  amount: number;
  type: TransactionType;
  description: string;
  timestamp?: Date | string; // allow string or Date coming in
}

export interface TransactionResponse {
  id: number;
  accountId: number;
  amount: number;
  type: TransactionType;
  description: string;
  timestamp: Date;    // guaranteed Date after normalization
  isDeleted: boolean;
  createdAt: Date;    // normalized
  updatedAt: Date;    // normalized
}

@Injectable({ providedIn: 'root' })
export class TransactionService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/transactions`;

  // helper: coerce a possibly undefined/string/date into a Date (with a fallback)
  private asDate(val: unknown, fallback?: Date): Date {
    if (val instanceof Date) return val;
    if (typeof val === 'string' && val) return new Date(val);
    return fallback ?? new Date();
  }

  private normalize(tx: any): TransactionResponse {
  const created = this.asDate(tx.createdAt);
  const updated = this.asDate(tx.updatedAt, created);

  // if backend sends 0001-01-01 or null, fall back to createdAt
  let ts = this.asDate(tx.timestamp, created);
  if (isNaN(ts.getTime()) || ts.getFullYear() < 2000) {
    ts = created;
  }

  return {
    id: tx.id,
    accountId: tx.accountId,
    amount: tx.amount,
    type: tx.type,
    description: tx.description,
    timestamp: ts,
    isDeleted: tx.isDeleted,
    createdAt: created,
    updatedAt: updated,
  };
}

  getTransactions(): Observable<TransactionResponse[]> {
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(list => (list ?? []).map(tx => this.normalize(tx)))
    );
  }

  getTransactionsByAccount(accountId: number): Observable<TransactionResponse[]> {
    return this.http.get<any[]>(`${this.apiUrl}/account/${accountId}`).pipe(
      map(list => (list ?? []).map(tx => this.normalize(tx)))
    );
  }

  createTransaction(transaction: CreateTransactionRequest): Observable<TransactionResponse> {
    // send ISO string to API for timestamp if provided
    const payload = {
      ...transaction,
      timestamp: transaction.timestamp
        ? (transaction.timestamp instanceof Date
            ? transaction.timestamp.toISOString()
            : transaction.timestamp)
        : undefined
    };
    return this.http.post<any>(this.apiUrl, payload).pipe(
      map(tx => this.normalize(tx))
    );
  }

  deleteTransaction(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}