import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { adminPassword, api, ApiError } from '../api'
import { exchangeDetail, exchangeSummary } from '../test/fixtures'
import { renderAt } from '../test/utils'
import AdminPage from './AdminPage'

function renderPage() {
  return renderAt(<AdminPage />, { route: '/admin', path: '/admin' })
}

describe('AdminPage', () => {
  it('asks for the password when not logged in', () => {
    const list = vi.spyOn(api, 'listExchanges')

    renderPage()

    expect(screen.getByRole('heading', { name: 'Organiser login' })).toBeInTheDocument()
    expect(list).not.toHaveBeenCalled()
  })

  it('lists exchanges once logged in', async () => {
    adminPassword.set('hunter2')
    vi.spyOn(api, 'listExchanges').mockResolvedValue([
      exchangeSummary(),
      exchangeSummary({ id: 'ex-2', name: 'Office party', participantCount: 8, isDrawn: true }),
    ])

    renderPage()

    const office = (await screen.findByText('Office party')).closest('a')!
    expect(office).toHaveAttribute('href', '/admin/exchanges/ex-2')
    expect(within(office).getByText(/8 people/)).toBeInTheDocument()
    expect(within(office).getByText('Drawn')).toBeInTheDocument()
    const family = screen.getByText('Family Christmas').closest('a')!
    expect(within(family).getByText('Not drawn')).toBeInTheDocument()
  })

  it('says when there are no exchanges yet', async () => {
    adminPassword.set('hunter2')
    vi.spyOn(api, 'listExchanges').mockResolvedValue([])

    renderPage()

    expect(await screen.findByText(/no exchanges yet/i)).toBeInTheDocument()
  })

  it('goes back to the login form when the stored password is rejected', async () => {
    adminPassword.set('stale')
    vi.spyOn(api, 'listExchanges').mockRejectedValue(new ApiError(401, 'Incorrect admin password.'))

    renderPage()

    expect(await screen.findByRole('heading', { name: 'Organiser login' })).toBeInTheDocument()
    expect(adminPassword.get()).toBeNull()
  })

  it('shows other loading errors', async () => {
    adminPassword.set('hunter2')
    vi.spyOn(api, 'listExchanges').mockRejectedValue(new ApiError(500, 'Request failed (500).'))

    renderPage()

    expect(await screen.findByText('Request failed (500).')).toBeInTheDocument()
  })

  it('logs out', async () => {
    const user = userEvent.setup()
    adminPassword.set('hunter2')
    vi.spyOn(api, 'listExchanges').mockResolvedValue([])
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Log out' }))

    expect(screen.getByRole('heading', { name: 'Organiser login' })).toBeInTheDocument()
    expect(adminPassword.get()).toBeNull()
  })

  it('creates an exchange from the form and opens it', async () => {
    const user = userEvent.setup()
    adminPassword.set('hunter2')
    vi.spyOn(api, 'listExchanges').mockResolvedValue([])
    const create = vi.spyOn(api, 'createExchange').mockResolvedValue(exchangeDetail({ id: 'new-ex' }))
    renderPage()

    await user.type(await screen.findByLabelText('Name'), 'Family Christmas')
    await user.type(screen.getByLabelText('Date'), '2026-12-25')
    await user.type(screen.getByLabelText('Description'), '£30 budget')
    await user.type(screen.getByLabelText('People (one per line)'), ' Alice \n\nBob\nCara ')
    await user.click(screen.getByRole('button', { name: 'Create exchange' }))

    expect(create).toHaveBeenCalledWith({
      name: 'Family Christmas',
      date: '2026-12-25',
      description: '£30 budget',
      participants: ['Alice', 'Bob', 'Cara'],
    })
    expect(screen.getByTestId('location')).toHaveTextContent('/admin/exchanges/new-ex')
  })

  it('shows an error when creating fails', async () => {
    const user = userEvent.setup()
    adminPassword.set('hunter2')
    vi.spyOn(api, 'listExchanges').mockResolvedValue([])
    vi.spyOn(api, 'createExchange').mockRejectedValue(new ApiError(400, 'Participant names must be unique.'))
    renderPage()

    await user.type(await screen.findByLabelText('Name'), 'Xmas')
    await user.type(screen.getByLabelText('Date'), '2026-12-25')
    await user.click(screen.getByRole('button', { name: 'Create exchange' }))

    expect(await screen.findByText('Participant names must be unique.')).toBeInTheDocument()
    expect(screen.getByTestId('location')).toHaveTextContent('/admin')
  })
})
