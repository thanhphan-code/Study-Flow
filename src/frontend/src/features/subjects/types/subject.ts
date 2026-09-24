export type Subject = {
  id: string
  name: string
  description: string | null
  createdAt: string
  updatedAt: string
}

export type SubjectInput = { name: string; description: string | null }
