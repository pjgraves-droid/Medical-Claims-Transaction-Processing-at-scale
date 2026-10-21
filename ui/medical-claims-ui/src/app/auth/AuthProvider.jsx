'use client';

import { useEffect, useState } from 'react';
import { EventType, InteractionType } from '@azure/msal-browser';
import { MsalAuthenticationTemplate, MsalProvider } from '@azure/msal-react';
import { apiScopes, authEnabled, getMsalInstance } from './msal';

export default function AuthProvider({ children }) {
	const [ msal, setMsal ] = useState(null);

	useEffect(() => {
		if (!authEnabled) return;
		const instance = getMsalInstance();
		instance.initialize().then(() => {
			instance.addEventCallback((event) => {
				if (event.eventType === EventType.LOGIN_SUCCESS && event.payload?.account) {
					instance.setActiveAccount(event.payload.account);
				}
			});
			setMsal(instance);
		});
	}, []);

	if (!authEnabled) return children;
	if (!msal) return null;

	return (
		<MsalProvider instance={msal}>
			<MsalAuthenticationTemplate interactionType={InteractionType.Redirect} authenticationRequest={{ scopes: apiScopes }}>
				{children}
			</MsalAuthenticationTemplate>
		</MsalProvider>
	);
}
