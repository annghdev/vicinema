import { httpClient } from "./httpClient"
import type { PromotionProgramDetailDto, PromotionProgramDto } from "../types/Promotion"

export async function getActivePromotions(): Promise<PromotionProgramDto[]> {
  const response = await httpClient.get<{ promotions: PromotionProgramDto[] }>("/api/promotions/active")
  return response.data.promotions
}

export async function getPromotionById(id: string): Promise<PromotionProgramDetailDto> {
  const response = await httpClient.get<PromotionProgramDetailDto>(`/api/promotions/${id}`)
  return response.data
}
