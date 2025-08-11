import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccountDetail } from './account-detail';

describe('AccountDetail', () => {
  let component: AccountDetail;
  let fixture: ComponentFixture<AccountDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AccountDetail]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AccountDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
