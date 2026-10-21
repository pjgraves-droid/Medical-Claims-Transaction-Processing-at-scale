import { PublicClientApplication } from '@azure/msal-browser';

const clientId = process.env.NEXT_PUBLIC_AAD_CLIENT_ID;
const tenantId = process.env.NEXT_PUBLIC_AAD_TENANT_ID;

export const apiScopes = process.env.NEXT_PUBLIC_API_SCOPE ? [process.env.NEXT_PUBLIC_API_SCOPE] : [];

// Sign-in is enabled only when an Entra ID SPA registration is configured.
export const authEnabled = Boolean(clientId && tenantId && apiScopes.length);

let instance;

export function getMsalInstance() {
	if (!authEnabled || typeof window === 'undefined') return null;
	instance ??= new PublicClientApplication({
		auth: {
			clientId,
			authority: `https://login.microsoftonline.com/${tenantId}`,
			redirectUri: window.location.origin,
		},
		cache: { cacheLocation: 'sessionStorage' },
	});
	return instance;
}

export async function getAccessToken() {
	const msal = getMsalInstance();
	if (!msal) return null;
	await msal.initialize();
	const account = msal.getActiveAccount() ?? msal.getAllAccounts()[0];
	if (!account) return null;
	try {
		const result = await msal.acquireTokenSilent({ scopes: apiScopes, account });
		return result.accessToken;
	} catch {
		await msal.acquireTokenRedirect({ scopes: apiScopes, account });
		return null;
	}
}
