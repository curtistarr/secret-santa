import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { adminPassword, api, ApiError } from '../api'
import AdminLogin from './AdminLogin'

describe('AdminLogin', () => {
  it('stores the password and reports success when it is correct', async () => {
    const user = userEvent.setup()
    const login = vi.spyOn(api, 'login').mockResolvedValue(undefined)
    const onLogin = vi.fn()
    render(<AdminLogin onLogin={onLogin} />)

    await user.type(screen.getByLabelText('Admin password'), 'hunter2')
    await user.click(screen.getByRole('button', { name: 'Log in' }))

    expect(login).toHaveBeenCalledWith('hunter2')
    expect(adminPassword.get()).toBe('hunter2')
    expect(onLogin).toHaveBeenCalledOnce()
  })

  it('shows the error and stores nothing when the password is wrong', async () => {
    const user = userEvent.setup()
    vi.spyOn(api, 'login').mockRejectedValue(new ApiError(401, 'Incorrect admin password.'))
    const onLogin = vi.fn()
    render(<AdminLogin onLogin={onLogin} />)

    await user.type(screen.getByLabelText('Admin password'), 'wrong')
    await user.click(screen.getByRole('button', { name: 'Log in' }))

    expect(await screen.findByText('Incorrect admin password.')).toBeInTheDocument()
    expect(adminPassword.get()).toBeNull()
    expect(onLogin).not.toHaveBeenCalled()
    expect(screen.getByRole('button', { name: 'Log in' })).toBeEnabled()
  })
})
