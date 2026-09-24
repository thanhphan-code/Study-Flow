export type AnswerEvaluation = { result: 'correct' | 'close' | 'incorrect'; similarity: number; matchedAnswer: string }

function normalize(value: string) {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase().replace(/[^\p{L}\p{N}]+/gu, ' ').trim().replace(/\s+/g, ' ')
}

function distance(left: string, right: string) {
  const previous = Array.from({ length: right.length + 1 }, (_, index) => index)
  for (let row = 1; row <= left.length; row += 1) {
    const current = [row]
    for (let column = 1; column <= right.length; column += 1) current[column] = Math.min(current[column - 1] + 1, previous[column] + 1, previous[column - 1] + (left[row - 1] === right[column - 1] ? 0 : 1))
    previous.splice(0, previous.length, ...current)
  }
  return previous[right.length]
}

export function evaluateAnswer(value: string, expected: string, alternatives: Array<string | null | undefined> = []): AnswerEvaluation {
  const answer = normalize(value)
  const candidates = [expected, ...alternatives].filter((item): item is string => Boolean(item?.trim())).flatMap(item => item.split(/[\n;]/)).map(normalize).filter(Boolean)
  let best = { similarity: 0, matchedAnswer: expected }
  for (const candidate of candidates) {
    const similarity = answer === candidate ? 1 : 1 - distance(answer, candidate) / Math.max(answer.length, candidate.length, 1)
    if (similarity > best.similarity) best = { similarity, matchedAnswer: candidate }
  }
  return { ...best, result: best.similarity === 1 ? 'correct' : best.similarity >= .82 ? 'close' : 'incorrect' }
}
