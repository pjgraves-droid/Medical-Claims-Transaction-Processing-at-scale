import { format, isValid, parse, parseISO } from 'date-fns';
import { utc } from '@date-fns/utc';

const toDate = (value) => (value instanceof Date ? value : typeof value === 'string' ? parseISO(value) : new Date(value));

export const FormatDateValue = (value, pattern = 'yyyy-MM-dd') => {
  const date = toDate(value);
  return isValid(date) ? format(date, pattern) : 'Invalid date';
};

export const FormatDate = (dateString) => {
  const parsers = [
    (s) => parseISO(s, { in: utc }),
    (s) => parse(s, 'MM/dd/yyyy h:mm:ss a', new Date(), { in: utc }),
  ];

  for (const tryParse of parsers) {
    const date = typeof dateString === 'string' ? tryParse(dateString) : null;
    if (date && isValid(date)) {
      return format(date, 'MMMM dd, yyyy hh:mm aaa', { in: utc });
    }
  }

  return 'Invalid date';
};

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
export const FormatMoney = (value) => {
  return money.format(value);
}

const Formatters = {
  FormatDate,
  FormatDateValue,
  FormatMoney
}

export default Formatters;
