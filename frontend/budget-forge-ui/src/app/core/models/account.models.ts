export enum AccountType {
  Checking = 0,
  Savings = 1,
  Credit = 2,
  Investment = 3,
  Cash = 4
}

export interface CreateAccountRequest {
  name: string;
  type: AccountType;
  initialBalance: number;
  currency?: string;
}

export interface UpdateAccountRequest {
  name?: string;
  type?: AccountType;
  currency?: string;
}

export interface AccountResponse {
  id: number;
  name: string;
  type: AccountType;
  balance: number;
  currency: string;
  isDeleted: boolean;
  createdAt: Date;
  updatedAt: Date;
}
