import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return <main className="grid min-h-screen place-items-center px-6 text-center"><div><p className="text-sm font-semibold text-indigo-600">404</p><h1 className="mt-2 text-3xl font-semibold text-slate-950">Page not found</h1><Link className="mt-6 inline-block text-indigo-600 hover:underline" to="/home">Return home</Link></div></main>
}
