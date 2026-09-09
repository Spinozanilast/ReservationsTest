import { createRouter } from '@tanstack/react-router'
import { routeTree } from './routeTree.gen'

export const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
  defaultErrorComponent: (err) => (
    <p>{err.error instanceof Error ? err.error.stack : String(err.error)}</p>
  ),
  defaultNotFoundComponent: () => <p>not found</p>,
  scrollRestoration: true,
})

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}