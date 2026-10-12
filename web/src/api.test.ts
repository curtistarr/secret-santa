import { beforeEach, describe, expect, it, vi } from 'vitest'
import { adminPassword, api, ApiError, formatDate, inviteUrl } from './api'

const fetchMock = vi.fn<typeof fetch>()

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

function lastRequest() {
  const [url, init] = fetchMock.mock.calls.at(-1)!
  return { url, init: init!, headers: init!.headers as Record<string, string> }
}

beforeEach(() => {
  fetchMock.mockReset()
  vi.stubGlobal('fetch', fetchMock)
})

describe('adminPassword', () => {
  it('stores, reads and clears the password', () => {
    expect(adminPassword.get()).toBeNull()

    adminPassword.set('hunter2')
    expect(adminPassword.get()).toBe('hunter2')

    adminPassword.clear()
    expect(adminPassword.get()).toBeNull()
  })
})

describe('admin requests', () => {
  it('login sends the given password, not the stored one', async () => {
    adminPassword.set('old')
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))

    await expect(api.login('new')).resolves.toBeUndefined()

    const { url, init, headers } = lastRequest()
    expect(url).toBe('/api/admin/login')
    expect(init.method).toBe('POST')
    expect(headers['X-Admin-Password']).toBe('new')
    expect(init.body).toBeUndefined()
  })

  it('sends the stored password and a JSON body', async () => {
    adminPassword.set('hunter2')
    fetchMock.mockResolvedValue(jsonResponse({ id: 'ex-1' }, 201))
    const input = { name: 'Xmas', date: '2026-12-25', description: '', participants: ['Alice', 'Bob'] }

    await expect(api.createExchange(input)).resolves.toEqual({ id: 'ex-1' })

    const { url, init, headers } = lastRequest()
    expect(url).toBe('/api/admin/exchanges')
    expect(init.method).toBe('POST')
    expect(headers['X-Admin-Password']).toBe('hunter2')
    expect(headers['Content-Type']).toBe('application/json')
    expect(JSON.parse(init.body as string)).toEqual(input)
  })

  it.each([
    ['listExchanges', () => api.listExchanges(), 'GET', '/api/admin/exchanges'],
    ['getExchange', () => api.getExchange('ex-1'), 'GET', '/api/admin/exchanges/ex-1'],
    ['updateExchange', () => api.updateExchange('ex-1', { name: 'X', date: '2026-12-25', description: '' }), 'PUT', '/api/admin/exchanges/ex-1'],
    ['addParticipant', () => api.addParticipant('ex-1', 'Dan'), 'POST', '/api/admin/exchanges/ex-1/participants'],
    ['removeParticipant', () => api.removeParticipant('ex-1', 'p-1'), 'DELETE', '/api/admin/exchanges/ex-1/participants/p-1'],
    ['draw', () => api.draw('ex-1'), 'POST', '/api/admin/exchanges/ex-1/draw'],
    ['resetDraw', () => api.resetDraw('ex-1'), 'POST', '/api/admin/exchanges/ex-1/reset'],
  ])('%s calls the right endpoint', async (_name, call, method, url) => {
    adminPassword.set('hunter2')
    fetchMock.mockResolvedValue(jsonResponse({}))

    await call()

    expect(lastRequest().url).toBe(url)
    expect(lastRequest().init.method).toBe(method)
    expect(lastRequest().headers['X-Admin-Password']).toBe('hunter2')
  })

  it('deleteExchange resolves to undefined on 204', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))

    await expect(api.deleteExchange('ex-1')).resolves.toBeUndefined()
    expect(lastRequest().init.method).toBe('DELETE')
  })
})

describe('getInvite', () => {
  it('URL-encodes the token and sends no admin password', async () => {
    adminPassword.set('hunter2')
    fetchMock.mockResolvedValue(jsonResponse({ yourName: 'Alice' }))

    await api.getInvite('a/b c')

    expect(lastRequest().url).toBe('/api/invite/a%2Fb%20c')
    expect(lastRequest().headers['X-Admin-Password']).toBeUndefined()
  })
})

describe('errors', () => {
  it('uses the problem detail from the server', async () => {
    fetchMock.mockResolvedValue(jsonResponse({ status: 409, detail: 'Names have already been drawn.' }, 409))

    const error = await api.draw('ex-1').catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 409, message: 'Names have already been drawn.' })
  })

  it('explains a 401 without a body as a wrong password', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 401 }))

    await expect(api.login('wrong')).rejects.toMatchObject({ status: 401, message: 'Incorrect admin password.' })
  })

  it('falls back to a generic message when the body is not JSON', async () => {
    fetchMock.mockResolvedValue(new Response('<html>oops</html>', { status: 500 }))

    await expect(api.listExchanges()).rejects.toMatchObject({ status: 500, message: 'Request failed (500).' })
  })
})

describe('formatDate', () => {
  it('formats the calendar date without shifting it across time zones', () => {
    const expected = new Date(2026, 11, 25).toLocaleDateString(undefined, {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    })

    expect(formatDate('2026-12-25')).toBe(expected)
  })
})

describe('inviteUrl', () => {
  it('builds an absolute link from the current origin', () => {
    expect(inviteUrl('abc123')).toBe(`${window.location.origin}/invite/abc123`)
  })
})
