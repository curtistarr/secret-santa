export interface ExchangeSummary {
  id: string
  name: string
  date: string
  participantCount: number
  isDrawn: boolean
}

export interface Participant {
  id: string
  name: string
  inviteToken: string
  hasViewed: boolean
}

export interface ExchangeDetail {
  id: string
  name: string
  date: string
  description: string
  drawnAt: string | null
  participants: Participant[]
}

export interface Invite {
  exchangeName: string
  date: string
  description: string
  participantCount: number
  yourName: string
  recipientName: string | null
}

export class ApiError extends Error {
  readonly status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

const PASSWORD_KEY = 'secret-santa-admin-password'

export const adminPassword = {
  get: () => localStorage.getItem(PASSWORD_KEY),
  set: (value: string) => localStorage.setItem(PASSWORD_KEY, value),
  clear: () => localStorage.removeItem(PASSWORD_KEY),
}

async function request<T>(method: string, url: string, body?: unknown, password?: string | null): Promise<T> {
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (password) headers['X-Admin-Password'] = password

  const res = await fetch(url, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!res.ok) {
    let message = res.status === 401 ? 'Incorrect admin password.' : `Request failed (${res.status}).`
    try {
      const problem = await res.json()
      if (problem?.detail) message = problem.detail
    } catch {
      // Response had no JSON body; keep the default message.
    }
    throw new ApiError(res.status, message)
  }

  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}

const admin = <T>(method: string, path: string, body?: unknown, password = adminPassword.get()) =>
  request<T>(method, `/api/admin${path}`, body, password)

export const api = {
  login: (password: string) => admin<void>('POST', '/login', undefined, password),
  listExchanges: () => admin<ExchangeSummary[]>('GET', '/exchanges'),
  getExchange: (id: string) => admin<ExchangeDetail>('GET', `/exchanges/${id}`),
  createExchange: (input: { name: string; date: string; description: string; participants: string[] }) =>
    admin<ExchangeDetail>('POST', '/exchanges', input),
  updateExchange: (id: string, input: { name: string; date: string; description: string }) =>
    admin<ExchangeDetail>('PUT', `/exchanges/${id}`, input),
  deleteExchange: (id: string) => admin<void>('DELETE', `/exchanges/${id}`),
  addParticipant: (id: string, name: string) => admin<ExchangeDetail>('POST', `/exchanges/${id}/participants`, { name }),
  removeParticipant: (id: string, participantId: string) =>
    admin<ExchangeDetail>('DELETE', `/exchanges/${id}/participants/${participantId}`),
  draw: (id: string) => admin<ExchangeDetail>('POST', `/exchanges/${id}/draw`),
  resetDraw: (id: string) => admin<ExchangeDetail>('POST', `/exchanges/${id}/reset`),
  getInvite: (token: string) => request<Invite>('GET', `/api/invite/${encodeURIComponent(token)}`),
}

export function formatDate(isoDate: string) {
  const [y, m, d] = isoDate.split('-').map(Number)
  return new Date(y, m - 1, d).toLocaleDateString(undefined, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  })
}

export function inviteUrl(token: string) {
  return `${window.location.origin}/invite/${token}`
}
