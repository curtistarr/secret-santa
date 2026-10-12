import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'

/** Shows the current path so tests can assert on navigation. */
function LocationDisplay() {
  return <div data-testid="location">{useLocation().pathname}</div>
}

/** Renders a page at `route`, matched against `path`, inside a router. */
export function renderAt(element: ReactElement, { route, path }: { route: string; path: string }) {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <Routes>
        <Route path={path} element={element} />
        <Route path="*" element={null} />
      </Routes>
      <LocationDisplay />
    </MemoryRouter>,
  )
}
