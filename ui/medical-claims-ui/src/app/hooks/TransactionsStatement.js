"use client";

import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import { getAccessToken } from "../auth/msal";

const API_URL = process.env.NEXT_PUBLIC_API_URL;

const request = async (url, { method = "GET", body } = {}) => {
  const headers = { Accept: "application/json" };
  const token = await getAccessToken();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers["Content-Type"] = "application/json";

  const res = await fetch(url, { method, headers, body: body !== undefined ? JSON.stringify(body) : undefined });
  if (!res.ok) {
    const error = new Error(`Request to ${url} failed with ${res.status}`);
    error.status = res.status;
    throw error;
  }
  const contentType = res.headers.get("content-type") ?? "";
  if (res.status === 204) return null;
  return contentType.includes("json") ? res.json() : res.text();
};

const fetcher = (url) => request(url);

const put = (url, { arg }) => request(url, { method: "PUT", body: arg });

const post = (url, { arg }) => request(url, { method: "POST", body: arg });


// Function to calculate the offset
const calculateOffset = (currentPage, pageSize) => {
  return (currentPage - 1) * pageSize;
};

const buildQueryString = ({ page, pageSize = 10, sort }) => {
  const offset = calculateOffset(page, pageSize);

  const params = {};
  params.offset = offset;
  params.limit = pageSize;
  sort?.column && sort?.direction && (params.sortColumn = sort.column);
  sort?.direction && sort?.column && (params.sortDirection = sort.direction);

  return new URLSearchParams(params).toString();
}


const GetMembersList = ({ page = 1, sort }) => {
  const queryString = buildQueryString({ page, sort });
  return useSWR(`${API_URL}/members?${queryString}`, fetcher);
};

const GetMember = (memberId) => {
  return useSWR(`${API_URL}/member/${memberId}`, fetcher);
};

const GetCoverageByMember = (memberId) => {
  return useSWR(`${API_URL}/member/${memberId}/coverage`, fetcher);
};

const GetClaimsByMemberId = (memberId, { page, sort }) => {
  const queryString = buildQueryString({ page, sort });  
  return useSWR(`${API_URL}/member/${memberId}/claims?${queryString}`, fetcher);
};

const GetAdjudicator = (adjudicatorId) => {
  return useSWR(`${API_URL}/adjudicator/${adjudicatorId}`, fetcher);
};

const GetClaimsByAdjudicatorId = (adjudicatorId, { page, sort }) => {
  const queryString = buildQueryString({ page, sort });  
  return useSWR(`${API_URL}/adjudicator/${adjudicatorId}/claims?${queryString}`, fetcher);
};

const GetProviders = ({ page = 1, sort }) => {
  const queryString = buildQueryString({ page, sort });
  return useSWR(`${API_URL}/providers?${queryString}`, fetcher);
};

const GetPayers = ({ page = 1, sort }) => {
  const queryString = buildQueryString({ page, sort });
  return useSWR(`${API_URL}/payers?${queryString}`, fetcher);
};

const GetClaimDetails = (claimId) => {
  return useSWR(`${API_URL}/claim/${claimId}`, fetcher);
};

const GetClaimHistory = (claimId) => {
  return useSWR(`${API_URL}/claim/${claimId}/history`, fetcher);
};

const GetClaimRecommendation = (claimId) =>
  useSWRMutation(`${API_URL}/claim/${claimId}/recommendation`, fetcher);

export const UpdateClaim = (claimId) =>
  useSWRMutation(`${API_URL}/claim/${claimId}`, put);

export const AcknowledgeClaim = (claimId) =>
  useSWRMutation(`${API_URL}/claim/${claimId}/acknowledge`, post);

export const GetBusinessRules = () =>
  useSWR(`${API_URL}/business-rules`, fetcher);

const TransactionsStatement = {
  GetMembersList,
  GetMember,
  GetAdjudicator,
  GetCoverageByMember,
  GetClaimsByMemberId,
  GetClaimsByAdjudicatorId,
  GetClaimDetails,
  GetProviders,
  GetPayers,
  GetClaimHistory,
  GetClaimRecommendation,
  UpdateClaim,
  GetBusinessRules
};

export default TransactionsStatement;
