export enum BillStatus { Pending = 0, DueSoon = 1, Overdue = 2 }
// Align with your backend enum if needed:
export enum BillFrequency { Weekly = 0, BiWeekly = 1, Monthly = 2, Custom = 3 }

export interface BillListItem {
  id: string;
  name: string;
  amount: number;
  dueDate: string;          // yyyy-MM-dd from API
  isRecurring: boolean;
  frequency?: BillFrequency | null;
  category?: string | null;
  autoPay: boolean;
  daysUntilDue: number;
  status: BillStatus;
}

export interface PayBillRequest {
  amount: number;
  paidAt?: string | null;   // ISO date-time
  notes?: string | null;
}

export interface PayBillResponse {
  paymentId: string;
}