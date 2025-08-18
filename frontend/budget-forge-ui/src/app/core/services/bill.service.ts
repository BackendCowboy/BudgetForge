import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BillListItem, PayBillRequest, PayBillResponse } from '../models/bill.models';

const API_BASE = 'http://localhost:5258/api/billing';

@Injectable({ providedIn: 'root' })
export class BillService {            // <-- make sure it's BillService (singular)
  private http = inject(HttpClient);

  getUpcoming(days = 30): Observable<BillListItem[]> {
    return this.http.get<BillListItem[]>(`${API_BASE}/upcoming`, { params: { days } as any });
  }

  pay(billId: string, body: PayBillRequest): Observable<PayBillResponse> {
    return this.http.post<PayBillResponse>(`${API_BASE}/bills/${billId}/pay`, body);
  }
}