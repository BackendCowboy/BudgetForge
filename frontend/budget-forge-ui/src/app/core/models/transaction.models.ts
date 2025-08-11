export interface Transaction {
  id: string;
  accountId: string;
  amount: number;
  description: string;
  category: string;
  transactionDate: Date;
  createdAt: Date;
}

export interface CreateTransactionRequest {
  accountId: string;
  amount: number;
  description: string;
  category: string;
  transactionDate: Date;
}