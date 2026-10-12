import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { api, ApiError } from '../api'
import { invite } from '../test/fixtures'
import { renderAt } from '../test/utils'
import InvitePage from './InvitePage'

function renderPage(token = 'token-alice') {
  return renderAt(<InvitePage />, { route: `/invite/${token}`, path: '/invite/:token' })
}

describe('InvitePage', () => {
  it('shows the exchange and says names are not drawn yet', async () => {
    const get = vi.spyOn(api, 'getInvite').mockResolvedValue(invite())

    renderPage()

    expect(screen.getByText('Loading…')).toBeInTheDocument()
    expect(await screen.findByRole('heading', { name: 'Family Christmas' })).toBeInTheDocument()
    expect(get).toHaveBeenCalledWith('token-alice')
    expect(screen.getByText(/Hi Alice!/)).toBeInTheDocument()
    expect(screen.getByText('£30 budget')).toBeInTheDocument()
    expect(screen.getByText('3 people taking part')).toBeInTheDocument()
    expect(screen.getByText(/haven't been drawn yet/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Reveal/ })).not.toBeInTheDocument()
  })

  it('keeps the recipient hidden until revealed', async () => {
    const user = userEvent.setup()
    vi.spyOn(api, 'getInvite').mockResolvedValue(invite({ recipientName: 'Bob' }))
    renderPage()

    const reveal = await screen.findByRole('button', { name: /Reveal who I'm buying for/ })
    expect(screen.queryByText('Bob')).not.toBeInTheDocument()

    await user.click(reveal)

    expect(screen.getByText("You're buying a gift for")).toBeInTheDocument()
    expect(screen.getByText('Bob')).toBeInTheDocument()
  })

  it('explains an invalid link', async () => {
    vi.spyOn(api, 'getInvite').mockRejectedValue(new ApiError(404, 'Request failed (404).'))

    renderPage('nope')

    expect(await screen.findByText(/This invite link isn't valid/)).toBeInTheDocument()
  })

  it('shows other errors as they are', async () => {
    vi.spyOn(api, 'getInvite').mockRejectedValue(new ApiError(500, 'Request failed (500).'))

    renderPage()

    expect(await screen.findByText('Request failed (500).')).toBeInTheDocument()
  })
})
