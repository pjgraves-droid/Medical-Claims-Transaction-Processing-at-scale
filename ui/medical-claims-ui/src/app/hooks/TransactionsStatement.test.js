import { renderHook } from '@testing-library/react';
import useSWR from 'swr';
import TransactionsStatement from './TransactionsStatement';

jest.mock('axios', () => ({ get: jest.fn(), put: jest.fn(), post: jest.fn() }));
jest.mock('swr', () => jest.fn());
jest.mock('swr/mutation', () => jest.fn());

describe('TransactionsStatement query construction', () => {
  beforeEach(() => {
    useSWR.mockReset();
    useSWR.mockReturnValue({ data: undefined, isLoading: true });
  });

  it('uses offset and limit for member list pages', () => {
    renderHook(() => TransactionsStatement.GetMembersList({ page: 2 }));
    expect(useSWR).toHaveBeenCalledWith('http://api.test/members?offset=10&limit=10', expect.any(Function));
  });

  it('includes sorting only when both sort values are present', () => {
    renderHook(() => TransactionsStatement.GetClaimsByMemberId('member-1', {
      page: 3,
      sort: { column: 'filingDate', direction: 'desc' },
    }));
    expect(useSWR).toHaveBeenCalledWith(
      'http://api.test/member/member-1/claims?offset=20&limit=10&sortColumn=filingDate&sortDirection=desc',
      expect.any(Function)
    );

    useSWR.mockReset();
    renderHook(() => TransactionsStatement.GetMembersList({
      page: 1,
      sort: { column: 'filingDate' },
    }));
    expect(useSWR).toHaveBeenCalledWith('http://api.test/members?offset=0&limit=10', expect.any(Function));
  });
});
