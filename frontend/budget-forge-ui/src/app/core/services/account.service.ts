import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AccountResponse, CreateAccountRequest, UpdateAccountRequest } from '../models/account.models';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/accounts`;

  getAccounts(): Observable<AccountResponse[]> {
    return this.http.get<AccountResponse[]>(this.apiUrl);
  }

  getAccount(id: number): Observable<AccountResponse> {
    return this.http.get<AccountResponse>(`${this.apiUrl}/${id}`);
  }

  createAccount(account: CreateAccountRequest): Observable<AccountResponse> {
    return this.http.post<AccountResponse>(this.apiUrl, account);
  }

  updateAccount(id: number, account: UpdateAccountRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, account);
  }

  deleteAccount(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
