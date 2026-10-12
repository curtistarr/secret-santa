import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { adminPassword, api, ApiError, inviteUrl } from '../api'
import { exchangeDetail, participant } from '../test/fixtures'
import { renderAt } from '../test/utils'
import ExchangePage from './ExchangePage'

function renderPage() {
  return renderAt(<ExchangePage />, { route: '/admin/exchanges/ex-1', path: '/admin/exchanges/:id' })
}

function rowFor(name: string) {
  return screen.getByText(name).closest('li')!
}

beforeEach(() => {
  adminPassword.set('hunter2')
})

describe('ExchangePage', () => {
  it('shows the exchange details and everyone’s invite link', async () => {
    const get = vi.spyOn(api, 'getExchange').mockResolvedValue(exchangeDetail())

    renderPage()

    expect(await screen.findByRole('heading', { name: 'Family Christmas' })).toBeInTheDocument()
    expect(get).toHaveBeenCalledWith('ex-1')
    expect(screen.getByText('£30 budget')).toBeInTheDocument()
    expect(screen.getByText('People (3)')).toBeInTheDocument()
    expect(screen.getByText('Not drawn yet')).toBeInTheDocument()
    expect(within(rowFor('Alice')).getByText(inviteUrl('token-alice'))).toBeInTheDocument()
  })

  it('never shows who has who after the draw, only who has looked', async () => {
    vi.spyOn(api, 'getExchange').mockResolvedValue(
      exchangeDetail({
        drawnAt: '2026-12-01T10:00:00Z',
        participants: [participant('Alice', { hasViewed: true }), participant('Bob')],
      }),
    )

    renderPage()

    expect(await screen.findByText('Names drawn')).toBeInTheDocument()
    expect(within(rowFor('Alice')).getByText(/has looked/)).toBeInTheDocument()
    expect(within(rowFor('Bob')).getByText(/not looked yet/)).toBeInTheDocument()
    expect(screen.queryByPlaceholderText('Add a person')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /^Remove/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Draw names/ })).not.toBeInTheDocument()
  })

  it('sends the organiser back to log in when the password is rejected', async () => {
    vi.spyOn(api, 'getExchange').mockRejectedValue(new ApiError(401, 'Incorrect admin password.'))

    renderPage()

    await vi.waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent(/^\/admin$/))
    expect(adminPassword.get()).toBeNull()
  })

  it('shows loading errors', async () => {
    vi.spyOn(api, 'getExchange').mockRejectedValue(new ApiError(404, 'Request failed (404).'))

    renderPage()

    expect(await screen.findByText('Request failed (404).')).toBeInTheDocument()
  })

  it('adds a person', async () => {
    const user = userEvent.setup()
    vi.spyOn(api, 'getExchange').mockResolvedValue(exchangeDetail())
    const add = vi
      .spyOn(api, 'addParticipant')
      .mockResolvedValue(exchangeDetail({ participants: [...exchangeDetail().participants, participant('Dan')] }))
    renderPage()

    await user.type(await screen.findByPlaceholderText('Add a person'), 'Dan')
    await user.click(screen.getByRole('button', { name: 'Add' }))

    expect(add).toHaveBeenCalledWith('ex-1', 'Dan')
    expect(screen.getByText('Dan')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('Add a person')).toHaveValue('')
  })

  it('keeps the name and shows the error when adding fails', async () => {
    const user = userEvent.setup()
    vi.spyOn(api, 'getExchange').mockResolvedValue(exchangeDetail())
    vi.spyOn(api, 'addParticipant').mockRejectedValue(new ApiError(400, 'alice is already in this exchange.'))
    renderPage()

    await user.type(await screen.findByPlaceholderText('Add a person'), 'alice')
    await user.click(screen.getByRole('button', { name: 'Add' }))

    expect(await screen.findByText('alice is already in this exchange.')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('Add a person')).toHaveValue('alice')
  })

  it('removes a person', async () => {
    const user = userEvent.setup()
    vi.spyOn(api, 'getExchange').mockResolvedValue(exchangeDetail())
    const remove = vi
      .spyOn(api, 'removeParticipant')
      .mockResolvedValue(exchangeDetail({ participants: [participant('Alice'), participant('Cara')] }))
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Remove Bob' }))

    expect(remove).toHaveBeenCalledWith('ex-1', 'id-bob')
    expect(screen.queryByText('Bob')).not.toBeInTheDocument()
  })

  it('only allows drawing with at least two people', async () => {
    vi.spyOn(api, 'getExchange').mockResolvedValue(exchangeDetail({ participants: [participant('Alice')] }))

    renderPage()

    expect(await screen.findByRole('button', { name: /Draw names/ })).toBeDisabled()
  })

  it('draws names after confirmation', async () => {
    const user = userEvent.setup()
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    vi.spyOn(api, 'getExchange').mockResolvedValue(exchangeDetail())
    const draw = vi.spyOn(api, 'draw').mockResolvedValue(exchangeDetail({ drawnAt: '2026-12-01T10:00:00Z' }))
    renderPage()

    await user.click(await screen.findByRole('button', { name: /Draw names/ }))

    expect(window.confirm).toHaveBeenCalledWith(expect.stringContaining('3 people'))
    expect(draw).toHaveBeenCalledWith('ex-1')
    expect(screen.getByText('Names drawn')).toBeInTheDocument()
  })

  it('does not draw when the organiser cancels', async () => {
    const user = userEvent.setup()
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    vi.spyOn(api, 'getExchange').mockResolvedValue(exchangeDetail())
    const draw = vi.spyOn(api, 'draw')
    renderPage()

    await user.click(await screen.findByRole('button', { name: /Draw names/ }))

    expect(draw).not.toHaveBeenCalled()
  })

  it('resets the draw after confirmation', async () => {
    const user = userEvent.setup()
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    vi.spyOn(api, 'getExchange').mockResolvedValue(exchangeDetail({ drawnAt: '2026-12-01T10:00:00Z' }))
    const reset = vi.spyOn(api, 'resetDraw').mockResolvedValue(exchangeDetail())
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Reset draw' }))

    expect(reset).toHaveBeenCalledWith('ex-1')
    expect(screen.getByText('Not drawn yet')).toBeInTheDocument()
  })

  it('copies a person’s invite link', async () => {
    const user = userEvent.setup()
    vi.spyOn(api, 'getExchange').mockResolvedValue(exchangeDetail())
    renderPage()

    await screen.findByText('Alice')
    await user.click(within(rowFor('Alice')).getByRole('button', { name: 'Copy link' }))

    expect(await navigator.clipboard.readText()).toBe(inviteUrl('token-alice'))
    expect(within(rowFor('Alice')).getByRole('button', { name: 'Copied!' })).toBeInTheDocument()
  })

  it('edits the exchange details', async () => {
    const user = userEvent.setup()
    vi.spyOn(api, 'getExchange').mockResolvedValue(exchangeDetail())
    const update = vi.spyOn(api, 'updateExchange').mockResolvedValue(exchangeDetail({ name: 'Christmas Eve' }))
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Edit' }))
    await user.clear(screen.getByLabelText('Name'))
    await user.type(screen.getByLabelText('Name'), 'Christmas Eve')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(update).toHaveBeenCalledWith('ex-1', { name: 'Christmas Eve', date: '2026-12-25', description: '£30 budget' })
    expect(screen.getByRole('heading', { name: 'Christmas Eve' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument()
  })

  it('cancels editing without saving', async () => {
    const user = userEvent.setup()
    vi.spyOn(api, 'getExchange').mockResolvedValue(exchangeDetail())
    const update = vi.spyOn(api, 'updateExchange')
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Edit' }))
    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(update).not.toHaveBeenCalled()
    expect(screen.getByRole('heading', { name: 'Family Christmas' })).toBeInTheDocument()
  })

  it('deletes the exchange after confirmation and returns to the list', async () => {
    const user = userEvent.setup()
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    vi.spyOn(api, 'getExchange').mockResolvedValue(exchangeDetail())
    const remove = vi.spyOn(api, 'deleteExchange').mockResolvedValue(undefined)
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Delete exchange' }))

    expect(remove).toHaveBeenCalledWith('ex-1')
    expect(screen.getByTestId('location')).toHaveTextContent(/^\/admin$/)
  })
})
