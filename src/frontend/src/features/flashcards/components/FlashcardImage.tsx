import { useEffect, useState } from 'react'
import { useAuthStore } from '@/features/auth/store/authStore'

export function FlashcardImage({ imageUrl, alt, className = '' }: { imageUrl: string; alt: string; className?: string }) {
  const token = useAuthStore(state => state.accessToken); const [source, setSource] = useState('')
  useEffect(() => {
    let objectUrl = ''; const controller = new AbortController()
    fetch(imageUrl, { headers: token ? { Authorization: `Bearer ${token}` } : {}, signal: controller.signal }).then(response => { if (!response.ok) throw new Error('Image could not be loaded.'); return response.blob() }).then(blob => { objectUrl = URL.createObjectURL(blob); setSource(objectUrl) }).catch(() => undefined)
    return () => { controller.abort(); if (objectUrl) URL.revokeObjectURL(objectUrl) }
  }, [imageUrl, token])
  if (!source) return <div className={`animate-pulse bg-slate-100 ${className}`} aria-label={alt} />
  return <img src={source} alt={alt} className={className} />
}
