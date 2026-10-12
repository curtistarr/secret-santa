import type { ExchangeDetail, ExchangeSummary, Invite, Participant } from '../api'

export function participant(name: string, overrides: Partial<Participant> = {}): Participant {
  return { id: `id-${name.toLowerCase()}`, name, inviteToken: `token-${name.toLowerCase()}`, hasViewed: false, ...overrides }
}

export function exchangeDetail(overrides: Partial<ExchangeDetail> = {}): ExchangeDetail {
  return {
    id: 'ex-1',
    name: 'Family Christmas',
    date: '2026-12-25',
    description: '£30 budget',
    drawnAt: null,
    participants: [participant('Alice'), participant('Bob'), participant('Cara')],
    ...overrides,
  }
}

export function exchangeSummary(overrides: Partial<ExchangeSummary> = {}): ExchangeSummary {
  return { id: 'ex-1', name: 'Family Christmas', date: '2026-12-25', participantCount: 3, isDrawn: false, ...overrides }
}

export function invite(overrides: Partial<Invite> = {}): Invite {
  return {
    exchangeName: 'Family Christmas',
    date: '2026-12-25',
    description: '£30 budget',
    participantCount: 3,
    yourName: 'Alice',
    recipientName: null,
    ...overrides,
  }
}
