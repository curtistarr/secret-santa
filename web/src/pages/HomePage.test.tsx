import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { renderAt } from '../test/utils'
import HomePage from './HomePage'

describe('HomePage', () => {
  it('explains invite links and links to the organiser login', () => {
    renderAt(<HomePage />, { route: '/', path: '/' })

    expect(screen.getByRole('heading', { name: 'Secret Santa' })).toBeInTheDocument()
    expect(screen.getByText(/got an invite link/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Organiser login' })).toHaveAttribute('href', '/admin')
  })
})
