
import { FormatDateAs } from '../hooks/Formatters';
import TransactionsStatement from '../hooks/TransactionsStatement';
import Modal from '../components/modal';


export default function MemberCoverageModal({ memberId, showCoverageModal, setShowCoverageModal }) {
	const memberRequest = TransactionsStatement.GetMember(memberId);
	const coverageRequest = TransactionsStatement.GetCoverageByMember(memberId);

	return(
		<Modal title="Member Coverage" showModal={showCoverageModal} setShowModal={setShowCoverageModal} showSaveButton={false}>
			{((!memberRequest.isLoading && memberRequest.data) ? (
				<>
					<h4 className="font-semibold mb-10">
						{memberRequest.data.firstName} {memberRequest.data.lastName}
					</h4>
					{!coverageRequest.isLoading ? (
						<div className="grid grid-cols-2">
							<div>Start Date:</div>
							<div>{ FormatDateAs(coverageRequest.data.startDate, 'MMM dd, yyyy') }</div>
							<div>End Date:</div>
							<div>{ FormatDateAs(coverageRequest.data.endDate, 'MMM dd, yyyy') }</div>						
						</div>
					) : null}
				</>
			) : null)}
		</Modal>
	);
}	