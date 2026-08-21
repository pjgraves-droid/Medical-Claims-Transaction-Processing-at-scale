import { FormatDate, FormatMoney } from './Formatters';

describe('Formatters', () => {
  it.each([
    ['2024-01-02T15:04:05Z', 'January 02, 2024 03:04 pm'],
    ['01/02/2024 3:04:05 PM', 'January 02, 2024 03:04 pm'],
  ])('formats supported date %s', (input, expected) => {
    expect(FormatDate(input)).toBe(expected);
  });

  it('returns Invalid date for unsupported input', () => {
    expect(FormatDate('not-a-date')).toBe('Invalid date');
  });

  it('formats money as US dollars', () => {
    expect(FormatMoney(1234.5)).toBe('$1,234.50');
  });
});
