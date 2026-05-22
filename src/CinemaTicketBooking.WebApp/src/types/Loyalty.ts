export type LoyaltyTier = "Bronze" | "Silver" | "Gold" | "Platinum" | "Diamond" | "Ruby"

export type LoyaltyTierDto = {
  id: string
  tier: LoyaltyTier
  name: string
  minPoints: number
  maxPoints: number | null
  ticketDiscountPercent: number
  concessionDiscountPercent: number
  description: string
  isActive: boolean
}

export type CustomerLoyaltyDto = {
  customerId: string
  currentTier: LoyaltyTier
  tierName: string
  accumulatedPoints: number
  pointsToNextTier: number | null
  ticketDiscountPercent: number
  concessionDiscountPercent: number
  nextTier: LoyaltyTier | null
}

/** Display labels for each tier */
export const TIER_LABELS: Record<LoyaltyTier, string> = {
  Bronze: "Đồng (Bronze)",
  Silver: "Bạc (Silver)",
  Gold: "Vàng (Gold)",
  Platinum: "Bạch Kim (Platinum)",
  Diamond: "Kim Cương (Diamond)",
  Ruby: "Ruby",
}

/** Map numeric tier from API (0-5) to string key */
export const TIER_NUMBER_MAP: Record<number, LoyaltyTier> = {
  0: "Bronze",
  1: "Silver",
  2: "Gold",
  3: "Platinum",
  4: "Diamond",
  5: "Ruby",
}

/** Tier icons for each level */
export const TIER_ICONS: Record<LoyaltyTier, string> = {
  Bronze: "payments",
  Silver: "diamond",
  Gold: "star_rate",
  Platinum: "workspace_premium",
  Diamond: "diamond",
  Ruby: "favorite",
}

/** Color mappings for each tier card UI */
export const TIER_COLORS: Record<LoyaltyTier, { gradient: string; textColor: string; bgBadge: string }> = {
  Bronze:   { gradient: "from-amber-800/40 to-amber-950/30 border-amber-700/50",    textColor: "text-amber-300",  bgBadge: "bg-amber-900/60" },
  Silver:   { gradient: "from-slate-400/30 to-slate-700/30 border-slate-400/40",    textColor: "text-slate-200",  bgBadge: "bg-slate-700/60" },
  Gold:     { gradient: "from-yellow-500/30 to-yellow-800/30 border-yellow-500/40", textColor: "text-yellow-300", bgBadge: "bg-yellow-800/60" },
  Platinum: { gradient: "from-indigo-400/30 to-indigo-800/30 border-indigo-400/40", textColor: "text-indigo-300", bgBadge: "bg-indigo-800/60" },
  Diamond:  { gradient: "from-cyan-400/30 to-cyan-800/30 border-cyan-400/40",       textColor: "text-cyan-300",   bgBadge: "bg-cyan-800/60" },
  Ruby:     { gradient: "from-rose-500/30 to-rose-900/30 border-rose-500/40",       textColor: "text-rose-300",   bgBadge: "bg-rose-800/60" },
}
