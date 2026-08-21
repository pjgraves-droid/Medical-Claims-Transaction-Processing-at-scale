import { render, screen } from '@testing-library/react';
import MemberCoverageModal from './MemberCoverageModal';
import TransactionsStatement from '../hooks/TransactionsStatement';

jest.mock('../hooks/TransactionsStatement', () => ({
  __esModule: true,
  default: {
    GetMember: jest.fn(),
    GetCoverageByMember: jest.fn(),
  },
}));

describe('MemberCoverageModal', () => {
  beforeEach(() => {
    TransactionsStatement.GetMember.mockReturnValue({
      isLoading: false,
      data: { firstName: 'Ada', lastName: 'Lovelace' },
    });
    TransactionsStatement.GetCoverageByMember.mockReturnValue({
      isLoading: false,
      data: { startDate: '2024-01-02', endDate: '2024-12-31' },
    });
  });

  it('renders nothing when hidden', () => {
    const { container } = render(
      <MemberCoverageModal memberId="member-1" showCoverageModal={false} setShowCoverageModal={jest.fn()} />
    );
    expect(container).toBeEmptyDOMElement();
  });

  it('renders the member and formatted coverage dates after loading', () => {
    render(
      <MemberCoverageModal memberId="member-1" showCoverageModal setShowCoverageModal={jest.fn()} />
    );
    expect(screen.getByText('Ada Lovelace')).toBeInTheDocument();
    expect(screen.getByText('Jan 02, 2024')).toBeInTheDocument();
    expect(screen.getByText('Dec 31, 2024')).toBeInTheDocument();
  });

  it('does not render coverage while coverage is loading', () => {
    TransactionsStatement.GetCoverageByMember.mockReturnValue({ isLoading: true });
    render(
      <MemberCoverageModal memberId="member-1" showCoverageModal setShowCoverageModal={jest.fn()} />
    );
    expect(screen.getByText('Ada Lovelace')).toBeInTheDocument();
    expect(screen.queryByText('Start Date:')).not.toBeInTheDocument();
  });
});
