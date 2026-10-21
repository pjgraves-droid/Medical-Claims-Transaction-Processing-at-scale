'use client';

import { Tabs } from '~/app/components/ui';
import ClaimsByAdjudicator from './ClaimsByAdjudicator';


const tabs = [
	{ title: 'Non-Manager', content: <ClaimsByAdjudicator adjudicatorId="df166300-5a78-3502-a46a-832842197811" isManager={false} /> },
	{ title: 'Manager', content: <ClaimsByAdjudicator adjudicatorId="a735bf55-83e9-331a-899d-a82a60b9f60c" isManager={true} /> },
];

export default function Adjudicator() {
	return <Tabs items={tabs} aria-label="Adjudicator tabs" />;
}
