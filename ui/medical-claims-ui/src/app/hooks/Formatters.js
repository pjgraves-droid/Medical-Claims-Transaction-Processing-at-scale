import { format, isValid, parse, parseISO } from 'date-fns';
import { UTCDate } from '@date-fns/utc';

const INVALID_DATE = 'Invalid date';

export const FormatDateAs = (value, pattern) => {
  if (value === null || value === undefined || value === '') {
    return INVALID_DATE;
  }
  const date = value instanceof Date ? value : parseISO(String(value));
  return isValid(date) ? format(date, pattern) : INVALID_DATE;
};

export const FormatDate = (dateString) => {
  const parsers = [
    (s) => parseISO(s), // "YYYY-MM-DDTHH:mm:ssZ"
    (s) => parse(s, 'MM/dd/yyyy h:mm:ss a', new UTCDate()) // "MM/DD/YYYY h:mm:ss A"
  ];

  for (const parser of parsers) {
    const date = parser(String(dateString));
    if (isValid(date)) {
      return format(new UTCDate(date), 'MMMM dd, yyyy hh:mm aaa');
    }
  }

  return INVALID_DATE; // Return default value if parsing fails for all formats
};

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
export const FormatMoney = (value) => {
  return money.format(value);
}

const Formatters = {
  FormatDate,
  FormatDateAs,
  FormatMoney
}

export default Formatters;
