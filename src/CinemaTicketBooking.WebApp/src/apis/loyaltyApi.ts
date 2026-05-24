import { httpClient } from "./httpClient"
import {
  type CustomerLoyaltyDto,
  type LoyaltyTierDto,
} from "../types/Loyalty"

export async function getCustomerLoyalty(): Promise<CustomerLoyaltyDto> {
  const response = await httpClient.get<CustomerLoyaltyDto>("/api/loyalty/me")
  return response.data
}

export async function getActiveTiers(): Promise<LoyaltyTierDto[]> {
  const response = await httpClient.get<LoyaltyTierDto[]>("/api/loyalty/tiers")
  return response.data
}
