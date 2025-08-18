export enum TransactionType {
  Income = 'Income',
  Expense = 'Expense'
}

export interface CreateTransactionRequest {
  accountId: number;  // int in C#, not string
  amount: number;
  type: TransactionType;  // not category
  description?: string;
  timestamp?: Date;  // not transactionDate
}

export interface UpdateTransactionRequest {
  amount?: number;
  type?: TransactionType;
  description?: string;
  timestamp?: Date;
}

export interface TransactionResponse {
  id: number;  // int in C#, not string
  accountId: number;  // int in C#, not string
  amount: number;
  type: TransactionType;
  description: string;
  timestamp: Date;  // not transactionDate
  isDeleted: boolean;
  createdAt: Date;
  updatedAt: Date;
}
