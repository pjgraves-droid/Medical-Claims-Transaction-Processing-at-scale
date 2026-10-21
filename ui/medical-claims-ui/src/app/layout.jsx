
import './assets/css/tailwind.css';
import './tailwind.css';
import './globals.scss';
import Navbar from './components/navbar';
import AuthProvider from './auth/AuthProvider';


export const metadata = {
	title: 'Azure Cosmos DB Medical Claims Demo App',
	description: '',
}

export default function RootLayout({ children }) {
	return (
		<html lang="en" data-layout-mode="light">
			<body>
				<AuthProvider>
					<Navbar>
						{ children }
					</Navbar>
				</AuthProvider>
			</body>
		</html>
	);
}
