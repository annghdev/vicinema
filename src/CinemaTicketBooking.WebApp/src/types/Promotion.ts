export interface PromotionProgramDto {
  id: string;
  name: string;
  description?: string;
  posterImage?: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  discountType: string;
  discountForm?: string;
  discountValue: number;
  maxDiscountAmount?: number;
  maxDiscountPercentage?: number;
  maxUsagePerCustomer?: number;
  conditionCount: number;
  createdAt: string;
}

export interface AppliedPromotionDto {
  promotionProgramId: string;
  promotionName: string;
  discountType: string;
  discountAmount: number;
}

export interface PromotionConditionDto {
  id: string | null;
  conditionType: string;
  value: string | null;
  secondaryValue: string | null;
}

export interface PromotionFreeConcessionItemDto {
  id: string | null;
  concessionId: string;
  quantity: number;
}

export interface PromotionProgramDetailDto extends PromotionProgramDto {
  isDeleted: boolean;
  conditions: PromotionConditionDto[];
  freeConcessionItems: PromotionFreeConcessionItemDto[];
  updatedAt: string | null;
}

export interface FreeConcessionItemDto {
  concessionId: string;
  concessionName: string;
  quantity: number;
}
