export interface Account {
  id: string;
  name: string;
  accountType: string;
  balance: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateAccountRequest {
  name: string;
  accountType: string;
  initialBalance: number;
}

export interface UpdateAccountRequest {
  name: string;
  accountType: string;
}