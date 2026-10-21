'use client';

import { Dialog, DialogPanel, DialogTitle, Tab, TabGroup, TabList, TabPanel, TabPanels } from '@headlessui/react';

const cx = (...classes) => classes.filter(Boolean).join(' ');

/* Table */
export function Table({ className, hoverable, children }) {
	return (
		<table className={cx('w-full text-left text-sm text-gray-500', hoverable && 'group/table', className)}>
			{children}
		</table>
	);
}
Table.Head = function TableHead({ children }) {
	return (
		<thead className="bg-gray-50 text-xs uppercase text-gray-700">
			<tr>{children}</tr>
		</thead>
	);
};
Table.HeadCell = function TableHeadCell({ className, children, ...props }) {
	return <th className={cx('px-6 py-3', className)} {...props}>{children}</th>;
};
Table.Body = function TableBody({ className, children }) {
	return <tbody className={className}>{children}</tbody>;
};
Table.Row = function TableRow({ className, children }) {
	return <tr className={cx('group-[.group\\/table]/table:hover:bg-gray-50', className)}>{children}</tr>;
};
Table.Cell = function TableCell({ className, children, ...props }) {
	return <td className={cx('px-6 py-4', className)} {...props}>{children}</td>;
};

/* Spinner */
const spinnerSizes = { sm: 'h-4 w-4', md: 'h-6 w-6', lg: 'h-8 w-8' };
export function Spinner({ size = 'lg', color, ...props }) {
	return (
		<span role="status" {...props}>
			<svg className={cx('inline animate-spin', spinnerSizes[size] ?? spinnerSizes.lg, color === 'white' ? 'text-white' : 'text-gray-600')}
				viewBox="0 0 24 24" fill="none" aria-hidden="true">
				<circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
				<path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
			</svg>
		</span>
	);
}

/* Button / Textarea */
const buttonColors = {
	dark: 'bg-gray-800 text-white hover:bg-gray-900',
	light: 'border border-gray-300 bg-white text-gray-900 hover:bg-gray-100',
};
export function Button({ color = 'dark', className, children, ...props }) {
	return (
		<button type="button" className={cx('inline-flex items-center justify-center rounded-lg px-5 py-2.5 text-sm font-medium', buttonColors[color], className)} {...props}>
			{children}
		</button>
	);
}
export function Textarea({ className, ...props }) {
	return <textarea className={cx('block w-full rounded-lg border border-gray-300 bg-gray-50 text-sm text-gray-900', className)} {...props} />;
}

/* Pagination */
const pageWindow = (current, total, size = 5) => {
	const start = Math.max(1, Math.min(current - Math.floor(size / 2), total - size + 1));
	return Array.from({ length: Math.min(size, total) }, (_, i) => start + i);
};
export function Pagination({ currentPage, totalPages, onPageChange, className }) {
	const nav = 'rounded-md bg-white px-3 py-2 leading-tight text-gray-500 enabled:hover:bg-gray-200 enabled:hover:text-gray-700 disabled:opacity-50';
	return (
		<nav className={className} aria-label="Pagination">
			<ul className="mt-2 inline-flex items-center -space-x-px">
				<li><button className={nav} disabled={currentPage <= 1} onClick={() => onPageChange(currentPage - 1)}>Previous</button></li>
				{pageWindow(currentPage, totalPages).map((p) => (
					<li key={p}>
						<button aria-current={p === currentPage ? 'page' : undefined} onClick={() => onPageChange(p)}
							className={cx('mx-1 w-9 rounded-md py-2 leading-tight', p === currentPage ? 'bg-[#444444]! text-white!' : 'bg-gray-100 hover:bg-gray-200')}>
							{p}
						</button>
					</li>
				))}
				<li><button className={nav} disabled={currentPage >= totalPages} onClick={() => onPageChange(currentPage + 1)}>Next</button></li>
			</ul>
		</nav>
	);
}

/* Modal (Headless UI Dialog) */
const modalSizes = { md: 'max-w-md', lg: 'max-w-lg', xl: 'max-w-xl', xxl: 'max-w-4xl' };
export function Modal({ show, onClose, size = 'xl', children }) {
	return (
		<Dialog open={!!show} onClose={onClose} className="relative z-50">
			<div className="fixed inset-0 bg-black/25" aria-hidden="true" />
			<div className="fixed inset-0 flex items-center justify-center overflow-y-auto p-4">
				<DialogPanel className={cx('w-full rounded-lg bg-white shadow-lg', modalSizes[size] ?? modalSizes.xl)}>
					{children}
				</DialogPanel>
			</div>
		</Dialog>
	);
}
Modal.Header = function ModalHeader({ className, children }) {
	return <DialogTitle as="div" className={cx('flex items-start justify-between border-b border-gray-200 font-semibold', className)}>{children}</DialogTitle>;
};
Modal.Body = function ModalBody({ className, children }) {
	return <div className={cx('p-6', className)}>{children}</div>;
};
Modal.Footer = function ModalFooter({ className, children }) {
	return <div className={cx('flex items-center justify-end border-t border-gray-200 p-6', className)}>{children}</div>;
};

/* Tabs (Headless UI TabGroup) */
export function Tabs({ items, ...props }) {
	return (
		<TabGroup className="flex flex-col" {...props}>
			<TabList className="flex border-b border-gray-200">
				{items.map((item) => (
					<Tab key={item.title}
						className="rounded-t-lg p-4 text-sm font-medium text-gray-500 hover:bg-gray-50 hover:text-gray-600 focus:outline-hidden data-selected:bg-gray-100 data-selected:text-gray-900">
						{item.title}
					</Tab>
				))}
			</TabList>
			<TabPanels>
				{items.map((item) => <TabPanel key={item.title}>{item.content}</TabPanel>)}
			</TabPanels>
		</TabGroup>
	);
}
